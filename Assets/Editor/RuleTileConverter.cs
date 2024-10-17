using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class RuleTileConverter : EditorWindow
{
    [MenuItem("Tools/Convert RuleTiles to CustomRuleTiles")]
    public static void ShowWindow()
    {
        GetWindow<RuleTileConverter>("RuleTile Converter");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Convert All RuleTiles"))
        {
            ConvertAllRuleTiles();
        }

        // 添加新按钮
        if (GUILayout.Button("Revert All CustomRuleTiles to RuleTiles"))
        {
            RevertAllCustomRuleTiles();
        }
    }

    private void ConvertAllRuleTiles()
    {
        string[] guids = AssetDatabase.FindAssets("t:RuleTile");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RuleTile originalTile = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            if (originalTile != null)
            {
                ConvertRuleTile(originalTile, path);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Finished converting all RuleTiles to CustomRuleTiles");
    }

    private void ConvertRuleTile(RuleTile originalTile, string originalPath)
    {
        // 创建新的 CustomRuleTile
        CustomRuleTile newTile = ScriptableObject.CreateInstance<CustomRuleTile>();

        // 复制基本属性
        newTile.m_DefaultSprite = originalTile.m_DefaultSprite;
        newTile.m_DefaultGameObject = originalTile.m_DefaultGameObject;
        newTile.m_DefaultColliderType = originalTile.m_DefaultColliderType;
        newTile.m_TilingRules = new List<RuleTile.TilingRule>(originalTile.m_TilingRules);

        // 保存新的 CustomRuleTile
        string newPath = originalPath.Replace(".asset", "_Custom.asset");
        AssetDatabase.CreateAsset(newTile, newPath);

        // 更新所有使用原 RuleTile 的 Tilemap
        UpdateTilemaps(originalTile, newTile);

        Debug.Log($"Converted {originalPath} to CustomRuleTile at {newPath}");
    }

    private void UpdateTilemaps(TileBase originalTile, TileBase newTile)
    {
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap tilemap in allTilemaps)
        {
            BoundsInt bounds = tilemap.cellBounds;
            TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

            for (int x = 0; x < bounds.size.x; x++)
            {
                for (int y = 0; y < bounds.size.y; y++)
                {
                    for (int z = 0; z < bounds.size.z; z++)
                    {
                        int index = x + y * bounds.size.x + z * bounds.size.x * bounds.size.y;
                        if (allTiles[index] == originalTile)
                        {
                            Vector3Int pos = new Vector3Int(x + bounds.xMin, y + bounds.yMin, z + bounds.zMin);
                            tilemap.SetTile(pos, newTile);
                        }
                    }
                }
            }
        }
    }

    private void RevertAllCustomRuleTiles()
    {
        string[] guids = AssetDatabase.FindAssets("t:CustomRuleTile");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CustomRuleTile customTile = AssetDatabase.LoadAssetAtPath<CustomRuleTile>(path);
            if (customTile != null)
            {
                RevertCustomRuleTile(customTile, path);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("完成将所有 CustomRuleTiles 转换回 RuleTiles");
    }

    private void RevertCustomRuleTile(CustomRuleTile customTile, string customPath)
    {
        // 创建新的 RuleTile
        RuleTile newTile = ScriptableObject.CreateInstance<RuleTile>();

        // 复制基本属性
        newTile.m_DefaultSprite = customTile.m_DefaultSprite;
        newTile.m_DefaultGameObject = customTile.m_DefaultGameObject;
        newTile.m_DefaultColliderType = customTile.m_DefaultColliderType;
        newTile.m_TilingRules = new List<RuleTile.TilingRule>(customTile.m_TilingRules);

        // 保存新的 RuleTile
        string newPath = customPath.Replace("_Custom.asset", ".asset");
        AssetDatabase.CreateAsset(newTile, newPath);

        // 更新所有使用原 CustomRuleTile 的 Tilemap
        UpdateTilemaps(customTile, newTile);

        // 删除原 CustomRuleTile 资源
        AssetDatabase.DeleteAsset(customPath);

        Debug.Log($"已将 {customPath} 转换回 RuleTile，保存在 {newPath}");
    }
}
