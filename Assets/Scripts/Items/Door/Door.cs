using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Door : MonoBehaviour,IButton
{
    public Sprite openSprite;
    private Sprite lockedSprite;
   
    public int buttonNeedNumber;
    private int _nowButtonNumber;
    private BoxCollider2D _collider;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        
        lockedSprite=_renderer.sprite;
    }


    public void ButtonDown()
    {
        _nowButtonNumber++;


        if (_nowButtonNumber == buttonNeedNumber)
        {
            OpenDoor();
        }
    }

    private void ButtonUp()
    {
        _nowButtonNumber--;

        if (_nowButtonNumber==0)
        {
            CloseDoor();
        }
    }

    void OpenDoor()
    {
        _renderer.sprite = openSprite;
        _collider.enabled = false;
    }

    private void CloseDoor()
    {
        _collider.enabled = true;
        _renderer.sprite = lockedSprite;
    }

    public void OnButtonDown()
    {
       ButtonDown();
    }

    public void OnButtonUp()
    {
        ButtonUp();
    }
}
