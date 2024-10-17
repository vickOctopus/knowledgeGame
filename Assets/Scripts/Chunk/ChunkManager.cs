using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    public const int chunkWidth = 50;
    public const int chunkHeight = 28;
    public int loadDistance = 1;

    private Dictionary<Vector2Int, AsyncOperationHandle<GameObject>> loadedChunks = new Dictionary<Vector2Int, AsyncOperationHandle<GameObject>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ChunkManager initialized");
        }
        else
        {
            Debug.Log("Duplicate ChunkManager found, destroying");
            Destroy(gameObject);
        }
    }

    public void InitializeChunks(Vector3 playerPosition)
    {
        currentChunk = GetChunkCoordFromWorldPos(playerPosition);
        Debug.Log($"Initializing chunks. Player position: {playerPosition}, Current chunk: {currentChunk}");
        StartCoroutine(UpdateVisibleChunksAsync());
    }

    public void ForceUpdateChunks(Vector3 cameraPosition)
    {
        Vector2Int newChunk = GetChunkCoordFromWorldPos(cameraPosition);
        Debug.Log($"Forcing chunk update. Camera position: {cameraPosition}, New chunk: {newChunk}, Current chunk: {currentChunk}");
        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            StartCoroutine(UpdateVisibleChunksAsync());
        }
    }

    private IEnumerator UpdateVisibleChunksAsync()
    {
        Debug.Log("Starting UpdateVisibleChunksAsync");
        HashSet<Vector2Int> newVisibleChunks = new HashSet<Vector2Int>();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                newVisibleChunks.Add(coord);
                if (!visibleChunks.Contains(coord))
                {
                    Debug.Log($"Loading new chunk: {coord}");
                    yield return LoadChunkAsync(coord);
                }
            }
        }

        foreach (Vector2Int chunk in visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                Debug.Log($"Unloading chunk: {chunk}");
                UnloadChunk(chunk);
            }
        }

        visibleChunks = newVisibleChunks;
        Debug.Log($"UpdateVisibleChunksAsync completed. Visible chunks: {string.Join(", ", visibleChunks)}");
    }

    private IEnumerator LoadChunkAsync(Vector2Int chunkCoord)
    {
        if (!loadedChunks.ContainsKey(chunkCoord))
        {
            string chunkAddress = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";
            Debug.Log($"Attempting to load chunk: {chunkAddress}");
            AsyncOperationHandle<GameObject> loadOperation = Addressables.InstantiateAsync(chunkAddress, 
                new Vector3(chunkCoord.x * chunkWidth, chunkCoord.y * chunkHeight, 0), 
                Quaternion.identity, transform);
            yield return loadOperation;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                loadedChunks[chunkCoord] = loadOperation;
                Debug.Log($"Successfully loaded chunk: {chunkAddress}");
            }
            else
            {
                Debug.LogError($"Failed to load chunk at {chunkCoord}. Status: {loadOperation.Status}");
            }
        }
        else
        {
            Debug.Log($"Chunk already loaded: {chunkCoord}");
        }
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out AsyncOperationHandle<GameObject> loadOperation))
        {
            Debug.Log($"Unloading chunk: {chunkCoord}");
            Addressables.ReleaseInstance(loadOperation);
            loadedChunks.Remove(chunkCoord);
        }
        else
        {
            Debug.LogWarning($"Attempted to unload non-existent chunk: {chunkCoord}");
        }
    }

    private Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkWidth),
            Mathf.FloorToInt(worldPos.y / chunkHeight)
        );
        Debug.Log($"World position {worldPos} corresponds to chunk coordinate {chunkCoord}");
        return chunkCoord;
    }

    public void UnloadAllChunks()
    {
        Debug.Log("Unloading all chunks");
        foreach (var loadOperation in loadedChunks.Values)
        {
            Addressables.ReleaseInstance(loadOperation);
        }
        loadedChunks.Clear();
        visibleChunks.Clear();
        Debug.Log("All chunks unloaded");
    }
}
