using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockPoolManager : MonoBehaviour,IButton
{
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform despawnAreaCenter;
    [SerializeField] private Vector2 despawnAreaSize = new Vector2(10f, 10f);
    [SerializeField] private Transform accelerationAreaCenter;
    [SerializeField] private Vector2 accelerationAreaSize = new Vector2(5f, 5f);
    
    public enum AccelerationDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    
    [SerializeField] private AccelerationDirection accelerationDirection = AccelerationDirection.Right;
    [SerializeField] private float accelerationForce = 10f;

    private Queue<Rock> rockPool = new Queue<Rock>();
    private List<Rock> activeRocks = new List<Rock>();
    private Coroutine spawnCoroutine;
    private HashSet<Rock> acceleratedRocks = new HashSet<Rock>();

    private Vector2 GetAccelerationVector()
    {
        switch (accelerationDirection)
        {
            case AccelerationDirection.Up:
                return Vector2.up;
            case AccelerationDirection.Down:
                return Vector2.down;
            case AccelerationDirection.Left:
                return Vector2.left;
            case AccelerationDirection.Right:
                return Vector2.right;
            default:
                return Vector2.right;
        }
    }

    private void Start()
    {
        InitializePool();
    }

    private void Update()
    {
        CheckDespawnArea();
        CheckAccelerationArea();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject rockObject = Instantiate(rockPrefab, Vector3.zero, Quaternion.identity, transform);
            Rock rock = rockObject.GetComponent<Rock>();
            rockObject.SetActive(false);
            rockPool.Enqueue(rock);
        }
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnRock();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnRock()
    {
        if (rockPool.Count > 0)
        {
            Rock rock = rockPool.Dequeue();
            rock.transform.position = spawnPoint.position;
            rock.gameObject.SetActive(true);
            activeRocks.Add(rock);
        }
    }

    private void CheckDespawnArea()
    {
        for (int i = activeRocks.Count - 1; i >= 0; i--)
        {
            Rock rock = activeRocks[i];
            if (IsInsideArea(rock.transform.position, despawnAreaCenter.position, despawnAreaSize))
            {
                DespawnRock(rock);
                activeRocks.RemoveAt(i);
            }
        }
    }

    private void CheckAccelerationArea()
    {
        Vector2 accelerationVector = GetAccelerationVector();
        foreach (Rock rock in activeRocks)
        {
            if (IsInsideArea(rock.transform.position, accelerationAreaCenter.position, accelerationAreaSize))
            {
                if (!acceleratedRocks.Contains(rock))
                {
                    Rigidbody2D rb = rock.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.AddForce(accelerationVector * accelerationForce, ForceMode2D.Impulse);
                        acceleratedRocks.Add(rock);
                    }
                }
            }
            else
            {
                acceleratedRocks.Remove(rock);
            }
        }
    }

    private bool IsInsideArea(Vector2 position, Vector2 areaCenter, Vector2 areaSize)
    {
        Vector2 relativePosition = position - areaCenter;
        return Mathf.Abs(relativePosition.x) <= areaSize.x / 2 &&
               Mathf.Abs(relativePosition.y) <= areaSize.y / 2;
    }

    private void DespawnRock(Rock rock)
    {
        rock.gameObject.SetActive(false);
        rockPool.Enqueue(rock);
        acceleratedRocks.Remove(rock);
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);
        }

        if (despawnAreaCenter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(despawnAreaCenter.position, despawnAreaSize);
        }

        if (accelerationAreaCenter != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(accelerationAreaCenter.position, accelerationAreaSize);
            Gizmos.DrawRay(accelerationAreaCenter.position, GetAccelerationVector() * 2);
        }
    }

    public void OnButtonDown()
    {
        StartSpawning();
    }

    public void OnButtonUp()
    {
        StopSpawning();
    }
}
