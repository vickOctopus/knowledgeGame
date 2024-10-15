using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button_ShadowWall : Button
{
    
    public Transform shadowWallTriggerPos;
    public override void OnButtonDown()
    {
        base.OnButtonDown();
        
        EventManager.instance.ButtonShadowWallDown(shadowWallTriggerPos.position);
    }
}
