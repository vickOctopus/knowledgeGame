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
    private string saveFilePath;
    private PlayerSaveData playerSaveData;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "playerData.json");
        playerSaveData = new PlayerSaveData();
        Debug.Log(saveFilePath);
    }

    public void Save()
    {
        if (Application.isEditor) return;

        if (PlayController.instance == null) return;

        playerSaveData.currentHp = PlayController.instance.currentHp;
        playerSaveData.maxHp = PlayController.instance.maxHp;
        playerSaveData.respawnPointX = transform.position.x;
        playerSaveData.respawnPointY = transform.position.y;

        string jsonData = JsonUtility.ToJson(playerSaveData);
        try
        {
            File.WriteAllText(saveFilePath, jsonData);
        }
        catch (Exception)
        {
            // 在这里可以添加错误处理逻辑，如果需要的话
        }
    }

    public void Load()
    {
        if (Application.isEditor) return;

        if (PlayController.instance == null) return;

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
                    Debug.Log(saveFilePath);
                }
                else
                {
                    SetDefaultValues();
                }
            }
            catch (Exception)
            {
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
