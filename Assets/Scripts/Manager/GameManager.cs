using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    [ContextMenu("Delete All JSON Files")]
    public void DeleteAllJsonFiles()
    {
        string path = Application.persistentDataPath;
        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string file in files)
        {
            File.Delete(file);
            Debug.Log($"Deleted: {file}");
        }

        Debug.Log($"Deleted {files.Length} JSON files from {path}");
    }

    public void DeleteAllJsonFilesWithConfirmation()
    {
        if (UnityEditor.EditorUtility.DisplayDialog("Delete All JSON Files",
            "Are you sure you want to delete all JSON files in the persistent data path?", "Yes", "No"))
        {
            DeleteAllJsonFiles();
        }
    }
}
