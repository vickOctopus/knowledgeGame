using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Texture2D cursorTexture;
    
    public static GameManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        Cursor.visible = false;
    }

    public event Action<int> OnSwitchChange; 
    public event Action<int> OnPlayerHpChange;
   

    public void SwitchChange(int switchID)
    {
        OnSwitchChange?.Invoke(switchID);
    }

    public void PlayerHpChange(int leftHp)
    {
        OnPlayerHpChange?.Invoke(leftHp);
    }
}
