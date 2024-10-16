using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button_ShadowWall : Button
{
    
    public Transform shadowWallTriggerPos;
    
    
    public override void ButtonDown()
    {
        base.ButtonDown();
        
        EventManager.instance.ButtonShadowWallDown(shadowWallTriggerPos.position);
    }
    
    
}
