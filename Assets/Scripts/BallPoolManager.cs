using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        ;
    }

    void OnDrawGizmos()
    {
        Vector3 spawnPosition = transform.position;

        // 绘制生成位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPosition, GIZMO_SIZE);
        Gizmos.DrawLine(spawnPosition, spawnPosition + transform.right * GIZMO_SIZE);
        Gizmos.DrawLine(spawnPosition, spawnPosition + transform.up * GIZMO_SIZE);

        // 绘制初始速度方向和大小
        Gizmos.color = Color.red;
        Vector2 initialDirection = GetInitialDirection();
        Vector3 velocityDirection = transform.TransformDirection(initialDirection);
        float arrowLength = initialSpeed * VELOCITY_ARROW_SCALE;
        Vector3 arrowEnd = spawnPosition + velocityDirection * arrowLength;
        Gizmos.DrawLine(spawnPosition, arrowEnd);
        
        // 绘制箭头
        Vector3 right = Quaternion.LookRotation(velocityDirection) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(velocityDirection) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawLine(arrowEnd, arrowEnd + right * (arrowLength * 0.2f));
        Gizmos.DrawLine(arrowEnd, arrowEnd + left * (arrowLength * 0.2f));

        // 绘制速度大小和角度文本
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(arrowEnd, $"速度: {initialSpeed}\n角度: {initialAngle}°");
    }
}
