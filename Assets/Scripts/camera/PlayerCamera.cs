using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private void Start()
    {
        var x=Mathf.Floor(Camera.main.WorldToScreenPoint(transform.position).x/Screen.width);
        var y=Mathf.Floor(Camera.main.WorldToScreenPoint(transform.position).y/Screen.height);
        CameraController.Instance.CameraStartResetPosition(x,y);
    }

    private void OnBecameInvisible()
    {
        if (Camera.main)
        {
             var x=Mathf.Floor(Camera.main.WorldToScreenPoint(transform.position).x/Screen.width);
             var y=Mathf.Floor(Camera.main.WorldToScreenPoint(transform.position).y/Screen.height);
             CameraController.Instance.CameraStartResetPosition(x,y);
        }
       
        //CheckPlayerPosition();
    }

    /*private void CheckPlayerPosition()
    {
        
        var playerPositionIndex=0;
        if (Camera.main != null)
        {
            if (Camera.main.WorldToScreenPoint(transform.position).x>Screen.width)
            {
                playerPositionIndex = 1;
            }
            else if (Camera.main.WorldToScreenPoint(transform.position).x<0)
            {
                playerPositionIndex = 2;
            }
            else if (Camera.main.WorldToScreenPoint(transform.position).y>Screen.height)
            {
                playerPositionIndex = 3;
            }
            else if (Camera.main.WorldToScreenPoint(transform.position).y<0)
            {
                playerPositionIndex = 4;
            }
            else
            {
                playerPositionIndex = 0;
            }
        }
        CameraManager.Instance.Event_OnPlayerLeftCamera(playerPositionIndex);
    }*/
}
