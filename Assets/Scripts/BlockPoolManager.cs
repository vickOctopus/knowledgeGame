using System;
using UnityEngine;
using System.Collections.Generic;

public class BlockPoolManager : MonoBehaviour
{
    // public static BlockPoolManager instance;
    public GameObject blockPrefab;
    public int rowCount = 10;
    public int columnCount = 10;
    public Vector2 blockSize = new Vector2(1f, 1f);
    public bool autoCalculateSize = true;

    private int initialPoolSize;
    private Queue<GameObject> blockPool;
    private Vector2 startPosition;
    private Vector2 blockSpacing;
    private List<GameObject> activeBlocks;
    private bool isInitialized = false;

    private void Awake()
    {
        ValidateComponents();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void ValidateComponents()
    {
        if (blockPrefab == null)
        {
            Debug.LogError($"BlockPrefab is not assigned in BlockPoolManager on {gameObject.name}!");
            enabled = false;
            return;
        }

        if (blockPrefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogWarning($"Block prefab is missing SpriteRenderer component on {gameObject.name}!");
        }
    }

    private void Initialize()
    {
        if (!enabled) return;

        try
        {
            initialPoolSize = rowCount * columnCount;
            activeBlocks = new List<GameObject>(initialPoolSize);
            CalculateBlockSizeAndSpacing();
            InitializePool();
            CalculateStartPosition();
            ArrangeBlocks();
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing BlockPoolManager on {gameObject.name}: {e.Message}\n{e.StackTrace}");
            enabled = false;
        }
    }

    void CalculateBlockSizeAndSpacing()
    {
        if (autoCalculateSize)
        {
            SpriteRenderer spriteRenderer = blockPrefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                blockSize = spriteRenderer.sprite.bounds.size;
            }
            else
            {
                Debug.LogWarning($"Block prefab does not have a SpriteRenderer or Sprite on {gameObject.name}. Using default size.");
            }
        }
        blockSpacing = blockSize;
    }

    void InitializePool()
    {
        blockPool = new Queue<GameObject>();
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBlock();
        }
    }

    void CalculateStartPosition()
    {
        startPosition = new Vector2(blockSize.x/2, -blockSize.y/2);
    }

    void ArrangeBlocks()
    {
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                Vector2 position = startPosition + new Vector2(col * blockSpacing.x, -row * blockSpacing.y);
                SpawnBlock(position);
            }
        }
    }

    void SpawnBlock(Vector2 position)
    {
        GameObject block = GetBlockFromPool();
        block.transform.localPosition = position;
        block.SetActive(true);
        activeBlocks.Add(block);
    }

    GameObject GetBlockFromPool()
    {
        if (blockPool.Count == 0)
        {
            CreateNewBlock();
        }
        return blockPool.Dequeue();
    }

    void CreateNewBlock()
    {
        GameObject newBlock = Instantiate(blockPrefab, transform);
        newBlock.SetActive(false);
        DisappearingBlock disappearingBlock = newBlock.GetComponent<DisappearingBlock>();
        if (disappearingBlock == null)
        {
            disappearingBlock = newBlock.AddComponent<DisappearingBlock>();
        }
        disappearingBlock.Initialize(this);
        blockPool.Enqueue(newBlock);
    }

    public void ReturnBlockToPool(GameObject block)
    {
        if (block != null)
        {
            block.SetActive(false);
            blockPool.Enqueue(block);
            activeBlocks.Remove(block);
        }
    }

    public void RestoreAllBlocks()
    {
        if (!enabled || !isInitialized) return;

        foreach (GameObject block in new List<GameObject>(activeBlocks))
        {
            ReturnBlockToPool(block);
        }
        ArrangeBlocks();
    }

    private void OnDisable()
    {
        if (activeBlocks != null)
        {
            foreach (var block in activeBlocks)
            {
                if (block != null)
                {
                    Destroy(block);
                }
            }
            activeBlocks.Clear();
        }

        if (blockPool != null)
        {
            while (blockPool.Count > 0)
            {
                var block = blockPool.Dequeue();
                if (block != null)
                {
                    Destroy(block);
                }
            }
        }
        
        isInitialized = false;
    }

    private void OnDrawGizmos()
    {
        if (!enabled) return;

        // 计算网格的总宽度和高度
        float totalWidth = columnCount * blockSize.x;
        float totalHeight = rowCount * blockSize.y;
        
        // 计算网格的左上角位置
        Vector3 gridOrigin = transform.position + new Vector3(startPosition.x - blockSize.x/2, startPosition.y + blockSize.y/2, 0);
        
        // 设置 Gizmos 颜色
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        
        // 绘制整个区域的半透明框
        Gizmos.DrawCube(gridOrigin + new Vector3(totalWidth/2, -totalHeight/2, 0), 
            new Vector3(totalWidth, totalHeight, 0.1f));
        
        // 设置网格线的颜色
        Gizmos.color = Color.green;
        
        // 绘制垂直线
        for (int col = 0; col <= columnCount; col++)
        {
            Vector3 startLine = gridOrigin + new Vector3(col * blockSize.x, 0, 0);
            Vector3 endLine = startLine + new Vector3(0, -totalHeight, 0);
            Gizmos.DrawLine(startLine, endLine);
        }
        
        // 绘制水平线
        for (int row = 0; row <= rowCount; row++)
        {
            Vector3 startLine = gridOrigin + new Vector3(0, -row * blockSize.y, 0);
            Vector3 endLine = startLine + new Vector3(totalWidth, 0, 0);
            Gizmos.DrawLine(startLine, endLine);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 如果在编辑器中选中对象，重新计算起始位置
        if (!Application.isPlaying)
        {
            CalculateBlockSizeAndSpacing();
            CalculateStartPosition();
        }
    }
}
