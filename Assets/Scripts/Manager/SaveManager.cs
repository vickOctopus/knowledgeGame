using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance; 
    
    public PlayerData playerData;
    private Vector2 _respawnPosition;
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
        DontDestroyOnLoad(this);
    }

    public void SaveGame()
    {
        playerData.respawnPoint = _respawnPosition;
    }

    public void LoadGame()
    {
        
    }

    public void GetRespawnPosition(Vector2 respawnPosition)
    {
        _respawnPosition = respawnPosition;
    }
}
