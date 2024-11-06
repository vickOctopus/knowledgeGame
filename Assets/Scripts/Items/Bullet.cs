using UnityEngine;

namespace Items
{
    public class Bullet : MonoBehaviour
    {
        private bool _isActive;
        private float _speed;
        private Vector2 _direction;
        private bool _isPlayerOnTop;
        private ContactFilter2D _playerContactFilter;
        private readonly Collider2D[] _playerCheckResults = new Collider2D[1];
        private Animator _animator;
        private static readonly int ExplodeTrigger = Animator.StringToHash("explode");
        
        [Header("爆炸设置")]
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private Vector2 explosionOffset = Vector2.zero;
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask explosionLayer;
        
        [Header("玩家检测设置")]
        [SerializeField] private Vector2 playerCheckOffset = Vector2.up;
        [SerializeField] private Vector2 playerCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask playerLayer;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _playerContactFilter = new ContactFilter2D();
            _playerContactFilter.SetLayerMask(playerLayer);
            _playerContactFilter.useLayerMask = true;
        }

        private void OnEnable()
        {
            _isActive = true;
            _isPlayerOnTop = false;
        }

        private void OnDisable()
        {
            _isActive = false;
            if (_isPlayerOnTop && PlayController.instance != null)
            {
                PlayController.instance.transform.SetParent(null);
            }
            _isPlayerOnTop = false;
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;
            
            // 直接使用 Translate 沿局部坐标系的正方向移动
            transform.Translate(Vector3.left * (_speed * Time.fixedDeltaTime), Space.Self);
            
            CheckAndMovePlayer();
        }

        private void CheckAndMovePlayer()
        {
            Vector2 rotatedOffset = GetRotatedOffset(playerCheckOffset);
            Vector2 checkPosition = (Vector2)transform.position + rotatedOffset;
            int count = Physics2D.OverlapBox(checkPosition, playerCheckSize, transform.rotation.eulerAngles.z, _playerContactFilter, _playerCheckResults);
            
            bool wasPlayerOnTop = _isPlayerOnTop;
            bool isPlayerDetected = count > 0 && _playerCheckResults[0] != null && _playerCheckResults[0].CompareTag("Player");

            // 玩家在子弹上方且接地，且不在梯子上
            if (isPlayerDetected && PlayController.instance != null && 
                PlayController.instance.IsGrounded() && !PlayController.instance.IsOnLadder)
            {
                if (!_isPlayerOnTop) // 玩家刚开始站上子弹
                {
                    PlayController.instance.transform.SetParent(transform);
                    _isPlayerOnTop = true;
                }
            }
            // 玩家之前在子弹上，但现在不在了
            else if (_isPlayerOnTop && PlayController.instance != null && PlayController.instance.transform.parent == transform)
            {
                PlayController.instance.transform.SetParent(null);
                _isPlayerOnTop = false;
            }
        }

        public void Launch(Vector2 direction, float speed)
        {
            _speed = speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;
            
            if (((1 << other.gameObject.layer) & explosionLayer) != 0)
            {
                Explode();
            }
        }

        private void Explode()
        {
            _isActive = false;
            
            if (_isPlayerOnTop && PlayController.instance != null)
            {
                PlayController.instance.transform.SetParent(null);
                _isPlayerOnTop = false;
            }
            
            Vector2 rotatedOffset = GetRotatedOffset(explosionOffset);
            Vector2 explosionCenter = (Vector2)transform.position + rotatedOffset;
            
            _animator.SetTrigger(ExplodeTrigger);
            
            Collider2D[] playerColliders = Physics2D.OverlapCircleAll(explosionCenter, explosionRadius, playerLayer);
            foreach (var col in playerColliders)
            {
                if (col.TryGetComponent<ITakeDamage>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        // 由动画事件调用
        public void DisableBullet()
        {
            gameObject.SetActive(false);
        }

        private Vector2 GetRotatedOffset(Vector2 offset)
        {
            float angle = transform.rotation.eulerAngles.z;
            return angle switch
            {
                0f => offset,                                          // 朝左
                180f => new Vector2(-offset.x, offset.y),             // 朝右
                90f => new Vector2(-offset.y, offset.x),              // 朝下
                270f => new Vector2(offset.y, -offset.x),             // 朝上
                _ => offset
            };
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 rotatedExplosionOffset = GetRotatedOffset(explosionOffset);
            Vector2 explosionCenter = (Vector2)transform.position + rotatedExplosionOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(explosionCenter, explosionRadius);
            
            Vector2 rotatedCheckOffset = GetRotatedOffset(playerCheckOffset);
            Vector2 checkPosition = (Vector2)transform.position + rotatedCheckOffset;
            Gizmos.color = Color.blue;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(checkPosition, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.zero, playerCheckSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
} 