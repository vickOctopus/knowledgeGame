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
        Debug.Log($"Tilemap bounds: {_tilemap.cellBounds}");
        Debug.Log($"Player world position: {playerPosition}");

        Queue<Vector3Int> tileQueue = new Queue<Vector3Int>();
        HashSet<Vector3Int> processedTiles = new HashSet<Vector3Int>();

        // 检查周围的瓦片，并将非空的瓦片加入队列
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPosition = startTilePosition + new Vector3Int(x, y, 0);
                TileBase tile = _tilemap.GetTile(checkPosition);
                Debug.Log($"Checking tile at {checkPosition}, tile is {(tile != null ? "not null" : "null")}");
                if (tile != null)
                {
                    tileQueue.Enqueue(checkPosition);
                }
            }
        }

        int processedCount = 0;
        while (tileQueue.Count > 0)
        {
            Vector3Int tilePosition = tileQueue.Dequeue();
            if (processedTiles.Contains(tilePosition)) continue;

            processedTiles.Add(tilePosition);

            TileBase tile = _tilemap.GetTile(tilePosition);
            Debug.Log($"Processing tile at {tilePosition}, tile is {(tile != null ? "not null" : "null")}");
            
            if (tile != null)
            {
                _tilemap.SetTile(tilePosition, null);
                RemoveTileFromChunkData(tilePosition);
                processedCount++;

                Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
                foreach (var direction in directions)
                {
                    Vector3Int neighborPos = tilePosition + direction;
                    if (!processedTiles.Contains(neighborPos) && _tilemap.GetTile(neighborPos) != null)
                    {
                        tileQueue.Enqueue(neighborPos);
                    }
                }
            }
        }

        Debug.Log($"Processed and removed {processedCount} tiles for ShadowWall on {gameObject.name}");
        Debug.Log($"Tilemap has {_tilemap.GetTilesBlock(_tilemap.cellBounds).Count(t => t != null)} tiles remaining");

        // 强制更新 Tilemap
        _tilemap.RefreshAllTiles();
        Debug.Log($"Tilemap refreshed for ShadowWall on {gameObject.name}");
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
            Debug.Log($"Removed tile at local position {localPosition} from chunk {chunkCoord} for Tilemap: {_tilemapName}");
        }
        else
        {
            Debug.LogWarning($"ChunkData not found for chunk {chunkCoord} when trying to remove tile at {tilePosition}");
        }
    }
}
