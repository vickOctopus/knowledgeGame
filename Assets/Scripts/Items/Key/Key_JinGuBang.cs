using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key_JinGuBang : MonoBehaviour,ISceneInteraction
{
    private void Awake()
    {
        if (!PlayerPrefs.HasKey("JinGuBang_Key"))
        {
            PlayerPrefs.SetInt("JinGuBang_Key", 0);
        }
        else if (PlayerPrefs.GetInt("JinGuBang_Key") == 1)
        {
            Destroy(gameObject);
        }
        
    }

    public void Interact()
    {
        PlayerPrefs.SetInt("JinGuBang_Key", 1);
        Destroy(gameObject);
    }
}
