
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;

public class ShadowWall : MonoBehaviour
{
    private Tilemap _tilemap;
    private HashSet<Vector3Int> _hiddenTiles = new HashSet<Vector3Int>(); // 记录隐藏的 Tile 位置
    private Dictionary<Vector3Int, TileBase> _originalTiles = new Dictionary<Vector3Int, TileBase>(); // 原始 Tile
    private string _saveFilePath;


    private void Awake()
    {
         _tilemap = GetComponent<Tilemap>();
         _saveFilePath = Application.persistentDataPath + "/hiddenTiles.json";

         if (!Directory.Exists(Application.persistentDataPath + "/hiddenTiles"))
         {
             Directory.CreateDirectory(Application.persistentDataPath + "/hiddenTiles");
         }
         
    }

   private void Start()
    {
        // 加载之前隐藏的 Tile
        LoadHiddenTiles();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 获取玩家的位置
            Vector3 playerPosition = other.transform.position;

            // 将玩家的世界坐标转换为 Tilemap 坐标
            Vector3Int tilePosition = _tilemap.WorldToCell(playerPosition);

            HideTileAndNeighbors(tilePosition);
            
        }
    }

    // 当玩家与某个 Tile 发生碰撞时调用这个函数
    private void HideTileAndNeighbors(Vector3Int tilePosition)
    {
        // 递归隐藏所有相邻 Tile
        HideTileRecursive(tilePosition);

        // 保存隐藏状态
        SaveHiddenTiles();
    }

    // 递归隐藏相邻的 Tile
    private void HideTileRecursive(Vector3Int tilePosition)
    {
        if (_hiddenTiles.Contains(tilePosition)) return; // 防止重复处理

        // 记录原始 Tile
        if (!_originalTiles.ContainsKey(tilePosition))
        {
            _originalTiles[tilePosition] = _tilemap.GetTile(tilePosition);
        }

        _tilemap.SetTile(tilePosition, null); // 隐藏当前 Tile
        _hiddenTiles.Add(tilePosition); // 将该 Tile 位置加入隐藏列表

        // 四个方向查找相邻 Tile
        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        foreach (var direction in directions)
        {
            Vector3Int neighborPos = tilePosition + direction;
            if (_tilemap.GetTile(neighborPos) != null) // 如果相邻 Tile 存在
            {
                HideTileRecursive(neighborPos); // 递归隐藏
            }
        }
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
        if (File.Exists(Application.persistentDataPath + "/hiddenTiles.json"))
        {
            File.Delete(Application.persistentDataPath + "/hiddenTiles.json");
        }
        
    }

    // 保存隐藏的 Tile 到文件
    private void SaveHiddenTiles()
    {
        List<Vector3IntSerializable> hiddenTilesList = new List<Vector3IntSerializable>();

        foreach (Vector3Int tilePosition in _hiddenTiles)
        {
            hiddenTilesList.Add(new Vector3IntSerializable(tilePosition));
        }

        string json = JsonUtility.ToJson(new TileDataList(hiddenTilesList));
        File.WriteAllText(_saveFilePath, json);

    }

    // 从文件加载隐藏的 Tile 状态
    private void LoadHiddenTiles()
    {
        if (File.Exists(_saveFilePath))
        {
            var json = File.ReadAllText(_saveFilePath);
            TileDataList data = JsonUtility.FromJson<TileDataList>(json);

            foreach (Vector3IntSerializable tilePosition in data.hiddenTiles)
            {
                Vector3Int pos = tilePosition.ToVector3Int();

                // 记录原始 Tile
                if (!_originalTiles.ContainsKey(pos))
                {
                    _originalTiles[pos] = _tilemap.GetTile(pos);
                }

                _tilemap.SetTile(pos, null);  // 隐藏 Tile
                _hiddenTiles.Add(pos);
            }
        }
    }

    // 游戏退出时自动保存
    private void OnApplicationQuit()
    {
        SaveHiddenTiles();
    }
}

// 用于保存 Vector3Int 数据到 JSON
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

    public TileDataList(List<Vector3IntSerializable> hiddenTiles)
    {
        this.hiddenTiles = hiddenTiles;
    }
}