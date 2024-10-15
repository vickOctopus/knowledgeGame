using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkManager : MonoBehaviour
{
    public const int chunkWidth = 50;
    public const int chunkHeight = 28;
    public int loadDistance = 1;

    public Tilemap[] managedTilemaps;
    public LayerMask objectLayers;

    private Dictionary<Vector2Int, HashSet<GameObject>> chunkObjects = new Dictionary<Vector2Int, HashSet<GameObject>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;

    private void Start()
    {
        InitializeChunks();
        UpdateVisibleChunks();
    }

    private void InitializeChunks()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & objectLayers) != 0)
            {
                Vector2Int chunkCoord = GetChunkCoordFromWorldPos(obj.transform.position);
                AddObjectToChunk(chunkCoord, obj);
                obj.SetActive(false);
            }
        }

        foreach (Tilemap tilemap in managedTilemaps)
        {
            HideAllTiles(tilemap);
        }
    }

    private void UpdateVisibleChunks()
    {
        HashSet<Vector2Int> newVisibleChunks = new HashSet<Vector2Int>();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int y = -loadDistance; y <= loadDistance; y++)
            {
                Vector2Int coord = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                newVisibleChunks.Add(coord);
            }
        }

        foreach (Vector2Int chunk in visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                SetChunkVisibility(chunk, false);
            }
        }

        foreach (Vector2Int chunk in newVisibleChunks)
        {
            if (!visibleChunks.Contains(chunk))
            {
                SetChunkVisibility(chunk, true);
            }
        }

        visibleChunks = newVisibleChunks;
    }

    private void SetChunkVisibility(Vector2Int chunkCoord, bool visible)
    {
        foreach (Tilemap tilemap in managedTilemaps)
        {
            SetTilemapChunkVisibility(tilemap, chunkCoord, visible);
        }

        if (chunkObjects.TryGetValue(chunkCoord, out HashSet<GameObject> objects))
        {
            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    if (visible)
                    {
                        EnableObject(obj);
                    }
                    else
                    {
                        DisableObject(obj);
                    }
                }
            }
        }
    }

    private void SetTilemapChunkVisibility(Tilemap tilemap, Vector2Int chunkCoord, bool visible)
    {
        Vector3Int chunkOrigin = new Vector3Int(
            chunkCoord.x * chunkWidth - chunkWidth / 2,
            chunkCoord.y * chunkHeight - chunkHeight / 2,
            0
        );
        BoundsInt bounds = new BoundsInt(chunkOrigin, new Vector3Int(chunkWidth, chunkHeight, 1));

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, visible ? Color.white : Color.clear);
        }
    }

    private Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x + chunkWidth / 2) / chunkWidth),
            Mathf.FloorToInt((worldPos.y + chunkHeight / 2) / chunkHeight)
        );
    }

    private void HideAllTiles(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, Color.clear);
        }
    }

    private void AddObjectToChunk(Vector2Int chunkCoord, GameObject obj)
    {
        if (!chunkObjects.ContainsKey(chunkCoord))
        {
            chunkObjects[chunkCoord] = new HashSet<GameObject>();
        }
        chunkObjects[chunkCoord].Add(obj);
    }

    public void ForceUpdateChunks(Vector3 cameraPosition)
    {
        Vector2Int newChunk = GetChunkCoordFromWorldPos(cameraPosition);
        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            UpdateVisibleChunks();
        }
    }

    private void EnableObject(GameObject obj)
    {
        obj.SetActive(true);
        foreach (Behaviour component in obj.GetComponents<Behaviour>())
        {
            component.enabled = true;
        }
    }

    private void DisableObject(GameObject obj)
    {
        foreach (Behaviour component in obj.GetComponents<Behaviour>())
        {
            component.enabled = false;
        }
        obj.SetActive(false);
    }
}
