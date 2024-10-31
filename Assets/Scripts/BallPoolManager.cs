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

    // 添加边界设置
    [Header("边界设置")]
    public Vector2 boundarySize = new Vector2(10f, 10f);  // 边界大小
    public Vector2 boundaryOffset = Vector2.zero;  // 边界中心的偏移量
    public bool showBoundary = true;  // 是否显示边界
    public Color boundaryColor = Color.white;  // 边界颜色

    private const float GIZMO_SIZE = 0.5f;
    private const float VELOCITY_ARROW_SCALE = 0.2f;
    protected Queue<GameObject> ballPool = new Queue<GameObject>();
    private bool isRespawning = false; // 新增：标记是否正在重生

    // 添加一个列表来跟踪活动的球体
    protected List<GameObject> activeBalls = new List<GameObject>();

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
            activeBalls.Add(ball); // 添加到活动球体列表
            
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
        activeBalls.Remove(ball); // 从活动球体列表中移除
        ballPool.Enqueue(ball);
        if (!isRespawning)
        {
            StartCoroutine(RespawnBallWithDelay());
        }
    }

    // 新增：带延迟的重生协程
    protected virtual IEnumerator RespawnBallWithDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        isRespawning = false;
        SpawnBall();
    }

    private void Update()
    {
        // 使用临时列表来存储需要回收的球体
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            if (!IsInsideBoundary(activeBalls[i].transform.position))
            {
                ReturnBallToPool(activeBalls[i]);
            }
        }
    }

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        // 绘制生成点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GIZMO_SIZE);

        // 绘制发射方向
        Vector2 direction = GetInitialDirection();
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3(direction.x, direction.y, 0) * GIZMO_SIZE * 2;
        
        Handles.color = Color.green;
        Handles.DrawLine(start, end);
        
        // 绘制箭头
        Vector3 right = Quaternion.Euler(0, 0, -30) * (end - start).normalized * GIZMO_SIZE;
        Vector3 left = Quaternion.Euler(0, 0, 30) * (end - start).normalized * GIZMO_SIZE;
        Handles.DrawLine(end, end - right);
        Handles.DrawLine(end, end - left);

        // 绘制速度指示器
        Handles.color = Color.red;
        Vector3 velocityEnd = start + new Vector3(direction.x, direction.y, 0) * initialSpeed * VELOCITY_ARROW_SCALE;
        Handles.DrawLine(start, velocityEnd);
        
        // 绘制速度箭头
        right = Quaternion.Euler(0, 0, -30) * (velocityEnd - start).normalized * GIZMO_SIZE * 0.5f;
        left = Quaternion.Euler(0, 0, 30) * (velocityEnd - start).normalized * GIZMO_SIZE * 0.5f;
        Handles.DrawLine(velocityEnd, velocityEnd - right);
        Handles.DrawLine(velocityEnd, velocityEnd - left);

        // 绘制边界框
        if (showBoundary)
        {
            Handles.color = boundaryColor;
            Vector3 center = (Vector3)GetBoundaryCenter();
            Vector3 size = new Vector3(boundarySize.x, boundarySize.y, 0);
            Handles.DrawWireCube(center, size);

            // 添加边界尺寸标签
            Handles.Label(center + new Vector3(boundarySize.x/2, 0, 0), $"{boundarySize.x}m");
            Handles.Label(center + new Vector3(0, boundarySize.y/2, 0), $"{boundarySize.y}m");
            
            // 绘制边界中心点
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.2f);
            Handles.Label(center + Vector3.up * 0.3f, "边界中心");
        }

        // 在Scene视图中显示角度和速度信息
        Handles.Label(transform.position + Vector3.up * 2f, 
            $"角度: {initialAngle}°\n速度: {initialSpeed}");
        #endif
    }

    // 获取边界的世界坐标中心点
    private Vector2 GetBoundaryCenter()
    {
        return (Vector2)transform.position + boundaryOffset;
    }

    // 检查位置是否在边界内
    public bool IsInsideBoundary(Vector2 position)
    {
        Vector2 center = GetBoundaryCenter();
        float halfWidth = boundarySize.x / 2f;
        float halfHeight = boundarySize.y / 2f;

        return position.x >= center.x - halfWidth && 
               position.x <= center.x + halfWidth &&
               position.y >= center.y - halfHeight && 
               position.y <= center.y + halfHeight;
    }

    // 获取边界信息的方法
    public Bounds GetBoundary()
    {
        return new Bounds((Vector3)GetBoundaryCenter(), new Vector3(boundarySize.x, boundarySize.y, 1f));
    }
}
