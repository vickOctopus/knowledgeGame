using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Collections;  // 确保添加了这个引用

public class ShadowWall : MonoBehaviour, ISaveable
{
    [SerializeField] private string _saveFileName = "hiddenTiles.json";
    private string _tilemapName; // 移除 SerializeField 特性
    private string _saveFilePath;

    private Tilemap _tilemap;
    private HashSet<Vector3Int> _hiddenTiles = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> _originalTiles = new Dictionary<Vector3Int, TileBase>();

    private ChunkManager _chunkManager;

    private void Awake()
    {
        // 自动获取挂载物体的名字
        _tilemapName = gameObject.name;

        // 首先尝试在当前对象上查找 Tilemap
        _tilemap = GetComponent<Tilemap>();
        
        // 如果在当前对象上没有找到，则在子物体中查找
        if (_tilemap == null)
        {
            _tilemap = GetComponentInChildren<Tilemap>();
        }

        // 如果仍然没有找到，则在整个场景中查找
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
        
        Load(PlayerPrefs.GetInt("CurrentSlotIndex"));
        if (EventManager.instance != null)
        {
            EventManager.instance.OnButtonShadowWallDown += HideTileAndNeighbors;
        }
        else
        {
            Debug.LogError($"EventManager instance is null in ShadowWall on {gameObject.name}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            Debug.LogError($"Null collider in OnTriggerEnter2D for ShadowWall on {gameObject.name}");
            return;
        }

        if (other.CompareTag("Player"))
        {
            Vector3 playerPosition = other.transform.position;
            HideTileAndNeighbors(playerPosition);
        }
    }

    private void HideTileAndNeighbors(Vector3 playerPosition)
    {
        if (_tilemap == null)
        {
            Debug.LogError($"Tilemap is null in HideTileAndNeighbors for {gameObject.name}");
            return;
        }

        if (_chunkManager == null)
        {
            _chunkManager = ChunkManager.Instance;
            if (_chunkManager == null)
            {
                Debug.LogError($"ChunkManager is still null in HideTileAndNeighbors for {gameObject.name}");
                return;
            }
        }

        Vector3Int startTilePosition = _tilemap.WorldToCell(playerPosition);
        Vector2Int startChunkCoord = _chunkManager.GetChunkCoordFromWorldPos(playerPosition);
        
        Debug.Log($"Player entered tile in chunk: {startChunkCoord} for Tilemap: {_tilemapName}");

        Queue<Vector3Int> tileQueue = new Queue<Vector3Int>();
        tileQueue.Enqueue(startTilePosition);

        while (tileQueue.Count > 0)
        {
            Vector3Int tilePosition = tileQueue.Dequeue();
            if (_hiddenTiles.Contains(tilePosition)) continue;

            if (!_originalTiles.ContainsKey(tilePosition))
            {
                _originalTiles[tilePosition] = _tilemap.GetTile(tilePosition);
            }

            _tilemap.SetTile(tilePosition, null);
            _hiddenTiles.Add(tilePosition);

            RemoveTileFromChunkData(tilePosition);

            Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            foreach (var direction in directions)
            {
                Vector3Int neighborPos = tilePosition + direction;
                if (_tilemap.GetTile(neighborPos) != null)
                {
                    tileQueue.Enqueue(neighborPos);
                }
            }
        }
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

    public void RestoreHiddenTiles()
    {
        foreach (var kvp in _originalTiles)
        {
            _tilemap.SetTile(kvp.Key, kvp.Value);
        }

        _hiddenTiles.Clear();
        _originalTiles.Clear();

        if (File.Exists(_saveFilePath))
        {
            File.Delete(_saveFilePath);
        }
    }

    private void SaveHiddenTiles(int slotIndex)
    {
        string json = JsonUtility.ToJson(new TileDataList(_hiddenTiles));
        _saveFilePath = SaveManager.GetSavePath(slotIndex, $"{_tilemapName}_{_saveFileName}");
        File.WriteAllText(_saveFilePath, json);
    }

    private void LoadHiddenTiles(int slotIndex)
    {
        _saveFilePath = SaveManager.GetSavePath(slotIndex, $"{_tilemapName}_{_saveFileName}");
        if (File.Exists(_saveFilePath))
        {
            try
            {
                var json = File.ReadAllText(_saveFilePath);
                TileDataList data = JsonUtility.FromJson<TileDataList>(json);

                foreach (Vector3IntSerializable tilePosition in data.hiddenTiles)
                {
                    Vector3Int pos = tilePosition.ToVector3Int();
                    if (!_originalTiles.ContainsKey(pos))
                    {
                        _originalTiles[pos] = _tilemap.GetTile(pos);
                    }
                    _tilemap.SetTile(pos, null);
                    _hiddenTiles.Add(pos);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载隐藏瓦片时出错：{e.Message}");
            }
        }
    }

    public void Save(int slotIndex)
    {
        SaveHiddenTiles(slotIndex);
    }

    public void Load(int slotIndex)
    {
        LoadHiddenTiles(slotIndex);
    }
}

[System.Serializable]
public class Vector3IntSerializable
{
    public int x, y, z;

    public Vector3IntSerializable(Vector3Int vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3Int ToVector3Int()
    {
        return new Vector3Int(x, y, z);
    }
}

[System.Serializable]
public class TileDataList
{
    public List<Vector3IntSerializable> hiddenTiles;

    public TileDataList(HashSet<Vector3Int> hiddenTiles)
    {
        this.hiddenTiles = hiddenTiles.Select(v => new Vector3IntSerializable(v)).ToList();
    }
}
