using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch_JinGuBangCheckBox : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Physics2D.IgnoreLayerCollision(7,10,false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Physics2D.IgnoreLayerCollision(7,10,true);
        }
    }
}
