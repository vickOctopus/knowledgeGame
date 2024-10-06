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
    public PlayerData playerData;
    public GameObject heart;
   
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70*playerData.currentHp, rect.sizeDelta.y);
        
        
        for (var i = 0; i < playerData.maxHp; i++)
        {
            var tem = Instantiate(heart, transform.position, Quaternion.identity);
            tem.transform.SetParent(transform);
        }
        
        
        HpChange(playerData.currentHp);
    }

    private void Start()
    {
        GameManager.instance.OnPlayerHpChange += HpChange;
        HpChange(playerData.currentHp);
    }
    

    private void OnDisable()
    {
        GameManager.instance.OnPlayerHpChange -= HpChange;
    }

    private void HpChange(int hp)
    {
        for (var i = 0; i < playerData.maxHp; i++)
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
