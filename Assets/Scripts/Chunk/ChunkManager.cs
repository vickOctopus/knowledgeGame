using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Pool; // 添加这行，如果你使用的是 Unity 2020.1 或更新版本

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

    // 如果使用 Unity 2020.1 或更新版本，使用内置的 ObjectPool
    private ObjectPool<HashSet<GameObject>> chunkObjectSetPool;

    // 如果使用较旧版本的 Unity，使用自定义的 SimpleObjectPool
    // private SimpleObjectPool<HashSet<GameObject>> chunkObjectSetPool;

    private Dictionary<Vector2Int, BoundsInt> chunkBoundsCache = new Dictionary<Vector2Int, BoundsInt>();

    private void Start()
    {
        InitializeChunks();
        UpdateVisibleChunks();
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

    private void UpdateVisibleChunks()
    {
        // 使用 HashSet<Vector2Int>.Enumerator 来避免额外的内存分配
        HashSet<Vector2Int>.Enumerator enumerator = visibleChunks.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Vector2Int chunk = enumerator.Current;
            if (!IsChunkVisible(chunk))
            {
                SetChunkVisibility(chunk, false);
            }
        }

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                if (!visibleChunks.Contains(coord))
                {
                    SetChunkVisibility(coord, true);
                    visibleChunks.Add(coord);
                }
            }
        }
    }

    private bool IsChunkVisible(Vector2Int chunk)
    {
        return Mathf.Abs(chunk.x - currentChunk.x) <= loadDistance &&
               Mathf.Abs(chunk.y - currentChunk.y) <= loadDistance;
    }

    private void SetChunkVisibility(Vector2Int chunkCoord, bool visible)
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
            UpdateVisibleChunks();
        }
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
        // 如果使用 Unity 2020.1 或更新版本，初始化 ObjectPool
        chunkObjectSetPool = new ObjectPool<HashSet<GameObject>>(
            createFunc: () => new HashSet<GameObject>(),
            actionOnRelease: set => set.Clear()
        );

        // 如果使用较旧版本的 Unity，初始化 SimpleObjectPool
        // chunkObjectSetPool = new SimpleObjectPool<HashSet<GameObject>>(
        //     () => new HashSet<GameObject>(),
        //     set => set.Clear()
        // );
    }
}

// 如果使用较旧版本的 Unity，添加这个简单的对象池实现
// public class SimpleObjectPool<T>
// {
//     private readonly System.Func<T> createFunc;
//     private readonly System.Action<T> actionOnRelease;
//     private readonly Stack<T> pool = new Stack<T>();
// 
//     public SimpleObjectPool(System.Func<T> createFunc, System.Action<T> actionOnRelease)
//     {
//         this.createFunc = createFunc;
//         this.actionOnRelease = actionOnRelease;
//     }
// 
//     public T Get()
//     {
//         return pool.Count > 0 ? pool.Pop() : createFunc();
//     }
// 
//     public void Release(T item)
//     {
//         actionOnRelease(item);
//         pool.Push(item);
//     }
// }
