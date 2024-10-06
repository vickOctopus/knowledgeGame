using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door_Key : MonoBehaviour
{
    public int DoorID;
    public Sprite doorOpened;
    private BoxCollider2D _boxCollider;
    private SpriteRenderer _renderer;
    private void Awake()
    {
         _boxCollider = GetComponent<BoxCollider2D>();
         _renderer = GetComponent<SpriteRenderer>();
        

        if (!PlayerPrefs.HasKey(name))
        {
            PlayerPrefs.SetInt(name, 0);
        }
        else if (PlayerPrefs.GetInt(name) == 1)
        {
            DoorOpen();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (PlayerPrefs.HasKey("Key"+DoorID))
        {
            if (PlayerPrefs.GetInt("Key" + DoorID) == 1)
            {
                DoorOpen();
                PlayerPrefs.SetInt(name,1);
            }
        }
    }

   private void DoorOpen()
    {
        _boxCollider.enabled = false;
        _renderer.sprite = doorOpened;
    }
}
