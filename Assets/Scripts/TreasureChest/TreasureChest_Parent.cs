using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChest_Parent : MonoBehaviour,ISceneInteraction
{
    public Sprite openSprite;
    private BoxCollider2D _boxCollider;
    private SpriteRenderer _spriteRenderer;
    protected void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (!PlayerPrefs.HasKey(name))
        {
            PlayerPrefs.SetInt(name, 0);
        }
        else if(PlayerPrefs.GetInt(name) == 1)
        {
            ChestOpen();
        }
    }

    private void ChestOpen()
    {
        _spriteRenderer.sprite = openSprite;
        _boxCollider.enabled = false;
        PlayerPrefs.SetInt(name, 1);
    }

    public virtual void Interact()
    {
        ChestOpen();
    }

}
