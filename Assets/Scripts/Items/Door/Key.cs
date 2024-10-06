using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    public int KeyID;
   
    private void Awake()
    {
        if (!PlayerPrefs.HasKey("Key"+KeyID))
        {
            PlayerPrefs.SetInt("Key" + KeyID, 0);
        }
        else if(PlayerPrefs.GetInt("Key" + KeyID) == 1)
        {
            Destroy(gameObject);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (PlayerPrefs.GetInt("Key"+KeyID)==0)
        {
            PlayerPrefs.SetInt("Key" + KeyID, 1);
        }
    }
}
