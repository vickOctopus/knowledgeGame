using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    //public Sprite ButtonDownSprite;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (!PlayerPrefs.HasKey(name+transform.parent.name))
        {
            PlayerPrefs.SetInt(name+transform.parent.name, 0);
        }
        else if (PlayerPrefs.GetInt(name+transform.parent.name) == 1)
        {
            OnButtonDown();
        }
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (PlayerPrefs.GetInt(name+transform.parent.name) == 0)
        {
             OnButtonDown();
             PlayerPrefs.SetInt(this.name+transform.parent.name, 1);
             SendMessageUpwards("ButtonDown");
        }
           
    }

    void OnButtonDown()
    {
        _spriteRenderer.color=Color.grey;
    }
}
