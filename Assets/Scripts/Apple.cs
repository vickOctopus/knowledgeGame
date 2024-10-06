using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apple : MonoBehaviour
{

    public int recoverHp;
    public PlayerData playerData;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            BeEaten();
        }
    }

    private void BeEaten()
    {
        if (playerData.currentHp+recoverHp<=playerData.maxHp)
        {
             PlayController.instance.Recover(recoverHp);
             Destroy(gameObject);
        }
       
    }
}
