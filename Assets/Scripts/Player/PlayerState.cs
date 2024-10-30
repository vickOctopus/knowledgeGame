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
    public PlayerSaveData playerSaveData;

    private void Awake()
    {
        playerSaveData = new PlayerSaveData();
        Debug.Log("[PlayerState] Initialized");
    }

    public void Load(int slotIndex)
    { 
        Debug.Log($"[PlayerState] Starting Load for slot {slotIndex}");
        if (PlayController.instance == null)
        {
            Debug.LogError("[PlayerState] PlayController.instance is null");
            return;
        }

        string saveFilePath = SaveManager.GetSavePath(slotIndex, "playerData.json");
        Debug.Log($"[PlayerState] Loading from path: {saveFilePath}");

        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                Debug.Log($"[PlayerState] Loaded JSON data: {jsonData}");
                playerSaveData = JsonUtility.FromJson<PlayerSaveData>(jsonData);

                if (playerSaveData != null)
                {
                    Debug.Log($"[PlayerState] Successfully deserialized playerSaveData: " +
                             $"currentHp={playerSaveData.currentHp}, " +
                             $"maxHp={playerSaveData.maxHp}, " +
                             $"position=({playerSaveData.respawnPointX}, {playerSaveData.respawnPointY})");
                    ApplyLoadedData();
                }
                else
                {
                    Debug.LogWarning("[PlayerState] playerSaveData is null after deserialization");
                    SetDefaultValues();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerState] Error loading player data: {e.Message}\n{e.StackTrace}");
                SetDefaultValues();
            }
        }
        else
        {
            Debug.Log($"[PlayerState] No save file found at {saveFilePath}, using default values");
            SetDefaultValues();
        }
        
        PlayController.instance.HpChange();
    }

    private void ApplyLoadedData()
    {
        Debug.Log($"[PlayerState] Applying loaded position: ({playerSaveData.respawnPointX}, {playerSaveData.respawnPointY})");
        PlayController.instance.currentHp = playerSaveData.currentHp;
        PlayController.instance.maxHp = playerSaveData.maxHp;
        
        Vector3 newPosition = new Vector3(playerSaveData.respawnPointX, playerSaveData.respawnPointY, 0);
        PlayController.instance.transform.position = newPosition;
        Debug.Log($"[PlayerState] Set player position to: {newPosition}");
        
        if (CameraController.Instance != null)
        {
            CameraController.Instance.CameraStartResetPosition(newPosition);
        }
    }

    public void Save(int slotIndex)
    {
        if (PlayController.instance == null) return;

        playerSaveData.currentHp = PlayController.instance.currentHp;
        playerSaveData.maxHp = PlayController.instance.maxHp;
        playerSaveData.respawnPointX = PlayController.instance.transform.position.x;
        playerSaveData.respawnPointY = PlayController.instance.transform.position.y;

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

    private void SetDefaultValues()
    {
        Debug.Log("[PlayerState] Setting default values");
        PlayController.instance.currentHp = 4;
        PlayController.instance.maxHp = 4;
        
        Debug.Log($"[PlayerState] Default spawn point: {SaveManager.instance.defaultSpawnPoint}");
        
        PlayController.instance.transform.position = SaveManager.instance.defaultSpawnPoint;
        Debug.Log($"[PlayerState] Set player position to default: {SaveManager.instance.defaultSpawnPoint}");
        
        if (CameraController.Instance != null)
        {
            CameraController.Instance.CameraStartResetPosition(SaveManager.instance.defaultSpawnPoint);
        }
    }
}
