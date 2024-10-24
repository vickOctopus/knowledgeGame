using UnityEngine;
using DG.Tweening;


public class Switch : MonoBehaviour
{
    [SerializeField] private bool _isRight = true;
    private Vector2Int chunkCoord;

    private void Start()
    {
        RotateSwitch();
        if (ChunkManager.Instance != null)
        {
            chunkCoord = ChunkManager.Instance.GetChunkCoordFromWorldPos(transform.position);
        }
        else
        {
            // Debug.LogError("ChunkManager.Instance is null in Switch Start method");
        }
    }

    public void SwitchChange(bool dir)
    {
        if (dir != _isRight)
        {
            _isRight = dir;
            RotateSwitch();
            NotifyChunkManager();
        }
    }

    public void PlayerTrigger()
    {
        _isRight = !_isRight;
        RotateSwitch();
        NotifyChunkManager();
    }

    private void NotifyChunkManager()
    {
        ChunkManager.Instance.NotifySwitchChange(chunkCoord);
    }

    private void RotateSwitch()
    {
        transform.DOLocalRotate(new Vector3(0, 0, 40 * (_isRight ? -1 : 1)), 0.3f, RotateMode.Fast);
    }
}