using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BallPoolManager : MonoBehaviour
{
    public GameObject ballPrefab;
    public int poolSize = 10;
    public float initialSpeed = 5f; // 初始速度大小
    [Range(0, 360)]
    public float initialAngle = 0f; // 新增：初始角度
    public float respawnDelay = 1f; // 新增：重生延迟时间

    private const float GIZMO_SIZE = 0.5f;
    private const float VELOCITY_ARROW_SCALE = 0.2f;
    protected Queue<GameObject> ballPool = new Queue<GameObject>();
    private bool isRespawning = false; // 新增：标记是否正在重生

    void Start()
    {
        InitializePool();
        SpawnBall();
    }

    protected virtual void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject ball = Instantiate(ballPrefab, transform);
            ball.SetActive(false);
            ballPool.Enqueue(ball);
        }
    }

    public virtual void SpawnBall()
    {
        if (ballPool.Count > 0 && !isRespawning)
        {
            GameObject ball = ballPool.Dequeue();
            ball.transform.position = transform.position;
            ball.SetActive(true);
            BouncingBall bouncingBall = ball.GetComponent<BouncingBall>();
            if (bouncingBall != null)
            {
                Vector2 initialDirection = GetInitialDirection();
                Vector2 initialVelocity = initialDirection * initialSpeed;
                bouncingBall.Initialize(this, initialVelocity);
            }
        }
        else if (ballPool.Count == 0)
        {
            Debug.LogWarning("对象池中没有可用的球体!");
        }
    }

    private Vector2 GetInitialDirection()
    {
        float radians = initialAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    public virtual void ReturnBallToPool(GameObject ball)
    {
        ball.SetActive(false);
        ballPool.Enqueue(ball);
        if (!isRespawning)
        {
            StartCoroutine(RespawnBallWithDelay());
        }
    }

    // 新增：带延迟的重生协程
    private IEnumerator RespawnBallWithDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        isRespawning = false;
        SpawnBall();
        
    }

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        // 将所有使用 Handles 的代码放在这里
        Handles.DrawWireCube(transform.position, new Vector3(10, 10, 0));
        // 其他使用 Handles 的代码...
        #endif
    }
}
