using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    public static MySceneManager instance;

    private void Awake()
    {
        if (instance==null)
        {
            instance=this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadSceneIfNotLoaded("PersistentScene");
        LoadSceneIfNotLoaded("Room_01");
       
    }

    private void LoadSceneIfNotLoaded(string sceneName)
    {
        if (!IsSceneLoaded(sceneName))
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        else
        {
            Debug.Log($"场景 {sceneName} 已经加载,跳过加载过程。");
        }
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}
