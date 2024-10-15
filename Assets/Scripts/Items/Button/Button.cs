using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    public Sprite ButtonDownSprite;
    private SpriteRenderer _spriteRenderer;

    public void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        
    }


    public void Start()
    {
        
        if (Application.isEditor)
        {
            return;
        }
        
        if (!PlayerPrefs.HasKey(name + transform.parent.name))
        {
            PlayerPrefs.SetInt(name + transform.parent.name, 0);
        }
        else if (PlayerPrefs.GetInt(name + transform.parent.name) == 1)
        {
            OnButtonDown();
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isEditor)
        {
            if (PlayerPrefs.GetInt(name + transform.parent.name) == 0)
            {
                OnButtonDown();
                PlayerPrefs.SetInt(this.name + transform.parent.name, 1);
            }
        }
        
        else
        {
            OnButtonDown();
        }
    }

    public virtual void OnButtonDown()
    {
        _spriteRenderer.sprite = ButtonDownSprite;
        SendMessageUpwards("ButtonDown");
    }
}
