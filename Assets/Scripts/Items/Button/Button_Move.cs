using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button_Move : Button
{
   public GameObject MoveObject;

   private void Update()
   {
      transform.position = new Vector3(MoveObject.transform.position.x, transform.position.y+0.2f, transform.position.z);
   }
}
