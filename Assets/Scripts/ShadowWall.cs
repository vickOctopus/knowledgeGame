using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ShadowWall : MonoBehaviour, ISaveable
{
    [SerializeField] private string _saveFileName = "hiddenTiles.json";
    private string _saveFilePath;

    private Tilemap _tilemap;
    private HashSet<Vector3Int> _hiddenTiles = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> _originalTiles = new Dictionary<Vector3Int, TileBase>();

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);
    }

    private void Start()
    {
        //LoadHiddenTiles();
        EventManager.instance.OnButtonShadowWallDown += HideTileAndNeighbors;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 playerPosition = other.transform.position;
            HideTileAndNeighbors(playerPosition);
        }
    }

    private void HideTileAndNeighbors(Vector3 playerPosition)
    {
        Vector3Int startTilePosition = _tilemap.WorldToCell(playerPosition);
        
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

    private void SaveHiddenTiles()
    {
        string json = JsonUtility.ToJson(new TileDataList(_hiddenTiles));
        File.WriteAllText(_saveFilePath, json);
    }

    private void LoadHiddenTiles()
    {
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
            catch (System.Exception e)
            {
                Debug.LogError($"加载隐藏瓦片时出错：{e.Message}");
            }
        }
    }

    public void PrepareForSave()
    {
        // 如果需要，可以在这里进行保存前的准备工作
    }

    public void Save()
    {
        SaveHiddenTiles();
    }

    public void Load()
    {
        LoadHiddenTiles();
    }
}

// 用于序列化 Vector3Int 的类
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

// 用于保存隐藏的 Tile 列表
[System.Serializable]
public class TileDataList
{
    public List<Vector3IntSerializable> hiddenTiles;

    public TileDataList(HashSet<Vector3Int> hiddenTiles)
    {
        this.hiddenTiles = hiddenTiles.Select(v => new Vector3IntSerializable(v)).ToList();
    }
}
