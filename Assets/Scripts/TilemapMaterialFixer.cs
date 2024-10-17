using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapMaterialFixer : MonoBehaviour
{
    public Material defaultTilemapMaterial;

    void Start()
    {
        FixTilemapMaterials();
    }

    public void FixTilemapMaterials()
    {
        if (defaultTilemapMaterial == null)
        {
            Debug.LogError("Default Tilemap Material is not set in TilemapMaterialFixer!");
            return;
        }

        TilemapRenderer[] renderers = GetComponentsInChildren<TilemapRenderer>(true);
        foreach (TilemapRenderer renderer in renderers)
        {
            if (renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline"))
            {
                renderer.sharedMaterial = defaultTilemapMaterial;
                Debug.Log($"Fixed material for {renderer.gameObject.name} with URP material");
            }
        }
    }
}
