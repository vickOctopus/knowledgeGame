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
    public LayerMask platformMask;
    public float originalRotateSpeed;
    [SerializeField] private float _rotateSpeed;
    public float elongationSpeed;
    public float maxScale;
    public float moveSpeed;
    public float width; //无法根据spriteRender得出sprite的宽度，故直接在inspect中进行调整到合适值

    private float _height;
    private float _originalMass;
    private float _originalGravity;
    private float _moveSpeed;

    private Rigidbody2D _rg;
    private HingeJoint2D _joint;

    private float _anchorMoveAxis;

    private CustomCollider2D _collider;

    private PhysicsShapeGroup2D _shapeGroup = new PhysicsShapeGroup2D();

    private float _colliderHeight;
    private SpriteRenderer _spriteRenderer;
    private PlayerInput _playerInput;
   
   private void Awake()
   {
       _rg = GetComponent<Rigidbody2D>();
       _spriteRenderer = GetComponent<SpriteRenderer>();
       _joint = GetComponent<HingeJoint2D>(); 
       _collider = GetComponent<CustomCollider2D>();
       _height = _spriteRenderer.bounds.size.y;
       _originalMass=_rg.mass;
       _originalGravity = _rg.gravityScale;
       _colliderHeight = _height;
       UpdateCollider();
       _playerInput=new PlayerInput();
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

   private void FixedUpdate()
   {
       if (_joint.enabled)
       {
           AnchorMove();
           
            FollowMouseRotate();
       }
      
   }
   
   void ChangeSpeedWithScaleAndAnchorPosition()
   {
       _rotateSpeed = originalRotateSpeed  * (_joint.anchor.y+_colliderHeight);
       _moveSpeed=moveSpeed*_colliderHeight/_height;
   }


   void AnchorMove()
   {
       
       _joint.anchor+=new Vector2(0,_anchorMoveAxis*Time.fixedDeltaTime*_moveSpeed);
       
       
       if (Mathf.Abs(_joint.anchor.y)>=_colliderHeight)
       {
            _joint.anchor = new Vector2(0, _colliderHeight);
       }
       else if (_joint.anchor.y <= 0)
       {
           _joint.anchor = new Vector2(0, 0);
       }
   }
   
   private void FollowMouseRotate()
   {
       var gameObjectToMouse = _playerInput.GamePLay.JinGuBangDir.ReadValue<Vector2>() - new Vector2(Camera.main.WorldToScreenPoint(transform.position).x, Camera.main.WorldToScreenPoint(transform.position).y);

       var targetAngle= wrapAngleAroundZero(Mathf.Atan2(gameObjectToMouse.y, gameObjectToMouse.x) * Mathf.Rad2Deg-90.0f);
       
       // Debug.Log(targetAngle);
       
       var currentAngle = wrapAngleAroundZero(transform.eulerAngles.z);
       
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
   }

    private void Elongation()
   {
        var temHeight = _colliderHeight + elongationSpeed * Time.deltaTime;
       
        var upPos=transform.up*(temHeight+0.2f)+transform.position;
        var downPos = transform.position - transform.up * 0.2f;

      

        if (!Physics2D.OverlapCircle(upPos, width / 3, platformMask) ||
            !Physics2D.OverlapCircle(downPos, width / 3, platformMask))
        {
            if (temHeight <= maxScale)
            {
                _colliderHeight += elongationSpeed * Time.deltaTime;
                UpdateCollider();
               
            }
        }
       
       
   }

    void Shorten()
   {
       //缩短
      var temHeight = _colliderHeight - elongationSpeed*Time.deltaTime;
      
      if (temHeight>_height)
      {
          _colliderHeight-=elongationSpeed*Time.deltaTime;
         UpdateCollider();
         
      }
       
   }


   private void UpdateCollider()
    {
        _shapeGroup.Clear();
        _shapeGroup.AddCapsule(new Vector2(0,_colliderHeight),new Vector2(0,0),width/2);
        _collider.SetCustomShapes(_shapeGroup);
        _spriteRenderer.size=new Vector2(_spriteRenderer.size.x,_colliderHeight);
    }
    
    void UnloadJinGuBang()
    {
        _joint.enabled=false;
        _rg.mass = 10.0f;
        _rg.gravityScale = 1.0f;
        PlayController.instance.UnloadJinGuBangPlayerMove();

        StartCoroutine(DelayStartCollisionWithPlayer());

        Cursor.visible = false;
    }

    void EquipJinGuBang()
    {
        _rg.mass = _originalMass; 
        _rg.gravityScale = _originalGravity;
        _colliderHeight = _height;
        UpdateCollider();
        transform.localPosition = new Vector2(0f,0.5f);
        _joint.enabled = true;
        _joint.anchor = Vector2.zero;
        _joint.connectedAnchor = new Vector2(0f, 0.5f);
       
        Physics2D.IgnoreLayerCollision(3,7,true);
       
        PlayController.instance.isEquipJinGuBang = true;
        
        Cursor.visible = true;
    }

    private void OnDrawGizmos()
    {
       Gizmos.DrawWireSphere(transform.up*(_colliderHeight+0.2f)+transform.position,width/3);
       Gizmos.DrawWireSphere(transform.position-transform.up*+0.2f,width/3);
        
    }

    private IEnumerator DelayStartCollisionWithPlayer()
    {
        yield return new WaitForSeconds(0.3f);
        Physics2D.IgnoreLayerCollision(3,7,false);
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
}

 