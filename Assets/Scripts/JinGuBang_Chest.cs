using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JinGuBang_Chest : MonoBehaviour,ISceneInteraction
{
    public PlayerData playerData;
    public Sprite ChestOpenedSprite;
    
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (playerData.hasGetJinGuBang)
        {
            _spriteRenderer.sprite = ChestOpenedSprite;
        }
    }

    public void Interact()
    {
        if (!playerData.hasGetJinGuBang)
        {
            playerData.hasGetJinGuBang = true;
            OpenChest();
        }
    }

    private void OpenChest()
    {
        PlayController.instance.SpawnJinGuBang();
        _spriteRenderer.sprite = ChestOpenedSprite;
    }
}
