using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq; // 添加这行

public class ShadowWall : MonoBehaviour
{
    [SerializeField]private string _saveFileName = "hiddenTiles.json";
    private string _saveFilePath;

    private Tilemap _tilemap;
    private HashSet<Vector3Int> _hiddenTiles = new HashSet<Vector3Int>(); // 记录隐藏的 Tile 位置
    private Dictionary<Vector3Int, TileBase> _originalTiles = new Dictionary<Vector3Int, TileBase>(); // 原始 Tile


    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);
    }

    private void Start()
    {
        // 只在非编辑器模式下加载隐藏的 Tile
        if (!Application.isEditor)
        {
            LoadHiddenTiles();
        }


        EventManager.instance.OnButtonShadowWallDown += HideTileAndNeighbors;

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 获取玩家的位置
            Vector3 playerPosition = other.transform.position;
            

            HideTileAndNeighbors(playerPosition);
            
        }
    }

    // 当玩家与某个 Tile 发生碰撞时调用这个函数
    private void HideTileAndNeighbors(Vector3 playerPosition)
    {
        
        // 将玩家的世界坐标转换为 Tilemap 坐标
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

        SaveHiddenTiles();
    }

    // 还原所有隐藏的 Tile
    public void RestoreHiddenTiles()
    {
        foreach (var kvp in _originalTiles)
        {
            _tilemap.SetTile(kvp.Key, kvp.Value); // 恢复 Tile
        }

        _hiddenTiles.Clear();
        _originalTiles.Clear();

        // 删除保存的文件
        if (File.Exists(_saveFilePath))
        {
            File.Delete(_saveFilePath);
        }
    }

    // 保存隐藏的 Tile 到文件
    private void SaveHiddenTiles()
    {
        // 只在非编辑器模式下保存隐藏的 Tile
        if (!Application.isEditor)
        {
            string json = JsonUtility.ToJson(new TileDataList(_hiddenTiles));
            File.WriteAllText(_saveFilePath, json);
        }
    }

    // 从文件加载隐藏的 Tile 状态
    private void LoadHiddenTiles()
    {
        // 只在非编辑器模式下加载隐藏的 Tile
        if (!Application.isEditor)
        {
            try
            {
                if (File.Exists(_saveFilePath))
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
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载隐藏瓦片时出错：{e.Message}");
            }
        }
    }

    // 游戏退出时自动保存
    private void OnApplicationQuit()
    {
        // 只在非编辑器模式下自动保存
        if (!Application.isEditor)
        {
            SaveHiddenTiles();
        }
    }
}

// 用于保存 Vector3Int 数组到 JSON
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
