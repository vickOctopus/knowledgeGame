using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

public class JinGuBang : MonoBehaviour
{
    public static JinGuBang instance;
    
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

       // if (Input.GetKeyDown(KeyCode.J))
       // {
       //     _rg.MoveRotation(90);
       // }
       
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


        // if (!_hasCallPlayerAboutInsert&&_isGetTargetAngle)
        // {
        //     _rg.constraints = RigidbodyConstraints2D.FreezePositionY;
        //      _hasCallPlayerAboutInsert = true;
        // }
   }

   private void FixedUpdate()
   {
       if (_joint.enabled)
       {
           AnchorMove(); 
           RotateJinGuBang();
       }

       /*if (_isInserted)
       {
           if (!_isGetTargetAngle)
           {
               RotateToTarget();
           }
           else if (!_hasCallPlayerAboutInsert)
           {
               _rg.constraints = RigidbodyConstraints2D.FreezePositionY;
               _hasCallPlayerAboutInsert = true;
           }
       }*/
       
      
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
       else
       {
           PlayController.instance.isOnJinGuBang = true;
       }
   }


   private void RotateJinGuBang()
   {
       // 获取鼠标在世界中的位置
       var mousePosition = Camera.main.ScreenToWorldPoint(_playerInput.GamePLay.JinGuBangDir.ReadValue<Vector2>());
       mousePosition.z = 0; // 确保z轴为0，以适应2D场景

       // 计算方向
       var direction = (mousePosition - transform.position).normalized;

       // 计算目标旋转
       var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg-90.0f;

       // 平滑旋转
       var step = rotateSpeed * Time.fixedDeltaTime; // 每帧的旋转步长
       var newAngle = Mathf.MoveTowardsAngle(_rg.rotation, angle, step);
    
       // 使用 MoveRotation 方法进行旋转
       _rg.MoveRotation(newAngle);
   }
   
   
   /*private void FollowMouseRotate()
   {
       
       var gameObjectToMouse = _playerInput.GamePLay.JinGuBangDir.ReadValue<Vector2>() - new Vector2(
           Camera.main.WorldToScreenPoint(transform.position).x, Camera.main.WorldToScreenPoint(transform.position).y);


       var targetAngle =
           wrapAngleAroundZero(Mathf.Atan2(gameObjectToMouse.y, gameObjectToMouse.x) * Mathf.Rad2Deg - 90.0f);


       var currentAngle = wrapAngleAroundZero(transform.eulerAngles.z);

       if (Mathf.Abs(targetAngle-currentAngle)<=0.3f)
       {
           return;
       }

       Debug.Log(Mathf.Abs(targetAngle-currentAngle));
       
        var torque = _rotateSpeed * Time.fixedDeltaTime;
        // I have no idea what this actually is or what to call it, but it works...
        var angularDelta = torque / _rg.inertia;
        
        // How long would it take us to stop? We need this in case we should actually be slamming on the brakes.
        var timeRequiredToStop = Mathf.Abs(_rg.angularVelocity / angularDelta * Time.fixedDeltaTime);

        // Which direction should we go? Depends on which way is faster.
        // This doesn't factor in current speed or direction, but eh, close enough.
        var timeUntilDestinationReachedCCW = (targetAngle - currentAngle) / Mathf.Abs(_rg.angularVelocity);
        var timeUntilDestinationReachedCW = -timeUntilDestinationReachedCCW;
        
        var circleRotationTime = 360 / Mathf.Abs(_rg.angularVelocity);
        if (timeUntilDestinationReachedCCW < 0) timeUntilDestinationReachedCCW += circleRotationTime;
        if (timeUntilDestinationReachedCW < 0) timeUntilDestinationReachedCW += circleRotationTime;
        var timeUntilDestination = Mathf.Min(timeUntilDestinationReachedCCW, timeUntilDestinationReachedCW);

        if (timeRequiredToStop > timeUntilDestination) 
        {
            _rg.AddTorque(-1 * Mathf.Sign(_rg.angularVelocity) * torque);
        } 
        else if (timeUntilDestinationReachedCW < timeUntilDestinationReachedCCW) 
        {
            _rg.AddTorque(-torque);
        } else {
            _rg.AddTorque(torque);
        }
        
   }
   
   private static float wrapAngleAroundZero(float a) 
   {
       if (a >= 0) {
           float rotation = a % 360;
           if (rotation > 180) rotation -= 360;
           return rotation;
       } else {
           float rotation = -a % 360;
           if (rotation > 180) rotation -= 360;
           return -rotation;
       }
   }*/


   private void TipsCheck()
   {
       var upPos=transform.up*(_colliderHeight+0.2f)+transform.position;
       var downPos = transform.position - transform.up * 0.2f;

       var upCheck = Physics2D.OverlapCircle(upPos, width / 3, platformMask);
       var downCheck = Physics2D.OverlapCircle(downPos, width / 1.5f, platformMask);

       if (upCheck&&downCheck)
       {
           _isTipsBlock = true;
       }
       else if (upCheck || downCheck)
       {
           PlayController.instance.isOnJinGuBang = true;
           _isTipsBlock = false;
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

       _colliderHeight += elongationSpeed * Time.deltaTime;
       PlayController.instance.isOnJinGuBang = true;
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
       _shapeGroup.AddCapsule(new Vector2(0, _colliderHeight), new Vector2(0, 0), width / 2);
       _collider.SetCustomShapes(_shapeGroup);
       Physics2D.SyncTransforms();
       _spriteRenderer.size = new Vector2(_spriteRenderer.size.x, _colliderHeight);
   }

   private void UnloadJinGuBang()
   {
       _joint.enabled = false;
       _rg.mass = 10.0f;
       _rg.gravityScale = 1.0f;
       PlayController.instance.UnloadJinGuBangPlayerMove();

       StartCoroutine(DelayStartCollisionWithPlayer());

       Cursor.visible = false;

       PlayController.instance.isOnJinGuBang = false;
   }

   private void EquipJinGuBang()
   {
       rotateSpeed = _originalRotationSpeed;
       _rg.mass = _originalMass;
       _rg.gravityScale = _originalGravity;
       _colliderHeight = _height;
       UpdateCollider();
       transform.localPosition = new Vector2(0f, 0.5f);
       _joint.enabled = true;
       _joint.anchor = new Vector2(0.0f, 0.3f);
       _joint.connectedAnchor = new Vector2(0f, 0.5f);

       Physics2D.IgnoreLayerCollision(3, 7, true);

       PlayController.instance.isEquipJinGuBang = true;

       Cursor.visible = true;
   }

   private void OnDrawGizmos()
   {
       Gizmos.DrawWireSphere(transform.up * (_colliderHeight + 0.2f) + transform.position, width / 3);
       Gizmos.DrawWireSphere(transform.position - transform.up * +0.2f, width / 1.5f);
   }

   private IEnumerator DelayStartCollisionWithPlayer()
   {
       yield return new WaitForSeconds(0.3f);
       Physics2D.IgnoreLayerCollision(3, 7, false);
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

    // public void IsInsert(float angle)
    // {
    //     _isInserted = true;
    //     _insertAngle = angle;
    // }
    //
    // private void RotateToTarget()
    // {
    //     _rg.MoveRotation(_insertAngle);
    //     
    //     if (Mathf.Abs(transform.rotation.eulerAngles.z-_insertAngle) <=Mathf.Epsilon)
    //     {
    //         _isGetTargetAngle = true;    
    //     }
    // }

    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (!other.CompareTag("InsertableGround"))
    //     {
    //         return;
    //     }
    //
    //     if (Mathf.Abs(transform.rotation.eulerAngles.z-270.0f)<=10.0f)
    //     {
    //         //_rg.constraints = RigidbodyConstraints2D.FreezePositionY;
    //         _rg.MoveRotation(90);
    //         _playerInput.Disable();
    //     }
    // }
}

 