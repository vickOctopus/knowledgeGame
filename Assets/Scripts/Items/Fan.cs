using UnityEngine;

public class Fan : MonoBehaviour, IButton
{
    [Header("检测设置")]
    [SerializeField] private float floatHeight = 5f; // 漂浮高度
    [SerializeField] private Vector2 detectSize = new Vector2(2f, 10f); // 检测区域大小
    [SerializeField] private bool needButton = true; // 是否需要按钮控制
    [SerializeField] private Transform buttonTransform; // 按钮的Transform

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

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _lastPosition = transform.position;
        _initialPosition = transform.position;
        
        InitializeLineRenderer();
        
        if (!needButton && buttonTransform != null)
        {
            buttonTransform.gameObject.SetActive(false);
        }
    }

    private void InitializeLineRenderer()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = Color.cyan;
        _lineRenderer.endColor = Color.cyan;
        _lineRenderer.startWidth = 0.1f;
        _lineRenderer.endWidth = 0.1f;
        _lineRenderer.positionCount = 5; // 5个点形成一个矩形（首尾相连）
        _lineRenderer.useWorldSpace = true;
    }

    private void OnEnable()
    {
        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.OnChunkUnloaded += OnChunkUnloaded;
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
    }

    private void Update()
    {
        if (!_isActive)
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;
        
        // 更新检测区域位置和显示
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (detectSize.y * 0.5f);
        UpdateDetectionArea(boxCenter);

        // 使用OverlapBoxAll检测所有碰撞体
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, detectSize, 0f);
        
        bool foundPlayer = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
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

    private void UpdateDetectionArea(Vector2 center)
    {
        Vector2 halfSize = detectSize * 0.5f;
        Vector2 topLeft = center + new Vector2(-halfSize.x, halfSize.y);
        Vector2 topRight = center + new Vector2(halfSize.x, halfSize.y);
        Vector2 bottomRight = center + new Vector2(halfSize.x, -halfSize.y);
        Vector2 bottomLeft = center + new Vector2(-halfSize.x, -halfSize.y);

        _lineRenderer.SetPosition(0, topLeft);
        _lineRenderer.SetPosition(1, topRight);
        _lineRenderer.SetPosition(2, bottomRight);
        _lineRenderer.SetPosition(3, bottomLeft);
        _lineRenderer.SetPosition(4, topLeft); // 闭合矩形
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
        // 只在编辑器模式或者风扇激活时绘制检测区域
        if (!Application.isPlaying || _isActive)
        {
            // 绘制检测域
            Gizmos.color = new Color(0, 1, 1, 0.3f); // 半透明青色
            Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (detectSize.y * 0.5f);
            Gizmos.DrawCube(boxCenter, detectSize); // 实心区域

            Gizmos.color = Color.cyan; // 青色
            Gizmos.DrawWireCube(boxCenter, detectSize); // 线框

            // 只在编辑器模式下绘制目标高度线
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 targetHeightPos = transform.position + Vector3.up * floatHeight;
                float lineWidth = detectSize.x * 0.5f;
                Gizmos.DrawLine(targetHeightPos - Vector3.right * lineWidth, 
                              targetHeightPos + Vector3.right * lineWidth);
            }
        }
    }

    private void LateUpdate()
    {
        // 更新按钮位置
        if (buttonTransform != null && needButton && transform.position != _lastPosition)
        {
            Vector3 delta = transform.position - _lastPosition;
            buttonTransform.position -= delta;
        }
        
        // 在所有更新完成后再更新上一帧位置
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