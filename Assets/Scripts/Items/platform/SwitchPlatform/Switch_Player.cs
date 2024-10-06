using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch_Player : MonoBehaviour,ISceneInteraction
{
    public Switch Switch;
    

    public void Interact()
    {
       Switch.PlayerTrigger();
    }
}
