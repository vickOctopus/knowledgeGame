using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps; // 添加此行

[CreateAssetMenu(fileName = "ChunkData", menuName = "ScriptableObjects/ChunkData", order = 1)]
public class ChunkData : ScriptableObject, ISaveable
{
    public Vector2Int chunkCoord;
    public List<TilemapLayerData> tilemapLayers = new List<TilemapLayerData>(); // 存储Tilemap层级信息
    public List<ObjectData> objects = new List<ObjectData>();

    private void OnEnable()
    {
        if (tilemapLayers != null)
        {
            Debug.Log($"ChunkData {chunkCoord} loaded with {tilemapLayers.Count} tilemap layers.");
        }
        else
        {
            Debug.LogWarning($"ChunkData {chunkCoord} loaded, but tilemapLayers list is null.");
        }
    }

    public void Save(int slotIndex)
    {
        // Implement save logic here
    }

    public void Load(int slotIndex)
    {
        // Implement load logic here
    }
}

[System.Serializable]
public class TilemapLayerData
{
    public string layerName; // 添加此字段来存储Tilemap的名称
    public int sortingOrder;
    public List<TileData> tiles = new List<TileData>();
}

[System.Serializable]
public class TileData
{
    public Vector3Int position;
    public TileBase tile;
    public Color color = Color.white;
    public Tile.ColliderType colliderType = Tile.ColliderType.None;
}

[System.Serializable]
public class ObjectData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string prefabName;
}
