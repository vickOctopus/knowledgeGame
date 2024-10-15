using System;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }
        UpdateCameraPosition();
    }

    private void OnBecameInvisible()
    {
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        if (mainCamera == null || CameraController.Instance == null)
        {
            return;
        }

        var x = Mathf.Floor(mainCamera.WorldToScreenPoint(transform.position).x / Screen.width);
        var y = Mathf.Floor(mainCamera.WorldToScreenPoint(transform.position).y / Screen.height);
        CameraController.Instance.CameraStartResetPosition(x, y);
    }
}
