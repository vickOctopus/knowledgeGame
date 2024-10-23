using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // 添加这行来使用 LINQ

public class ShadowWall : MonoBehaviour
{
    private string _tilemapName;
    private Tilemap _tilemap;
    private ChunkManager _chunkManager;

    private const int SEARCH_RADIUS = 1;

    private Queue<Vector3Int> _vector3IntPool = new Queue<Vector3Int>();

    private Vector3Int GetVector3Int(int x, int y, int z)
    {
        if (_vector3IntPool.Count > 0)
        {
            Vector3Int v = _vector3IntPool.Dequeue();
            v.x = x;
            v.y = y;
            v.z = z;
            return v;
        }
        return new Vector3Int(x, y, z);
    }

    private void ReleaseVector3Int(Vector3Int v)
    {
        _vector3IntPool.Enqueue(v);
    }

    private void Awake()
    {
        _tilemapName = gameObject.name;
        _tilemap = GetComponent<Tilemap>();
        
        if (_tilemap == null)
        {
            _tilemap = GetComponentInChildren<Tilemap>();
        }

        if (_tilemap == null)
        {
            _tilemap = GameObject.Find(_tilemapName)?.GetComponent<Tilemap>();
        }

        if (_tilemap == null)
        {
            Debug.LogError($"Tilemap '{_tilemapName}' not found for ShadowWall on {gameObject.name}!");
        }
        else
        {
            Debug.Log($"ShadowWall on {gameObject.name} successfully found Tilemap '{_tilemapName}'");
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeWithChunkManager());
    }

    private IEnumerator InitializeWithChunkManager()
    {
        while (ChunkManager.Instance == null)
        {
            yield return null;
        }
        _chunkManager = ChunkManager.Instance;
        Debug.Log($"ChunkManager initialized for ShadowWall on {gameObject.name}");
        
        if (EventManager.instance != null)
        {
            EventManager.instance.OnButtonShadowWallDown += HideTileAndNeighbors;
            Debug.Log($"OnButtonShadowWallDown event subscribed for ShadowWall on {gameObject.name}");
        }
        else
        {
            Debug.LogError($"EventManager instance is null in ShadowWall on {gameObject.name}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            Vector3 playerPosition = other.transform.position;
            Debug.Log($"Player entered trigger for ShadowWall on {gameObject.name} at position {playerPosition}");
            HideTileAndNeighbors(playerPosition);
        }
    }

    private void HideTileAndNeighbors(Vector3 playerPosition)
    {
        if (_tilemap == null || _chunkManager == null)
        {
            Debug.LogError($"Tilemap or ChunkManager is null in HideTileAndNeighbors for {gameObject.name}");
            return;
        }

        Vector3Int startTilePosition = _tilemap.WorldToCell(playerPosition);
        Vector2Int startChunkCoord = _chunkManager.GetChunkCoordFromWorldPos(playerPosition);
        
        Debug.Log($"Player entered tile at {startTilePosition} in chunk: {startChunkCoord} for Tilemap: {_tilemapName}");

        HashSet<Vector3Int> tilesToProcess = new HashSet<Vector3Int>();
        HashSet<Vector3Int> processedTiles = new HashSet<Vector3Int>();

        // 初始化待处理瓦片
        for (int x = -SEARCH_RADIUS; x <= SEARCH_RADIUS; x++)
        {
            for (int y = -SEARCH_RADIUS; y <= SEARCH_RADIUS; y++)
            {
                tilesToProcess.Add(startTilePosition + new Vector3Int(x, y, 0));
            }
        }

        int processedCount = 0;
        while (tilesToProcess.Count > 0)
        {
            Vector3Int tilePosition = tilesToProcess.First();
            tilesToProcess.Remove(tilePosition);

            if (processedTiles.Contains(tilePosition)) continue;
            processedTiles.Add(tilePosition);

            TileBase tile = _tilemap.GetTile(tilePosition);
            if (tile != null)
            {
                _tilemap.SetTile(tilePosition, null);
                
                #if !UNITY_EDITOR
                RemoveTileFromChunkData(tilePosition);
                #endif
                
                processedCount++;

                // 添加相邻瓦片到待处理集合
                foreach (var direction in new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right })
                {
                    Vector3Int neighborPos = tilePosition + direction;
                    if (!processedTiles.Contains(neighborPos))
                    {
                        tilesToProcess.Add(neighborPos);
                    }
                }
            }
        }

        Debug.Log($"Processed and removed {processedCount} tiles for ShadowWall on {gameObject.name}");

        // 强制更新 Tilemap
        _tilemap.RefreshAllTiles();
    }

    private void RemoveTileFromChunkData(Vector3Int tilePosition)
    {
        Vector2Int chunkCoord = _chunkManager.GetChunkCoordFromWorldPos(_tilemap.CellToWorld(tilePosition));
        ChunkData chunkData = _chunkManager.GetChunkData(chunkCoord);
        if (chunkData != null)
        {
            Vector3Int localPosition = new Vector3Int(
                tilePosition.x - chunkCoord.x * ChunkManager.ChunkWidth + ChunkManager.ChunkWidth / 2,
                tilePosition.y - chunkCoord.y * ChunkManager.ChunkHeight + ChunkManager.ChunkHeight / 2,
                tilePosition.z
            );
            chunkData.RemoveTile(localPosition, _tilemapName);
        }
    }
}
