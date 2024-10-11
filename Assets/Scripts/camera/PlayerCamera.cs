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
        
    }
    
}
