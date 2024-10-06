using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch_Dir : MonoBehaviour
{
    public bool isRight;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("JinGuBang"))
        {
            SendMessageUpwards("SwitchChange",isRight);
        }
        
    }
}
