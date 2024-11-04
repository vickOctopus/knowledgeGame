using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct JinGuBangPhysicsParams
{
    [Header("Mass")]
    public float mass;
    public float massMultiplier;

    [Header("Gravity")]
    public float gravity;
    public float gravityMultiplier;

    [Header("Rotation")]
    public float rotationSpeed;
    public bool freezeRotation;
    public float rotationDamping;

    [Header("Movement")]
    public float moveSpeed;
    public float velocityXMultiplier;
}

public class JinGuBang : MonoBehaviour
{
    #region Singleton
    public static JinGuBang instance;
    #endregion

    #region Constants
    // 竖直状态相关常量
    private const float VERTICAL_ANGLE_THRESHOLD = 15f;          // 伸长时的角度阈值
    private const float ANCHOR_VERTICAL_ANGLE_THRESHOLD = 5f;    // 锚点移动时的角度阈值（更严格）
    private const float BALANCE_FORCE = 300f;                   // 平衡力大小
    private const float VERTICAL_MASS_MULTIPLIER = 200f;        // 竖直状态时的质量倍数
    private const float VERTICAL_GRAVITY_MULTIPLIER = 20f;      // 竖直状态时的重力倍数
    private const float VERTICAL_ROTATION_DAMPING = 30f;        // 角度调整的阻尼系数
    private const float INITIAL_ROTATION_SPEED = 800f;          // 初始旋转到竖直位置的速度

    // 伸长力相关常量
    private const int RAY_COUNT = 5;                      // 射线检测数量
    private const float EDGE_OFFSET = 0.1f;               // 边缘偏移量
    private const float RAY_LENGTH_EXTRA = 0.1f;          // 射线额外长度
    private const float FORCE_MULTIPLIER = 2.0f;          // 力度倍数
    #endregion

    #region Serialized Fields
    [Header("References")]
    public Transform tip;
    
    [Header("Layer Masks")]
    public LayerMask hitLayerMask;
    public LayerMask platformMask;

    [Header("Movement Parameters")]
    [FormerlySerializedAs("_rotateSpeed")] 
    [SerializeField] private float rotateSpeed;
    public float elongationSpeed;
    public float maxScale;
    public float moveSpeed;
    public float width;

    [Header("Physics Parameters")]
    [SerializeField] private JinGuBangPhysicsParams _normalPhysics = new JinGuBangPhysicsParams
    {
        mass = 1f,
        massMultiplier = 1f,
        gravity = 1f,
        gravityMultiplier = 1f,
        rotationSpeed = 100f,
        freezeRotation = false,
        rotationDamping = 10f,
        moveSpeed = 5f,
        velocityXMultiplier = 1f
    };

    [SerializeField] private JinGuBangPhysicsParams _verticalPhysics = new JinGuBangPhysicsParams
    {
        mass = 1f,
        massMultiplier = 200f,
        gravity = 1f,
        gravityMultiplier = 20f,
        rotationSpeed = 800f,
        freezeRotation = true,
        rotationDamping = 30f,
        moveSpeed = 2f,
        velocityXMultiplier = 0.2f
    };
    #endregion

    #region Private Fields
    // 组件引用
    private Rigidbody2D _rg;
    private HingeJoint2D _joint;
    private CustomCollider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private PlayerInput _playerInput;
    private PhysicsShapeGroup2D _shapeGroup = new PhysicsShapeGroup2D();
    private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[5];

    // 原始值和缓存
    private float _height;
    private float _originalMass;
    private float _originalGravity;
    private float _originalRotationSpeed;
    private float _colliderHeight;

    // 状态标记
    private bool _isTipsBlock;
    private bool _isGamepadActive = true;
    private bool _isRotatingToVertical = false;
    private bool _isInitialRotation = false;

    // 输入和控制相关
    private float _anchorMoveAxis;
    private float _moveSpeed;
    private float _lastValidRotation = float.MinValue;
    private float _targetVerticalAngle;
    private Vector2 _lastMousePosition;
    private Vector2 _lastValidStickDirection;
    #endregion

    #region Physics Methods
    /// <summary>
    /// 应用物理参数设置
    /// </summary>
    private void ApplyPhysicsParams(JinGuBangPhysicsParams parameters)
    {
        _rg.mass = _originalMass * parameters.massMultiplier;
        _rg.gravityScale = _originalGravity * parameters.gravityMultiplier;
        _rg.freezeRotation = parameters.freezeRotation;
        rotateSpeed = parameters.rotationSpeed;
        _rg.velocity = new Vector2(_rg.velocity.x * parameters.velocityXMultiplier, _rg.velocity.y);
    }

    /// <summary>
    /// 应用竖直状态的物理属性
    /// </summary>
    private void ApplyVerticalPhysics()
    {
        ApplyPhysicsParams(_verticalPhysics);
        PlayController.instance.AdjustForVerticalJinGuBang(true);
    }

    /// <summary>
    /// 重置物理状态
    /// </summary>
    private void ResetPhysicsState()
    {
        ApplyPhysicsParams(_normalPhysics);
        PlayController.instance.AdjustForVerticalJinGuBang(false);
    }

    /// <summary>
    /// 应用伸长时的推力
    /// </summary>
    private void ApplyElongationForce(float oldHeight)
    {
        Vector2 rayStart = transform.position + transform.up * oldHeight;
        float rayLength = (_colliderHeight - oldHeight) + RAY_LENGTH_EXTRA;
        Vector2 rayDirection = transform.up * rayLength;

        for (int i = 0; i < RAY_COUNT; i++)
        {
            float t = (float)i / (RAY_COUNT - 1);
            t = Mathf.Pow(t - 0.5f, 5) * 16 + 0.5f;
            float offsetX = Mathf.Lerp(-width / 2 + EDGE_OFFSET, width / 2 - EDGE_OFFSET, t);

            Vector2 offset = transform.right * offsetX;
            Vector2 currentRayStart = rayStart + offset;

            int hitCount = Physics2D.RaycastNonAlloc(currentRayStart, transform.up, _raycastHits, rayLength, hitLayerMask);
            if (hitCount > 0)
            {
                RaycastHit2D hit = _raycastHits[0];
                Rigidbody2D hitRb = hit.collider.GetComponent<Rigidbody2D>();
                if (hitRb != null)
                {
                    float centerDistance = Mathf.Abs(t - 0.5f) * 2;
                    float forceMagnitude = elongationSpeed * FORCE_MULTIPLIER * (1 + (1 - centerDistance));
                    Vector2 pushForce = forceMagnitude * transform.up / RAY_COUNT;
                    hitRb.AddForceAtPosition(pushForce, hit.point, ForceMode2D.Impulse);
                }
            }
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// 更新碰撞体形状和大小
    /// </summary>
    private void UpdateCollider()
    {
        _shapeGroup.Clear();
        _shapeGroup.AddCapsule(new Vector2(0, 0), new Vector2(0, _colliderHeight), width / 2);
        _collider.SetCustomShapes(_shapeGroup);
        Physics2D.SyncTransforms();
        _spriteRenderer.size = new Vector2(_spriteRenderer.size.x, _colliderHeight);
        tip.localPosition = new Vector2(tip.localPosition.x, _colliderHeight);
    }

    /// <summary>
    /// 检查金箍棒两端是否被阻挡
    /// </summary>
    private void TipsCheck()
    {
        var upPos = transform.up * (_colliderHeight + 0.2f) + transform.position;
        var downPos = transform.position - transform.up * 0.2f;

        var upCheck = Physics2D.OverlapCircle(upPos, width / 3, platformMask);
        var downCheck = Physics2D.OverlapCircle(downPos, width / 1.5f, platformMask);

        _isTipsBlock = upCheck && downCheck;
    }

    /// <summary>
    /// 更新光标显示状态
    /// </summary>
    private void UpdateCursorVisibility()
    {
        if (_joint.enabled)
        {
            Cursor.visible = !_isGamepadActive;
        }
    }
    #endregion

    #region Equipment Methods
    /// <summary>
    /// 装备金箍棒
    /// </summary>
    private void EquipJinGuBang()
    {
        transform.parent = PlayController.instance.transform;
        rotateSpeed = _originalRotationSpeed;
        _rg.mass = _originalMass;
        _rg.gravityScale = _originalGravity;
        _colliderHeight = _height;
        UpdateCollider();
        transform.localPosition = new Vector2(0f, 0.5f);
        _joint.enabled = true;
        _joint.anchor = new Vector2(0.0f, 0.3f);
        _joint.connectedAnchor = new Vector2(0f, 0.5f);
        
        _lastValidRotation = _rg.rotation;

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("JinGuBang"), true);

        PlayController.instance.isEquipJinGuBang = true;
        PlayController.instance.isTakingJinGuBang = true;
    }

    /// <summary>
    /// 卸下金箍棒
    /// </summary>
    private void UnloadJinGuBang()
    {
        if (_isInitialRotation)
        {
            ExitVerticalState();
        }
        
        _joint.enabled = false;
        _rg.mass = 10.0f;
        _rg.gravityScale = 3.0f;
        
        PlayController.instance.UnloadJinGuBangPlayerMove();
        transform.parent = null;
        PlayController.instance.isOnJinGuBang = false;
        PlayController.instance.isTakingJinGuBang = false;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 禁用控制
    /// </summary>
    public void DisableControl()
    {
        _playerInput.Disable();
        _rg.freezeRotation = true;
    }

    /// <summary>
    /// 启用控制
    /// </summary>
    public void EnableControl()
    {
        _playerInput.Enable();
        _rg.freezeRotation = false;
    }

    // 修改公共方法，只返回碰撞体高度
    public float GetColliderHeight()
    {
        return _colliderHeight;
    }
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        // 单例初始化
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 组件获取和初始化
        _rg = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _joint = GetComponent<HingeJoint2D>();
        _collider = GetComponent<CustomCollider2D>();
        _playerInput = new PlayerInput();

        // 物理设置
        _rg.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 初始值缓存
        _height = _spriteRenderer.bounds.size.y;
        _originalMass = _rg.mass;
        _originalRotationSpeed = rotateSpeed;
        _originalGravity = _rg.gravityScale;
        _colliderHeight = _height;
        _lastMousePosition = Input.mousePosition;

        // 初始化碰撞体
        UpdateCollider();
    }

    private void OnEnable()
    {
        _playerInput.Enable();
        EquipJinGuBang();
    }

    private void OnDisable()
    {
        // 确保在禁用时退出竖直状态
        if (_isInitialRotation)
        {
            ExitVerticalState();
        }
        
        _playerInput.Disable();
        PlayController.instance.isEquipJinGuBang = false;
    }

    private void Update()
    {
        // 输入检测
        _anchorMoveAxis = _playerInput.GamePLay.AnchorMove.ReadValue<float>();
        
        // 状态更新
        TipsCheck();
        UpdateInputDevice();
        UpdateCursorVisibility();

        // 卸载检测
        if (_playerInput.GamePLay.Unload.IsPressed() && _joint.enabled)
        {
            UnloadJinGuBang();
        }

        // 速度更新
        if (_joint.enabled)
        {
            ChangeSpeedWithScaleAndAnchorPosition();
        }
        
        // 伸缩控制
        if (_playerInput.GamePLay.Elongation.IsPressed())
        {
            Elongation();
        }
        else if (_playerInput.GamePLay.Shorten.IsPressed())
        {
            Shorten();
        }
    }

    private void FixedUpdate()
    {
        if (_joint.enabled)
        {
            // 物理相关更新
            AnchorMove(); 
            RotateJinGuBang();
            
            // 竖直状态检查
            if (_isInitialRotation && PlayController.instance.IsGrounded())
            {
                ExitVerticalState();
            }
        }
    }

    private void OnDrawGizmos()
    {
        // 调试可视化
        Gizmos.DrawWireSphere(transform.up * (_colliderHeight + 0.2f) + transform.position, width / 3);
        Gizmos.DrawWireSphere(transform.position - transform.up * +0.2f, width / 1.5f);
    }
    #endregion

    #region Input Management
    /// <summary>
    /// 更新输入设备状态（鼠标/手柄）
    /// </summary>
    private void UpdateInputDevice()
    {
        // 检测鼠标移动
        Vector2 currentMousePosition = Input.mousePosition;
        bool hasMouseMovement = Vector2.Distance(currentMousePosition, _lastMousePosition) > 0.1f;
        _lastMousePosition = currentMousePosition;

        // 检测手柄输入
        bool hasGamepadInput = false;
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is UnityEngine.InputSystem.Gamepad gamepad)
            {
                Vector2 stickValue = gamepad.rightStick.ReadValue();
                hasGamepadInput = stickValue.magnitude > 0.1f;
                break;
            }
        }

        // 自动切换输入模式
        if (hasGamepadInput && !_isGamepadActive)
        {
            _isGamepadActive = true; // 切换到手柄模式
        }
        else if (hasMouseMovement && _isGamepadActive)
        {
            _isGamepadActive = false; // 切换到鼠标模式
        }
    }

    /// <summary>
    /// 根据金箍棒的长度调整移动和旋转速度
    /// </summary>
    private void ChangeSpeedWithScaleAndAnchorPosition()
    {
        // 移动速度随长度增加而增加
        _moveSpeed = moveSpeed * _colliderHeight/_height;
        
        // 旋转速度随长度增加而减小
        rotateSpeed = _originalRotationSpeed - _colliderHeight * 10.0f;
    }
    #endregion

    #region Vertical State Management
    /// <summary>
    /// 检测并处理金箍棒的竖直平衡状态
    /// </summary>
    private void ApplyVerticalBalance()
    {
        // 状态检查：如果不满足基本条件，退出竖直状态
        if (!_joint.enabled || !PlayController.instance.isTakingJinGuBang || PlayController.instance.IsGrounded())
        {
            ExitVerticalState();
            return;
        }

        // 计算与竖直方向的角度
        float angleToUp = Mathf.Abs(Vector2.Angle(transform.up, Vector2.up));
        float angleToDown = Mathf.Abs(Vector2.Angle(transform.up, Vector2.down));
        
        if (_isInitialRotation)
        {
            // 已在竖直状态：维持当前状态
            float currentAngle = (_targetVerticalAngle == 0f) ? angleToUp : angleToDown;
            
            if (currentAngle < VERTICAL_ANGLE_THRESHOLD)
            {
                ApplyVerticalPhysics();
                MaintainVerticalState(_targetVerticalAngle);
            }
            else
            {
                ExitVerticalState();
            }
        }
        else
        {
            // 新进入状态：只检查向下的角度
            if (angleToDown < VERTICAL_ANGLE_THRESHOLD)
            {
                const float targetAngle = 180f;
                EnterVerticalState(targetAngle);
                ApplyVerticalPhysics();
                MaintainVerticalState(targetAngle);
            }
        }
    }

    /// <summary>
    /// 进入竖直状态，调整初始位置和角度
    /// </summary>
    private void EnterVerticalState(float targetAngle)
    {
        _isInitialRotation = true;
        _isRotatingToVertical = true;
        _targetVerticalAngle = targetAngle;

        // 根据目标角度调整位置
        if (targetAngle == 0f) // 向上竖直
        {
            AdjustPositionForUpwardVertical();
        }
        else // 向下竖直
        {
            AdjustPositionForDownwardVertical();
        }
    }

    /// <summary>
    /// 维持竖直状态，控制旋转和位置
    /// </summary>
    private void MaintainVerticalState(float targetAngle)
    {
        float currentRotation = NormalizeAngle(_rg.rotation);
        float angleDifference = Mathf.DeltaAngle(currentRotation, targetAngle);
        
        if (_isRotatingToVertical)
        {
            // 旋转到竖直状态的过程
            RotateTowardsVertical(currentRotation, targetAngle, angleDifference);
        }
        else
        {
            // 锁定竖直状态
            LockVerticalRotation(targetAngle);
        }
    }

    /// <summary>
    /// 退出竖直状态，恢复正常物理属性
    /// </summary>
    private void ExitVerticalState()
    {
        if (_isInitialRotation)
        {
            _isRotatingToVertical = false;
            _isInitialRotation = false;
            
            ResetPhysicsState();
        }
    }

    #region Vertical State Helpers
    private void AdjustPositionForUpwardVertical()
    {
        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            0.2f, 
            platformMask);

        if (groundHit.collider != null)
        {
            transform.position = groundHit.point;
        }
    }

    private void AdjustPositionForDownwardVertical()
    {
        Vector2 topPoint = (Vector2)transform.position + (Vector2)transform.up * _colliderHeight;
        RaycastHit2D groundHit = Physics2D.Raycast(
            topPoint, 
            Vector2.down, 
            0.2f, 
            platformMask);

        if (groundHit.collider != null)
        {
            transform.position = groundHit.point - (Vector2)transform.up * _colliderHeight;
        }
    }

    private void RotateTowardsVertical(float currentRotation, float targetAngle, float angleDifference)
    {
        _rg.freezeRotation = false;
        _rg.angularVelocity = 0;
        
        float rotationSpeed = Mathf.Lerp(100f, _verticalPhysics.rotationSpeed, 
            1f - (Mathf.Abs(angleDifference) / 180f));
        
        float step = rotationSpeed * Time.fixedDeltaTime;
        float newRotation = Mathf.MoveTowardsAngle(currentRotation, targetAngle, step);
        _rg.MoveRotation(newRotation);
        
        if (Mathf.Abs(angleDifference) < 0.1f)
        {
            CompleteRotation(targetAngle);
        }
    }

    private void CompleteRotation(float targetAngle)
    {
        _isRotatingToVertical = false;
        _rg.rotation = targetAngle;
        _rg.freezeRotation = true;
        _rg.angularVelocity = 0;
    }

    private void LockVerticalRotation(float targetAngle)
    {
        _rg.freezeRotation = true;
        _rg.rotation = targetAngle;
        _rg.angularVelocity = 0;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 360f;
        return angle % 360f;
    }
    #endregion
    #endregion

    #region JinGuBang Control
    /// <summary>
    /// 处理金箍棒的伸长
    /// </summary>
    private void Elongation()
    {
        var temHeight = _colliderHeight + elongationSpeed * Time.deltaTime;

        if (_isTipsBlock || temHeight >= maxScale)
        {
            return;
        }

        float oldHeight = _colliderHeight;
        _colliderHeight += elongationSpeed * Time.deltaTime;
        
        if (_isInitialRotation)
        {
            AdjustPositionDuringScale(oldHeight);
            ApplyVerticalBalance();
        }
        else
        {
            ApplyVerticalBalance();
            ApplyElongationForce(oldHeight);
            UpdateCollider();
        }
    }

    /// <summary>
    /// 处理金箍棒的缩短
    /// </summary>
    private void Shorten()
    {
        var temHeight = _colliderHeight - elongationSpeed * Time.deltaTime;

        if (temHeight > _height)
        {
            float oldHeight = _colliderHeight;
            _colliderHeight -= elongationSpeed * Time.deltaTime;
            
            if (_isInitialRotation)
            {
                AdjustPositionDuringScale(oldHeight);
                ApplyVerticalBalance();
            }
            else
            {
                UpdateCollider();
            }
        }
        else if (_isInitialRotation)
        {
            ExitVerticalState();
        }
    }

    /// <summary>
    /// 在伸缩时根据当前状态调整位置
    /// </summary>
    private void AdjustPositionDuringScale(float oldHeight)
    {
        if (_targetVerticalAngle == 0f) // 向上的竖直状态，保持底部位置
        {
            Vector2 bottomPoint = transform.position;
            UpdateCollider();
            transform.position = bottomPoint;
        }
        else // 向下的竖直状态，保持顶部位置
        {
            Vector2 topPoint = (Vector2)transform.position + (Vector2)transform.up * oldHeight;
            UpdateCollider();
            transform.position = topPoint - (Vector2)transform.up * _colliderHeight;
        }
    }

    /// <summary>
    /// 处理锚点移动
    /// </summary>
    private void AnchorMove()
    {
        // 移动锚点
        _joint.anchor += new Vector2(0, _anchorMoveAxis * Time.fixedDeltaTime * _moveSpeed);

        // 限制锚点移动范围
        ClampAnchorPosition();

        // 检查是否可以进入竖直状态
        CheckVerticalStateEntry();
    }

    /// <summary>
    /// 限制锚点移动范围
    /// </summary>
    private void ClampAnchorPosition()
    {
        if (Mathf.Abs(_joint.anchor.y) >= _colliderHeight)
        {
            _joint.anchor = new Vector2(0, _colliderHeight);
        }
        else if (_joint.anchor.y <= 0)
        {
            _joint.anchor = new Vector2(0, 0);
        }
    }

    /// <summary>
    /// 检查是否可以进入竖直状态
    /// </summary>
    private void CheckVerticalStateEntry()
    {
        if (Mathf.Abs(_anchorMoveAxis) > 0.1f && !_isInitialRotation && _colliderHeight >= _height * 2f)
        {
            Vector2 bottomPoint = (Vector2)transform.position;
            float rayLength = 0.2f;
            
            RaycastHit2D bottomHit = Physics2D.Raycast(bottomPoint, Vector2.down, rayLength, platformMask);

            if (bottomHit.collider != null)
            {
                AdjustPositionToGround(bottomPoint, bottomHit, rayLength);
                CheckAndEnterVerticalState();
            }
        }
    }

    /// <summary>
    /// 调整位置使底部紧贴地面
    /// </summary>
    private void AdjustPositionToGround(Vector2 bottomPoint, RaycastHit2D hit, float rayLength)
    {
        float distanceToGround = hit.distance;
        if (distanceToGround < rayLength)
        {
            float adjustDistance = rayLength - distanceToGround;
            transform.position += (Vector3)(Vector2.up * adjustDistance);
        }
    }

    /// <summary>
    /// 检查并进入竖直状态
    /// </summary>
    private void CheckAndEnterVerticalState()
    {
        float currentAngle = Mathf.Abs(Vector2.Angle(transform.up, Vector2.up));
        if (currentAngle < ANCHOR_VERTICAL_ANGLE_THRESHOLD)
        {
            EnterVerticalState(0f);
            ApplyVerticalPhysics();
            MaintainVerticalState(0f);
        }
    }

    /// <summary>
    /// 处理金箍棒的旋转
    /// </summary>
    private void RotateJinGuBang()
    {
        if (_isInitialRotation) return;

        if (_isGamepadActive)
        {
            HandleGamepadRotation();
        }
        else
        {
            HandleMouseRotation();
        }
    }

    /// <summary>
    /// 处理手柄输入的旋转
    /// </summary>
    private void HandleGamepadRotation()
    {
        Vector2 stickValue = GetGamepadStickValue();
        
        if (stickValue.magnitude > 0.1f)
        {
            ApplyGamepadRotation(stickValue);
        }
        else if (_lastValidRotation != float.MinValue)
        {
            MaintainLastValidRotation();
        }
    }

    /// <summary>
    /// 处理鼠标输入的旋转
    /// </summary>
    private void HandleMouseRotation()
    {
        Vector2 mousePos = _playerInput.GamePLay.JinGuBangDir.ReadValue<Vector2>();
        var mousePosition = Camera.main.ScreenToWorldPoint(mousePos);
        mousePosition.z = 0;
        
        var direction = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f;

        var step = rotateSpeed * Time.fixedDeltaTime;
        var newAngle = Mathf.MoveTowardsAngle(_rg.rotation, angle, step);
        _rg.MoveRotation(newAngle);
    }

    private Vector2 GetGamepadStickValue()
    {
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is UnityEngine.InputSystem.Gamepad gamepad)
            {
                return gamepad.rightStick.ReadValue();
            }
        }
        return Vector2.zero;
    }

    private void ApplyGamepadRotation(Vector2 stickValue)
    {
        float rotationDirection = -stickValue.x;
        float rotationAmount = rotationDirection * rotateSpeed * Time.fixedDeltaTime;
        
        float newAngle = _rg.rotation + rotationAmount;
        _rg.MoveRotation(newAngle);
        _lastValidRotation = newAngle;
    }

    private void MaintainLastValidRotation()
    {
        var step = rotateSpeed * Time.fixedDeltaTime;
        var newAngle = Mathf.MoveTowardsAngle(_rg.rotation, _lastValidRotation, step);
        _rg.MoveRotation(newAngle);
    }
    #endregion

    public bool IsInVerticalState => _isInitialRotation;
}

 
