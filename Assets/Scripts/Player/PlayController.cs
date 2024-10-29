using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Cursor = UnityEngine.Cursor;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayController : MonoBehaviour,ITakeDamage
{
    private enum PlayerState
    {
        Idle,
        Running,
        Jumping,
        Rolling,
        Climbing,
        Airing,
        Underwater,
        TakingJinGuBang,
    }
    private PlayerState _currentState = PlayerState.Idle;
    
    public static PlayController instance;//单例模式

    #region AnimatorPar

    private readonly int _rollHash = Animator.StringToHash("isRolling");
    private readonly int _climbHash = Animator.StringToHash("onLadder");
    private readonly int _airingHash = Animator.StringToHash("isAir");
    private readonly int _underwaterHash = Animator.StringToHash("underWater");
    private readonly int _velocityXHash = Animator.StringToHash("velocityX");
    private readonly int _velocityYHash = Animator.StringToHash("velocityY");
    private readonly int _onJinGuBangHash = Animator.StringToHash("onJinGuBang");

    #endregion
    
    private float _horizontalMove; 
    private Vector2 _respawnPosition; 
    private Rigidbody2D _rg;
    private float _gravityScale; 
    private bool _isGrounded;
    private float _verticalMove; 
    private bool _isOnLadder; 
    [HideInInspector][FormerlySerializedAs("_currentHp")] public int currentHp;
    [HideInInspector]public bool isTakingJinGuBang=true;
    private Vector2 _archivePosition;
    private SpriteRenderer _spriteRenderer;
    private bool _isRolling;
    private BoxCollider2D _boxCollider;
    private CapsuleCollider2D _capsuleCollider;
    private CircleCollider2D _circleCollider;
    private Animator _animator;
    [HideInInspector]public bool isEquipJinGuBang;
    [HideInInspector]public bool isOnJinGuBang;
    private bool _canBeDamaged=true;
    private PlayerInput _playerInput;
    private bool _canRoll=true;
    
    

    #region InspectorPar

    [Header("Locomotion")]
    [SerializeField]private float moveSpeed;
    [SerializeField]private float jumpForce;
    [SerializeField]private float fallGravityScale;
    [SerializeField]private float maxFallVelocity;
    [Range(0.0f, 5.0f)]
    public float airControl;

    [Header("Property")] 
    [SerializeField]private PlayerData playerData;
    public int maxHp=4;
    
    [Header("Check")]
    [SerializeField]private Transform groundCheckPoint;
    [SerializeField]private LayerMask groundLayer;
    [SerializeField]private Vector2 groundCheckSize;
    [SerializeField]private Transform rollingCheckPoint;
    [SerializeField]private Vector2 rollingCheckSize;
    public LayerMask canJumpLayer;
    
    [Header("Ladder")]
    [SerializeField] private float ladderSpeed;
    
    [Header("Arm")]
    [SerializeField]private LayerMask jinGuBangLayer;
    public GameObject jinGuBang;
    private bool _underWater;

    #endregion

    private float _lastGroundCheckTime;
    private const float GROUND_CHECK_INTERVAL = 0.1f; // 检测间隔
    private readonly Collider2D[] _groundCheckResults = new Collider2D[1];
    private ContactFilter2D _groundContactFilter;

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
       // _boxCollider = GetComponent<BoxCollider2D>();
       _circleCollider = GetComponent<CircleCollider2D>();
       _capsuleCollider = GetComponent<CapsuleCollider2D>();
       _animator = GetComponent<Animator>();
       _playerInput=new PlayerInput();
       
       currentHp = maxHp;
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

        Physics2D.IgnoreCollision(_capsuleCollider, _circleCollider, true);

        StartCoroutine(RespawnPositionRecord());

        SpawnJinGuBang();
        jinGuBang.SetActive(false);

        _capsuleCollider.enabled = true;
        _circleCollider.enabled = false;

        _groundContactFilter = new ContactFilter2D();
        _groundContactFilter.SetLayerMask(canJumpLayer);
        _groundContactFilter.useLayerMask = true;

        // 初始化Chunk
    }

    private void Update()
    {
        _horizontalMove = _playerInput.GamePLay.Move.ReadValue<Vector2>().x;
        _verticalMove = _playerInput.GamePLay.Move.ReadValue<Vector2>().y;

        CheckGround();
        Flip();
        HandleState();
        UpdateAnimator();
        UpdateIsOnJinGuBang();
    }

    private void UpdateAnimator()
    {
        _animator.SetFloat(_velocityXHash, Mathf.Abs(_rg.velocity.x));
        _animator.SetFloat(_velocityYHash, _rg.velocity.y);


        _animator.SetBool(_onJinGuBangHash, isOnJinGuBang);
    }

    #region Locomotion

    private void HandleState()
    {
        switch (_currentState)
        {
            case PlayerState.Idle:
                HandleIdleState();
                break;

            case PlayerState.Running:
                HandleRunState();
                break;

            case PlayerState.Jumping:
                HandleJumpState();
                break;

            case PlayerState.Climbing:
                HandleClimbState();
                break;

            case PlayerState.Rolling:
                HandleRollState();
                break;
            
            case PlayerState.Airing:
                HandleAirState();
                break;
            
            case PlayerState.Underwater:
                HandleUnderwaterState();
                break;
            
            case PlayerState.TakingJinGuBang:
                HandleTakingState();
                break;
        }
    }

    public void HandleTakingState()
    {
        jinGuBang.SetActive(!jinGuBang.activeInHierarchy);
        Cursor.visible = jinGuBang.activeInHierarchy;
        _currentState = PlayerState.Idle;
        isTakingJinGuBang = true;
        
        if (!jinGuBang.activeInHierarchy)
        {
            isOnJinGuBang = false;
        }
    }

    private void HandleUnderwaterState()
    {
       if (!_underWater)
       {
           _currentState = PlayerState.Idle;
       }
    }

    private void HandleAirState()
    {
        if (_underWater)
        {
            _currentState = PlayerState.Underwater;
            return;
        }
        
        if (_isGrounded)
        {
            _currentState = PlayerState.Running;
            return;
        }
        
        if (_isOnLadder)
        {
            _currentState = PlayerState.Climbing;
        }
        
        if (_playerInput.GamePLay.Equip.triggered)
        {
            _currentState = PlayerState.TakingJinGuBang;
        }
        
        if (_rg.velocity.y < -0.1f) //下落时重力变大
        {
            _rg.gravityScale = fallGravityScale * _gravityScale;

            if (_rg.velocity.y<=maxFallVelocity)
            {
                _rg.velocity = new Vector2(_rg.velocity.x, maxFallVelocity);
            }
        }
        else
        {
            _rg.gravityScale = _gravityScale;
        }
        
        
        if (_rg.velocity.x > _horizontalMove * moveSpeed)
        {
            _rg.velocity =
                new Vector2(Mathf.Max(_rg.velocity.x - moveSpeed * airControl * Time.deltaTime, _horizontalMove * moveSpeed),
                    _rg.velocity.y);
        }

        else if (_rg.velocity.x < _horizontalMove * moveSpeed)
        {
            _rg.velocity =
                new Vector2(Mathf.Min(_rg.velocity.x + moveSpeed * airControl * Time.deltaTime, _horizontalMove * moveSpeed),
                    _rg.velocity.y);
        }
    }

    private void HandleRollState()
    {
        _rg.velocity = new Vector2(_horizontalMove * moveSpeed, _rg.velocity.y);

        if (jinGuBang.activeInHierarchy)
        {
            _currentState = PlayerState.Idle;
            return;
        }

        if (_underWater)
        {
            _currentState = PlayerState.Underwater;
            return;
        }

        if (!_isRolling)
        {
            StartRolling();
        }
        else if (!_playerInput.GamePLay.Roll.IsPressed() && _isGrounded)
        {
            EndRolling();
        }
    }

    private void StartRolling()
    {
        // _boxCollider.enabled = false;
        _capsuleCollider.enabled = false;
        _circleCollider.enabled = true;
        _isRolling = true;
        _animator.SetBool(_rollHash, _isRolling);
        if (isEquipJinGuBang)
        {
            jinGuBang.SetActive(false);
        }
    }

    private void EndRolling()
    {
        var tem = Physics2D.OverlapBox(rollingCheckPoint.position, rollingCheckSize, 0.0f,
            LayerMask.GetMask("Platform", "MovePlatform", "JinGuBang"));

        if (tem)
        {
            return;
        }

        // _boxCollider.enabled = true;
        _capsuleCollider.enabled = true;
        _circleCollider.enabled = false;
        _isRolling = false;
        _animator.SetBool(_rollHash, _isRolling);
        _currentState = PlayerState.Running;
    }

    private void HandleClimbState()
    {
        _rg.velocity = new Vector2(_horizontalMove * moveSpeed * 0.2f, _verticalMove * ladderSpeed);
        _rg.gravityScale = 0.0f;
        
        if (_isOnLadder)
        {
            return;
        }
        
        _currentState = PlayerState.Running;
    }

    private void HandleJumpState()
    {
        
        _rg.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        _currentState = PlayerState.Airing;
        
    }

    private void HandleRunState()
    {
        _rg.velocity = new Vector2(_horizontalMove * moveSpeed, _rg.velocity.y);
        
        if (_underWater)
        {
            _currentState = PlayerState.Underwater;
            return;
        }
        
        if (_playerInput.GamePLay.Equip.triggered)
        {
            _currentState = PlayerState.TakingJinGuBang;
        }

        if (!_isGrounded)
        {
            _currentState = PlayerState.Airing;
        }
        
        if (Mathf.Abs(_horizontalMove) <= 0.1f)
        {
            _currentState = PlayerState.Idle;
        }
        
        if (_isGrounded && _playerInput.GamePLay.Jump.triggered)
        {
            _currentState = PlayerState.Jumping;
        }

        if (_playerInput.GamePLay.Roll.triggered)
        {
            _currentState = PlayerState.Rolling;
        }
        
    }

    private void HandleIdleState()
    {
        if (_underWater)
        {
            _currentState = PlayerState.Underwater;
        }
        
        else if (_playerInput.GamePLay.Equip.triggered)
        {
            _currentState = PlayerState.TakingJinGuBang;
        }
        
        else if (!_isGrounded)
        {
            _currentState = PlayerState.Airing;
        }

        else if (Mathf.Abs(_horizontalMove) >= 0.1f||Mathf.Abs(_rg.velocity.x) >= 0.01f)
        {
            _currentState = PlayerState.Running;
        }

        else if (_playerInput.GamePLay.Jump.triggered)
        {
            _currentState = PlayerState.Jumping;
        }
        
        else if (_isOnLadder)
        {
            _currentState = PlayerState.Climbing;
        }
        
        else if (_playerInput.GamePLay.Roll.triggered&&_canRoll)
        {
            _currentState = PlayerState.Rolling;
        }
        
      
    }


    #region Ladder

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Ladder") || _isRolling)
        {
            return;
        }

        // _canRoll = false;
        if (!_isOnLadder)
        {
            if (Mathf.Abs(_verticalMove) > 0) //在地面如果按攀爬键则进入攀爬状态，不然则继续正常移动
            {
                OnLadder();
            }
        }
        else if (_isGrounded && Mathf.Abs(_verticalMove) == 0) //在底部时如果不攀爬则正常移动
        {
            LeftLadder();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Ladder"))
        {
            return;
        }
        
        Debug.Log("Exit");
        LeftLadder(); 
        // Debug.Log("Exit Ladder");
    }

    private void OnLadder()
    {
        _isOnLadder = true;
        // _currentState = PlayerState.Climbing;
        _animator.SetBool(_climbHash, _isOnLadder);
        _rg.gravityScale = 0;
        EventManager.instance.ClimbLadder();
        if (jinGuBang != null&&isTakingJinGuBang)
        {
             jinGuBang.SetActive(false);
        }
       
    }

   public void LeftLadder()
    {
        // _currentState=PlayerState.Idle;
        _isOnLadder = false;
        _animator.SetBool(_climbHash, _isOnLadder);
        _rg.gravityScale = _gravityScale;
        EventManager.instance.LeftLadder();
    }

    #endregion
    private void Flip()
    {
        if (isOnJinGuBang)
        {
            return;
        }
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
        if (Time.time - _lastGroundCheckTime < GROUND_CHECK_INTERVAL)
        {
            return;
        }

        _lastGroundCheckTime = Time.time;

        int hitCount = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, _groundContactFilter, _groundCheckResults);
        
        if (hitCount > 0)
        {
            _isGrounded = !(isEquipJinGuBang && _groundCheckResults[0].gameObject.layer == LayerMask.NameToLayer("JinGuBang"));
        }
        else
        {
            _isGrounded = false;
        }

        _animator.SetBool(_airingHash, !_isGrounded);
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

            var tem = Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Platform"));

            if (tem)
            {
                if (_isGrounded && !_underWater && !tem.collider.CompareTag("Floating Objects")&&_canBeDamaged)
                {
                    //Debug.DrawRay(transform.position,Vector2.down*1.0f,Color.red,1.0f);
                    _respawnPosition = transform.position;
                    //Debug.Log("Respawn position recorded");
                }
            }
            
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void Respawn(float respawnTime)
    {
        DisableControl();
        // _rg.velocity = Vector2.zero;
        _underWater = true;
        isOnJinGuBang = false;
        _animator.SetBool(_underwaterHash,_underWater);
        jinGuBang.SetActive(false);
        StartCoroutine(RespawnTimer(respawnTime));
    }

    private IEnumerator RespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        transform.position = _respawnPosition;
        EnableControl();
       _underWater = false;
       _animator.SetBool(_underwaterHash,_underWater);
    }

    #endregion

    #region JinGuBang

    public void SpawnJinGuBang()
    {
        // if (!PlayerPrefs.HasKey("HasJinGuBang")) return;
        
        jinGuBang = Instantiate(jinGuBang, transform.position, Quaternion.identity);
        jinGuBang.transform.SetParent(this.transform);
        jinGuBang.transform.localPosition = Vector3.zero;
        jinGuBang.GetComponent<HingeJoint2D>().connectedBody = _rg;
        jinGuBang.SetActive(true);
        
        // 忽略玩家和金箍棒之间的碰撞
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), jinGuBang.GetComponent<Collider2D>(), true);
        
    }

    public void UnloadJinGuBangPlayerMove()
    {
        // _rg.velocity += jumpForce * 0.3f * Vector2.up;
        isEquipJinGuBang = false;
        isTakingJinGuBang = false;
        // 直接开始检测碰撞
        StartCoroutine(CheckCollisionRestore());
    }

    private IEnumerator CheckCollisionRestore()
    {
        while (true)
        {
            // 检查玩家和金箍棒是否重叠
            Collider2D[] results = new Collider2D[1];
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("JinGuBang")); // 只检测与金箍棒的重叠
            
            int count = _capsuleCollider.OverlapCollider(filter, results);
            
            if (count == 0) // 如果没有重叠
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("JinGuBang"), false);
                yield break; // 结束协程
            }
            
            yield return new WaitForFixedUpdate();
        }
    }

    #endregion
    
    #region HP

    public void TakeDamage(int damage)
    {
        if (_canBeDamaged)
        {
            _rg.velocity = Vector2.zero;
            _canBeDamaged = false;
            StartCoroutine(CanBeDamaged());
            
            
       
            currentHp -= damage;
           
            HpChange();

            if (currentHp == 0)
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

    public void HpChange()
    {
        GameManager.instance.PlayerHpChange(currentHp);
        //playerData.currentHp=currentHp;
    }

    private void PlayerDead()
    {
        SaveManager.instance.LoadGame();
        currentHp = maxHp;
        HpChange();
    }

    public void Recover(int recoverHp)
    {
        currentHp += recoverHp;
        HpChange();
    }

    #endregion

    public void DisableControl()
    {
        _playerInput.Disable();
        
        
        // if (PlayerPrefs.HasKey("HasJinGuBang"))
        // {
        //     jinGuBang.GetComponent<JinGuBang>().DisableControl();
        // }
        
        jinGuBang.GetComponent<JinGuBang>().DisableControl();
    }

    public void EnableControl()
    {
        _playerInput.Enable();
        
        // if (PlayerPrefs.HasKey("HasJinGuBang"))
        // {
        //     jinGuBang.GetComponent<JinGuBang>().EnableControl();
        // }
        //
        jinGuBang.GetComponent<JinGuBang>().EnableControl();
    }

    private void UpdateIsOnJinGuBang()
    {
        isOnJinGuBang = isEquipJinGuBang && !_isGrounded;
    }

    
}

