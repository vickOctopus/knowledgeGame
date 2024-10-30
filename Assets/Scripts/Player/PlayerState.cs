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
    public static PlayerState instance;

    private void Awake()
    {
        playerSaveData = new PlayerSaveData();

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        } 
    }

    private void Start()
    {
        // 移除 Start 方法中的调试代码
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
        Debug.Log($"[PlayerState] Starting Load for slot {slotIndex}");
        if (PlayController.instance == null)
        {
            Debug.LogError("[PlayerState] PlayController.instance is null");
            return;
        }

        if (ChunkManager.Instance == null)
        {
            Debug.LogError("[PlayerState] ChunkManager.Instance is null");
            SetDefaultValues();
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
                    Debug.Log($"[PlayerState] Loaded position: ({playerSaveData.respawnPointX}, {playerSaveData.respawnPointY})");
                    PlayController.instance.currentHp = playerSaveData.currentHp;
                    PlayController.instance.maxHp = playerSaveData.maxHp;
                    
                    Vector3 newPosition = new Vector3(playerSaveData.respawnPointX, playerSaveData.respawnPointY, 0);
                    transform.position = newPosition;
                    Debug.Log($"[PlayerState] Set player position to: {transform.position}");
                    
                    ChunkManager.Instance.InitializeChunks(newPosition);
                    SaveManager.instance.GetRespawnPosition(transform.position);
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

    private void SetDefaultValues()
    {
        Debug.Log("[PlayerState] Setting default values");
        PlayController.instance.currentHp = 4;
        PlayController.instance.maxHp = 4;
        
        Debug.Log($"[PlayerState] Default spawn point: {SaveManager.instance.defaultSpawnPoint}");
        ChunkManager.Instance.InitializeChunks(SaveManager.instance.defaultSpawnPoint);
        
        transform.position = SaveManager.instance.defaultSpawnPoint;
        Debug.Log($"[PlayerState] Set player position to default: {transform.position}");
    }
}
