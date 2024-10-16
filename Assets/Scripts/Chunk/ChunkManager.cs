using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Pool;

public class ChunkManager : MonoBehaviour
{
    public const int chunkWidth = 50;
    public const int chunkHeight = 28;
    public int loadDistance = 1;

    public Tilemap[] managedTilemaps;
    public LayerMask objectLayers;

    private Dictionary<Vector2Int, HashSet<GameObject>> chunkObjects = new Dictionary<Vector2Int, HashSet<GameObject>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;

    private ObjectPool<HashSet<GameObject>> chunkObjectSetPool;

    private Dictionary<Vector2Int, BoundsInt> chunkBoundsCache = new Dictionary<Vector2Int, BoundsInt>();

    private Coroutine updateChunksCoroutine;

    private void Start()
    {
        InitializeChunks();
        // 同步加载玩家周围的 chunks
        Vector2Int playerChunk = GetChunkCoordFromWorldPos(PlayController.instance.transform.position);
        currentChunk = playerChunk;
        UpdateVisibleChunksImmediate();
    }

    private void InitializeChunks()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & objectLayers) != 0)
            {
                Vector2Int chunkCoord = GetChunkCoordFromWorldPos(obj.transform.position);
                AddObjectToChunk(chunkCoord, obj);
                obj.SetActive(false);
            }
        }

        foreach (Tilemap tilemap in managedTilemaps)
        {
            HideAllTiles(tilemap);
        }
    }

    // 新增的同步更新方法
    private void UpdateVisibleChunksImmediate()
    {
       
        HashSet<Vector2Int> newVisibleChunks = new HashSet<Vector2Int>();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                newVisibleChunks.Add(coord);
                if (!visibleChunks.Contains(coord))
                {
                    SetChunkVisibilityImmediate(coord, true);
                }
            }
        }

        foreach (Vector2Int chunk in visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                SetChunkVisibilityImmediate(chunk, false);
            }
        }

        visibleChunks = newVisibleChunks;
    }

    private bool IsChunkVisible(Vector2Int chunk)
    {
        return Mathf.Abs(chunk.x - currentChunk.x) <= loadDistance &&
               Mathf.Abs(chunk.y - currentChunk.y) <= loadDistance;
    }

    private void SetChunkVisibility(Vector2Int chunkCoord, bool visible)
    {
        StartCoroutine(SetChunkVisibilityAsync(chunkCoord, visible));
    }

    private IEnumerator SetChunkVisibilityAsync(Vector2Int chunkCoord, bool visible)
    {
        foreach (Tilemap tilemap in managedTilemaps)
        {
            SetTilemapChunkVisibility(tilemap, chunkCoord, visible);
            yield return null; // 让出控制权，等待下一帧
        }

        if (chunkObjects.TryGetValue(chunkCoord, out HashSet<GameObject> objects))
        {
            foreach (GameObject obj in objects)
            {
                SetObjectVisibility(obj, visible);
                yield return null; // 让出控制权，等待下一帧
            }
        }
    }

    private void SetTilemapChunkVisibility(Tilemap tilemap, Vector2Int chunkCoord, bool visible)
    {
        if (!chunkBoundsCache.TryGetValue(chunkCoord, out BoundsInt bounds))
        {
            Vector3Int chunkOrigin = new Vector3Int(
                chunkCoord.x * chunkWidth - chunkWidth / 2,
                chunkCoord.y * chunkHeight - chunkHeight / 2,
                0
            );
            bounds = new BoundsInt(chunkOrigin, new Vector3Int(chunkWidth, chunkHeight, 1));
            chunkBoundsCache[chunkCoord] = bounds;
        }

        // 使用缓存的 bounds
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, visible ? Color.white : Color.clear);
        }
    }

    private Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x + chunkWidth / 2) / chunkWidth),
            Mathf.FloorToInt((worldPos.y + chunkHeight / 2) / chunkHeight)
        );
    }

    private void HideAllTiles(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, Color.clear);
        }
    }

    private void AddObjectToChunk(Vector2Int chunkCoord, GameObject obj)
    {
        if (!chunkObjects.TryGetValue(chunkCoord, out HashSet<GameObject> objects))
        {
            objects = chunkObjectSetPool.Get();
            chunkObjects[chunkCoord] = objects;
        }
        objects.Add(obj);
    }

    public void ForceUpdateChunks(Vector3 cameraPosition)
    {
        Vector2Int newChunk = GetChunkCoordFromWorldPos(cameraPosition);
        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            if (updateChunksCoroutine != null)
            {
                StopCoroutine(updateChunksCoroutine);
            }
            updateChunksCoroutine = StartCoroutine(UpdateVisibleChunksAsync());
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
            }
        }

        // 隐藏不再可见的 chunks
        foreach (Vector2Int chunk in visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                SetChunkVisibility(chunk, false);
                yield return null; // 让出控制权，等待下一帧
            }
        }

        // 显示新可见的 chunks
        foreach (Vector2Int chunk in newVisibleChunks)
        {
            if (!visibleChunks.Contains(chunk))
            {
                SetChunkVisibility(chunk, true);
                yield return null; // 让出控制权，等待下一帧
            }
        }

        visibleChunks = newVisibleChunks;
    }

    private void EnableObject(GameObject obj)
    {
        obj.SetActive(true);
        foreach (Behaviour component in obj.GetComponents<Behaviour>())
        {
            component.enabled = true;
        }
    }

    private void DisableObject(GameObject obj)
    {
        foreach (Behaviour component in obj.GetComponents<Behaviour>())
        {
            component.enabled = false;
        }
        obj.SetActive(false);
    }

    // 添加这个方法来处理单个对象的可见性
    private void SetObjectVisibility(GameObject obj, bool visible)
    {
        if (obj != null)
        {
            if (visible)
            {
                EnableObject(obj);
            }
            else
            {
                DisableObject(obj);
            }
        }
    }

    // 添加这个方法来清理不再使用的 chunk
    private void CleanupChunk(Vector2Int chunkCoord)
    {
        if (chunkObjects.TryGetValue(chunkCoord, out HashSet<GameObject> objects))
        {
            chunkObjectSetPool.Release(objects);
            chunkObjects.Remove(chunkCoord);
        }
    }

    private void Awake()
    {
        chunkObjectSetPool = new ObjectPool<HashSet<GameObject>>(
            createFunc: () => new HashSet<GameObject>(),
            actionOnRelease: set => set.Clear()
        );
    }

    // 新增的同步设置 chunk 可见性的方法
    private void SetChunkVisibilityImmediate(Vector2Int chunkCoord, bool visible)
    {
        foreach (Tilemap tilemap in managedTilemaps)
        {
            SetTilemapChunkVisibility(tilemap, chunkCoord, visible);
        }

        if (chunkObjects.TryGetValue(chunkCoord, out HashSet<GameObject> objects))
        {
            foreach (GameObject obj in objects)
            {
                SetObjectVisibility(obj, visible);
            }
        }
    }
}
