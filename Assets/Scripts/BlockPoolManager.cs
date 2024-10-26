using System;
using UnityEngine;
using System.Collections.Generic;

public class BlockPoolManager : MonoBehaviour
{
    public static BlockPoolManager instance;
    public GameObject blockPrefab;
    public int rowCount = 10;
    public int columnCount = 10;
    public Vector2 blockSize = new Vector2(1f, 1f);
    public bool autoCalculateSize = true;

    private int initialPoolSize;
    private Queue<GameObject> blockPool;
    private Vector2 startPosition;
    private Vector2 blockSpacing;
    private List<GameObject> activeBlocks; // 新增：用于跟踪活跃的方块

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        initialPoolSize = rowCount * columnCount;
        activeBlocks = new List<GameObject>(initialPoolSize); // 初始化活跃方块列表
        CalculateBlockSizeAndSpacing();
        InitializePool();
        CalculateStartPosition();
        ArrangeBlocks();
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
                Debug.LogWarning("Block prefab does not have a SpriteRenderer or Sprite. Using default size.");
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
        startPosition = new Vector2(transform.position.x+blockSize.x/2, transform.position.y-blockSize.y/2);
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
        block.transform.position = position;
        block.SetActive(true);
        activeBlocks.Add(block); // 将新生成的方块添加到活跃列表
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
        block.SetActive(false);
        blockPool.Enqueue(block);
        activeBlocks.Remove(block); // 从活跃列表中移除
    }

    // 修改后的恢复所有方块方法
    public void RestoreAllBlocks()
    {
        // 使用我们自己维护的活跃方块列表
        foreach (GameObject block in new List<GameObject>(activeBlocks))
        {
            ReturnBlockToPool(block);
        }

        // 重新排列所有方块
        ArrangeBlocks();
    }
}
