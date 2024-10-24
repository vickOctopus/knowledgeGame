using UnityEngine;

public class BouncingBall : MonoBehaviour
{
    public Vector2 initialVelocity = new Vector2(5f, 0f); // 初始速度
    public float bounceThreshold = 0.1f; // 反弹阈值

    private Vector2 currentVelocity;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private BallPoolManager poolManager;

    public void Initialize(BallPoolManager manager, Vector2 initialVelocity)
    {
        poolManager = manager;
        this.initialVelocity = initialVelocity;
        ResetVelocity();
    }

    void Start()
    {
        SetupComponents();
        ResetVelocity();
    }

    void SetupComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f; // 禁用重力
        rb.bodyType = RigidbodyType2D.Kinematic; // 使用运动学模式

        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        circleCollider.isTrigger = false; // 将碰撞体设置为非触发器
    }

    void ResetVelocity()
    {
        currentVelocity = initialVelocity;
    }

    void FixedUpdate()
    {
        Vector2 movement = currentVelocity * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(rb.position, circleCollider.radius, currentVelocity.normalized, movement.magnitude);
        
        bool collided = false;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Spikes"))
                {
                    DestroyBall();
                    return; // 立即结束方法，因为球已被销毁
                }
                else if (!hit.collider.isTrigger)
                {
                    HandleCollision(hit.collider, hit.normal, hit.point);
                    collided = true;
                    break;
                }
            }
        }

        if (!collided)
        {
            rb.MovePosition(rb.position + movement);
        }
    }

    void HandleCollision(Collider2D collider, Vector2 normal, Vector2 point)
    {
        currentVelocity = Vector2.Reflect(currentVelocity, normal);
        Vector2 newPosition = point + normal * (circleCollider.radius + bounceThreshold);
        rb.MovePosition(newPosition);
    }

    void DestroyBall()
    {
        if (poolManager != null)
        {
            poolManager.ReturnBallToPool(gameObject);
        }
    }

    void OnEnable()
    {
        ResetVelocity();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Spikes"))
        {
            DestroyBall();
        }
    }
}
