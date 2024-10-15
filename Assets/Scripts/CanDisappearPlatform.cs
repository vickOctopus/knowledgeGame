using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanDisappearPlatform : MonoBehaviour
{
    
    public void Disappear()
    {
       gameObject.SetActive(false);
    }

    public void Appear()
    {
        gameObject.SetActive(true);
    }
}
