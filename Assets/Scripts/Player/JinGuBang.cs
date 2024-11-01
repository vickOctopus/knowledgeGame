using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class JinGuBang : MonoBehaviour
{
    public static JinGuBang instance;
    
    public LayerMask hitLayerMask;
    
    public LayerMask platformMask;
   
    [FormerlySerializedAs("_rotateSpeed")] 
    [SerializeField] private float rotateSpeed;
    public float elongationSpeed;
    public float maxScale;
    public float moveSpeed;
    public float width; //无法根据spriteRender得出sprite的宽度，故直接在inspect中进行调整到合适值

    private float _height;
    private float _originalMass;
    private float _originalGravity;
    private float _moveSpeed;
    private float _originalRotationSpeed;

    private Rigidbody2D _rg;
    private HingeJoint2D _joint;

    private float _anchorMoveAxis;

    private CustomCollider2D _collider;

    private PhysicsShapeGroup2D _shapeGroup = new PhysicsShapeGroup2D();

    private float _colliderHeight;
    private SpriteRenderer _spriteRenderer;
    private PlayerInput _playerInput;
    private Vector2 _formMousePosition;

    private bool _isTipsBlock;

    // private bool _isInserted;
    private float _insertAngle;

    // private bool _isGetTargetAngle;
    private bool _hasCallPlayerAboutInsert;
    
    private bool _isGamepadActive = true;
    private Vector2 _lastMousePosition;

    private Vector2 _lastValidStickDirection; // 添加这个变量来保存最后的有效方向

    private float _lastValidRotation = float.MinValue;

   private void Awake()
   {
       if (instance == null)
       {
           instance = this;
       }
       else
       {
           Destroy(gameObject);
       }

       _rg = GetComponent<Rigidbody2D>();
       _rg.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
       _spriteRenderer = GetComponent<SpriteRenderer>();
       _joint = GetComponent<HingeJoint2D>();
       _collider = GetComponent<CustomCollider2D>();
       _height = _spriteRenderer.bounds.size.y;
       _originalMass = _rg.mass;
       _originalRotationSpeed = rotateSpeed;
       _originalGravity = _rg.gravityScale;
       _colliderHeight = _height;
       UpdateCollider();
       _playerInput = new PlayerInput();
       
       _lastMousePosition = Input.mousePosition;
   }


   private void OnEnable()
   {
       _playerInput.Enable();
       EquipJinGuBang();
   }

   private void OnDisable()
   {
       _playerInput.Disable();
       PlayController.instance.isEquipJinGuBang = false;
   }

   private void Update()
   {
       _anchorMoveAxis = _playerInput.GamePLay.AnchorMove.ReadValue<float>();
       
       TipsCheck();
       UpdateInputDevice();
       UpdateCursorVisibility();

       
        if (_playerInput.GamePLay.Unload.IsPressed()&&_joint.enabled)
        {
           UnloadJinGuBang();
        }
       

        if (_joint.enabled)
        {
            ChangeSpeedWithScaleAndAnchorPosition();
        }
        
        
        if (_playerInput.GamePLay.Elongation.IsPressed())
        {
            Elongation();
        }
        else if (_playerInput.GamePLay.Shorten.IsPressed())
        {
            Shorten();
        }
   }

   private void UpdateInputDevice()
   {
       // 检查鼠标位置变化
       Vector2 currentMousePosition = Input.mousePosition;
       bool hasMouseMovement = Vector2.Distance(currentMousePosition, _lastMousePosition) > 0.1f;
       _lastMousePosition = currentMousePosition;

       // 检查手柄右摇杆输入
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

       // 更新输入状态
       if (hasGamepadInput && !_isGamepadActive)
       {
           _isGamepadActive = true; // 切换到手柄模式
       }
       else if (hasMouseMovement && _isGamepadActive)
       {
           _isGamepadActive = false; // 切换到鼠标模式
       }
   }

   private void FixedUpdate()
   {
       if (_joint.enabled)
       {
           AnchorMove(); 
           RotateJinGuBang();
       }
       
      
   }
   
   private void ChangeSpeedWithScaleAndAnchorPosition()
   {
       _moveSpeed=moveSpeed*_colliderHeight/_height;
       rotateSpeed = _originalRotationSpeed - _colliderHeight*10.0f;
   }


   private void AnchorMove()
   {
       _joint.anchor += new Vector2(0, _anchorMoveAxis * Time.fixedDeltaTime * _moveSpeed);

       if (Mathf.Abs(_joint.anchor.y) >= _colliderHeight)
       {
           _joint.anchor = new Vector2(0, _colliderHeight);
       }
       else if (_joint.anchor.y <= 0)
       {
           _joint.anchor = new Vector2(0, 0);
       }
       
   }


   private void RotateJinGuBang()
   {
       if (_isGamepadActive)
       {
           // 手柄输入 - 获取右摇杆输入
           Vector2 stickValue = Vector2.zero;
           foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
           {
               if (device is UnityEngine.InputSystem.Gamepad gamepad)
               {
                   stickValue = gamepad.rightStick.ReadValue();
                   break;
               }
           }
           
           if (stickValue.magnitude > 0.1f)
           {
               // 根据摇杆的x值决定旋转方向和速度
               float rotationDirection = -stickValue.x;
               float rotationAmount = rotationDirection * rotateSpeed * Time.fixedDeltaTime; 
               
               // 直接添加旋转角度
               float newAngle = _rg.rotation + rotationAmount;
               _rg.MoveRotation(newAngle);
               _lastValidRotation = newAngle;
           }
           else if (_lastValidRotation != float.MinValue)
           {
               // 使用 MoveRotation 平滑地保持最后的有效角度
               var step = rotateSpeed * Time.fixedDeltaTime;
               var newAngle = Mathf.MoveTowardsAngle(_rg.rotation, _lastValidRotation, step);
               _rg.MoveRotation(newAngle);
           }
       }
       else
       {
           // 鼠标输入 - 使用鼠标位置
           Vector2 mousePos = _playerInput.GamePLay.JinGuBangDir.ReadValue<Vector2>();
           var mousePosition = Camera.main.ScreenToWorldPoint(mousePos);
           mousePosition.z = 0;
           var direction = (mousePosition - transform.position).normalized;
           float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f;

           var step = rotateSpeed * Time.fixedDeltaTime;
           var newAngle = Mathf.MoveTowardsAngle(_rg.rotation, angle, step);
           _rg.MoveRotation(newAngle);
       }
   }
   


   private void TipsCheck()
   {
       var upPos = transform.up * (_colliderHeight + 0.2f) + transform.position;
       var downPos = transform.position - transform.up * 0.2f;

       var upCheck = Physics2D.OverlapCircle(upPos, width / 3, platformMask);
       var downCheck = Physics2D.OverlapCircle(downPos, width / 1.5f, platformMask);

       if (upCheck && downCheck)
       {
           _isTipsBlock = true;
       }
       else
       {
           _isTipsBlock = false;
       }
      
   }

   private void Elongation()
   {
       var temHeight = _colliderHeight + elongationSpeed * Time.deltaTime;

       if (_isTipsBlock || temHeight >= maxScale)
       {
           return;
       }

       float oldHeight = _colliderHeight;
       _colliderHeight += elongationSpeed * Time.deltaTime;

       // 预检测
       RaycastHit2D hit = Physics2D.Raycast(transform.position + transform.up * oldHeight, transform.up, _colliderHeight - oldHeight + 0.1f, platformMask);
       if (hit.collider != null)
       {
           // 调整伸长距离
           _colliderHeight = hit.distance + oldHeight - 0.05f; // 留出一点空间
       }

       ApplyElongationForce(oldHeight);
       UpdateCollider();
   }

   private void Shorten()
   {
       //缩短
       var temHeight = _colliderHeight - elongationSpeed * Time.deltaTime;

       if (temHeight > _height)
       {
           _colliderHeight -= elongationSpeed * Time.deltaTime;
           UpdateCollider();
       }
   }

   private void UpdateCollider()
   {
       _shapeGroup.Clear();
       _shapeGroup.AddCapsule(new Vector2(0, 0), new Vector2(0, _colliderHeight), width / 2);
       _collider.SetCustomShapes(_shapeGroup);
       Physics2D.SyncTransforms();
       _spriteRenderer.size = new Vector2(_spriteRenderer.size.x, _colliderHeight);
   }

   private void UnloadJinGuBang()
   {
       _joint.enabled = false;
       _rg.mass = 10.0f;
       _rg.gravityScale = 3.0f;
       PlayController.instance.UnloadJinGuBangPlayerMove();
       transform.parent = null;

       PlayController.instance.isOnJinGuBang = false;
       PlayController.instance.isTakingJinGuBang = false;
       
       // 移除原有的延迟恢复碰撞代码
       // StartCoroutine(DelayStartCollisionWithPlayer());
   }

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
       
       // 初始化最后有效旋转为当前旋转
       _lastValidRotation = _rg.rotation;

       Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("JinGuBang"),true);

       PlayController.instance.isEquipJinGuBang = true;
       PlayController.instance.isTakingJinGuBang = true;

       // 移除这里的光标显示控制
   }

   private void OnDrawGizmos()
   {
       Gizmos.DrawWireSphere(transform.up * (_colliderHeight + 0.2f) + transform.position, width / 3);
       Gizmos.DrawWireSphere(transform.position - transform.up * +0.2f, width / 1.5f);
   }

   public void DisableControl()
   {
       _playerInput.Disable();
       _rg.freezeRotation = true;
   }

   public void EnableControl()
   {
       _playerInput.Enable();
       _rg.freezeRotation = false;
   }

   // 常量定义
   private const int RAY_COUNT = 5;
   private const float EDGE_OFFSET = 0.1f;
   private const float RAY_LENGTH_EXTRA = 0.1f;
   private const float FORCE_MULTIPLIER = 2.0f;

   private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[5]; // 预分配数组

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

           Debug.DrawRay(currentRayStart, rayDirection, Color.red, 0.3f);

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

                   Debug.DrawLine(currentRayStart, hit.point, Color.green, 0.3f);
               }
           }
       }
   }

   // 新增方法，统一管理光标显示
   private void UpdateCursorVisibility()
   {
       if (_joint.enabled) // 只在装备金箍棒时控制光标
       {
           Cursor.visible = !_isGamepadActive;
       }
   }
}

 
