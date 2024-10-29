using UnityEngine;
using System.Collections;

public class AdvancedBallPoolManager : BallPoolManager, IButton
{
    private int activeBallCount = 0;
    private BlockPoolManager blockPoolManager;

    private void Awake()
    {
        base.InitializePool();
    }

    private void Start()
    {
        StartCoroutine(InitializeBlockPoolManager());
    }

    private IEnumerator InitializeBlockPoolManager()
    {
        yield return null;  // 等待一帧确保所有组件初始化

        // 在父级层次结构中查找BlockPoolManager
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

        if (blockPoolManager == null)
        {
            Debug.LogWarning($"Failed to find BlockPoolManager for {gameObject.name}");
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

    public void OnButtonDown()
    {
        SpawnBall();
    }

    public void OnButtonUp()
    {
    }
}

