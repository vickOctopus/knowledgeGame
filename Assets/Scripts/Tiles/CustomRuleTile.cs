using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Custom Rule Tile", menuName = "Tiles/Custom Rule Tile")]
public class CustomRuleTile : RuleTile<CustomRuleTile.Neighbor>
{
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int ThisTile = 1;
        public const int NotThisTile = 2;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int neighborPosition = new Vector3Int(position.x + x, position.y + y, position.z);
                if (HasRule(neighborPosition, tilemap))
                {
                    tilemap.RefreshTile(neighborPosition);
                }
            }
        }
    }

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        switch (neighbor)
        {
            case Neighbor.ThisTile: return other is CustomRuleTile;
            case Neighbor.NotThisTile: return !(other is CustomRuleTile);
        }
        return base.RuleMatch(neighbor, other);
    }

    private bool HasRule(Vector3Int position, ITilemap tilemap)
    {
        TileBase tile = tilemap.GetTile(position);
        return tile == this || tile is CustomRuleTile;
    }

    public void UpdateTile(Tilemap currentTilemap, Tilemap adjacentTilemap, Vector3Int position)
    {
        RefreshTile(position, currentTilemap);
    }

    public void UpdateTileAcrossChunks(Tilemap currentTilemap, Tilemap[] adjacentTilemaps, Vector3Int position)
    {
        Debug.Log($"UpdateTileAcrossChunks called for position: {position} in tilemap: {currentTilemap.name}");
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int neighborPosition = new Vector3Int(position.x + x, position.y + y, position.z);
                TileBase neighborTile = GetTileAcrossChunks(currentTilemap, adjacentTilemaps, neighborPosition);
                
                Debug.Log($"Neighbor tile at {neighborPosition}: {(neighborTile != null ? neighborTile.name : "null")}");
                
                UpdateRuleMatches(currentTilemap, neighborPosition, neighborTile);
            }
        }
        currentTilemap.RefreshTile(position);
        Debug.Log($"Refreshed tile at position: {position} in tilemap: {currentTilemap.name}");
    }

    private TileBase GetTileAcrossChunks(Tilemap currentTilemap, Tilemap[] adjacentTilemaps, Vector3Int position)
    {
        if (currentTilemap.cellBounds.Contains(position))
        {
            return currentTilemap.GetTile(position);
        }
        
        foreach (Tilemap adjacentTilemap in adjacentTilemaps)
        {
            if (adjacentTilemap != null && adjacentTilemap.cellBounds.Contains(position))
            {
                return adjacentTilemap.GetTile(position);
            }
        }

        return null;
    }

    private void UpdateRuleMatches(Tilemap tilemap, Vector3Int position, TileBase neighborTile)
    {
        Debug.Log($"UpdateRuleMatches called for position: {position}, neighbor tile: {(neighborTile != null ? neighborTile.name : "null")}");
        // 在这里实现您的规则匹配逻辑
        // 例如：
        // if (neighborTile is CustomRuleTile)
        // {
        //     // 执行特定的规则匹配逻辑
        // }
        // tilemap.RefreshTile(position);
    }
}
