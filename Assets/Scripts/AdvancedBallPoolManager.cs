using UnityEngine;

public class AdvancedBallPoolManager : BallPoolManager, IButton
{
    private int activeBallCount = 0;
    private BlockPoolManager blockPoolManager;

    private void Awake()
    {
        base.InitializePool();
        // 在初始化时就找到对应的 BlockPoolManager
        Transform current = transform;
        while (current != null)
        {
            if (current.name.Contains("Chunk_"))
            {
                blockPoolManager = current.GetComponentInChildren<BlockPoolManager>();
                break;
            }
            current = current.parent;
        }
    }

    public override void SpawnBall()
    {
        if (activeBallCount == 0)
        {
            // 调用基类的 SpawnBall() 方法，它会正确设置速度和方向
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

    private void Start()
    {
        // 空实现，因为初始化已经在 Awake 中完成
    }

    public void OnButtonDown()
    {
        SpawnBall();
    }

    public void OnButtonUp()
    {
    }
}
