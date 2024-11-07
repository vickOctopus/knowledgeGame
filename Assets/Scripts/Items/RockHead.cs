using UnityEngine;

public class RockHead : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 2f;    // 移动速度
    [SerializeField] private LayerMask collisionLayer;// 碰撞层设置
    [SerializeField] private float crushDistance = 0.5f; // 挤压判定距离

    [Header("检测设置")]
    [SerializeField] private LayerMask detectionLayer; // 检测层（玩家和金箍棒的组合）
    [SerializeField] private LayerMask playerLayer;   // 玩家层
    [SerializeField] private LayerMask jinGuBangLayer;    // 金箍棒层
    [SerializeField] private float angleThreshold = 5f;   // 角度阈值

    private bool isBlockedByJinGuBang = false;  // 是否被金箍棒阻挡

    private const float RAY_DISTANCE = 50f;  // 足够大的射线检测距离
    private bool movingUp = true;            // 移动方向
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Vector2 boxSize;  // 用于检测的盒体大小

    private float topLimit;    // 上边界
    private float bottomLimit; // 下边界

    private bool isTrackingPlayer = false;    // 是否正在追踪玩家
    private bool playerOnTop = false;         // 玩家是否在顶部

    [Header("停留设置")]
    [SerializeField] private float boundaryStayTime = 0.5f; // 边界停留时间
    private bool _isStaying = false;
    private float _stayTimer = 0f;

    private float _gameStartTime;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        InitializeBoxSize();
        InitializeMovementLimits();
        
        _gameStartTime = Time.time;
        _isStaying = false;  // 确保开始时不处于停留状态
        
        // 将物体放置在下边界
        Vector3 startPosition = transform.position;
        startPosition.y = bottomLimit;
        transform.position = startPosition;
        
        // 设置初始移动方向为向上
        movingUp = true;
    }

    private void InitializeBoxSize()
    {
        if (boxCollider != null)
        {
            boxSize = new Vector2(boxCollider.size.x * 0.9f, 0.1f);
        }
    }

    private void InitializeMovementLimits()
    {
        Vector2 rayStart = (Vector2)transform.position;
        float halfHeight = boxCollider.size.y / 2;

        // 向上检测
        RaycastHit2D upHit = Physics2D.Raycast(rayStart, Vector2.up, RAY_DISTANCE, collisionLayer);
        if (upHit.collider != null)
        {
            topLimit = upHit.point.y - halfHeight;
        }
        else
        {
            topLimit = transform.position.y + RAY_DISTANCE;
        }

        // 向下检测
        RaycastHit2D downHit = Physics2D.Raycast(rayStart, Vector2.down, RAY_DISTANCE, collisionLayer);
        if (downHit.collider != null)
        {
            bottomLimit = downHit.point.y + halfHeight;
        }
        else
        {
            bottomLimit = transform.position.y - RAY_DISTANCE;
        }

        // 确保边界有效
        if (topLimit <= bottomLimit)
        {
            float center = (topLimit + bottomLimit) / 2;
            float minDistance = 1f; // 最小边界距离
            topLimit = center + minDistance;
            bottomLimit = center - minDistance;
        }
    }

    private void FixedUpdate()
    {
        // 检查是否到达边界并改变方向
        CheckBoundaries();

        // 如果正在停留，处理停留逻辑
        if (_isStaying)
        {
            _stayTimer += Time.fixedDeltaTime;
            if (_stayTimer >= boundaryStayTime)
            {
                _isStaying = false;
                _stayTimer = 0f;
            }
            rb.velocity = Vector2.zero;
            return;
        }

        // 进行统一检测
        PerformDetection();

        // 如果被金箍棒阻挡，停止移动
        if (isBlockedByJinGuBang)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 检查挤压条件
        CheckCrushCondition();

        // 移动
        Vector2 movement = movingUp ? Vector2.up : Vector2.down;
        rb.velocity = movement * moveSpeed;
    }

    private void CheckBoundaries()
    {
        if (!_isStaying)
        {
            if (movingUp && transform.position.y >= topLimit)
            {
                StartStaying();
                movingUp = false;
                isTrackingPlayer = false;
            }
            else if (!movingUp && transform.position.y <= bottomLimit)
            {
                StartStaying();
                movingUp = true;
                isTrackingPlayer = false;
            }
        }
    }

    private void StartStaying()
    {
        // 确保游戏开始1秒后才能进入停留状态
        if (Time.time - _gameStartTime < 1f) return;
        
        _isStaying = true;
        _stayTimer = 0f;
        rb.velocity = Vector2.zero;
    }

    private void PerformDetection()
    {
        // 获取检测起点
        Vector2 checkPoint = (Vector2)transform.position + 
            (movingUp ? Vector2.up : Vector2.down) * (boxCollider.size.y / 2);
        
        // 进行统一检测，移除 crushDistance
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            checkPoint,
            boxSize,
            0f,
            detectionLayer
        );

        // 重置状态
        isBlockedByJinGuBang = false;
        isTrackingPlayer = false;

        // 处理检测结果
        foreach (Collider2D hit in hits)
        {
            // 检查是否是玩家
            if (((1 << hit.gameObject.layer) & playerLayer) != 0)
            {
                isTrackingPlayer = true;
                playerOnTop = movingUp;
            }
            // 检查是否是金箍棒
            else if (((1 << hit.gameObject.layer) & jinGuBangLayer) != 0)
            {
                CheckJinGuBangBlocking(hit);
            }
        }
    }

    private void CheckJinGuBangBlocking(Collider2D jinGuBangCollider)
    {
        JinGuBang jinGuBang = jinGuBangCollider.GetComponent<JinGuBang>();
        if (jinGuBang != null)
        {
            if (jinGuBang.IsInVerticalState)
            {
                // 如果金箍棒处于竖直状态，直接阻挡
                isBlockedByJinGuBang = true;
                jinGuBang.SetBlockingRockHead(true);
                return;
            }

            // 如果不是竖直状态，检查角度和碰撞
            float angleToUp = Vector2.Angle(jinGuBang.transform.up, Vector2.up);
            bool isNearVerticalAngle = angleToUp < angleThreshold || 
                                      Mathf.Abs(angleToUp - 180f) < angleThreshold;

            if (isNearVerticalAngle)
            {
                // 检测两端是否接触碰撞体
                Vector2 jinGuBangTop = (Vector2)jinGuBang.transform.position + 
                    (Vector2)jinGuBang.transform.up * jinGuBang.GetColliderHeight();
                Vector2 jinGuBangBottom = (Vector2)jinGuBang.transform.position;

                bool topBlocked = Physics2D.OverlapCircle(jinGuBangTop, 0.2f, collisionLayer);
                bool bottomBlocked = Physics2D.OverlapCircle(jinGuBangBottom, 0.2f, collisionLayer);

                isBlockedByJinGuBang = topBlocked && bottomBlocked;
                jinGuBang.SetBlockingRockHead(isBlockedByJinGuBang);
            }
            else
            {
                jinGuBang.SetBlockingRockHead(false);
            }
        }
    }

    private void CheckCrushCondition()
    {
        if (isTrackingPlayer)
        {
            float distanceToBoundary = movingUp ? 
                (topLimit - transform.position.y) : 
                (transform.position.y - bottomLimit);

            if (distanceToBoundary <= crushDistance)
            {
                PlayController.instance.PlayerDead();
                isTrackingPlayer = false;
            }
        }
    }

    // 添加用于在编辑器中可视化检测范围的方法
    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        // 显示检测范围
        Gizmos.color = Color.yellow;
        Vector2 topCheckStart = (Vector2)transform.position + Vector2.up * (boxCollider.size.y / 2);
        Vector2 bottomCheckStart = (Vector2)transform.position - Vector2.up * (boxCollider.size.y / 2);

        // 显示实际的检测盒范围
        Matrix4x4 originalMatrix = Gizmos.matrix;
        
        // 顶部检测盒
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            topCheckStart + Vector2.up * crushDistance,
            boxSize
        );

        // 底部检测盒
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(
            bottomCheckStart + Vector2.down * crushDistance,
            boxSize
        );

        Gizmos.matrix = originalMatrix;

        // 显示移动方向
        Gizmos.color = Color.green;
        Vector2 arrowStart = transform.position;
        Vector2 arrowEnd = arrowStart + (movingUp ? Vector2.up : Vector2.down) * 1f;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        // 如果在运行时且检测到金箍棒
        if (Application.isPlaying)
        {
            // 遍历场景中的金箍棒
            foreach (var jinGuBang in FindObjectsOfType<JinGuBang>())
            {
                if (jinGuBang != null)
                {
                    // 显示金箍棒的检测点
                    Vector2 jinGuBangTop = (Vector2)jinGuBang.transform.position + 
                        (Vector2)jinGuBang.transform.up * jinGuBang.GetColliderHeight();
                    Vector2 jinGuBangBottom = (Vector2)jinGuBang.transform.position;

                    // 增大检测圆圈到0.2f
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(jinGuBangTop, 0.2f);
                    Gizmos.DrawWireSphere(jinGuBangBottom, 0.2f);

                    // 如果检测到碰撞，显示不同颜色，实心球也增大到0.1f
                    if (Physics2D.OverlapCircle(jinGuBangTop, 0.2f, collisionLayer))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(jinGuBangTop, 0.1f);
                    }
                    if (Physics2D.OverlapCircle(jinGuBangBottom, 0.2f, collisionLayer))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(jinGuBangBottom, 0.1f);
                    }
                }
            }

            // 如果被金箍棒阻挡，显示更大的标记
            if (isBlockedByJinGuBang)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.8f);
                // 添加十字线标记
                Gizmos.DrawLine(transform.position + Vector3.left * 1f, transform.position + Vector3.right * 1f);
                Gizmos.DrawLine(transform.position + Vector3.up * 1f, transform.position + Vector3.down * 1f);
            }
        }
    }

    // 添加OnDrawGizmosSelected来显示更多调试信息
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null) return;

        // 显示检测范围
        Gizmos.color = Color.yellow;
        Vector2 topCheckStart = (Vector2)transform.position + Vector2.up * (boxCol.size.y / 2);
        Vector2 bottomCheckStart = (Vector2)transform.position - Vector2.up * (boxCol.size.y / 2);
        
        Gizmos.DrawWireCube(topCheckStart, new Vector2(boxCol.size.x * 0.9f, crushDistance * 2));
        Gizmos.DrawWireCube(bottomCheckStart, new Vector2(boxCol.size.x * 0.9f, crushDistance * 2));

        // 如果正在追踪玩家，显示追踪状态
        if (isTrackingPlayer)
        {
            Gizmos.color = Color.red;
            Vector3 trackPos = playerOnTop ? topCheckStart : bottomCheckStart;
            Gizmos.DrawWireSphere(trackPos, 0.2f);
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.right * 2,
            $"移动速度: {moveSpeed}\n" +
            $"当前方向: {(movingUp ? "向上" : "向下")}\n" +
            $"追踪状态: {(isTrackingPlayer ? "追踪中" : "未追踪")}\n" +
            $"玩家位置: {(playerOnTop ? "顶部" : "底部")}\n" +
            $"上边界: {topLimit:F2}\n" +
            $"下边界: {bottomLimit:F2}\n" +
            $"停留状态: {(_isStaying ? $"停留中 ({_stayTimer:F2}s)" : "移动中")}");
        #endif
    }
} 