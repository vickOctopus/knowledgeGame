using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SwitchPlatform : MonoBehaviour
{
    [SerializeField] private TileBase trueTile;
    [SerializeField] private TileBase falseTile;
    private Tilemap _tilemap;
    private Dictionary<Vector2Int, List<Vector3Int>> _switchableTilesCache = new Dictionary<Vector2Int, List<Vector3Int>>();
    private HashSet<Vector3Int> _pendingTrueChanges = new HashSet<Vector3Int>();

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        if (_tilemap == null)
        {
            Debug.LogError("[SwitchPlatform] Tilemap component not found!");
            return;
        }

        if (trueTile == null || falseTile == null)
        {
            Debug.LogError("[SwitchPlatform] trueTile or falseTile is not set!");
            return;
        }
    }

    private void Start()
    {
        if (ChunkManager.Instance == null)
        {
            Debug.LogError("[SwitchPlatform] ChunkManager.Instance is null!");
            return;
        }

        ChunkManager.Instance.OnChunkLoaded += OnChunkLoaded;
        ChunkManager.Instance.OnChunkUnloaded += OnChunkUnloaded;
        ChunkManager.Instance.OnSwitchStateChanged += PlatformChange;
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.OnChunkLoaded -= OnChunkLoaded;
            ChunkManager.Instance.OnChunkUnloaded -= OnChunkUnloaded;
            ChunkManager.Instance.OnSwitchStateChanged -= PlatformChange;
        }
    }

    private void OnChunkLoaded(Vector2Int chunkCoord)
    {
        // 当新的 chunk 加载时，清除该 chunk 的缓存
        _switchableTilesCache.Remove(chunkCoord);
    }

    private void OnChunkUnloaded(Vector2Int chunkCoord)
    {
        // 当 chunk 卸载时，清除该 chunk 的缓存
        _switchableTilesCache.Remove(chunkCoord);
    }

    public void PlatformChange(Vector2Int chunkCoord)
    {
        List<Vector3Int> switchableTiles = GetSwitchableTiles(chunkCoord);
        
        if (switchableTiles.Count > 0)
        {
            foreach (var position in switchableTiles)
            {
                TileBase currentTile = _tilemap.GetTile(position);
                if (currentTile != null && currentTile.name == trueTile.name)
                {
                    _tilemap.SetTile(position, falseTile);
                }
                else if (currentTile != null && currentTile.name == falseTile.name)
                {
                    if (HasColliderAtPosition(position))
                    {
                        _pendingTrueChanges.Add(position);
                    }
                    else
                    {
                        _tilemap.SetTile(position, trueTile);
                    }
                }
            }
        }
    }

    private bool HasColliderAtPosition(Vector3Int position)
    {
        Vector3 worldPosition = _tilemap.GetCellCenterWorld(position);
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);
        return colliders.Length > 0;
    }

    private void Update()
    {
        if (_pendingTrueChanges.Count > 0)
        {
            List<Vector3Int> positionsToRemove = new List<Vector3Int>();

            foreach (var position in _pendingTrueChanges)
            {
                if (!HasColliderAtPosition(position))
                {
                    _tilemap.SetTile(position, trueTile);
                    positionsToRemove.Add(position);
                }
            }

            foreach (var position in positionsToRemove)
            {
                _pendingTrueChanges.Remove(position);
            }
        }
    }

    private List<Vector3Int> GetSwitchableTiles(Vector2Int chunkCoord)
    {
        if (_switchableTilesCache.TryGetValue(chunkCoord, out List<Vector3Int> cachedTiles))
        {
            return cachedTiles;
        }

        List<Vector3Int> switchableTiles = ScanChunkForSwitchableTiles(chunkCoord);
        _switchableTilesCache[chunkCoord] = switchableTiles;
        return switchableTiles;
    }

    private List<Vector3Int> ScanChunkForSwitchableTiles(Vector2Int chunkCoord)
    {
        // 计算 chunk 的世界坐标（左下角）
        Vector2Int chunkWorldPosition = new Vector2Int(
            chunkCoord.x * ChunkManager.ChunkWidth - ChunkManager.ChunkWidth / 2,
            chunkCoord.y * ChunkManager.ChunkHeight - ChunkManager.ChunkHeight / 2
        );
        
        // 计算 chunk 的边界
        BoundsInt chunkBounds = new BoundsInt(
            new Vector3Int(chunkWorldPosition.x, chunkWorldPosition.y, 0),
            new Vector3Int(ChunkManager.ChunkWidth, ChunkManager.ChunkHeight, 1)
        );

        List<Vector3Int> switchableTiles = new List<Vector3Int>();

        // 扫描 chunk 区域
        for (int x = chunkBounds.xMin; x < chunkBounds.xMax; x++)
        {
            for (int y = chunkBounds.yMin; y < chunkBounds.yMax; y++)
            {
                Vector3Int worldPosition = new Vector3Int(x, y, 0);
                Vector3Int cellPosition = _tilemap.WorldToCell(worldPosition);

                if (_tilemap.HasTile(cellPosition))
                {
                    TileBase tile = _tilemap.GetTile(cellPosition);
                    
                    if (tile != null && (tile.name == trueTile.name || tile.name == falseTile.name))
                    {
                        switchableTiles.Add(cellPosition);
                    }
                }
            }
        }

        return switchableTiles;
    }
}
