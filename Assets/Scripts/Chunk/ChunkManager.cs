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
    private bool isInitializing = false;

    [SerializeField] private GameObject levelGrid;

    private const int MAX_CACHE_SIZE = 20; // 可以根据需要调整
    private Dictionary<Vector2Int, LinkedListNode<ChunkData>> chunkCache = new Dictionary<Vector2Int, LinkedListNode<ChunkData>>();
    private LinkedList<ChunkData> cacheOrder = new LinkedList<ChunkData>();

    private const int TILES_PER_FRAME = 100; // 每帧处理的瓦片数量
    private const int OBJECTS_PER_FRAME = 10; // 每帧处理的对象数量

    private const int MAX_CONCURRENT_LOADS = 2; // 最大并发加载数
    private Queue<Vector2Int> loadQueue = new Queue<Vector2Int>();
    private int currentLoads = 0;

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
        HashSet<Vector2Int> newVisibleChunks = new HashSet<Vector2Int>();
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                newVisibleChunks.Add(coord);
                if (!visibleChunks.Contains(coord) && !loadedChunks.ContainsKey(coord) && !chunksBeingLoaded.Contains(coord))
                {
                    chunksToLoad.Add(coord);
                }
            }
        }

        // 按照到当前区块的距离排序
        chunksToLoad.Sort((a, b) => 
            Vector2Int.Distance(a, currentChunk).CompareTo(Vector2Int.Distance(b, currentChunk)));

        foreach (Vector2Int coord in chunksToLoad)
        {
            loadQueue.Enqueue(coord);
        }

        // 等待 ProcessLoadQueueAsync 完成
        await ProcessLoadQueueAsync();

        foreach (Vector2Int chunk in visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                UnloadChunk(chunk);
            }
        }

        visibleChunks = newVisibleChunks;
        isInitializing = false;
    }

    private async Task ProcessLoadQueueAsync()
    {
        List<Task> loadTasks = new List<Task>();

        while (loadQueue.Count > 0)
        {
            while (currentLoads < MAX_CONCURRENT_LOADS && loadQueue.Count > 0)
            {
                Vector2Int coord = loadQueue.Dequeue();
                loadTasks.Add(LoadChunkAsync(coord));
                currentLoads++;
            }

            // 等待当前批次的加载任务完成
            await Task.WhenAll(loadTasks);
            loadTasks.Clear();

            // 使用 Task.Delay(1) 代替 Task.Yield()
            await Task.Delay(1);
        }
    }

    private async Task LoadChunkAsync(Vector2Int chunkCoord)
    {
        if (!loadedChunks.ContainsKey(chunkCoord) && !chunksBeingLoaded.Contains(chunkCoord))
        {
            chunksBeingLoaded.Add(chunkCoord);
            string chunkDataAddress = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";

            Debug.Log($"开始加载区块: {chunkCoord}");

            var loadOperation = Addressables.LoadAssetAsync<ChunkData>(chunkDataAddress);
            await loadOperation.Task; // 确保这里使用的是 loadOperation.Task

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                ChunkData chunkData = loadOperation.Result;

                if (chunkData == null || levelGrid == null)
                {
                    Debug.LogError($"加载区块失败: {chunkCoord}");
                    chunksBeingLoaded.Remove(chunkCoord);
                    Addressables.Release(loadOperation);
                    currentLoads--;
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

                loadedChunks[chunkCoord] = loadOperation;
                Debug.Log($"区块加载完成: {chunkCoord}");
            }
            else
            {
                Debug.LogError($"加载区块失败: {chunkCoord}, 错误: {loadOperation.OperationException}");
                Addressables.Release(loadOperation);
            }

            chunksBeingLoaded.Remove(chunkCoord);
            currentLoads--;
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

            for (int i = 0; i < layerKVP.Value.Count; i += TILES_PER_FRAME * 2) // 增加批量处理数量
            {
                int endIndex = Mathf.Min(i + TILES_PER_FRAME * 2, layerKVP.Value.Count); // 增加批量处理数量
                var batch = layerKVP.Value.GetRange(i, endIndex - i);

                var positions = batch.Select(t => t.Item1).ToArray();
                var tiles = batch.Select(t => t.Item2).ToArray();
                tilemap.SetTiles(positions, tiles);

                foreach (var (pos, _, color, colliderType) in batch)
                {
                    tilemap.SetColor(pos, color);
                    tilemap.SetColliderType(pos, colliderType);
                }

                await Task.Yield();
            }
        }
    }

    private async Task InstantiateObjectsAsync(ChunkData chunkData, GameObject chunkParent, Vector2Int chunkCoord)
    {
        for (int i = 0; i < chunkData.objects.Count; i += OBJECTS_PER_FRAME * 2) // 增加批量处理数量
        {
            int endIndex = Mathf.Min(i + OBJECTS_PER_FRAME * 2, chunkData.objects.Count); // 增加批量处理数量
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
            // 将使用的区块移动到链表头部
            cacheOrder.Remove(node);
            cacheOrder.AddFirst(node);
        }
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<ChunkData> loadOperation))
        {
            // 检查资源是否已经被释放
            if (!loadOperation.IsValid())
            {
                Debug.LogWarning($"尝试卸载无效的区块: {chunkCoord}");
                loadedChunks.Remove(chunkCoord);
                return;
            }

            foreach (var layerData in loadOperation.Result.tilemapLayers)
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
                            tilemap.SetTile(globalPos, null);
                        }
                    }
                }
            }

            GameObject chunkParent = GameObject.Find($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Objects");
            if (chunkParent != null)
            {
                Destroy(chunkParent);
            }

            // 更新缓存
            if (!chunkCache.ContainsKey(chunkCoord))
            {
                AddToCache(chunkCoord, loadOperation.Result);
            }

            // 确保释放资源
            Addressables.Release(loadOperation);
            loadedChunks.Remove(chunkCoord);
        }
    }

    private Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
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

            var loadOperation = Addressables.LoadAssetAsync<ChunkData>(chunkDataAddress);
            loadOperation.WaitForCompletion();

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
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

    private void CleanUpUnusedResources()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
