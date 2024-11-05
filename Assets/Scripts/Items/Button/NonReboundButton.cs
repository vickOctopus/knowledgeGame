using UnityEngine;

public class NonReboundButton : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
   
    protected Sprite upSprite;
    public Sprite downSprite;
    protected BoxCollider2D boxCollider;
    private int _triggerCount = 0;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        upSprite = spriteRenderer.sprite;
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody != null && !collision.CompareTag("Platform"))
        {
            _triggerCount++;
            if (_triggerCount == 1)
            {
                spriteRenderer.sprite = downSprite;
                SendMessageUpwards("OnButtonDown");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.attachedRigidbody != null && !collision.CompareTag("Platform"))
        {
            _triggerCount--;
            if (_triggerCount <= 0)
            {
                _triggerCount = 0;
                spriteRenderer.sprite = upSprite;
                SendMessageUpwards("OnButtonUp");
            }
        }
    }

    [System.Obsolete("This method is kept for compatibility with ChunkButton. Use trigger count system instead.")]
    protected bool IsAnyBodyInTrigger()
    {
        return _triggerCount > 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);
    }
}
