using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class PlayController : MonoBehaviour,ITakeDamage
{
    public static PlayController instance;//单例模式
    
    private float _horizontalMove; 
    private Vector2 _respawnPosition; 
   // private bool _isControllable=true; 
    private Rigidbody2D _rg;
    private float _gravityScale; 
    private bool _isGrounded;
    private float _verticalMove; 
    private bool _isOnLadder; 
    private int _currentHp;
    private Vector2 _archivePosition;
    private SpriteRenderer _spriteRenderer;
    private bool _isRolling;
    private BoxCollider2D _boxCollider;
    private CircleCollider2D _circleCollider;
    private Animator _animator;
    [HideInInspector]public bool isEquipJinGuBang;
    private bool _canBeDamaged=true;
    private PlayerInput _playerInput;
    
    [Header("Locomotion")]
    [SerializeField]private float moveSpeed;
    [SerializeField]private float jumpForce;
    [SerializeField]private float fallGravityScale;
    [Range(0.0f, 1.0f)]
    public float airControl;

    [Header("Property")] 
    [SerializeField]private PlayerData playerData;
    [SerializeField] private int MAXhp;
    
    [Header("Check")]
    [SerializeField]private Transform groundCheckPoint;
    [SerializeField]private LayerMask groundLayer;
    [SerializeField]private Vector2 groundCheckSize;
    [SerializeField]private Transform rollingCheckPoint;
    [SerializeField]private Vector2 rollingCheckSize;
    
    [Header("Ladder")]
    [SerializeField] private float ladderSpeed;
    
    [Header("Arm")]
    [SerializeField]private LayerMask jinGuBangLayer;
    public GameObject jinGuBang;
    private bool _underWater;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
        
        _rg = GetComponent<Rigidbody2D>();
       _spriteRenderer = GetComponent<SpriteRenderer>();
       _boxCollider = GetComponent<BoxCollider2D>();
       _circleCollider = GetComponent<CircleCollider2D>();
       _animator = GetComponent<Animator>();
       _playerInput=new PlayerInput();
       
    }

    private void OnEnable()
    {
       _playerInput.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Disable();
    }

    private void Start()
    {
        _gravityScale = _rg.gravityScale;
        _respawnPosition = transform.position;
        
        Physics2D.IgnoreCollision(_boxCollider,_circleCollider,true);
        
        //读取当前血量
        _currentHp = playerData.currentHp;
        GameManager.instance.PlayerHpChange(_currentHp);
        //HpChange();

        StartCoroutine(RespawnPositionRecord()); //启动重生位置检测定时器

        if (playerData.hasGetJinGuBang) //启动游戏时检测是否获得了金箍棒，获得了便生成
        {
            SpawnJinGuBang();
            jinGuBang.SetActive(false);
        }
    }

    private void Update()
    {
        
        _horizontalMove = _playerInput.GamePLay.Move.ReadValue<Vector2>().x;
        _verticalMove = _playerInput.GamePLay.Move.ReadValue<Vector2>().y;

        CheckGround();
        Flip();

        if ( !_isOnLadder&&!_isRolling)
        {
            Jump();
        }

        if (_playerInput.GamePLay.Equip.triggered&&!_isRolling)
        {
            jinGuBang.SetActive(!jinGuBang.activeInHierarchy);
            Cursor.visible=jinGuBang.activeInHierarchy;
        }

        if (!isEquipJinGuBang)
        {
            Rolling();
        }
        
    }

    private void FixedUpdate()
    {
        if (_isOnLadder)
        {
            OnLadderMovement();
        }
        else
        {
            Move();
        }
        
    }

    #region Locomotion

    private void Move()
    {
        if (!_isGrounded)//在空中有加速度
        {
            if (_rg.velocity.x > _horizontalMove * moveSpeed)
            {
                _rg.velocity=new Vector2(Mathf.Max(_rg.velocity.x-moveSpeed*airControl*0.1f,_horizontalMove * moveSpeed),_rg.velocity.y);
            }

            else if (_rg.velocity.x < _horizontalMove * moveSpeed)
            {
                _rg.velocity=new Vector2(Mathf.Min(_rg.velocity.x+moveSpeed*airControl*0.1f,_horizontalMove * moveSpeed),_rg.velocity.y);
            }
            else
            {
                _rg.velocity=new Vector2(_horizontalMove * moveSpeed,_rg.velocity.y);
            }
            
        }
        else//在地面上速度立即改变
        {
            _rg.velocity=new Vector2(_horizontalMove*moveSpeed,_rg.velocity.y);
        }
    }

    private void Jump()
    {
        if (_isGrounded&&_playerInput.GamePLay.Jump.triggered)
        {
            //_respawnPosition=transform.position;//跳跃即重生更新位置
            _rg.AddForce(Vector2.up*jumpForce,ForceMode2D.Impulse);
        }

        if (_rg.velocity.y < -0.1f)//下落时重力变大
        {
            _rg.gravityScale=fallGravityScale*_gravityScale;
        }
        else
        {
            _rg.gravityScale=_gravityScale;
        }
    }

    private void Rolling()
    {
        if (_isGrounded  && _playerInput.GamePLay.Roll.IsPressed())
        {
            if (!_isRolling)
            {
                _boxCollider.enabled = false;
                _isRolling = true;
                _animator.SetBool("isRolling",_isRolling);
                _spriteRenderer.size=new Vector2(_spriteRenderer.size.x*0.7f,_spriteRenderer.size.y*0.7f);
                if (isEquipJinGuBang)
                {
                     jinGuBang.SetActive(false);
                }
               
            }
            
        }
        else if (_isRolling&&_isGrounded)
        {
            var tem=Physics2D.OverlapBox(rollingCheckPoint.position,rollingCheckSize,0.0f,LayerMask.GetMask("Platform","MovePlatform","JinGuBang"));
            if (!tem)
            {
                _boxCollider.enabled = true;
                _isRolling = false;
                _animator.SetBool("isRolling",_isRolling);
                _spriteRenderer.size=new Vector2(_spriteRenderer.size.x/0.7f,_spriteRenderer.size.y/0.7f);
            }
        }
    }
    
    private void Flip()
    {
        if (_rg.velocity.x>=0.1)
        {
            _spriteRenderer.flipX = false;
        }
        else if (_rg.velocity.x<=-0.1)
        {
            _spriteRenderer.flipX = true;
        }
    }

    #endregion

    #region SurroundingCheck

    private void CheckGround()
    {
        //Collider2D groundCheck = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0.0f,groundLayer);
        if (isEquipJinGuBang)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0.0f,LayerMask.GetMask("Platform","MovePlatform","OneWayPlatform")))
            {
                _isGrounded = true;
                _animator.SetBool("isAir",!_isGrounded);
            }
            else
            {
                _isGrounded = false;
                _animator.SetBool("isAir",!_isGrounded);
            }
        }
        else
        {
             if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0.0f,LayerMask.GetMask("Platform","JinGuBang","MovePlatform","OneWayPlatform")))
             {
                 _isGrounded = true;
                 _animator.SetBool("isAir",!_isGrounded);
             }
             else
             {
                 _isGrounded = false;
                 _animator.SetBool("isAir",!_isGrounded);
             }
        }

       
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.DrawWireCube(rollingCheckPoint.position, rollingCheckSize);
    }

        #endregion

    #region Respawn

    private IEnumerator RespawnPositionRecord()
    {
        while (true) //0.2秒更新一次
        {
            if (_isGrounded&&!_underWater&&
                Physics2D.Raycast(transform.position, Vector2.down, 1.0f, LayerMask.GetMask("Platform")))
            {
                //Debug.DrawRay(transform.position,Vector2.down*1.0f,Color.red,1.0f);
                _respawnPosition = transform.position;
                //Debug.Log("Respawn position recorded");
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void Respawn(float respawnTime)
    {
        DisableControl();
        _underWater = true;
        jinGuBang.SetActive(false);
        StartCoroutine(RespawnTimer(respawnTime));
    }

    private IEnumerator RespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        transform.position = _respawnPosition;
        EnableControl();
       _underWater = false;
    }

    #endregion

    #region JinGuBang

    public void SpawnJinGuBang()
    {
        jinGuBang=Instantiate(jinGuBang,transform.position, Quaternion.identity);
        jinGuBang.transform.SetParent(this.transform);
        jinGuBang.transform.localPosition = Vector3.zero;
        jinGuBang.GetComponent<HingeJoint2D>().connectedBody = _rg;
        jinGuBang.SetActive(true);
       
    }

    public void UnloadJinGuBangPlayerMove()
    {
        _rg.velocity+=Vector2.up*jumpForce;
        isEquipJinGuBang=false;
    }

    #endregion

    #region Ladder

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            if (!_isGrounded)
            {
                OnLadder();//在空中就进入攀爬状态
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Ladder")&&!_isOnLadder)
        {
            if (!_isGrounded)//在空中就进入攀爬状态
            {
                OnLadder();
            }
            else if (Mathf.Abs(_verticalMove)>0)//在地面上如果按攀爬键则进入攀爬状态，不然则继续正常移动
            {
                OnLadder();
            }
        }
        else if (_isOnLadder&&other.CompareTag("Ladder")&&_isGrounded&&Mathf.Abs(_verticalMove)==0)//在底部时如果不攀爬则正常移动
        {
            LeftLadder();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            LeftLadder();//离开碰撞盒区域则离开攀爬状态
        }
    }

    void OnLadder()
    {
        _isOnLadder = true;
        _rg.gravityScale = 0;
    }

    void LeftLadder()
    {
        _isOnLadder = false;
        _rg.gravityScale = _gravityScale;
    }
    void OnLadderMovement()
    {
        _rg.velocity = new Vector2(_horizontalMove*moveSpeed*0.2f, _verticalMove*ladderSpeed);
    }

    #endregion
    
    #region HP

    public void TakeDamage(int damage)
    {
        if (_canBeDamaged)
        {
            _canBeDamaged = false;
            StartCoroutine(CanBeDamaged());
            
            _currentHp -= damage;
            HpChange();

            if (_currentHp == 0)
            {
                PlayerDead();
            }
            else
            {
                Respawn(0.3f);
            }
        }
        
    }

    private IEnumerator CanBeDamaged()
    {
        yield return new WaitForSeconds(0.3f);
        _canBeDamaged = true;
    }

    private void HpChange()
    {
        GameManager.instance.PlayerHpChange(_currentHp);
        playerData.currentHp=_currentHp;
    }

    private void PlayerDead()
    {
        transform.position = playerData.respawnPoint;
        _currentHp = playerData.maxHp;
        HpChange();
    }

    public void Recover(int recoverHp)
    {
        _currentHp += recoverHp;
        HpChange();
    }

    #endregion

    public void DisableControl()
    {
        _playerInput.Disable();
        jinGuBang.GetComponent<JinGuBang>().DisableControl();
    }

    public void EnableControl()
    {
        _playerInput.Enable();
        jinGuBang.GetComponent<JinGuBang>().EnableControl();
    }
}
