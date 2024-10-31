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
        if (ChunkManager.Instance)
        {
            StartCoroutine(InitializeBlockPoolManager());
        }
    }

    private IEnumerator InitializeBlockPoolManager()
    {
        int maxRetries = 5;
        int currentRetry = 0;
        
        while (blockPoolManager == null && currentRetry < maxRetries)
        {
            yield return new WaitForSeconds(0.1f);
            
            Transform current = transform;
            while (current != null)
            {
                if (current.name.Contains("Chunk_"))
                {
                    blockPoolManager = current.GetComponentInChildren<BlockPoolManager>();
                    if (blockPoolManager != null) break;
                }
                current = current.parent;
            }
            
            currentRetry++;
        }

        if (blockPoolManager == null)
        {
            Debug.LogWarning($"Failed to find BlockPoolManager for {gameObject.name} after {maxRetries} retries");
            Transform current = transform;
            string hierarchy = gameObject.name;
            while (current.parent != null)
            {
                current = current.parent;
                hierarchy = current.name + "/" + hierarchy;
            }
            Debug.LogWarning($"Object hierarchy: {hierarchy}");
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
        if (ball.activeSelf)
        {
            activeBallCount--;
        }
        
        ball.SetActive(false);
        ballPool.Enqueue(ball);
        
        if (blockPoolManager != null)
        {
            blockPoolManager.RestoreAllBlocks();
        }
    }

    protected override IEnumerator RespawnBallWithDelay()
    {
        yield break;
    }

    public void OnButtonDown()
    {
        SpawnBall();
    }

    public void OnButtonUp()
    {
    }

    private void OnEnable()
    {
        ChunkManager.OnChunkLoadedEvent += OnChunkFullyLoaded;
    }

    private void OnDisable()
    {
        ChunkManager.OnChunkLoadedEvent -= OnChunkFullyLoaded;
    }

    private void OnChunkFullyLoaded()
    {
        StartCoroutine(InitializeBlockPoolManager());
    }
}

