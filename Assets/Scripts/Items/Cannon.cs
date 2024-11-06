using System.Collections.Generic;
using UnityEngine;
using Items;

public class Cannon : MonoBehaviour
{
    private class PooledBullet
    {
        public GameObject gameObject;
        public Bullet bullet;

        public PooledBullet(GameObject obj, Bullet bulletComponent)
        {
            gameObject = obj;
            bullet = bulletComponent;
        }
    }
    
    public enum ShootDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    
    [Header("发射设置")]
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private ShootDirection direction = ShootDirection.Right;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    
    [Header("对象池设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 5;
    
    [Header("动画设置")]
    [SerializeField] private Animator animator;
    
    private float _nextFireTime;
    private Queue<PooledBullet> _bulletPool;
    private Transform _poolContainer;
    private Vector2 _shootDirection;
    private static readonly int FireTrigger = Animator.StringToHash("fire");
    private PooledBullet _preparedBullet;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateCannonOrientation();
    }

    private void Start()
    {
        InitializeBulletPool();
        _nextFireTime = Time.time + fireInterval;
        UpdateShootDirection();
    }

    private void Update()
    {
        if (Time.time >= _nextFireTime)
        {
            PrepareBullet();
            animator.SetTrigger(FireTrigger);
            _nextFireTime = Time.time + fireInterval;
        }
    }

    private void UpdateShootDirection()
    {
        _shootDirection = direction switch
        {
            ShootDirection.Up => Vector2.up,
            ShootDirection.Down => Vector2.down,
            ShootDirection.Left => Vector2.left,
            ShootDirection.Right => Vector2.right,
            _ => Vector2.right
        };
        
        UpdateCannonOrientation();
    }

    private void UpdateCannonOrientation()
    {
        // 假设大炮默认朝左
        switch (direction)
        {
            case ShootDirection.Up:
                transform.rotation = Quaternion.Euler(0, 0, -90);
                transform.localScale = Vector3.one;
                break;
            case ShootDirection.Down:
                transform.rotation = Quaternion.Euler(0, 0, 90);
                transform.localScale = Vector3.one;
                break;
            case ShootDirection.Left:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                transform.localScale = Vector3.one;
                break;
            case ShootDirection.Right:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                transform.localScale = new Vector3(-1, 1, 1);
                break;
        }
    }

    private void InitializeBulletPool()
    {
        _bulletPool = new Queue<PooledBullet>();
        _poolContainer = new GameObject("BulletPool").transform;
        _poolContainer.SetParent(transform);
        
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewBullet();
        }
    }

    private void CreateNewBullet()
    {
        GameObject bulletObj = Instantiate(bulletPrefab, _poolContainer);
        Bullet bulletComponent = bulletObj.GetComponent<Bullet>();
        bulletObj.SetActive(false);
        _bulletPool.Enqueue(new PooledBullet(bulletObj, bulletComponent));
    }

    private void PrepareBullet()
    {
        if (_bulletPool.Count == 0)
        {
            CreateNewBullet();
        }

        _preparedBullet = _bulletPool.Dequeue();
        
        // 根据方向调整发射点偏移
        Vector2 adjustedSpawnOffset = direction switch
        {
            ShootDirection.Up => new Vector2(spawnOffset.y, -spawnOffset.x),
            ShootDirection.Down => new Vector2(-spawnOffset.y, spawnOffset.x),
            ShootDirection.Left => spawnOffset,
            ShootDirection.Right => new Vector2(-spawnOffset.x, spawnOffset.y),
            _ => spawnOffset
        };
        
        Vector2 spawnPosition = (Vector2)transform.position + adjustedSpawnOffset;
        _preparedBullet.gameObject.transform.position = spawnPosition;
        
        float rotation = direction switch
        {
            ShootDirection.Up => 270f,
            ShootDirection.Down => 90f,
            ShootDirection.Left => 0f,
            ShootDirection.Right => 180f,
            _ => 0f
        };
        _preparedBullet.gameObject.transform.rotation = Quaternion.Euler(0, 0, rotation);
        _preparedBullet.gameObject.SetActive(false);
        
        _bulletPool.Enqueue(_preparedBullet);
    }

    public void FireBullet()
    {
        if (_preparedBullet != null)
        {
            _preparedBullet.gameObject.SetActive(true);
            _preparedBullet.bullet.Launch(_shootDirection, bulletSpeed);
            _preparedBullet = null;
        }
    }

    private void OnValidate()
    {
        // 只在编辑器模式下更新方向
        if (!Application.isPlaying)
        {
            UpdateShootDirection();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 根据方向调整发射点偏移
        Vector2 adjustedSpawnOffset = direction switch
        {
            ShootDirection.Up => new Vector2(spawnOffset.y, -spawnOffset.x),
            ShootDirection.Down => new Vector2(-spawnOffset.y, spawnOffset.x),
            ShootDirection.Left => spawnOffset,
            ShootDirection.Right => new Vector2(-spawnOffset.x, spawnOffset.y),
            _ => spawnOffset
        };
        
        Vector2 spawnPosition = (Vector2)transform.position + adjustedSpawnOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPosition, 0.2f);
        
        Vector2 gizmosDirection = direction switch
        {
            ShootDirection.Up => Vector2.up,
            ShootDirection.Down => Vector2.down,
            ShootDirection.Left => Vector2.left,
            ShootDirection.Right => Vector2.right,
            _ => Vector2.right
        };
        Gizmos.DrawLine(spawnPosition, spawnPosition + gizmosDirection * 2);
    }
} 