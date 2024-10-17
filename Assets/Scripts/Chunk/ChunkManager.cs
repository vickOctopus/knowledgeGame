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

    private Dictionary<Vector2Int, AsyncOperationHandle<GameObject>> loadedChunks = new Dictionary<Vector2Int, AsyncOperationHandle<GameObject>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;
    private HashSet<Vector2Int> chunksBeingLoaded = new HashSet<Vector2Int>();
    private bool isInitializing = false;

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
            string chunkAddress = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";
            
            int retryCount = 0;
            const int maxRetries = 3;
            
            while (retryCount < maxRetries)
            {
                Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkWidth, chunkCoord.y * chunkHeight, 0);
                AsyncOperationHandle<GameObject> loadOperation = Addressables.InstantiateAsync(chunkAddress, chunkPosition, Quaternion.identity, transform);
                yield return loadOperation;

                if (loadOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject chunkObject = loadOperation.Result;
                    chunkObject.transform.position = chunkPosition;
                    loadedChunks[chunkCoord] = loadOperation;
                    
                    chunksBeingLoaded.Remove(chunkCoord);
                    yield break;
                }
                else
                {
                    Addressables.Release(loadOperation);
                    retryCount++;
                    
                    if (retryCount < maxRetries)
                    {
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
            
            chunksBeingLoaded.Remove(chunkCoord);
        }
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<GameObject> loadOperation))
        {
            Addressables.ReleaseInstance(loadOperation);
            loadedChunks.Remove(chunkCoord);
        }
    }

    private Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkWidth),
            Mathf.FloorToInt(worldPos.y / chunkHeight)
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
        Addressables.GetDownloadSizeAsync("Chunk_0_0").Completed += (op) => { };
    }
}
