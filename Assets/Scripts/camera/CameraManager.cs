using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    public event Action<int> OnPlayerLeftCamera;

    private void Awake()
    {
        if (Instance==null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void Event_OnPlayerLeftCamera(int playerIndex)
    {
        OnPlayerLeftCamera?.Invoke(playerIndex);
    }
}
