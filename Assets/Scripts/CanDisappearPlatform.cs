using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanDisappearPlatform : MonoBehaviour,IButton
{
    
    
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    public bool disappearStart;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (disappearStart)
        {
             Disappear();
        }
       
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
        if (disappearStart)
        {
            Appear();
        }
        else
        {
            Disappear();
        }
        
    }

    public void OnButtonUp()
    {
        if (disappearStart)
        {
             Disappear();
        }
        else
        {
            Appear();
        }
    }
}
