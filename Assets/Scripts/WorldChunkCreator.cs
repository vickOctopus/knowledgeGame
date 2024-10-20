using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class WorldChunkCreator : MonoBehaviour
{
    public int chunkWidth = 50;
    public int chunkHeight = 28;

    public void CreateChunks()
    {
        #if UNITY_EDITOR
        if (!EditorUtility.DisplayDialog("确认重新创建区块", 
            "这将删除所有现有的区块数据并重新创建它们。你确定吗？", 
            "是", "取消"))
        {
            return;
        }

        CleanupOldChunks();

        Bounds worldBounds = CalculateWorldBounds(gameObject);

        int startX = Mathf.FloorToInt(worldBounds.min.x / chunkWidth);
        int startY = Mathf.FloorToInt(worldBounds.min.y / chunkHeight);
        int endX = Mathf.CeilToInt(worldBounds.max.x / chunkWidth);
        int endY = Mathf.CeilToInt(worldBounds.max.y / chunkHeight);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                CreateChunk(new Vector2Int(x, y));
            }
        }

        AssetDatabase.Refresh();
        #endif
    }

    #if UNITY_EDITOR
    private void CleanupOldChunks()
    {
        string directoryPath = "Assets/ChunkData";
        string[] guids = AssetDatabase.FindAssets("t:ChunkData", new[] { directoryPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var entriesToRemove = new List<AddressableAssetEntry>();
            foreach (var group in settings.groups)
            {
                if (group != null)
                {
                    foreach (var entry in group.entries)
                    {
                        if (entry.address.StartsWith("Chunk_"))
                        {
                            entriesToRemove.Add(entry);
                        }
                    }
                }
            }

            foreach (var entry in entriesToRemove)
            {
                settings.RemoveAssetEntry(entry.guid);
            }
        }

        AssetDatabase.SaveAssets();
    }

    private void CreateChunk(Vector2Int chunkCoord)
    {
        string directoryPath = "Assets/ChunkData";
        if (!AssetDatabase.IsValidFolder(directoryPath))
        {
            AssetDatabase.CreateFolder("Assets", "ChunkData");
        }

        GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObject.AddComponent<Grid>();

        Vector3 chunkWorldPosition = new Vector3(
            chunkCoord.x * chunkWidth,
            chunkCoord.y * chunkHeight,
            0
        );
        chunkObject.transform.position = chunkWorldPosition;

        BoundsInt chunkBoundsInt = new BoundsInt(
            Vector3Int.FloorToInt(chunkWorldPosition) - new Vector3Int(chunkWidth / 2, chunkHeight / 2, 0),
            new Vector3Int(chunkWidth, chunkHeight, 1)
        );

        Tilemap[] sourceTilemaps = GetComponentsInChildren<Tilemap>();
        if (sourceTilemaps == null || sourceTilemaps.Length == 0)
        {
            return;
        }

        ChunkData chunkData = ScriptableObject.CreateInstance<ChunkData>();
        chunkData.chunkCoord = chunkCoord;
        chunkData.tilemapLayers = new List<TilemapLayerData>();
        chunkData.objects = new List<ObjectData>();

        for (int i = 0; i < sourceTilemaps.Length; i++)
        {
            Tilemap sourceTilemap = sourceTilemaps[i];
            if (sourceTilemap == null) continue;

            TilemapRenderer sourceRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
            if (sourceRenderer == null) continue;

            TilemapLayerData layerData = new TilemapLayerData
            {
                layerName = sourceTilemap.name,
                sortingOrder = sourceRenderer.sortingOrder,
                tiles = new List<TileData>()
            };

            BoundsInt overlap = new BoundsInt(
                Vector3Int.Max(sourceTilemap.cellBounds.min, chunkBoundsInt.min),
                Vector3Int.Min(sourceTilemap.cellBounds.max, chunkBoundsInt.max) - Vector3Int.Max(sourceTilemap.cellBounds.min, chunkBoundsInt.min)
            );

            for (int x = 0; x < overlap.size.x; x++)
            {
                for (int y = 0; y < overlap.size.y; y++)
                {
                    Vector3Int sourcePos = new Vector3Int(x + overlap.x, y + overlap.y, 0);
                    TileBase tile = sourceTilemap.GetTile(sourcePos);
                    if (tile != null)
                    {
                        Vector3Int localPos = sourcePos - chunkBoundsInt.min;
                        Color tileColor = sourceTilemap.GetColor(sourcePos);
                        Tile.ColliderType colliderType = sourceTilemap.GetColliderType(sourcePos);

                        layerData.tiles.Add(new TileData
                        {
                            position = localPos,
                            tile = tile,
                            color = tileColor,
                            colliderType = colliderType
                        });
                    }
                }
            }

            if (layerData.tiles.Count > 0)
            {
                chunkData.tilemapLayers.Add(layerData);
            }
        }

        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child == null || child.GetComponent<Tilemap>() != null) continue;

            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
            if (prefabRoot != child.gameObject)
            {
                continue;
            }

            Vector3Int childPosInt = new Vector3Int(Mathf.FloorToInt(child.position.x), Mathf.FloorToInt(child.position.y), 0);

            if (chunkBoundsInt.Contains(childPosInt))
            {
                string prefabAssetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject));
                if (string.IsNullOrEmpty(prefabAssetPath))
                {
                    continue;
                }

                PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(child.gameObject);
                if (prefabStatus != PrefabInstanceStatus.Connected)
                {
                    continue;
                }

                chunkData.objects.Add(new ObjectData
                {
                    position = child.position - chunkWorldPosition + new Vector3(chunkWidth / 2, chunkHeight / 2, 0),
                    rotation = child.rotation,
                    scale = child.localScale,
                    prefabName = prefabAssetPath
                });

                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (addressableSettings != null)
                {
                    var guid = AssetDatabase.AssetPathToGUID(prefabAssetPath);
                    if (string.IsNullOrEmpty(guid))
                    {
                        continue;
                    }

                    var entry = addressableSettings.CreateOrMoveEntry(guid, addressableSettings.DefaultGroup);
                    entry.address = prefabAssetPath;
                }
            }
        }

        string assetPath = $"{directoryPath}/ChunkData_{chunkCoord.x}_{chunkCoord.y}.asset";
        AssetDatabase.CreateAsset(chunkData, assetPath);
        AssetDatabase.SaveAssets();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";
        }

        DestroyImmediate(chunkObject);
    }
    #endif

    private Bounds CalculateWorldBounds(GameObject worldRoot)
    {
        Bounds bounds = new Bounds(worldRoot.transform.position, Vector3.zero);
        bool boundsInitialized = false;

        foreach (Renderer renderer in worldRoot.GetComponentsInChildren<Renderer>())
        {
            if (!boundsInitialized)
            {
                bounds = renderer.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (Collider collider in worldRoot.GetComponentsInChildren<Collider>())
        {
            if (!boundsInitialized)
            {
                bounds = collider.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!boundsInitialized)
        {
            foreach (Transform transform in worldRoot.GetComponentsInChildren<Transform>())
            {
                if (!boundsInitialized)
                {
                    bounds = new Bounds(transform.position, Vector3.zero);
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(transform.position);
                }
            }
        }

        bounds.Encapsulate(worldRoot.transform.position);

        return bounds;
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(WorldChunkCreator))]
    public class WorldChunkCreatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WorldChunkCreator creator = (WorldChunkCreator)target;
            if (GUILayout.Button("创建区块"))
            {
                creator.CreateChunks();
            }
        }
    }
    #endif
}
