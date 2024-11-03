using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Cursor = UnityEngine.Cursor;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using System.Threading.Tasks;

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
    // private CapsuleCollider2D _capsuleCollider;
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
    [SerializeField] private Vector2 groundCheckOffset;
    [SerializeField] private Vector2 groundCheckSize;
    [SerializeField] private Vector2 rollingCheckOffset;
    [SerializeField] private Vector2 rollingCheckSize;
    public LayerMask canJumpLayer;
    
    [Header("Ladder")]
    [SerializeField] private float ladderSpeed;
    [SerializeField] private Vector2 ladderCheckOffset;
    [SerializeField] private Vector2 ladderCheckSize = new Vector2(0.8f, 1f);
    [SerializeField] private LayerMask ladderLayer;
    private readonly Collider2D[] _ladderCheckResults = new Collider2D[1];
    private ContactFilter2D _ladderContactFilter;
    
    [Header("Arm")]
    [SerializeField]private LayerMask jinGuBangLayer;
    public GameObject jinGuBang;
    private bool _underWater;

    #endregion

    private float _lastGroundCheckTime;
    private const float GROUND_CHECK_INTERVAL = 0.1f; // 检测间隔
    private readonly Collider2D[] _groundCheckResults = new Collider2D[1];
    private ContactFilter2D _groundContactFilter;

    private bool _checkingLadder = false;

    private Coroutine _respawnCoroutine;

    [HideInInspector] public bool isVerticalOnJinGuBang = false;  // 标记是否在金箍棒竖直状态
    private float _originalPlayerMass;
    private float _originalPlayerGravity;

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
        _circleCollider = GetComponent<CircleCollider2D>();
        // _capsuleCollider = GetComponent<CapsuleCollider2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _animator = GetComponent<Animator>();
        _playerInput = new PlayerInput();
        
        currentHp = maxHp;
        gameObject.SetActive(false);

        _originalPlayerMass = _rg.mass;
        _originalPlayerGravity = _rg.gravityScale;
    }

    private void OnEnable()
    {
        _playerInput.Enable();
        
        if (_respawnCoroutine == null)
        {
            _respawnCoroutine = StartCoroutine(RespawnPositionRecord());
        }
    }

    private void OnDisable()
    {
        _playerInput.Disable();
        
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }
    }

    private void Start()
    {
        _gravityScale = _rg.gravityScale;
        _respawnPosition = transform.position;

        Physics2D.IgnoreCollision(_boxCollider, _circleCollider, true);

        _respawnCoroutine = StartCoroutine(RespawnPositionRecord());

        SpawnJinGuBang();
        jinGuBang.SetActive(false);

        // _capsuleCollider.enabled = true;
        _circleCollider.enabled = false;

        _groundContactFilter = new ContactFilter2D();
        _groundContactFilter.SetLayerMask(canJumpLayer);
        _groundContactFilter.useLayerMask = true;

        _ladderContactFilter = new ContactFilter2D();
        _ladderContactFilter.SetLayerMask(1 << LayerMask.NameToLayer("Ladder"));
        _ladderContactFilter.useLayerMask = true;
    }

    private void Update()
    {
        _horizontalMove = _playerInput.GamePLay.Move.ReadValue<Vector2>().x;
        _verticalMove = _playerInput.GamePLay.Move.ReadValue<Vector2>().y;

        // 只在有垂直输入或已经在梯子上时检测
        if (Mathf.Abs(_verticalMove) > 0.9)
        {
            _checkingLadder = true;
        }
        
        if (_checkingLadder)
        {
            CheckLadder();
        }

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
        
        if (_rg.velocity.y<=maxFallVelocity)
        {
            _rg.velocity = new Vector2(_rg.velocity.x, maxFallVelocity);
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
        _boxCollider.enabled = false;
        // _capsuleCollider.enabled = false;
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
        Vector2 checkPosition = (Vector2)transform.position + rollingCheckOffset;
        var tem = Physics2D.OverlapBox(checkPosition, rollingCheckSize, 0.0f,
            LayerMask.GetMask("Platform", "MovePlatform", "JinGuBang"));

        if (tem)
        {
            return;
        }

        _boxCollider.enabled = true;
        // _capsuleCollider.enabled = true;
        _circleCollider.enabled = false;
        _isRolling = false;
        _animator.SetBool(_rollHash, _isRolling);
        _currentState = PlayerState.Running;
    }

    private void HandleClimbState()
    {
        if (!_isOnLadder)
        {
            _currentState = PlayerState.Idle;
            return;
        }

        _rg.velocity = new Vector2(_horizontalMove * moveSpeed * 0.2f, _verticalMove * ladderSpeed);
        _rg.gravityScale = 0.0f;
        
        if (_playerInput.GamePLay.Jump.triggered)
        {
            LeftLadder();
            _currentState = PlayerState.Jumping;
        }
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
        
        else if (_isOnLadder && Mathf.Abs(_verticalMove) > 0)
        {
            _currentState = PlayerState.Climbing;
        }
        
        else if (Mathf.Abs(_horizontalMove) >= 0.1f||Mathf.Abs(_rg.velocity.x) >= 0.01f)
        {
            _currentState = PlayerState.Running;
        }
        
        else if (_playerInput.GamePLay.Jump.triggered)
        {
            _currentState = PlayerState.Jumping;
        }
        
        else if (_playerInput.GamePLay.Roll.triggered&&_canRoll)
        {
            _currentState = PlayerState.Rolling;
        }
        
      
    }


    #region Ladder

    private void CheckLadder()
    {
        Vector2 checkPosition = (Vector2)transform.position + ladderCheckOffset;
        Collider2D hitCollider = Physics2D.OverlapBox(checkPosition, ladderCheckSize, 0f, 1 << LayerMask.NameToLayer("Ladder"));
        
        if (hitCollider != null)
        {
            if (!_isRolling)
            {
                if (!_isOnLadder && Mathf.Abs(_verticalMove) > 0)
                {
                    OnLadder();
                    _currentState = PlayerState.Climbing;
                }
                else if (_isOnLadder && _isGrounded && Mathf.Abs(_verticalMove) < 0.1f)
                {
                    LeftLadder();
                }
            }
        }
        else if (_isOnLadder)
        {
            LeftLadder();
        }
    }

    private void OnLadder()
    {
        _isOnLadder = true;
        _animator.SetBool(_climbHash, _isOnLadder);
        _rg.gravityScale = 0;
        EventManager.instance.ClimbLadder();
        if (jinGuBang != null && isTakingJinGuBang)
        {
             jinGuBang.SetActive(false);
        }
    }

    public void LeftLadder()
    {
        _isOnLadder = false;
        _animator.SetBool(_climbHash, _isOnLadder);
        _rg.gravityScale = _gravityScale;
        EventManager.instance.LeftLadder();
        _checkingLadder = false; // 离开梯子时停止检测
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
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
        
        int hitCount = Physics2D.OverlapBox(checkPosition, groundCheckSize, 0f, _groundContactFilter, _groundCheckResults);
        
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
        Vector3 position = transform.position;
        Gizmos.DrawWireCube((Vector2)position + groundCheckOffset, groundCheckSize);
        Gizmos.DrawWireCube((Vector2)position + rollingCheckOffset, rollingCheckSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)position + ladderCheckOffset, ladderCheckSize);
    }

        #endregion

    #region Respawn

    private IEnumerator RespawnPositionRecord()
    {
        while (true)
        {
            var tem = Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Platform"));

            if (tem)
            {
                if (_isGrounded && !_underWater && !tem.collider.CompareTag("Floating Objects") && _canBeDamaged)
                {
                    _respawnPosition = transform.position;
                }
            }
            
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void RetreatToSafePosition(float retreatTime)
    {
        _underWater = true;
        isOnJinGuBang = false;
        _animator.SetBool(_underwaterHash,_underWater);
        if (isTakingJinGuBang)
        {
             jinGuBang.SetActive(false);
        }
       
        StartCoroutine(RetreatTimer(retreatTime));
    }

    private IEnumerator RetreatTimer(float time)
    {
        yield return new WaitForSeconds(time);
        transform.position = _respawnPosition;
        _underWater = false;
        _animator.SetBool(_underwaterHash,_underWater);
    }

    #endregion

    #region JinGuBang

    public void SpawnJinGuBang()
    {
        if (!Application.isEditor)
        {
             if (!PlayerPrefs.HasKey("HasJinGuBang")) return;
        }
        
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
        // jinGuBang.transform.parent = null;
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
            
            int count = _boxCollider.OverlapCollider(filter, results);
            
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

            if (currentHp <= 0)
            {
                PlayerDead();
            }
            else
            {
                RetreatToSafePosition(0.3f);
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

    public async void PlayerDead()
    {
        DisableControl(); // 禁用控制
        await PlayerDeadCoroutine();
    }

    private async Task PlayerDeadCoroutine()
    {
        await Task.Delay(500);
        
        try 
        {
            await SaveManager.instance.HandlePlayerRespawn();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during respawn: {e.Message}");
        }
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
        
        // 只有当金箍棒存在且已初始化时才禁用其控制
        if (jinGuBang != null && jinGuBang.activeInHierarchy && jinGuBang.GetComponent<JinGuBang>() != null)
        {
            if (Application.isEditor)
            {
                jinGuBang.GetComponent<JinGuBang>()?.DisableControl();
            }
            else
            {
                if (PlayerPrefs.GetInt("HasJinGuBang") == 1)
                {
                    jinGuBang.GetComponent<JinGuBang>()?.DisableControl();
                }
            }
        }
    }

    public void EnableControl()
    {
        _playerInput.Enable();
        
        if (jinGuBang != null && jinGuBang.activeInHierarchy && jinGuBang.GetComponent<JinGuBang>() != null)
        {
            if (Application.isEditor)
            {
                jinGuBang.GetComponent<JinGuBang>()?.EnableControl();
            }
            else
            {
                if (PlayerPrefs.GetInt("HasJinGuBang") == 1)
                {
                    jinGuBang.GetComponent<JinGuBang>()?.EnableControl();
                }
            }
        }
    }

    private void UpdateIsOnJinGuBang()
    {
        isOnJinGuBang = (isEquipJinGuBang && !_isGrounded) || isVerticalOnJinGuBang;
    }

    // 添加这个方法用于兼容性
    public void Respawn(float respawnTime)
    {
        // 调用新的方法
        RetreatToSafePosition(respawnTime);
    }

    // 新：调整玩家在金箍棒竖直状态时的物理属性
    public void AdjustForVerticalJinGuBang(bool isVertical)
    {
        if (isVertical)
        {
            _rg.mass = _originalPlayerMass * 0.05f;       // 保持很小的质量
            _rg.gravityScale = 0f;                        // 完全禁用重力
            
            float currentVelocityY = _rg.velocity.y;
            float clampedVelocityY = Mathf.Clamp(currentVelocityY, -2f, 0.5f);
            _rg.velocity = new Vector2(_rg.velocity.x * 0.3f, clampedVelocityY);
        }
        else
        {
            _rg.mass = _originalPlayerMass;
            _rg.gravityScale = _originalPlayerGravity;
        }
        isVerticalOnJinGuBang = isVertical;
    }

    // 添加公共方法来调整重力
    public void SetGravityScale(float scale)
    {
        _rg.gravityScale = _originalPlayerGravity * scale;
    }
}

