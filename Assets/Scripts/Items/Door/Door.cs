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

    private void Start()
    {
        _nowButtonNumber = PlayerPrefs.GetInt(name);
        if (!PlayerPrefs.HasKey(name+"IsOpen"))
        {
            PlayerPrefs.SetInt(name+"IsOpen", 0);
        }
        else if (PlayerPrefs.GetInt(name+"IsOpen")==1)
        {
            OpenDoor();
        }
    }
    

    public void ButtonDown()
    {
        
        _nowButtonNumber++;
        PlayerPrefs.SetInt(name,_nowButtonNumber);
        
        if (_nowButtonNumber==buttonNeedNumber)
        {
            OpenDoor();
            PlayerPrefs.SetInt(name+"IsOpen",1);
        }
    }

    void OpenDoor()
    {
        //gameObject.SetActive(false);
        _renderer.sprite = openSprite;
        _collider.enabled = false;
    }
    
    
}
