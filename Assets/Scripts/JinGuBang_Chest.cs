using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class JinGuBang_Chest : MonoBehaviour,ISceneInteraction
{
    public Sprite ChestOpenedSprite;
    
    private BoxCollider2D boxCollider;
    
    private SpriteRenderer _spriteRenderer;
   

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer=GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
            if (!PlayerPrefs.HasKey(name))
            {
                PlayerPrefs.SetInt(name,0);
            }
            else if(PlayerPrefs.GetInt(name)==1)
            {
                OpenChest();
            }
        
    }


    void OpenChest()
    {
        _spriteRenderer.sprite = ChestOpenedSprite;
        boxCollider.enabled = false;
        
    }

    public void Interact()
    {
        if (PlayerPrefs.GetInt("JinGuBang_Key") == 1)
        {
            PlayerPrefs.SetInt(name,1);
            PlayerGetJinGuBang();
            OpenChest();
        }
    }

    private void PlayerGetJinGuBang()
    {
        
        PlayerPrefs.SetInt("HasJinGuBang", 1);
        PlayController.instance.SpawnJinGuBang();
    }
}
