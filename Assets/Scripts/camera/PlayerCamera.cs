using System;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private Camera mainCamera;
    // [SerializeField] private float checkInterval = 0.1f; // 检查间隔时间

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("主相机未找到！");
            return;
        }
        
        UpdateCameraPosition();

        // InvokeRepeating(nameof(CheckPlayerVisibility), 0f, checkInterval);
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
        
        
        if (!PlayController.instance.isTakingJinGuBang)
        {
            PlayController.instance.HandleTakingState();
            PlayController.instance.isTakingJinGuBang = true;
        }
    }
}
