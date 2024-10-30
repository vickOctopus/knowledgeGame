using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;

public class ChunkManager : MonoBehaviour
{
    // public GameObject worldRoot;
    
    public static ChunkManager Instance { get; private set; }

    public const int chunkWidth = 50;
    public const int chunkHeight = 28;
    public int loadDistance = 1;
    private int objectLoadDistance => Mathf.Max(0, loadDistance - 1);

    private Dictionary<Vector2Int, AsyncOperationHandle<ChunkData>> loadedChunks = new Dictionary<Vector2Int, AsyncOperationHandle<ChunkData>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;
    private HashSet<Vector2Int> chunksBeingLoaded = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> chunksBeingUnloaded = new HashSet<Vector2Int>();
    private bool isInitializing = false;

    [SerializeField] private GameObject levelGrid;

    private const float MEMORY_CHECK_INTERVAL = 10f; // 内存检隔（秒）
    private const float MEMORY_WARNING_THRESHOLD = 0.8f; // 内存警告阈值（80%）
    private const int MIN_CACHE_SIZE = 5; // 最小缓存大小
    private const int DEFAULT_CACHE_SIZE = 20; // 默认缓存大小
    private const int MAX_ABSOLUTE_CACHE_SIZE = 50; // 绝对最大缓存大小

    private int currentMaxCacheSize;
    private float lastMemoryCheckTime;

    private int MAX_CACHE_SIZE 
    {
        get => currentMaxCacheSize;
        set => currentMaxCacheSize = Mathf.Clamp(value, MIN_CACHE_SIZE, MAX_ABSOLUTE_CACHE_SIZE);
    }

    private HashSet<Vector2Int> chunksBeingProcessed = new HashSet<Vector2Int>();
    
    private const int TILES_PER_FRAME = 20;
    private const int OBJECTS_PER_FRAME = 20;
    private const int MAX_CONCURRENT_LOADS = 2;
    private const int TILES_TO_CLEAR_PER_FRAME = 50;
    
    private Queue<Vector2Int> loadQueue = new Queue<Vector2Int>();
    private int currentLoads = 0;

    private Dictionary<Vector2Int, LinkedListNode<ChunkData>> chunkCache = new Dictionary<Vector2Int, LinkedListNode<ChunkData>>();
    private LinkedList<ChunkData> cacheOrder = new LinkedList<ChunkData>();

    public static int ChunkWidth => chunkWidth;
    public static int ChunkHeight => chunkHeight;

    public event System.Action<Vector2Int> OnChunkLoaded;
    public event System.Action<Vector2Int> OnChunkUnloaded;
    public event System.Action<Vector2Int> OnSwitchStateChanged;

    public static event Action OnChunkLoadedEvent;

    public void NotifySwitchChange(Vector2Int chunkCoord)
    {
        OnSwitchStateChanged?.Invoke(chunkCoord);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CheckAddressablesStatus();
            
            // 初始化缓存大小
            currentMaxCacheSize = DEFAULT_CACHE_SIZE;
            StartCoroutine(MonitorMemoryUsage());
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
        // worldRoot.SetActive(false);
    }

    private void Start()
    {
        
        // 等待PlayerState加载完成后再初始化区块
        StartCoroutine(WaitForPlayerStateAndInitialize());
    }

    private IEnumerator WaitForPlayerStateAndInitialize()
    {
        // if (Application.isEditor)
        // {
        //     // 在编辑模式下等待PlayController实例
        while (PlayController.instance == null)
        {
            yield return null;
        }
        
        yield return new WaitForEndOfFrame();
        
        // 使用PlayController的位置
        Vector3 playerPosition = PlayController.instance.transform.position;
        InitializeChunks(playerPosition);
        // }
        // else
        // {
        //     // 在发布版本中使用PlayerState的存档数据
        //     while (PlayerState.instance == null)
        //     {
        //         yield return null;
        //     }
        //     
        //     yield return new WaitForEndOfFrame();
        //     
        //     Vector2 respawnPoint = new Vector2(
        //         PlayerState.instance.playerSaveData.respawnPointX,
        //         PlayerState.instance.playerSaveData.respawnPointY
        //     );
        //     
        //     InitializeChunks(respawnPoint);
        // // }
        
        InvokeRepeating(nameof(CleanUpUnusedResources), 60f, 60f);
    }

    public async void InitializeChunks(Vector3 playerPosition)
    {
        if (isInitializing) return;
        isInitializing = true;

        currentChunk = GetChunkCoordFromWorldPos(playerPosition);

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                bool shouldLoadObjects = Mathf.Abs(x) <= objectLoadDistance && Mathf.Abs(y) <= objectLoadDistance;
                LoadChunkSync(coord, shouldLoadObjects);
            }
        }

        await UpdateVisibleChunksAsync();
        isInitializing = false;
    }

    public async void ForceUpdateChunks(Vector3 cameraPosition)
    {
        if (isInitializing) return;
        Vector2Int newChunk = GetChunkCoordFromWorldPos(cameraPosition);
        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            await UpdateVisibleChunksAsync();
        }
    }

    private async Task UpdateVisibleChunksAsync()
    {
        try
        {
            // 使用 List 代替 HashSet 来避免可能的容量问题
            List<Vector2Int> newVisibleChunks = new List<Vector2Int>();
            List<Vector2Int> newVisibleObjectChunks = new List<Vector2Int>();
            List<Vector2Int> chunksToLoad = new List<Vector2Int>();
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();

            // 计算新的可见区块
            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                    
                    if (!newVisibleChunks.Contains(coord))
                    {
                        newVisibleChunks.Add(coord);
                    }
                    
                    if (Mathf.Abs(x) <= objectLoadDistance && Mathf.Abs(y) <= objectLoadDistance)
                    {
                        if (!newVisibleObjectChunks.Contains(coord))
                        {
                            newVisibleObjectChunks.Add(coord);
                        }
                    }

                    if (!visibleChunks.Contains(coord) && !loadedChunks.ContainsKey(coord))
                    {
                        bool exists = await ChunkExistsAsync(coord);
                        if (exists && !chunksToLoad.Contains(coord))
                        {
                            chunksToLoad.Add(coord);
                        }
                    }
                }
            }

            // 找出需要卸载的区块
            foreach (Vector2Int chunk in visibleChunks)
            {
                bool shouldUnloadObjects = !newVisibleObjectChunks.Contains(chunk);
                bool shouldUnloadCompletely = !newVisibleChunks.Contains(chunk);

                if (shouldUnloadCompletely && !chunksToUnload.Contains(chunk))
                {
                    chunksToUnload.Add(chunk);
                }
                else if (shouldUnloadObjects)
                {
                    GameObject chunkParent = GameObject.Find($"Chunk_{chunk.x}_{chunk.y}_Objects");
                    if (chunkParent != null)
                    {
                        Destroy(chunkParent);
                    }
                }
            }

            // 按距离排序加载顺序
            chunksToLoad.Sort((a, b) => 
                Vector2Int.Distance(a, currentChunk).CompareTo(Vector2Int.Distance(b, currentChunk)));

            // 加载新区块
            foreach (Vector2Int coord in chunksToLoad)
            {
                if (!chunksBeingProcessed.Contains(coord))
                {
                    chunksBeingProcessed.Add(coord);
                    await LoadChunkAsync(coord, newVisibleObjectChunks.Contains(coord));
                    chunksBeingProcessed.Remove(coord);
                }
            }

            // 更新对象
            foreach (Vector2Int coord in newVisibleObjectChunks)
            {
                if (loadedChunks.ContainsKey(coord))
                {
                    GameObject chunkParent = GameObject.Find($"Chunk_{coord.x}_{coord.y}_Objects");
                    if (chunkParent == null)
                    {
                        ChunkData chunkData = loadedChunks[coord].Result;
                        chunkParent = new GameObject($"Chunk_{coord.x}_{coord.y}_Objects");
                        chunkParent.transform.position = new Vector3(
                            coord.x * chunkWidth,
                            coord.y * chunkHeight,
                            0
                        );
                        await InstantiateObjectsAsync(chunkData, chunkParent, coord);
                    }
                }
            }

            // 更新可见区块集合
            visibleChunks = new HashSet<Vector2Int>(newVisibleChunks);

            // 处理卸载
            foreach (Vector2Int chunk in chunksToUnload)
            {
                if (!chunksBeingProcessed.Contains(chunk))
                {
                    chunksBeingProcessed.Add(chunk);
                    await UnloadChunkAsync(chunk);
                    chunksBeingProcessed.Remove(chunk);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UpdateVisibleChunksAsync error: {e.Message}\n{e.StackTrace}");
        }
    }

    private async Task ProcessLoadQueueAsync()
    {
        while (loadQueue.Count > 0)
        {
            if (currentLoads < MAX_CONCURRENT_LOADS)
            {
                Vector2Int coord = loadQueue.Dequeue();
                currentLoads++;
                _ = LoadChunkAsync(coord).ContinueWith(_ => currentLoads--);
            }

            await Task.Delay(1);
        }
    }

    private async Task LoadChunkAsync(Vector2Int chunkCoord, bool loadObjects = true)
    {
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            return;
        }

        string chunkDataAddress = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";

        try
        {
            var loadOperation = Addressables.LoadAssetAsync<ChunkData>(chunkDataAddress);
            await loadOperation.Task;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
            {
                ChunkData chunkData = loadOperation.Result;

                if (chunkData == null || levelGrid == null)
                {
                    Debug.LogError($"ChunkData or levelGrid is null for chunk {chunkCoord}");
                    Addressables.Release(loadOperation);
                    return;
                }

                GameObject chunkParent = null;
                if (loadObjects)
                {
                    chunkParent = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Objects");
                    chunkParent.transform.position = new Vector3(
                        chunkCoord.x * chunkWidth,
                        chunkCoord.y * chunkHeight,
                        0
                    );
                }

                await InstantiateTilesAsync(chunkData, chunkCoord);
                if (loadObjects)
                {
                    await InstantiateObjectsAsync(chunkData, chunkParent, chunkCoord);
                }

                if (loadOperation.IsValid())
                {
                    loadedChunks[chunkCoord] = loadOperation;
                }
                else
                {
                    Debug.LogWarning($"LoadChunkAsync: Operation became invalid for chunk {chunkCoord}");
                }

                AddToCache(chunkCoord, chunkData);

                // 触发区块加载完成事件
                OnChunkLoaded?.Invoke(chunkCoord);
                
                // 触发静态事件，表示区块完全加载完成
                OnChunkLoadedEvent?.Invoke();
            }
            else
            {
                Debug.LogError($"Failed to load chunk data for {chunkCoord}. Status: {loadOperation.Status}");
                if (loadOperation.IsValid())
                {
                    Addressables.Release(loadOperation);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading chunk {chunkCoord}: {e.Message}\n{e.StackTrace}");
        }

        // OnChunkLoaded?.Invoke(chunkCoord);
    }

    private async Task InstantiateTilesAsync(ChunkData chunkData, Vector2Int chunkCoord)
    {
        Dictionary<string, List<(Vector3Int, TileBase, Color, Tile.ColliderType)>> tilesPerLayer = new Dictionary<string, List<(Vector3Int, TileBase, Color, Tile.ColliderType)>>();

        foreach (var layerData in chunkData.tilemapLayers)
        {
            if (!tilesPerLayer.ContainsKey(layerData.layerName))
            {
                tilesPerLayer[layerData.layerName] = new List<(Vector3Int, TileBase, Color, Tile.ColliderType)>();
            }

            foreach (var tileData in layerData.tiles)
            {
                Vector3Int globalPos = new Vector3Int(
                    tileData.position.x + chunkCoord.x * chunkWidth - chunkWidth / 2,
                    tileData.position.y + chunkCoord.y * chunkHeight - chunkHeight / 2,
                    tileData.position.z
                );
                tilesPerLayer[layerData.layerName].Add((globalPos, tileData.tile, tileData.color, tileData.colliderType));
            }
        }

        foreach (var layerKVP in tilesPerLayer)
        {
            Transform tilemapTransform = levelGrid.transform.Find(layerKVP.Key);
            if (tilemapTransform == null)
            {
                Debug.LogWarning($"{layerKVP.Key} not found in LevelGrid.");
                continue;
            }

            Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogWarning($"Tilemap component not found on {layerKVP.Key}.");
                continue;
            }

            int getTileDataCalls = 0;

            for (int i = 0; i < layerKVP.Value.Count; i += TILES_PER_FRAME)
            {
                int endIndex = Mathf.Min(i + TILES_PER_FRAME, layerKVP.Value.Count);
                var batch = layerKVP.Value.GetRange(i, endIndex - i);

                var positions = batch.Select(t => t.Item1).ToArray();
                var tiles = batch.Select(t => t.Item2).ToArray();
                tilemap.SetTiles(positions, tiles);

                foreach (var (pos, tile, color, colliderType) in batch)
                {
                    if (tile is RuleTile ruleTile)
                    {
                        UnityEngine.Tilemaps.TileData tileData = new UnityEngine.Tilemaps.TileData();
                        ruleTile.GetTileData(pos, tilemap, ref tileData);

                        Color finalColor = tileData.color != default ? tileData.color : color;
                        if (tilemap.GetColor(pos) != finalColor)
                        {
                            tilemap.SetColor(pos, finalColor);
                        }
                        if (tilemap.GetColliderType(pos) != tileData.colliderType)
                        {
                            tilemap.SetColliderType(pos, tileData.colliderType);
                        }

                        getTileDataCalls++;
                        if (getTileDataCalls >= TILES_PER_FRAME)
                        {
                            getTileDataCalls = 0;
                            await Task.Yield();
                        }
                    }
                    else
                    {
                        if (tilemap.GetColor(pos) != color)
                        {
                            tilemap.SetColor(pos, color);
                        }
                        if (tilemap.GetColliderType(pos) != colliderType)
                        {
                            tilemap.SetColliderType(pos, colliderType);
                        }
                    }
                }

                await Task.Yield();
            }
        }
    }

    private async Task InstantiateObjectsAsync(ChunkData chunkData, GameObject chunkParent, Vector2Int chunkCoord)
    {
        for (int i = 0; i < chunkData.objects.Count; i += OBJECTS_PER_FRAME)
        {
            int endIndex = Mathf.Min(i + OBJECTS_PER_FRAME, chunkData.objects.Count);
            for (int j = i; j < endIndex; j++)
            {
                var objectData = chunkData.objects[j];
                var handle = Addressables.LoadAssetAsync<GameObject>(objectData.prefabName);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject prefab = handle.Result;
                    GameObject obj = Instantiate(prefab, chunkParent.transform);
                    obj.transform.position = objectData.position + new Vector3(
                        chunkCoord.x * chunkWidth - chunkWidth / 2,
                        chunkCoord.y * chunkHeight - chunkHeight / 2,
                        0
                    );
                    obj.transform.rotation = objectData.rotation;
                    obj.transform.localScale = objectData.scale;
                }
                else
                {
                    Debug.LogError($"Failed to load prefab from path {objectData.prefabName} using Addressables.");
                }

                Addressables.Release(handle);
            }
            await Task.Yield();
        }
    }

    private IEnumerator InstantiateChunkFromData(Vector2Int chunkCoord, ChunkData chunkData)
    {
        // 这里放置原来在 LoadChunkAsync 中实例化区块的代码
        // ...

        yield break;
    }

    private void AddToCache(Vector2Int chunkCoord, ChunkData chunkData)
    {
        if (chunkCache.ContainsKey(chunkCoord))
        {
            var node = chunkCache[chunkCoord];
            cacheOrder.Remove(node);
            cacheOrder.AddFirst(node);
        }
        else
        {
            if (chunkCache.Count >= MAX_CACHE_SIZE)
            {
                // 检查内存使用情况
                float memoryUsageRatio = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 
                                       (SystemInfo.systemMemorySize * 1024f * 1024f);
                
                if (memoryUsageRatio > MEMORY_WARNING_THRESHOLD)
                {
                    // 如果内存使用率高，立即清理多个缓存项
                    TrimCache();
                }
                else
                {
                    // 正常移除最旧的缓存项
                    var oldestNode = cacheOrder.Last;
                    cacheOrder.RemoveLast();
                    chunkCache.Remove(oldestNode.Value.chunkCoord);
                }
            }

            var newNode = new LinkedListNode<ChunkData>(chunkData);
            cacheOrder.AddFirst(newNode);
            chunkCache[chunkCoord] = newNode;
        }
    }

    private void UseCache(Vector2Int chunkCoord)
    {
        if (chunkCache.TryGetValue(chunkCoord, out var node))
        {
            cacheOrder.Remove(node);
            cacheOrder.AddFirst(node);
        }
    }

    private async Task UnloadChunkAsync(Vector2Int chunkCoord)
    {
        if (!loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<ChunkData> loadOperation))
        {
            return;
        }

        if (loadOperation.IsValid())
        {
            ChunkData chunkData = loadOperation.Result;

            GameObject chunkParent = GameObject.Find($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Objects");
            if (chunkParent != null)
            {
                Destroy(chunkParent);
                await Task.Yield();
            }

            await UnloadTilesAsync(chunkData, chunkCoord);

            if (!chunkCache.ContainsKey(chunkCoord))
            {
                AddToCache(chunkCoord, chunkData);
            }

            if (loadOperation.IsValid())
            {
                Addressables.Release(loadOperation);
            }
        }

        loadedChunks.Remove(chunkCoord);
        OnChunkUnloaded?.Invoke(chunkCoord);
    }

    private async Task UnloadTilesAsync(ChunkData chunkData, Vector2Int chunkCoord)
    {
        if (chunkData == null || levelGrid == null) return;

        foreach (var layerData in chunkData.tilemapLayers)
        {
            Transform tilemapTransform = levelGrid.transform.Find(layerData.layerName);
            if (tilemapTransform == null) continue;

            Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
            if (tilemap == null) continue;

            List<Vector3Int> positions = new List<Vector3Int>();
            foreach (var tileData in layerData.tiles)
            {
                Vector3Int globalPos = new Vector3Int(
                    tileData.position.x + chunkCoord.x * chunkWidth - chunkWidth / 2,
                    tileData.position.y + chunkCoord.y * chunkHeight - chunkHeight / 2,
                    tileData.position.z
                );
                positions.Add(globalPos);
            }

            // 批量清除瓦片
            for (int i = 0; i < positions.Count; i += TILES_TO_CLEAR_PER_FRAME)
            {
                int endIndex = Mathf.Min(i + TILES_TO_CLEAR_PER_FRAME, positions.Count);
                var batch = positions.GetRange(i, endIndex - i).ToArray();
                tilemap.SetTiles(batch, new TileBase[batch.Length]);
                await Task.Yield();
            }
        }
    }

    public Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x + chunkWidth / 2) / chunkWidth),
            Mathf.FloorToInt((worldPos.y + chunkHeight / 2) / chunkHeight)
        );
    }

    public void UnloadAllChunks()
    {
        foreach (var loadOperation in loadedChunks.Values)
        {
            if (loadOperation.IsValid())
            {
                Addressables.ReleaseInstance(loadOperation);
            }
        }
        loadedChunks.Clear();
        visibleChunks.Clear();

        chunkCache.Clear();
        cacheOrder.Clear();
    }

    private void CheckAddressablesStatus()
    {
        string testKey = "ChunkData_0_0";
        Addressables.GetDownloadSizeAsync(testKey).Completed += (op) => 
        {
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to get download size for {testKey}: {op.OperationException}");
            }
        };
    }

    private void LoadChunkSync(Vector2Int chunkCoord, bool loadObjects = true)
    {
        if (!loadedChunks.ContainsKey(chunkCoord))
        {
            string chunkDataAddress = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";

            try
            {
                var checkOperation = Addressables.LoadResourceLocationsAsync(chunkDataAddress);
                checkOperation.WaitForCompletion();
                bool exists = checkOperation.Result != null && checkOperation.Result.Count > 0;
                Addressables.Release(checkOperation);

                if (exists)
                {
                    var loadOperation = Addressables.LoadAssetAsync<ChunkData>(chunkDataAddress);
                    loadOperation.WaitForCompletion();

                    if (loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
                    {
                        ChunkData chunkData = loadOperation.Result;

                        if (levelGrid == null)
                        {
                            Debug.LogError("LevelGrid is not assigned.");
                            return;
                        }

                        GameObject chunkParent = null;
                        if (loadObjects && chunkData.objects.Count > 0)
                        {
                            chunkParent = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Objects");
                            chunkParent.transform.position = new Vector3(
                                chunkCoord.x * chunkWidth,
                                chunkCoord.y * chunkHeight,
                                0
                            );
                        }

                        foreach (var layerData in chunkData.tilemapLayers)
                        {
                            Transform tilemapTransform = levelGrid.transform.Find(layerData.layerName);
                            if (tilemapTransform == null)
                            {
                                continue;
                            }

                            Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
                            if (tilemap == null)
                            {
                                continue;
                            }

                            foreach (var tileData in layerData.tiles)
                            {
                                Vector3Int globalPos = new Vector3Int(
                                    tileData.position.x + chunkCoord.x * chunkWidth - chunkWidth / 2,
                                    tileData.position.y + chunkCoord.y * chunkHeight - chunkHeight / 2,
                                    tileData.position.z
                                );
                                tilemap.SetTile(globalPos, tileData.tile);
                                tilemap.SetColor(globalPos, tileData.color);
                                tilemap.SetColliderType(globalPos, tileData.colliderType);
                            }
                        }

                        if (loadObjects)
                        {
                            foreach (var objectData in chunkData.objects)
                            {
                                Addressables.LoadAssetAsync<GameObject>(objectData.prefabName).Completed += handle =>
                                {
                                    if (handle.Status == AsyncOperationStatus.Succeeded)
                                    {
                                        GameObject prefab = handle.Result;
                                        GameObject obj = Instantiate(prefab, chunkParent != null ? chunkParent.transform : null);
                                        obj.transform.position = objectData.position + new Vector3(
                                            chunkCoord.x * chunkWidth - chunkWidth / 2,
                                            chunkCoord.y * chunkHeight - chunkHeight / 2,
                                            0
                                        );
                                        obj.transform.rotation = objectData.rotation;
                                        obj.transform.localScale = objectData.scale;
                                    }
                                    else
                                    {
                                        Debug.LogError($"Failed to load prefab from path {objectData.prefabName} using Addressables.");
                                    }
                                };
                            }
                        }

                        loadedChunks[chunkCoord] = loadOperation;
                    }
                    else
                    {
                        Debug.LogError($"Failed to load chunk data for {chunkCoord}. Status: {loadOperation.Status}");
                        Addressables.Release(loadOperation);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading chunk {chunkCoord}: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    private async Task<bool> ChunkExistsAsync(Vector2Int chunkCoord)
    {
        string chunkDataAddress = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";
        try
        {
            var loadOperation = Addressables.LoadResourceLocationsAsync(chunkDataAddress);
            await loadOperation.Task;
            bool exists = loadOperation.Result != null && loadOperation.Result.Count > 0;
            Addressables.Release(loadOperation);
            return exists;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void CleanUpUnusedResources()
    {
        AdjustCacheSizeBasedOnMemory(); // 在清理前检查并调整缓存大小
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    public ChunkData GetChunkData(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<ChunkData> loadOperation))
        {
            return loadOperation.Result;
        }
        return null;
    }

    private void ChunkLoaded(Vector2Int chunkCoord)
    {
        OnChunkLoaded?.Invoke(chunkCoord);
    }

    private void ChunkUnloaded(Vector2Int chunkCoord)
    {
        OnChunkUnloaded?.Invoke(chunkCoord);
    }

    private IEnumerator MonitorMemoryUsage()
    {
        while (true)
        {
            yield return new WaitForSeconds(MEMORY_CHECK_INTERVAL);
            AdjustCacheSizeBasedOnMemory();
        }
    }

    private void AdjustCacheSizeBasedOnMemory()
    {
        float totalMemory = SystemInfo.systemMemorySize;
        float reservedMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f); // 转换为MB
        float memoryUsageRatio = reservedMemory / totalMemory;

        if (memoryUsageRatio > MEMORY_WARNING_THRESHOLD)
        {
            // 内存使用过高，减少缓存大小
            MAX_CACHE_SIZE = Mathf.Max(MIN_CACHE_SIZE, MAX_CACHE_SIZE - 5);
            TrimCache();
            Debug.LogWarning($"High memory usage detected ({memoryUsageRatio:P2}). Reducing cache size to {MAX_CACHE_SIZE}");
        }
        else if (memoryUsageRatio < MEMORY_WARNING_THRESHOLD * 0.7f) // 有足够内存余量
        {
            // 逐渐增加缓存大小
            MAX_CACHE_SIZE = Mathf.Min(MAX_ABSOLUTE_CACHE_SIZE, MAX_CACHE_SIZE + 2);
        }
    }

    private void TrimCache()
    {
        while (chunkCache.Count > MAX_CACHE_SIZE)
        {
            var oldestNode = cacheOrder.Last;
            cacheOrder.RemoveLast();
            chunkCache.Remove(oldestNode.Value.chunkCoord);
            
            // 强制进行垃圾回收
            Resources.UnloadUnusedAssets();
        }
    }
}
