using UnityEngine;

public class RockHead : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 2f;    // 移动速度
    [SerializeField] private LayerMask collisionLayer;// 碰撞层设置
    [SerializeField] private float crushDistance = 0.5f; // 挤压判定距离
    [SerializeField] private LayerMask playerLayer;   // 玩家层

    private const float RAY_DISTANCE = 50f;  // 足够大的射线检测距离
    private bool movingUp = true;            // 移动方向
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Vector2 boxSize;  // 用于检测的盒体大小

    private float topLimit;    // 上边界
    private float bottomLimit; // 下边界

    private bool isTrackingPlayer = false;    // 是否正在追踪玩家
    private bool playerOnTop = false;         // 玩家是否在顶部

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        InitializeBoxSize();
        InitializeMovementLimits();
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

        // 向上检测
        RaycastHit2D upHit = Physics2D.Raycast(rayStart, Vector2.up, RAY_DISTANCE, collisionLayer);
        if (upHit.collider != null)
        {
            topLimit = upHit.point.y - boxCollider.size.y / 2;
        }
        else
        {
            topLimit = transform.position.y + RAY_DISTANCE;
        }

        // 向下检测
        RaycastHit2D downHit = Physics2D.Raycast(rayStart, Vector2.down, RAY_DISTANCE, collisionLayer);
        if (downHit.collider != null)
        {
            bottomLimit = downHit.point.y + boxCollider.size.y / 2;
        }
        else
        {
            bottomLimit = transform.position.y - RAY_DISTANCE;
        }
    }

    private void FixedUpdate()
    {
        // 检查是否到达边界并改变方向
        if (movingUp && transform.position.y >= topLimit)
        {
            movingUp = false;
            isTrackingPlayer = false;
        }
        else if (!movingUp && transform.position.y <= bottomLimit)
        {
            movingUp = true;
            isTrackingPlayer = false;
        }

        // 检测上方和下方的玩家
        Vector2 topCheckStart = (Vector2)transform.position + Vector2.up * (boxCollider.size.y / 2);
        Vector2 bottomCheckStart = (Vector2)transform.position - Vector2.up * (boxCollider.size.y / 2);

        // 根据移动方向检测玩家
        Collider2D playerCheck = Physics2D.OverlapBox(
            movingUp ? topCheckStart + Vector2.up * crushDistance : bottomCheckStart + Vector2.down * crushDistance,
            boxSize,
            0f,
            playerLayer
        );

        // 更新追踪状态
        if (playerCheck != null)
        {
            isTrackingPlayer = true;
            playerOnTop = movingUp;
        }
        else
        {
            isTrackingPlayer = false;
        }

        // 检查挤压条件
        if (isTrackingPlayer)
        {
            float distanceToBoundary = movingUp ? 
                (topLimit - transform.position.y) : 
                (transform.position.y - bottomLimit);

            if (distanceToBoundary <= crushDistance)
            {
                Debug.Log($"玩家被挤压死亡！移动方向: {(movingUp ? "向上" : "向下")}, " +
                         $"与边界距离: {distanceToBoundary:F3}");
                PlayController.instance.PlayerDead();
                isTrackingPlayer = false;
            }
        }

        // 移动
        Vector2 movement = movingUp ? Vector2.up : Vector2.down;
        rb.velocity = movement * moveSpeed;
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
            $"下边界: {bottomLimit:F2}");
        #endif
    }
} 