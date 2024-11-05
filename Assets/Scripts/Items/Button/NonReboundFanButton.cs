using UnityEngine;

public class NonReboundFanButton : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
    protected Sprite upSprite;
    public Sprite downSprite;
    protected BoxCollider2D boxCollider;
    private bool _isPressed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        upSprite = spriteRenderer.sprite;
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        // 如果已经按下，就不再检测
        if (_isPressed) return;

        if (CheckButtonPress())
        {
            _isPressed = true;
            spriteRenderer.sprite = downSprite;
            SendMessageUpwards("OnButtonDown");
        }
    }

    private bool CheckButtonPress()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, boxCollider.size, 0);
        foreach (Collider2D collider in colliders)
        {
            // 跳过按钮自身的碰撞体
            if (collider == boxCollider) continue;

            // 检查是否是父物体的一部分
            if (transform.parent != null && collider.transform.IsChildOf(transform.parent))
            {
                return true;
            }

            // 检查是否是其他带有刚体的物体（除了平台）
            if (collider.attachedRigidbody != null && !collider.CompareTag("Platform"))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().bounds.size);
    }
} 