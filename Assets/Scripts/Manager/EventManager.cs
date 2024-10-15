using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager instance;

    public event Action OnClimbLadder;
    public event Action OnLeftLadder;

    public event Action<Vector3> OnButtonShadowWallDown;
    
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

    public void ClimbLadder()
    {
        OnClimbLadder?.Invoke();
    }

    public void LeftLadder()
    {
        OnLeftLadder?.Invoke();
    }

    public void ButtonShadowWallDown(Vector3 pos)
    {
        OnButtonShadowWallDown?.Invoke(pos);
    }
    
    
}
