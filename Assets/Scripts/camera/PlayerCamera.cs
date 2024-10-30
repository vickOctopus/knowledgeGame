using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("主相机未找到！");
            return;
        }
        
        // 初始化时更新一次相机位置
        if (CameraController.Instance != null)
        {
            CameraController.Instance.CameraStartResetPosition(transform.position);
        }
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

        // 获取玩家在屏幕上的位置
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        
        // 计算玩家相对于当前相机视图的偏移方向
        float x = Mathf.Floor(screenPos.x / Screen.width);
        float y = Mathf.Floor(screenPos.y / Screen.height);

        // 如果玩家超出视图，更新相机位置
        if (x != 0 || y != 0)
        {
            Vector3 playerPos = transform.position;
            CameraController.Instance.CameraStartResetPosition(playerPos);
            
            // 处理金箍棒状态
            if (!PlayController.instance.isTakingJinGuBang)
            {
                PlayController.instance.HandleTakingState();
                PlayController.instance.isTakingJinGuBang = true;
            }
        }
    }
}
