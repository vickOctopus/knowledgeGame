using UnityEngine;

public class AdvancedBallPoolManager : BallPoolManager, IButton
{
    private int activeBallCount = 0;

    // 覆盖 SpawnBall 方法
    public override void SpawnBall()
    {
        if (activeBallCount == 0)
        {
            base.SpawnBall();
            activeBallCount++;
        }
        else
        {
            // Debug.Log("世界中已存在活跃的球，不生成新的球。");
        }
    }

    // 覆盖 ReturnBallToPool 方法
    public override void ReturnBallToPool(GameObject ball)
    {
        ball.SetActive(false);
        ballPool.Enqueue(ball);
        activeBallCount--;
        BlockPoolManager.instance.RestoreAllBlocks();
    }

    // 覆盖 Start 方法
    void Start()  // 而不是 new void Start()
    {
        base.InitializePool();
        // 注意：我们不调用 SpawnBall()
    }

    public void OnButtonDown()
    {
        SpawnBall();
    }

    public void OnButtonUp()
    {
        // 按钮释放时不做任何操作
    }
}
