using UnityEngine;

public class AdvancedBallPoolManager : BallPoolManager, IButton
{
    private int activeBallCount = 0;
    private BlockPoolManager blockPoolManager;

    private void Awake()
    {
        // 获取当前所在的区块对象
        Transform chunkParent = transform.parent;
        if (chunkParent != null && chunkParent.name.StartsWith("Chunk_"))
        {
            // 在当前区块中查找 BlockPoolManager
            blockPoolManager = chunkParent.GetComponentInChildren<BlockPoolManager>();
            
            if (blockPoolManager == null)
            {
                Debug.LogWarning($"BlockPoolManager not found in chunk {chunkParent.name}");
            }
        }
        else
        {
            Debug.LogWarning($"AdvancedBallPoolManager is not in a chunk: {gameObject.name}");
        }
    }

    public override void SpawnBall()
    {
        if (activeBallCount == 0)
        {
            base.SpawnBall();
            activeBallCount++;
        }
    }

    public override void ReturnBallToPool(GameObject ball)
    {
        ball.SetActive(false);
        ballPool.Enqueue(ball);
        activeBallCount--;
        
        if (blockPoolManager != null)
        {
            blockPoolManager.RestoreAllBlocks();
        }
    }

    void Start()
    {
        base.InitializePool();
    }

    public void OnButtonDown()
    {
        SpawnBall();
    }

    public void OnButtonUp()
    {
    }
}
