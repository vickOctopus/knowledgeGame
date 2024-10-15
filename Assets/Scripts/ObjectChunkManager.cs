using System.Collections.Generic;
using UnityEngine;

public class ObjectChunkManager : MonoBehaviour
{
    public const int chunkWidth = 50;
    public const int chunkHeight = 28;
    public int loadDistance = 1;
    public LayerMask managedLayers;

    private Dictionary<Vector2Int, HashSet<GameObject>> chunkObjects = new Dictionary<Vector2Int, HashSet<GameObject>>();
    private HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunk;

    private void Start()
    {
        InitializeChunks();
        UpdateVisibleChunks();
    }

    private void LateUpdate()
    {
        Vector2Int newChunk = GetChunkCoordFromWorldPos(Camera.main.transform.position);
        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            UpdateVisibleChunks();
        }
    }

    private void InitializeChunks()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & managedLayers) != 0)
            {
                Vector2Int chunkCoord = GetChunkCoordFromWorldPos(obj.transform.position);
                AddObjectToChunk(chunkCoord, obj);
            }
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
        if (chunkObjects.TryGetValue(chunkCoord, out HashSet<GameObject> objects))
        {
            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    obj.SetActive(visible);
                }
            }
        }
    }

    private Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x + chunkWidth / 2) / chunkWidth),
            Mathf.FloorToInt((worldPos.y + chunkHeight / 2) / chunkHeight)
        );
    }

    private void AddObjectToChunk(Vector2Int chunkCoord, GameObject obj)
    {
        if (!chunkObjects.ContainsKey(chunkCoord))
        {
            chunkObjects[chunkCoord] = new HashSet<GameObject>();
        }
        chunkObjects[chunkCoord].Add(obj);
    }

    public void ForceUpdateChunks()
    {
        currentChunk = GetChunkCoordFromWorldPos(Camera.main.transform.position);
        UpdateVisibleChunks();
    }
}
