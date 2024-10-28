using UnityEngine;

public class ChunkButton : NonReboundButton
{
    private Vector2Int chunkCoord;

    private void Awake()
    {
        // 初始化必要的组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        upSprite = spriteRenderer.sprite;
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        // 获取当前按钮所在的chunk坐标
        if (ChunkManager.Instance != null)
        {
            chunkCoord = ChunkManager.Instance.GetChunkCoordFromWorldPos(transform.position);
        }
    }

    // 完全重写触发器方法，不调用基类方法
    private void OnTriggerEnter2D(Collider2D collision)
    {
        spriteRenderer.sprite = downSprite;
        NotifyChunkManager();
    }

    // 完全重写触发器方法，不调用基类方法
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsAnyBodyInTrigger())
        {
            spriteRenderer.sprite = upSprite;
            NotifyChunkManager();
        }
    }

    private void NotifyChunkManager()
    {
        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.NotifySwitchChange(chunkCoord);
        }
    }
}
