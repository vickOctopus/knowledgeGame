using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Diary : MonoBehaviour,ISceneInteraction
{
   
    public int diaryId;
    public float speed;
    public float height;
    private float _originalPosY;

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(name))
        {
            PlayerPrefs.SetInt(name, 0);
        }
        else if (PlayerPrefs.GetInt(name) == 1)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _originalPosY = transform.position.y;
    }


    private void Update()
    {
         var newYPosition = Mathf.Sin(Time.time * speed) * height;
         transform.position=new Vector2(transform.position.x,_originalPosY+newYPosition);
    }

    public void Interact()
    {
        UIManager.instance.OpenDiary(diaryId);
        PlayerPrefs.SetInt(name, 1);
        Destroy(gameObject);
    }
}
