using System;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        UpdateCameraPosition();
    }


    private void OnBecameInvisible()
    {
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        var x = Mathf.Floor(mainCamera.WorldToScreenPoint(transform.position).x / Screen.width);
        var y = Mathf.Floor(mainCamera.WorldToScreenPoint(transform.position).y / Screen.height);
        CameraController.Instance.CameraStartResetPosition(x, y);
    }
}
