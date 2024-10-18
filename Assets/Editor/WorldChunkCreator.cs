using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEditor.AddressableAssets;

public class WorldChunkCreator : EditorWindow
{
    private int chunkWidth = 50;
    private int chunkHeight = 28;
    private GameObject worldRoot;

    [MenuItem("Tools/World Chunk Creator")]
    public static void ShowWindow()
    {
        GetWindow<WorldChunkCreator>("World Chunk Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("World Chunk Creator", EditorStyles.boldLabel);

        chunkWidth = EditorGUILayout.IntField("Chunk Width", chunkWidth);
        chunkHeight = EditorGUILayout.IntField("Chunk Height", chunkHeight);
        worldRoot = (GameObject)EditorGUILayout.ObjectField("World Root", worldRoot, typeof(GameObject), true);

        if (GUILayout.Button("Create Chunks"))
        {
            CreateChunks();
        }
    }

    private void CreateChunks()
    {
        if (worldRoot == null)
        {
            Debug.LogError("World Root is not set.");
            return;
        }

        if (worldRoot.transform.childCount == 0)
        {
            Debug.LogError("World Root has no child objects.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Chunk Recreation", 
            "This will delete all existing chunk data and recreate them. Are you sure?", 
            "Yes", "Cancel"))
        {
            return;
        }

        CleanupOldChunks();

        Bounds worldBounds = CalculateWorldBounds(worldRoot);

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
    }

    private void CleanupOldChunks()
    {
        string directoryPath = "Assets/ChunkData";
        string[] guids = AssetDatabase.FindAssets("t:ChunkData", new[] { directoryPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }

        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var entriesToRemove = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
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

        Tilemap[] sourceTilemaps = worldRoot.GetComponentsInChildren<Tilemap>();
        if (sourceTilemaps == null || sourceTilemaps.Length == 0)
        {
            Debug.LogError("No Tilemaps found in worldRoot.");
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
                // Debug.Log($"Added Tilemap layer {layerData.layerName} with {layerData.tiles.Count} tiles to ChunkData {chunkCoord}");
            }
            else
            {
                // Debug.LogWarning($"No tiles found for Tilemap layer {layerData.layerName} in ChunkData {chunkCoord}");
            }
        }

        foreach (Transform child in worldRoot.GetComponentsInChildren<Transform>())
        {
            if (child == null || child.GetComponent<Tilemap>() != null) continue;

            // 检查子物体是否是预制体实例的根对象
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
            if (prefabRoot != child.gameObject)
            {
                Debug.LogWarning($"Skipping {child.name} because it is a child of a prefab instance. PrefabRoot: {prefabRoot}, Child: {child.gameObject}");
                continue;
            }

            // 确保 z 轴被忽略
            Vector3Int childPosInt = new Vector3Int(Mathf.FloorToInt(child.position.x), Mathf.FloorToInt(child.position.y), 0);

            // 使用已声明的 chunkBoundsInt
            if (chunkBoundsInt.Contains(childPosInt))
            {
                string prefabAssetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject));
                if (string.IsNullOrEmpty(prefabAssetPath))
                {
                    Debug.LogWarning($"Prefab asset path is null or empty for {child.name}. Skipping this object.");
                    continue;
                }

                PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(child.gameObject);
                if (prefabStatus != PrefabInstanceStatus.Connected)
                {
                    Debug.LogWarning($"GameObject {child.name} is not connected to a Prefab instance. Status: {prefabStatus}");
                    continue;
                }

                Debug.Log($"Storing GameObject {child.name} with Prefab path: {prefabAssetPath} and Prefab status: {prefabStatus}");

                chunkData.objects.Add(new ObjectData
                {
                    position = child.position - chunkWorldPosition + new Vector3(chunkWidth / 2, chunkHeight / 2, 0),
                    rotation = child.rotation,
                    scale = child.localScale,
                    prefabName = prefabAssetPath
                });

                var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (addressableSettings != null)
                {
                    var guid = AssetDatabase.AssetPathToGUID(prefabAssetPath);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogWarning($"GUID is null or empty for {prefabAssetPath}. Skipping this object.");
                        continue;
                    }

                    var entry = addressableSettings.CreateOrMoveEntry(guid, addressableSettings.DefaultGroup);
                    entry.address = prefabAssetPath;
                }
                else
                {
                    Debug.LogError("Addressable settings are not available.");
                }
            }
            else
            {
                Debug.LogWarning($"Skipping {child.name} because it is outside the chunk bounds. Position: {childPosInt}, Bounds: {chunkBoundsInt}");
            }
        }

        string assetPath = $"{directoryPath}/ChunkData_{chunkCoord.x}_{chunkCoord.y}.asset";
        AssetDatabase.CreateAsset(chunkData, assetPath);
        AssetDatabase.SaveAssets();

        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = $"ChunkData_{chunkCoord.x}_{chunkCoord.y}";
        }

        DestroyImmediate(chunkObject);
    }

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
}
