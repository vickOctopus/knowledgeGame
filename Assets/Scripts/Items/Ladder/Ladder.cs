using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Ladder : MonoBehaviour, IEditorInstantiatedObject
{
    public GameObject ladderTopColliderPrefab; // 顶部碰撞器预制体
    public GameObject ladderTopCollider;
    private Tilemap _ladderTilemap;// 梯子所在的 Tilemap


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
          // Debug.Log("Ladder Exited");
          PlayController.instance.LeftLadder();
        }
    }

    public List<EditorInstantiatedObjectInfo> InstantiateEditorObjects()
    {
        _ladderTilemap = GetComponent<Tilemap>();
        List<EditorInstantiatedObjectInfo> objectInfos = new List<EditorInstantiatedObjectInfo>();

        // 删除现有的子物体
        for (var i = ladderTopCollider.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(ladderTopCollider.transform.GetChild(i).gameObject);
        }

        // 遍历 Tilemap 并创建碰撞器
        for (var x = _ladderTilemap.cellBounds.xMin; x < _ladderTilemap.cellBounds.xMax; x++)
        {
            var ladderEndY = -1;

            // 遍历每一列，查找梯子的结束位置
            for (var y = _ladderTilemap.cellBounds.yMin; y < _ladderTilemap.cellBounds.yMax; y++)
            {
                var tilePos = new Vector3Int(x, y, 0);
                var tile = _ladderTilemap.GetTile(tilePos);

                if (tile != null)
                {
                    ladderEndY = y; // 记录梯子的顶部
                }
            }

            // 如果找到了梯子的顶部，生成顶部碰撞体
            if (ladderEndY != -1)
            {
                // 转换梯子顶部的 Tilemap 格子坐标为世界坐标
                var topPosition = _ladderTilemap.GetCellCenterWorld(new Vector3Int(x, ladderEndY, 0));
                
                // 在梯子的顶部生成碰撞体，调整 Y 坐标使其位于梯子顶部上方
                var colliderPosition = new Vector3(topPosition.x, topPosition.y + _ladderTilemap.cellSize.y / 2, topPosition.z);
                var colliderInstance = Instantiate(ladderTopColliderPrefab, colliderPosition, Quaternion.identity);
                colliderInstance.transform.SetParent(ladderTopCollider.transform);

                objectInfos.Add(new EditorInstantiatedObjectInfo
                {
                    position = colliderInstance.transform.position,
                    rotation = colliderInstance.transform.rotation,
                    scale = colliderInstance.transform.localScale,
                    prefabPath = AssetDatabase.GetAssetPath(ladderTopColliderPrefab)
                });
            }
        }

        return objectInfos;
    }
}
