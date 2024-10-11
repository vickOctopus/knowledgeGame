using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Ladder : MonoBehaviour
{
    public GameObject ladderTopColliderPrefab; // 顶部碰撞器预制体
    private Tilemap _ladderTilemap;// 梯子所在的 Tilemap
    
    //[ContextMenu("Generate Ladder top collider")]
    public void AddLadderColliders()
    {
        _ladderTilemap = GetComponent<Tilemap>();
        
        // 遍历删除每个子物体
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 遍历整个 Tilemap 的边界
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

            // 如果找到了一个梯子的起始和结束位置，生成顶部和底部的碰撞体
            if (ladderEndY != -1)
            {
                // 转换顶部和底部的 Tilemap 格子坐标为世界坐标
                var topPosition = _ladderTilemap.CellToWorld(new Vector3Int(x, ladderEndY + 1, 0)); // 梯子顶部

                // 在梯子的顶部和底部生成碰撞体
                var tem = Instantiate(ladderTopColliderPrefab, new Vector2(topPosition.x + 0.5f, topPosition.y),
                    Quaternion.identity);
                tem.transform.SetParent(_ladderTilemap.transform);
            }
        }
    }
}
