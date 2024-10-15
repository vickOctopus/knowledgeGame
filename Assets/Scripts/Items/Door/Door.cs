using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public Sprite openSprite;
   
    public int buttonNeedNumber;
    private int _nowButtonNumber;
    private BoxCollider2D _collider;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();
    }


    public void ButtonDown()
    {
        _nowButtonNumber++;


        if (_nowButtonNumber == buttonNeedNumber)
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        _renderer.sprite = openSprite;
        _collider.enabled = false;
    }
}
