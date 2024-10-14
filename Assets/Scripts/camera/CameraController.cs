using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
   
   public static CameraController Instance;

   private void Awake()
   {
      if (Instance == null)
      {
         Instance = this;
      }
      else
      {
         Destroy(gameObject);
      }
      DontDestroyOnLoad(gameObject);
   }



   /*void CameraMove(int playerPositionIndex)
   {
      switch (playerPositionIndex)//cameraSize是宽长的一半，当size为18时，长为64，宽为36。
      {
         case 0:
            break;
         case 1:
            transform.position=new Vector3(transform.position.x+64.0f,transform.position.y,transform.position.z);
            break;
         case 2:
            transform.position=new Vector3(transform.position.x-64.0f,transform.position.y,transform.position.z);
            break;
         case 3:
            transform.position=new Vector3(transform.position.x,transform.position.y+36.0f,transform.position.z);
            break;
         case 4:
            transform.position=new Vector3(transform.position.x,transform.position.y-36.0f,transform.position.z);
            Debug.Log("hello");
            break;
      }
   }*/

   public void CameraStartResetPosition(float x, float y)
   {
      transform.position = new Vector3(transform.position.x + x * 50.0f, transform.position.y + y * 28.0f, transform.position.z);
   }
   
   
}
