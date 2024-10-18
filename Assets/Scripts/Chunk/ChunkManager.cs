using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Tilemaps;

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
    }

    public void InitializeChunks(Vector3 playerPosition)
    {
        if (isInitializing) return;
        isInitializing = true;

        currentChunk = GetChunkCoordFromWorldPos(playerPosition);

        StartCoroutine(UpdateVisibleChunksAsync());
    }

    public void ForceUpdateChunks(Vector3 cameraPosition)
    {
        if (isInitializing) return;
        Vector2Int newChunk = GetChunkCoordFromWorldPos(cameraPosition);
        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            StartCoroutine(UpdateVisibleChunksAsync());
        }
    }

    private IEnumerator UpdateVisibleChunksAsync()
    {
        HashSet<Vector2Int> newVisibleChunks = new HashSet<Vector2Int>();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                newVisibleChunks.Add(coord);
                if (!visibleChunks.Contains(coord) && !loadedChunks.ContainsKey(coord) && !chunksBeingLoaded.Contains(coord))
                {
                    yield return LoadChunkAsync(coord);
                }
            }
        }

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

    private IEnumerator LoadChunkAsync(Vector2Int chunkCoord)
    {
        if (!loadedChunks.ContainsKey(chunkCoord) && !chunksBeingLoaded.Contains(chunkCoord))
        {
            chunksBeingLoaded.Add(chunkCoord);
            string chunkDataAddress = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";

            AsyncOperationHandle<ChunkData> loadOperation = Addressables.LoadAssetAsync<ChunkData>(chunkDataAddress);
            yield return loadOperation;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                ChunkData chunkData = loadOperation.Result;

                if (levelGrid == null)
                {
                    Debug.LogError("LevelGrid is not assigned.");
                    chunksBeingLoaded.Remove(chunkCoord);
                    yield break;
                }

                foreach (var layerData in chunkData.tilemapLayers)
                {
                    Transform tilemapTransform = levelGrid.transform.Find(layerData.layerName);
                    if (tilemapTransform == null)
                    {
                        Debug.LogError($"{layerData.layerName} not found in LevelGrid.");
                        continue;
                    }

                    Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
                    if (tilemap == null)
                    {
                        Debug.LogError($"Tilemap component not found on {layerData.layerName}.");
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
                    GameObject prefab = Resources.Load<GameObject>(objectData.prefabName);
                    if (prefab != null)
                    {
                        GameObject obj = Instantiate(prefab, levelGrid.transform);
                        obj.transform.localPosition = objectData.position;
                        obj.transform.rotation = objectData.rotation;
                        obj.transform.localScale = objectData.scale;
                    }
                }

                loadedChunks[chunkCoord] = loadOperation;
                chunksBeingLoaded.Remove(chunkCoord);
            }
            else
            {
                Addressables.Release(loadOperation);
                chunksBeingLoaded.Remove(chunkCoord);
            }
        }
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<ChunkData> loadOperation))
        {
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

            foreach (Transform child in levelGrid.transform)
            {
                if (child.name.StartsWith($"Chunk_{chunkCoord.x}_{chunkCoord.y}"))
                {
                    Destroy(child.gameObject);
                }
            }

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
    }

    private void CheckAddressablesStatus()
    {
        string testKey = "ChunkData_0_0";
        Addressables.GetDownloadSizeAsync(testKey).Completed += (op) => 
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Download size for {testKey}: {op.Result}");
            }
            else
            {
                Debug.LogError($"Failed to get download size for {testKey}: {op.OperationException}");
            }
        };
    }
}
