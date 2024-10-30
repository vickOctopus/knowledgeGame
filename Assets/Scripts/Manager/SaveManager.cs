using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 添加这行
using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance; 
    
    public PlayerData playerData;
    private Vector2 _respawnPosition;
    private const int MaxSaveSlots = 3; // 设置最大存档槽数量

    public static int CurrentSlotIndex { get; set; } = 0; // 新增：当前存档槽索引

    public Vector2 defaultSpawnPoint = new Vector2(0, 0); // 添加默认出生点

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameStart();
    }

    public void GameStart()
    {
        CurrentSlotIndex = PlayerPrefs.GetInt("CurrentSlotIndex");
        _respawnPosition = defaultSpawnPoint;
        LoadGame();
    }
    
    public void SaveGame()
    {
        Debug.Log("SaveGame");
        SaveGame(CurrentSlotIndex);
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
        {
            Debug.LogError("无效的存档槽索引");
            return;
        }

        // playerData.respawnPoint = _respawnPosition;

        // 保存所有实现了 ISaveable 接口的对象
        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.Save(slotIndex);
        }

        Debug.Log($"游戏已保存到槽位 {slotIndex}");
    }

    public void LoadGame()
    {
        LoadGame(CurrentSlotIndex);
    }

    public void LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
        {
            Debug.LogError("无效的存档槽索引");
            return;
        }

        // 加载所有实现了 ISaveable 接口的对象
        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.Load(slotIndex);
        }

        // Debug.Log($"已从槽位 {slotIndex} 加载游戏");
    }

    public bool DoesSaveExist(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
        {
            Debug.LogError("无效的存档槽索引");
            return false;
        }

        string savePath = GetSavePath(slotIndex, "playerData.json");
        return File.Exists(savePath);
    }

    public static string GetSavePath(int slotIndex, string fileName)
    {
        return Path.Combine(Application.persistentDataPath, $"SaveSlot_{slotIndex}", fileName);
    }

    public void GetRespawnPosition(Vector2 respawnPosition)
    {
        _respawnPosition = respawnPosition;
    }

    public Vector2 GetCurrentRespawnPosition()
    {
        return _respawnPosition;
    }
}
