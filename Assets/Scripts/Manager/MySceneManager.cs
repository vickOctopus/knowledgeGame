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
        // LoadSceneIfNotLoaded("Room_01");
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        AsyncOperation room01Load = LoadSceneIfNotLoaded("Room_01");

        // 等待两个场景都加载完成
        yield return new WaitUntil(() => room01Load.isDone);

        // 卸载 MainScene
        yield return SceneManager.UnloadSceneAsync("MainScene");
    }

    private AsyncOperation LoadSceneIfNotLoaded(string sceneName)
    {
        if (!IsSceneLoaded(sceneName))
        {
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        return null;
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
