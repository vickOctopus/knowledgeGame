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
    public static ChunkManager Instance { get; private set; }

    public const int chunkWidth = 50;
    public const int chunkHeight = 28;
    public int loadDistance = 1;

    private Dictionary<Vector2Int, AsyncOperationHandle<ChunkData>> loadedChunks = new Dictionary<Vector2Int, AsyncOperationHandle<ChunkData>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;
    private HashSet<Vector2Int> chunksBeingLoaded = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> chunksBeingUnloaded = new HashSet<Vector2Int>();
    private bool isInitializing = false;

    [SerializeField] private GameObject levelGrid;

    private const int MAX_CACHE_SIZE = 20; // 可以根据需要调整
    private Dictionary<Vector2Int, LinkedListNode<ChunkData>> chunkCache = new Dictionary<Vector2Int, LinkedListNode<ChunkData>>();
    private LinkedList<ChunkData> cacheOrder = new LinkedList<ChunkData>();

    private const int TILES_PER_FRAME = 20; // 增加每帧处理的瓦片数量
    private const int OBJECTS_PER_FRAME = 20; // 增加每帧处理的对象数量

    private const int MAX_CONCURRENT_LOADS = 2; // 最大并发加载数
    private Queue<Vector2Int> loadQueue = new Queue<Vector2Int>();
    private int currentLoads = 0;

    private const int TILES_TO_CLEAR_PER_FRAME = 50; // 每帧清除的瓦片数量

    private HashSet<Vector2Int> chunksBeingProcessed = new HashSet<Vector2Int>();

    public static int ChunkWidth => chunkWidth;
    public static int ChunkHeight => chunkHeight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CheckAddressablesStatus();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeChunks(PlayController.instance.transform.position);
        InvokeRepeating(nameof(CleanUpUnusedResources), 60f, 60f); // 每60秒清理一次
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
                LoadChunkSync(coord);
            }
        }

        await UpdateVisibleChunksAsync();
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
        Debug.Log($"Updating visible chunks. Current chunk: {currentChunk}");
        HashSet<Vector2Int> newVisibleChunks = new HashSet<Vector2Int>();
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                newVisibleChunks.Add(coord);
                if (!visibleChunks.Contains(coord) && !loadedChunks.ContainsKey(coord))
                {
                    bool exists = await ChunkExistsAsync(coord);
                    Debug.Log($"Chunk {coord} exists: {exists}");
                    if (exists)
                    {
                        chunksToLoad.Add(coord);
                    }
                }
            }
        }

        Debug.Log($"Chunks to load: {string.Join(", ", chunksToLoad)}");

        // 处理需要卸载的区块
        foreach (Vector2Int chunk in visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                chunksToUnload.Add(chunk);
            }
        }

        // 按照到当前区块的距离排序
        chunksToLoad.Sort((a, b) => 
            Vector2Int.Distance(a, currentChunk).CompareTo(Vector2Int.Distance(b, currentChunk)));

        // 处理加载
        foreach (Vector2Int coord in chunksToLoad)
        {
            if (!chunksBeingProcessed.Contains(coord))
            {
                chunksBeingProcessed.Add(coord);
                _ = LoadChunkAsync(coord).ContinueWith(t => 
                {
                    chunksBeingProcessed.Remove(coord);
                    if (t.IsFaulted)
                    {
                        Debug.LogError($"Failed to load chunk {coord}: {t.Exception}");
                    }
                });
            }
        }

        // 处理卸载
        foreach (Vector2Int chunk in chunksToUnload)
        {
            if (!chunksBeingProcessed.Contains(chunk))
            {
                chunksBeingProcessed.Add(chunk);
                _ = UnloadChunkAsync(chunk).ContinueWith(_ => 
                {
                    chunksBeingProcessed.Remove(chunk);
                });
            }
        }

        visibleChunks = newVisibleChunks;
        isInitializing = false;
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

            // 使用 Task.Delay 来分散加载操作
            await Task.Delay(1);
        }
    }

    private async Task LoadChunkAsync(Vector2Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            Debug.Log($"Chunk {chunkCoord} is already loaded");
            return;
        }

        Debug.Log($"Starting to load chunk {chunkCoord}");
        string chunkDataAddress = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";

        try
        {
            Debug.Log($"Loading chunk data for {chunkCoord}");
            var loadOperation = Addressables.LoadAssetAsync<ChunkData>(chunkDataAddress);
            await loadOperation.Task;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
            {
                ChunkData chunkData = loadOperation.Result;
                Debug.Log($"Successfully loaded chunk data for {chunkCoord}");

                if (chunkData == null || levelGrid == null)
                {
                    Debug.LogError($"ChunkData or levelGrid is null for chunk {chunkCoord}");
                    Addressables.Release(loadOperation);
                    return;
                }

                GameObject chunkParent = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Objects");
                chunkParent.transform.position = new Vector3(
                    chunkCoord.x * chunkWidth,
                    chunkCoord.y * chunkHeight,
                    0
                );

                await InstantiateTilesAsync(chunkData, chunkCoord);
                await InstantiateObjectsAsync(chunkData, chunkParent, chunkCoord);

                if (loadOperation.IsValid())
                {
                    loadedChunks[chunkCoord] = loadOperation;
                    Debug.Log($"Chunk {chunkCoord} added to loadedChunks");
                }
                else
                {
                    Debug.LogWarning($"LoadChunkAsync: Operation became invalid for chunk {chunkCoord}");
                }

                AddToCache(chunkCoord, chunkData);
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

            int getTileDataCalls = 0; // 计数器

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

                        // 仅在 TileData 确实发生变化时更新
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
                            await Task.Yield(); // 分帧处理
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

                // 确保在每个批次后进行分帧处理
                await Task.Yield();
            }
        }
    }

    private async Task InstantiateObjectsAsync(ChunkData chunkData, GameObject chunkParent, Vector2Int chunkCoord)
    {
        for (int i = 0; i < chunkData.objects.Count; i += OBJECTS_PER_FRAME) // 使用新的批量处理数量
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
            await Task.Yield(); // 确保异步执行
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
            // 如果缓存中已经存在该区块，则将其移动到链表头部
            var node = chunkCache[chunkCoord];
            cacheOrder.Remove(node);
            cacheOrder.AddFirst(node);
        }
        else
        {
            // 如果缓存已满，则移除最久未使用的区块
            if (chunkCache.Count >= MAX_CACHE_SIZE)
            {
                var oldestNode = cacheOrder.Last;
                cacheOrder.RemoveLast();
                chunkCache.Remove(oldestNode.Value.chunkCoord);
            }

            // 添加新的区块到缓存
            var newNode = new LinkedListNode<ChunkData>(chunkData);
            cacheOrder.AddFirst(newNode);
            chunkCache[chunkCoord] = newNode;
        }
    }

    private void UseCache(Vector2Int chunkCoord)
    {
        if (chunkCache.TryGetValue(chunkCoord, out var node))
        {
            // 将用的区块移动到链表头部
            cacheOrder.Remove(node);
            cacheOrder.AddFirst(node);
        }
    }

    private async Task UnloadChunkAsync(Vector2Int chunkCoord)
    {
        if (!loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<ChunkData> loadOperation))
        {
            Debug.Log($"Chunk {chunkCoord} is not loaded, cannot unload");
            return;
        }

        Debug.Log($"Starting to unload chunk {chunkCoord}");

        // 检查操作是否有效
        if (loadOperation.IsValid())
        {
            ChunkData chunkData = loadOperation.Result;

            // 1. 卸载游戏物体
            GameObject chunkParent = GameObject.Find($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Objects");
            if (chunkParent != null)
            {
                Destroy(chunkParent);
                await Task.Yield(); // 等待一帧，确保物体被销毁
            }

            // 2. 卸载瓦片
            await UnloadTilesAsync(chunkData, chunkCoord);

            if (!chunkCache.ContainsKey(chunkCoord))
            {
                AddToCache(chunkCoord, chunkData);
            }

            // 在释放之前再次检查操作是否有效
            if (loadOperation.IsValid())
            {
                Addressables.Release(loadOperation);
            }
        }
        
        // 无论操作是否有效，从 loadedChunks 中移除
        loadedChunks.Remove(chunkCoord);
        Debug.Log($"Chunk {chunkCoord} unloaded and removed from loadedChunks");
    }

    private async Task UnloadTilesAsync(ChunkData chunkData, Vector2Int chunkCoord)
    {
        List<(Vector3Int, Tilemap)> tilesToClear = new List<(Vector3Int, Tilemap)>();
        foreach (var layerData in chunkData.tilemapLayers)
        {
            Transform tilemapTransform = levelGrid.transform.Find(layerData.layerName);
            if (tilemapTransform != null)
            {
                Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    foreach (var tileData in layerData.tiles)
                    {
                        Vector3Int globalPos = new Vector3Int(
                            tileData.position.x + chunkCoord.x * chunkWidth - chunkWidth / 2,
                            tileData.position.y + chunkCoord.y * chunkHeight - chunkHeight / 2,
                            tileData.position.z
                        );
                        tilesToClear.Add((globalPos, tilemap));
                    }
                }
            }
        }

        // 分帧清除瓦片
        for (int i = 0; i < tilesToClear.Count; i += TILES_TO_CLEAR_PER_FRAME)
        {
            int endIndex = Mathf.Min(i + TILES_TO_CLEAR_PER_FRAME, tilesToClear.Count);
            for (int j = i; j < endIndex; j++)
            {
                var (pos, tilemap) = tilesToClear[j];
                tilemap.SetTile(pos, null);
            }
            await Task.Yield(); // 每处理完一批瓦片后等待一帧
        }
    }

    public Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt((worldPos.x + chunkWidth / 2) / chunkWidth),
            Mathf.FloorToInt((worldPos.y + chunkHeight / 2) / chunkHeight)
        );
        return chunkCoord;
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

        // 清除缓存
        chunkCache.Clear();
        cacheOrder.Clear();
    }

    private void CheckAddressablesStatus()
    {
        string testKey = "ChunkData_0_0";
        Addressables.GetDownloadSizeAsync(testKey).Completed += (op) => 
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                // Debug.Log($"Download size for {testKey}: {op.Result}");
            }
            else
            {
                Debug.LogError($"Failed to get download size for {testKey}: {op.OperationException}");
            }
        };
    }

    private void LoadChunkSync(Vector2Int chunkCoord)
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
                        if (chunkData.objects.Count > 0)
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
                                // Debug.LogError($"{layerData.layerName} not found in LevelGrid.");
                                continue;
                            }

                            Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
                            if (tilemap == null)
                            {
                                // Debug.LogError($"Tilemap component not found on {layerData.layerName}.");
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

                        loadedChunks[chunkCoord] = loadOperation;
                    }
                    else
                    {
                        Addressables.Release(loadOperation);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"同步加载区块时发生错误: {chunkCoord}, 错误: {e.Message}");
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
        catch (Exception e)
        {
            Debug.LogError($"Error checking if chunk {chunkCoord} exists: {e.Message}");
            return false;
        }
    }

    private void CleanUpUnusedResources()
    {
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
}
