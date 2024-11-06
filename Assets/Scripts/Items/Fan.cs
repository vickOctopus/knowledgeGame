using UnityEngine;
using System.Collections;

public class Fan : MonoBehaviour, IButton
{
    [Header("检测设置")]
    [SerializeField] private float floatHeight = 5f; // 漂浮高度
    [SerializeField] private Vector2 detectSize = new Vector2(2f, 10f); // 检测区域大小
    [SerializeField] private bool needButton = true; // 是否需要按钮控制
    [SerializeField] private Transform buttonTransform; // 按钮的Transform
    [SerializeField] private LayerMask obstacleLayer; // 障碍物检测层

    [Header("力度设置")]
    [SerializeField] private float forceMultiplier = 5f; // 力度系数
    [SerializeField] private float heightTolerance = 0.1f; // 高度容差
    [SerializeField] private float smoothSpeed = 5f; // 速度平滑系数

    private bool _isAffectingPlayer;
    private Rigidbody2D _playerRb;
    private float _originalGravity;
    private float _currentVelocity;
    private bool _isActive;
    private Animator _animator;
    private readonly int _onHash = Animator.StringToHash("On");

    private Vector3 _lastPosition;
    private Vector3 _initialPosition;

    private LineRenderer _lineRenderer;

    private bool _hasInitialPosition;  // 添加标志来表示是否已记录初始位置

    private float _currentDetectHeight; // 当前检测高度
    private float _floatHeightRatio; // 漂浮高度与检测区域高度的比值

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _lastPosition = transform.position;
        
        InitializeLineRenderer();
        
        if (!needButton && buttonTransform != null)
        {
            buttonTransform.gameObject.SetActive(false);
        }

        // 延迟记录初始位置
        StartCoroutine(RecordInitialPosition());
    }

    private IEnumerator RecordInitialPosition()
    {
        yield return new WaitForSeconds(0.5f);
        _initialPosition = transform.position;
        _hasInitialPosition = true;  // 置标志
    }

    private void InitializeLineRenderer()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = Color.cyan;
            _lineRenderer.endColor = Color.cyan;
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.positionCount = 5;
            _lineRenderer.useWorldSpace = true;
        }
    }

    private void OnEnable()
    {
        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.OnChunkUnloaded += OnChunkUnloaded;
        }
        
        // 确保LineRenderer在启用时正确初始化
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = _isActive;
        }
    }

    private void OnDisable()
    {
        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.OnChunkUnloaded -= OnChunkUnloaded;
        }
    }

    private void OnChunkUnloaded(Vector2Int chunkCoord)
    {
        // 只有在已记录初始位置后才响应事件
        if (!_hasInitialPosition) return;

        transform.position = _initialPosition;
        _lastPosition = _initialPosition;
        
        if (_isAffectingPlayer)
        {
            ResetPlayerState();
        }
    }

    private void Start()
    {
        _isActive = !needButton; // 如果不需要按钮控制，则默认开启
        _animator.SetBool(_onHash, _isActive);
        
        // 计算初始比值
        _floatHeightRatio = floatHeight / detectSize.y;
        _currentDetectHeight = detectSize.y;
    }

    private void Update()
    {
        if (!_isActive)
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;
        UpdateDetectionHeightAndArea();
        CheckAndUpdatePlayerState();
    }

    private void CheckAndUpdatePlayerState()
    {
        Vector2 currentDetectSize = new Vector2(detectSize.x, _currentDetectHeight);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (currentDetectSize.y * 0.5f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, currentDetectSize, 0f);
        
        bool foundPlayer = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") && !PlayController.instance.IsOnLadder)
            {
                foundPlayer = true;
                if (!_isAffectingPlayer)
                {
                    InitializePlayerEffect();
                }
                AdjustPlayerHeight();
                break;
            }
        }

        if (!foundPlayer && _isAffectingPlayer)
        {
            ResetPlayerState();
        }
    }

    private void UpdateDetectionHeightAndArea()
    {
        // 射线检测上方障碍物
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, detectSize.y, obstacleLayer);
        
        // 计算新的检测高度
        float newDetectHeight = hit.collider != null ? 
            Mathf.Min(hit.distance, detectSize.y) : detectSize.y;
        
        // 如果高度没有变化，直接返回
        if (Mathf.Approximately(_currentDetectHeight, newDetectHeight))
        {
            return;
        }
        
        // 更新检测高度
        _currentDetectHeight = newDetectHeight;
        
        // 更新漂浮高度，保持比例不变
        floatHeight = _currentDetectHeight * _floatHeightRatio;
        
        // 更新检测区域
        Vector2 currentDetectSize = new Vector2(detectSize.x, _currentDetectHeight);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (currentDetectSize.y * 0.5f);
        UpdateDetectionArea(boxCenter, currentDetectSize);
    }

    private void UpdateDetectionArea(Vector2 center, Vector2 size)
    {
        Vector2 halfSize = size * 0.5f;
        
        // 直接更新LineRenderer位置
        _lineRenderer.SetPosition(0, center + new Vector2(-halfSize.x, halfSize.y));
        _lineRenderer.SetPosition(1, center + new Vector2(halfSize.x, halfSize.y));
        _lineRenderer.SetPosition(2, center + new Vector2(halfSize.x, -halfSize.y));
        _lineRenderer.SetPosition(3, center + new Vector2(-halfSize.x, -halfSize.y));
        _lineRenderer.SetPosition(4, center + new Vector2(-halfSize.x, halfSize.y));
    }

    public void OnButtonDown()
    {
        if (!needButton) return;
        _isActive = true;
        _animator.SetBool(_onHash, true);
    }

    public void OnButtonUp()
    {
        if (!needButton) return;
        _isActive = false;
        _animator.SetBool(_onHash, false);
        if (_isAffectingPlayer)
        {
            ResetPlayerState();
        }
    }

    private void InitializePlayerEffect()
    {
        _playerRb = PlayController.instance.Rb;
        _originalGravity = _playerRb.gravityScale;
        _playerRb.gravityScale = 0f;
        _isAffectingPlayer = true;
        _currentVelocity = _playerRb.velocity.y;
        PlayController.instance.IsFloating = true;  // 设置浮空动画
    }

    private void AdjustPlayerHeight()
    {
        if (_playerRb == null) return;

        float targetHeight = transform.position.y + floatHeight;
        float currentHeight = _playerRb.transform.position.y;
        float heightDiff = targetHeight - currentHeight;

        float targetVelocity = 0f;
        if (Mathf.Abs(heightDiff) > heightTolerance)
        {
            targetVelocity = heightDiff * forceMultiplier;
        }

        _currentVelocity = Mathf.Lerp(_currentVelocity, targetVelocity, Time.deltaTime * smoothSpeed);
        _playerRb.velocity = new Vector2(_playerRb.velocity.x, _currentVelocity);
    }

    private void ResetPlayerState()
    {
        if (_playerRb != null)
        {
            _playerRb.gravityScale = _originalGravity;
            PlayController.instance.IsFloating = false;  // 取消浮空动画
        }
        _isAffectingPlayer = false;
        _playerRb = null;
        _currentVelocity = 0f;
    }

    private void OnDrawGizmos()
    {
        DrawDetectionArea();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetectionArea();
    }

    private void DrawDetectionArea()
    {
        if (!Application.isPlaying || _isActive)
        {
            // 在编辑器模式下使用 detectSize，在运行时使用 _currentDetectHeight
            Vector2 currentDetectSize = Application.isPlaying ? 
                new Vector2(detectSize.x, _currentDetectHeight) : detectSize;
                
            Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (currentDetectSize.y * 0.5f);
            
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawCube(boxCenter, currentDetectSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boxCenter, currentDetectSize);

            if (!Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 targetHeightPos = transform.position + Vector3.up * floatHeight;
                float lineWidth = currentDetectSize.x * 0.5f;
                Gizmos.DrawLine(targetHeightPos - Vector3.right * lineWidth, 
                              targetHeightPos + Vector3.right * lineWidth);
            }
        }
    }

    private void LateUpdate()
    {
        // 检查位置是否发生变化
        if (transform.position != _lastPosition)
        {
            // 更新按钮位置
            if (buttonTransform != null && needButton)
            {
                Vector3 delta = transform.position - _lastPosition;
                buttonTransform.position -= delta;
            }
            
            // 如果风扇处于激活状态，更新检测区域
            if (_isActive && _lineRenderer != null && _lineRenderer.enabled)
            {
                Vector2 currentDetectSize = new Vector2(detectSize.x, _currentDetectHeight);
                Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (currentDetectSize.y * 0.5f);
                UpdateDetectionArea(boxCenter, currentDetectSize);
            }
        }
        
        _lastPosition = transform.position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 延迟一帧执行，避免在不合适的时机调用
        UnityEditor.EditorApplication.delayCall += () =>
        {
            // 确保物体没有被销毁
            if (this != null && buttonTransform != null)
            {
                buttonTransform.gameObject.SetActive(needButton);
            }
        };
    }
#endif
} 