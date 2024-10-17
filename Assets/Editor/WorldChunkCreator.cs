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
    private string outputFolder = "Assets/Prefabs/Chunks";

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
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Create Chunks"))
        {
            CreateChunks();
        }
    }

    private void CreateChunks()
    {
        if (worldRoot == null)
        {
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Chunk Recreation", 
            "This will delete all existing chunk prefabs and recreate them. Are you sure?", 
            "Yes", "Cancel"))
        {
            return;
        }

        CleanupOldChunks();

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string[] folderPath = outputFolder.Split('/');
            string currentPath = folderPath[0];
            for (int i = 1; i < folderPath.Length; i++)
            {
                string newPath = currentPath + "/" + folderPath[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folderPath[i]);
                }
                currentPath = newPath;
            }
        }

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
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { outputFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Chunk_"))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        // 清理 Addressables 中的旧条目
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
        GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        if (chunkObject == null)
        {
            return;
        }

        chunkObject.AddComponent<Grid>();

        Vector3 chunkWorldPosition = new Vector3(
            (chunkCoord.x * chunkWidth) - (chunkWidth / 2f),
            (chunkCoord.y * chunkHeight) - (chunkHeight / 2f),
            0
        );
        chunkObject.transform.position = Vector3.zero;

        Bounds chunkBounds = new Bounds(
            chunkWorldPosition,
            new Vector3(chunkWidth, chunkHeight, 1)
        );

        BoundsInt chunkBoundsInt = new BoundsInt(
            Vector3Int.FloorToInt(chunkWorldPosition),
            new Vector3Int(chunkWidth, chunkHeight, 1)
        );

        Tilemap[] sourceTilemaps = worldRoot.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap sourceTilemap in sourceTilemaps)
        {
            if (sourceTilemap == null)
            {
                continue;
            }

            GameObject tilemapObject = new GameObject(sourceTilemap.name);
            tilemapObject.transform.SetParent(chunkObject.transform);
            Tilemap targetTilemap = tilemapObject.AddComponent<Tilemap>();

            CopyTilemap(sourceTilemap, targetTilemap, chunkBoundsInt, chunkWorldPosition);

            if (targetTilemap.GetUsedTilesCount() == 0)
            {
                DestroyImmediate(tilemapObject);
            }
            else
            {
                CopyTilemapComponents(sourceTilemap, targetTilemap);
            }
        }

        CopyObjects(worldRoot, chunkObject, chunkBounds, chunkWorldPosition);

        string prefabPath = $"{outputFolder}/Chunk_{chunkCoord.x}_{chunkCoord.y}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(chunkObject, prefabPath, InteractionMode.UserAction);

        SetAsAddressable(prefab, $"Chunk_{chunkCoord.x}_{chunkCoord.y}");

        DestroyImmediate(chunkObject);
    }

    private void SetAsAddressable(GameObject prefab, string address)
    {
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            return;
        }

        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
        var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        entry.address = address;

        AssetDatabase.SaveAssets();
    }

    private void CopyTilemap(Tilemap sourceTilemap, Tilemap targetTilemap, BoundsInt chunkBounds, Vector3 chunkWorldPosition)
    {
        if (sourceTilemap == null || targetTilemap == null)
        {
            return;
        }

        BoundsInt overlap = new BoundsInt(
            Vector3Int.Max(sourceTilemap.cellBounds.min, chunkBounds.min),
            Vector3Int.Min(sourceTilemap.cellBounds.max, chunkBounds.max) - Vector3Int.Max(sourceTilemap.cellBounds.min, chunkBounds.min)
        );

        if (overlap.size.x > 0 && overlap.size.y > 0)
        {
            for (int x = 0; x < overlap.size.x; x++)
            {
                for (int y = 0; y < overlap.size.y; y++)
                {
                    Vector3Int sourcePos = new Vector3Int(x + overlap.x, y + overlap.y, 0);
                    Vector3Int targetPos = sourcePos - Vector3Int.FloorToInt(chunkWorldPosition) - new Vector3Int(chunkWidth / 2, chunkHeight / 2, 0);
                    TileBase tile = sourceTilemap.GetTile(sourcePos);
                    if (tile != null)
                    {
                        targetTilemap.SetTile(targetPos, tile);
                    }
                }
            }
        }

        targetTilemap.RefreshAllTiles();
    }

    private void CopyObjects(GameObject source, GameObject target, Bounds chunkBounds, Vector3 chunkWorldPosition)
    {
        if (source == null || target == null)
        {
            return;
        }

        Transform[] childTransforms = source.GetComponentsInChildren<Transform>();
        if (childTransforms == null)
        {
            return;
        }

        foreach (Transform child in childTransforms)
        {
            if (child == null) continue;
            if (child.gameObject == source) continue;
            if (child.GetComponent<Tilemap>() != null) continue;

            if (chunkBounds.Contains(child.position))
            {
                try
                {
                    GameObject copy = new GameObject(child.name);
                    copy.transform.SetParent(target.transform);
                    // 调整复制对象的位置，考虑chunk中心偏移
                    copy.transform.localPosition = child.position - chunkWorldPosition - new Vector3(chunkWidth / 2f, chunkHeight / 2f, 0);
                    copy.transform.rotation = child.rotation;
                    copy.transform.localScale = child.localScale;

                    Component[] sourceComponents = child.GetComponents<Component>();
                    if (sourceComponents != null)
                    {
                        foreach (Component sourceComponent in sourceComponents)
                        {
                            if (sourceComponent == null || sourceComponent is Transform) continue;

                            System.Type componentType = sourceComponent.GetType();
                            Component targetComponent = copy.GetComponent(componentType);

                            if (targetComponent == null)
                            {
                                targetComponent = copy.AddComponent(componentType);
                            }

                            if (targetComponent != null)
                            {
                                EditorUtility.CopySerialized(sourceComponent, targetComponent);
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    return;
                }
            }
        }
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

        // 如果没有 Renderer 或 Collider，使用有 Transform 置
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

        // 确边界至少包含世界根对象的位置
        bounds.Encapsulate(worldRoot.transform.position);

        return bounds;
    }

    private void CopyTilemapComponents(Tilemap source, Tilemap target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.tileAnchor = source.tileAnchor;
        target.orientation = source.orientation;
        
        Grid sourceGrid = source.layoutGrid;
        Grid targetGrid = target.layoutGrid;
        if (targetGrid == null)
        {
            targetGrid = target.gameObject.AddComponent<Grid>();
        }
        targetGrid.cellSize = sourceGrid.cellSize;
        targetGrid.cellGap = sourceGrid.cellGap;
        targetGrid.cellLayout = sourceGrid.cellLayout;
        targetGrid.cellSwizzle = sourceGrid.cellSwizzle;

        // 复制 TilemapRenderer
        TilemapRenderer sourceRenderer = source.GetComponent<TilemapRenderer>();
        if (sourceRenderer != null)
        {
            TilemapRenderer targetRenderer = target.gameObject.GetComponent<TilemapRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = target.gameObject.AddComponent<TilemapRenderer>();
            }
            
            // 复制其他 TilemapRenderer 属性
            targetRenderer.mode = sourceRenderer.mode;
            targetRenderer.detectChunkCullingBounds = sourceRenderer.detectChunkCullingBounds;
            targetRenderer.maskInteraction = sourceRenderer.maskInteraction;
            targetRenderer.sortOrder = sourceRenderer.sortOrder;
        }

        // 复制 TilemapCollider2D
        TilemapCollider2D sourceCollider = source.GetComponent<TilemapCollider2D>();
        if (sourceCollider != null)
        {
            TilemapCollider2D targetCollider = target.gameObject.GetComponent<TilemapCollider2D>();
            if (targetCollider == null)
            {
                targetCollider = target.gameObject.AddComponent<TilemapCollider2D>();
            }
            targetCollider.usedByComposite = sourceCollider.usedByComposite;
            targetCollider.useDelaunayMesh = sourceCollider.useDelaunayMesh;
            targetCollider.maximumTileChangeCount = sourceCollider.maximumTileChangeCount;
            targetCollider.extrusionFactor = sourceCollider.extrusionFactor;
        }

        // 复制其他自定义组件
        Component[] sourceComponents = source.GetComponents<Component>();
        foreach (Component comp in sourceComponents)
        {
            if (comp is Tilemap || comp is Transform || comp is TilemapRenderer || comp is TilemapCollider2D)
                continue;

            System.Type componentType = comp.GetType();
            if (!target.gameObject.GetComponent(componentType))
            {
                Component newComp = target.gameObject.AddComponent(componentType);
                EditorUtility.CopySerialized(comp, newComp);
            }
        }
    }

    private GameObject CreateChunkPrefab(GameObject chunkObject, Vector2Int chunkPosition)
    {
        // 创建 prefab
        string prefabPath = $"{outputFolder}/Chunk_{chunkPosition.x}_{chunkPosition.y}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunkObject, prefabPath);
        
        // ... 其他代码保持不变 ...

        return prefab;
    }
}
