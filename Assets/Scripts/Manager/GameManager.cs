using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
       
        Screen.SetResolution(1920, 1080, true);
        StartCoroutine(SetCursorCoroutine());
        // ChunkManager.Instance.InitializeChunks(PlayController.instance.transform.position);
        
        
        PlayerPrefs.DeleteAll();//删除所有playerprefs
    }

    public void StartGame()//由mainmenu调用
    {
         StartCoroutine(SetCursorCoroutine());
    }

    private IEnumerator SetCursorCoroutine()
    {
        yield return new WaitForEndOfFrame();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        Cursor.visible = false;
        StartCoroutine(EnsureCursorHidden());
    }

    private IEnumerator EnsureCursorHidden()
    {
        while (Cursor.visible)
        {
            Cursor.visible = false;
            yield return new WaitForSeconds(0.1f);
        }
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
            Debug.Log($"已删除: {file}");
        }

        Debug.Log($"已从 {path} 删除 {files.Length} 个 JSON 文件");
    }

    public void DeleteAllJsonFilesWithConfirmation()
    {
        #if UNITY_EDITOR
        if (UnityEditor.EditorUtility.DisplayDialog("删除所有 JSON 文件",
            "你确定要删除持久化数据路径中的所有 JSON 文件吗？", "是", ""))
        {
            DeleteAllJsonFiles();
        }
        #else
        // 在运行时，直接删除文件或显示自定义确认对话框
        if (ConfirmDeletion())
        {
            DeleteAllJsonFiles();
        }
        #endif
    }

    private bool ConfirmDeletion()
    {
        // 在这里实现运行时的确认逻辑
        // 例如，可以使用 UI 显示一个确认对话框
        // 或者直接返回 true 如果你想在运行时始终允许删除
        Debug.Log("请确认是否删除所有 JSON 文件");
        return false; // 默认返回 false，防止意外删除
    }
}
