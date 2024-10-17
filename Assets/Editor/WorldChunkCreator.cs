using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEditor.AddressableAssets;
using static FileLogger;
using System.Linq;

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
            EditorLog("World Root is not set!");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Chunk Recreation", 
            "This will delete all existing chunk prefabs and recreate them. Are you sure?", 
            "Yes", "Cancel"))
        {
            return;
        }

        EditorLog("Starting chunk creation process");

        CleanupOldChunks();

        // 确保输出文件夹存在
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
        EditorLog($"World bounds: {worldBounds}");

        int startX = Mathf.FloorToInt(worldBounds.min.x / chunkWidth);
        int startY = Mathf.FloorToInt(worldBounds.min.y / chunkHeight);
        int endX = Mathf.CeilToInt(worldBounds.max.x / chunkWidth);
        int endY = Mathf.CeilToInt(worldBounds.max.y / chunkHeight);

        EditorLog($"Chunk range: X({startX} to {endX}), Y({startY} to {endY})");

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                EditorLog($"Creating chunk at ({x}, {y})");
                CreateChunk(new Vector2Int(x, y));
            }
        }

        AssetDatabase.Refresh();

        EditorLog("Chunk creation process completed");
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
        EditorLog($"Starting creation of chunk: {chunkCoord}");

        GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        if (chunkObject == null)
        {
            EditorLog("Failed to create chunk object!");
            return;
        }

        chunkObject.AddComponent<Grid>(); // 添加Grid组件
        Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkWidth, chunkCoord.y * chunkHeight, 0);
        chunkObject.transform.position = chunkPosition;

        Bounds chunkBounds = new Bounds(
            new Vector3(chunkCoord.x * chunkWidth, chunkCoord.y * chunkHeight, 0),
            new Vector3(chunkWidth, chunkHeight, 1)
        );

        BoundsInt chunkBoundsInt = new BoundsInt(
            new Vector3Int(Mathf.FloorToInt(chunkBounds.min.x), Mathf.FloorToInt(chunkBounds.min.y), Mathf.FloorToInt(chunkBounds.min.z)),
            new Vector3Int(Mathf.CeilToInt(chunkBounds.size.x), Mathf.CeilToInt(chunkBounds.size.y), Mathf.CeilToInt(chunkBounds.size.z))
        );

        EditorLog($"Chunk bounds: {chunkBounds}");
        EditorLog($"Chunk bounds (int): {chunkBoundsInt}");

        Tilemap[] sourceTilemaps = worldRoot.GetComponentsInChildren<Tilemap>();
        EditorLog($"Found {sourceTilemaps.Length} source tilemaps");

        foreach (Tilemap sourceTilemap in sourceTilemaps)
        {
            if (sourceTilemap == null)
            {
                EditorLog("Source tilemap is null!");
                continue;
            }

            EditorLog($"Processing tilemap: {sourceTilemap.name}");

            // 为每个源Tilemap创建一个新的GameObject和Tilemap
            GameObject tilemapObject = new GameObject(sourceTilemap.name);
            tilemapObject.transform.SetParent(chunkObject.transform);
            Tilemap targetTilemap = tilemapObject.AddComponent<Tilemap>();

            CopyTilemap(sourceTilemap, targetTilemap, chunkBoundsInt);

            // 如果目标tilemap为空，则移除它
            if (targetTilemap.GetUsedTilesCount() == 0)
            {
                EditorLog($"Removing empty tilemap: {targetTilemap.name}");
                DestroyImmediate(tilemapObject);
            }
            else
            {
                // 复制其他必要的组件和设置
                CopyTilemapComponents(sourceTilemap, targetTilemap);
            }
        }

        // 复制其他对象
        CopyObjects(worldRoot, chunkObject, chunkBounds);

        // 在创建预制体之前添加这些日志
        EditorLog($"Chunk {chunkCoord} contents before prefab creation:");
        LogChunkContents(chunkObject);

        // 创建预制体
        string prefabPath = $"{outputFolder}/Chunk_{chunkCoord.x}_{chunkCoord.y}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(chunkObject, prefabPath, InteractionMode.UserAction);

        // 在建预制体之后再次检查内容
        EditorLog($"Chunk {chunkCoord} contents after prefab creation:");
        LogChunkContents(prefab);

        // 设置为Addressable资产并置地址
        SetAsAddressable(prefab, $"Chunk_{chunkCoord.x}_{chunkCoord.y}");

        // 清理场景中的临时对象
        DestroyImmediate(chunkObject);

        EditorLog($"Finished creating chunk: {chunkCoord}");
    }

    private void SetAsAddressable(GameObject prefab, string address)
    {
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorLog("Addressable Asset Settings not found. Please create it from the Addressables window.");
            return;
        }

        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
        var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        entry.address = address;

        AssetDatabase.SaveAssets();
    }

    private void CopyTilemaps(GameObject source, GameObject target, BoundsInt chunkBounds)
    {
        Tilemap[] sourceTilemaps = source.GetComponentsInChildren<Tilemap>();
        EditorLog($"Copying {sourceTilemaps.Length} tilemaps");

        foreach (Tilemap sourceTilemap in sourceTilemaps)
        {
            // 在目标chunk中创建或获取对应的tilemap
            Transform targetTransform = target.transform.Find(sourceTilemap.name);
            if (targetTransform == null)
            {
                GameObject newTilemapObject = new GameObject(sourceTilemap.name);
                newTilemapObject.transform.SetParent(target.transform);
                targetTransform = newTilemapObject.transform;
            }

            Tilemap targetTilemap = targetTransform.GetComponent<Tilemap>();
            if (targetTilemap == null)
            {
                targetTilemap = targetTransform.gameObject.AddComponent<Tilemap>();
            }

            // 复制Tilemap组件的属性
            EditorUtility.CopySerialized(sourceTilemap, targetTilemap);

            // 获取源Tilemap的世界边界
            BoundsInt sourceBounds = sourceTilemap.cellBounds;

            // 计算chunk的tilemap边界（世界坐标）
            Vector3Int chunkMin = sourceTilemap.WorldToCell(chunkBounds.min);
            Vector3Int chunkMax = sourceTilemap.WorldToCell(chunkBounds.max);
            BoundsInt chunkTilemapBounds = new BoundsInt(chunkMin, chunkMax - chunkMin);

            // 计算重叠区域
            BoundsInt copyBounds = new BoundsInt(
                Mathf.Max(chunkTilemapBounds.xMin, sourceBounds.xMin),
                Mathf.Max(chunkTilemapBounds.yMin, sourceBounds.yMin),
                0,
                Mathf.Min(chunkTilemapBounds.xMax, sourceBounds.xMax) - Mathf.Max(chunkTilemapBounds.xMin, sourceBounds.xMin),
                Mathf.Min(chunkTilemapBounds.yMax, sourceBounds.yMax) - Mathf.Max(chunkTilemapBounds.yMin, sourceBounds.yMin),
                1
            );

            EditorLog($"Source tilemap {sourceTilemap.name} bounds: {sourceBounds}");
            EditorLog($"Chunk tilemap bounds: {chunkTilemapBounds}");
            EditorLog($"Copy bounds: {copyBounds}");

            if (copyBounds.size.x > 0 && copyBounds.size.y > 0)
            {
                // 复制瓦片
                TileBase[] tileArray = sourceTilemap.GetTilesBlock(copyBounds);
                targetTilemap.SetTilesBlock(copyBounds, tileArray);

                int nonNullTiles = tileArray.Count(t => t != null);
                EditorLog($"Copied {nonNullTiles} non-null tiles for {sourceTilemap.name}");

                // 调整目标Tilemap的边界以匹配复制的区域
                targetTilemap.ResizeBounds();
            }
            else
            {
                EditorLog($"No overlap between source and chunk for {sourceTilemap.name}");
            }

            // 复制其他组件
            Component[] sourceComponents = sourceTilemap.GetComponents<Component>();
            foreach (Component sourceComponent in sourceComponents)
            {
                if (sourceComponent is Tilemap || sourceComponent is Transform)
                    continue;

                Component targetComponent = targetTilemap.GetComponent(sourceComponent.GetType());
                if (targetComponent == null)
                {
                    targetComponent = targetTilemap.gameObject.AddComponent(sourceComponent.GetType());
                }
                EditorUtility.CopySerialized(sourceComponent, targetComponent);
            }
        }
    }

    private void CopyObjects(GameObject source, GameObject target, Bounds chunkBounds)
    {
        if (source == null || target == null)
        {
            EditorLog("Error: Source or target is null in CopyObjects method.");
            return;
        }

        Transform[] childTransforms = source.GetComponentsInChildren<Transform>();
        if (childTransforms == null)
        {
            EditorLog("Error: No child transforms found in source object.");
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
                    copy.transform.position = child.position;
                    copy.transform.rotation = child.rotation;
                    copy.transform.localScale = child.localScale;

                    // 复制所有组件
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
                                EditorLog($"Failed to add or find component of type {componentType} on {copy.name}");
                            }
                        }
                    }

                    EditorLog($"Copied object: {child.name} with {sourceComponents?.Length ?? 0} components");
                }
                catch (System.Exception e)
                {
                    EditorLog($"Error copying object {child.name}: {e.Message}");
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

        EditorLog($"Calculated world bounds: {bounds}");
        return bounds;
    }

    private void LogChunkContents(GameObject chunk)
    {
        foreach (Transform child in chunk.transform)
        {
            EditorLog($"- {child.name} ({child.GetType()})");
            if (child.GetComponent<Tilemap>() != null)
            {
                Tilemap tilemap = child.GetComponent<Tilemap>();
                BoundsInt bounds = tilemap.cellBounds;
                TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
                int nonNullTiles = System.Array.FindAll(allTiles, t => t != null).Length;
                EditorLog($"  Tilemap bounds: {bounds}, Non-null tiles: {nonNullTiles}");
            }
            Component[] components = child.GetComponents<Component>();
            EditorLog($"  Components: {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");
        }
    }

    private void CopyTilemap(Tilemap sourceTilemap, Tilemap targetTilemap, BoundsInt chunkBounds)
    {
        if (sourceTilemap == null || targetTilemap == null)
        {
            EditorLog("Source or target tilemap is null in CopyTilemap!");
            return;
        }

        // 计算源tilemap和chunk的重叠区域
        BoundsInt overlap = new BoundsInt(
            Vector3Int.Max(sourceTilemap.cellBounds.min, chunkBounds.min),
            Vector3Int.Min(sourceTilemap.cellBounds.max, chunkBounds.max) - Vector3Int.Max(sourceTilemap.cellBounds.min, chunkBounds.min)
        );

        // 只有在有重叠时才复制
        if (overlap.size.x > 0 && overlap.size.y > 0)
        {
            int copiedTiles = 0;
            for (int x = 0; x < overlap.size.x; x++)
            {
                for (int y = 0; y < overlap.size.y; y++)
                {
                    Vector3Int sourcePos = new Vector3Int(x + overlap.x, y + overlap.y, 0);
                    Vector3Int targetPos = new Vector3Int(x + overlap.x - chunkBounds.x, y + overlap.y - chunkBounds.y, 0);
                    TileBase tile = sourceTilemap.GetTile(sourcePos);
                    if (tile != null)
                    {
                        targetTilemap.SetTile(targetPos, tile);
                        copiedTiles++;
                    }
                }
            }

            EditorLog($"Copied {copiedTiles} tiles from {sourceTilemap.name} to chunk");
        }
    }

    private void CopyTilemapComponents(Tilemap source, Tilemap target)
    {
        if (source == null || target == null)
        {
            EditorLog("Source or target tilemap is null in CopyTilemapComponents!");
            return;
        }

        EditorLog($"Copying components from {source.name} to {target.name}");

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

        EditorLog("Finished copying tilemap components");
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