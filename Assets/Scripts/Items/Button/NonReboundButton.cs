using System;
using UnityEngine;
using UnityEngine.Events;

public class NonReboundButton : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
   
    private Sprite upSprite;
    public Sprite downSprite;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        upSprite = spriteRenderer.sprite;
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
            spriteRenderer.sprite = downSprite;
            
            SendMessageUpwards("OnButtonDown");
          
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsAnyBodyInTrigger())
        {
            spriteRenderer.sprite = upSprite;
            SendMessageUpwards("OnButtonUp");
        }
    }

    private bool IsAnyBodyInTrigger()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, boxCollider.size, 0);
        foreach (Collider2D collider in colliders)
        {
            if (collider.attachedRigidbody != null && collider != boxCollider&&!collider.CompareTag("Platform"))
            {
                Debug.Log(collider.name);
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);
    }
}
