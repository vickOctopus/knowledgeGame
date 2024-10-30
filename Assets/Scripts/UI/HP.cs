using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class HP : MonoBehaviour
{
    // public PlayerData playerData;
    public GameObject heart;
   
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
      
        
       
    }

    private void Start()
    {  rect.sizeDelta = new Vector2(70*PlayController.instance.currentHp, rect.sizeDelta.y);
             
             
             for (var i = 0; i < PlayController.instance.maxHp; i++)
             {
                 var tem = Instantiate(heart, transform.position, Quaternion.identity);
                 tem.transform.SetParent(transform);
             }
             
        GameManager.instance.OnPlayerHpChange += HpChange;
        HpChange(PlayController.instance.currentHp);
    }
    

    private void OnDisable()
    {
        GameManager.instance.OnPlayerHpChange -= HpChange;
    }

    private void HpChange(int hp)
    {
        for (var i = 0; i < PlayController.instance.maxHp; i++)
        {
            if (i<hp)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        
        rect.sizeDelta = new Vector2(70*hp, rect.sizeDelta.y);
    }
}
