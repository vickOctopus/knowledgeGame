using UnityEngine;
using System;
using System.IO;

[System.Serializable]
public class PlayerSaveData
{
    public int currentHp;
    public int maxHp;
    public float respawnPointX;
    public float respawnPointY;
}

public class PlayerState : MonoBehaviour, ISaveable
{
    private PlayerSaveData playerSaveData;

    private void Awake()
    {
        playerSaveData = new PlayerSaveData();
    }

    private void Start()
    {
        Load(PlayerPrefs.GetInt("CurrentSlotIndex"));
     
    }

    public void Save(int slotIndex)
    {
       // if (Application.isEditor) return;
        if (PlayController.instance == null) return;

        playerSaveData.currentHp = PlayController.instance.currentHp;
        playerSaveData.maxHp = PlayController.instance.maxHp;
        playerSaveData.respawnPointX = transform.position.x;
        playerSaveData.respawnPointY = transform.position.y;

        string jsonData = JsonUtility.ToJson(playerSaveData);
        string saveFilePath = SaveManager.GetSavePath(slotIndex, "playerData.json");
        
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));
            File.WriteAllText(saveFilePath, jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存玩家数据时出错：{e.Message}");
        }
    }

    public void Load(int slotIndex)
    { 
        if (Application.isEditor) return;//编辑模式不读取，方便测试
        
        if (PlayController.instance == null) return;

        string saveFilePath = SaveManager.GetSavePath(slotIndex, "playerData.json");

        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                playerSaveData = JsonUtility.FromJson<PlayerSaveData>(jsonData);

                if (playerSaveData != null)
                {
                    PlayController.instance.currentHp = playerSaveData.currentHp;
                    PlayController.instance.maxHp = playerSaveData.maxHp;
                    transform.position = new Vector2(playerSaveData.respawnPointX, playerSaveData.respawnPointY);
                }
                else
                {
                    SetDefaultValues();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载玩家数据时出错：{e.Message}");
                SetDefaultValues();
            }
        }
        else
        {
            SetDefaultValues();
        }
        
        PlayController.instance.HpChange();
    }

    private void SetDefaultValues()
    {
        PlayController.instance.currentHp = 4;
        PlayController.instance.maxHp = 4;
        transform.position = Vector2.zero;
    }
}
