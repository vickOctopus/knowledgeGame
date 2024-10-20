using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanDisappearPlatform : MonoBehaviour,IButton
{
    
    
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Disappear();
    }

    public void Disappear()
    {
        boxCollider.enabled = false;
        spriteRenderer.enabled = false;
        // gameObject.SetActive(false);
    }
    

    public void Appear()
    {
    
        boxCollider.enabled = true;
        spriteRenderer.enabled = true;
        // gameObject.SetActive(true);
    }
    
    

    public void OnButtonDown()
    {
        Appear();
    }

    public void OnButtonUp()
    {
       Disappear();
    }
}
