using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 添加这行
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

    private void Start()
    {
        LoadGame();
    }

    public void SaveGame()
    {
        playerData.respawnPoint = _respawnPosition;

        // 保存所有实现了 ISaveable 接口的对象
        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            //saveable.PrepareForSave();
            saveable.Save();
        }

        // 这里可以添加其他需要保存的游戏数据
    }

    public void LoadGame()
    {
        // 加载所有实现了 ISaveable 接口的对象
        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.Load();
        }

        // 这里可以添加其他需要加载的游戏数据
    }

    public void GetRespawnPosition(Vector2 respawnPosition)
    {
        _respawnPosition = respawnPosition;
    }
}
