using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Rock : MonoBehaviour
{
    private string savePath;
    private Rigidbody2D rb;
    [SerializeField] private float frictionCoefficient = 1f;
    private bool _isInWater = false;

    private const float CHECK_INTERVAL = 0.1f;
    private float lastCheckTime;
    private readonly RaycastHit2D[] raycastResults = new RaycastHit2D[1];
    private ContactFilter2D contactFilter;

    [System.Serializable]
    private class RockPositions
    {
        public List<RockPosition> positions;
    }

    [System.Serializable]
    private class RockPosition
    {
        public string name;
        public Vector3 position;

        public RockPosition(string name, Vector3 position)
        {
            this.name = name;
            this.position = position;
        }
    }

    void Start()
    {
        if (!Application.isEditor)
        {
            savePath = System.IO.Path.Combine(Application.persistentDataPath, "rockPositions.json");
            LoadPosition();
        }
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(LayerMask.GetMask("Player"));
        contactFilter.useLayerMask = true;
    }

    void FixedUpdate()
    {
        if (!_isInWater && Time.time - lastCheckTime >= CHECK_INTERVAL)
        {
            CheckPlayerAbove();
            lastCheckTime = Time.time;
        }
    }

    void CheckPlayerAbove()
    {
        Vector2 checkStart = (Vector2)transform.position + Vector2.up * 0.1f;
        int hitCount = Physics2D.RaycastNonAlloc(checkStart, Vector2.up, raycastResults, 1f, contactFilter.layerMask);
        
        Debug.DrawRay(checkStart, Vector2.up * 1f, Color.blue, CHECK_INTERVAL);
        
        if (hitCount > 0 && raycastResults[0].collider.CompareTag("Player"))
        {
            ApplyFrictionForce(raycastResults[0].collider.gameObject);
        }
    }

    void ApplyFrictionForce(GameObject player)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            Vector2 playerVelocity = new Vector2(playerRb.velocity.x, 0);
            Vector2 frictionForce = playerVelocity * frictionCoefficient;
            rb.AddForce(frictionForce, ForceMode2D.Force);

            Debug.DrawRay(transform.position, frictionForce, Color.red, CHECK_INTERVAL);
            Debug.DrawRay(transform.position, Vector2.up, Color.green, CHECK_INTERVAL);
        }
    }

    public void SavePosition()
    {
        if (!Application.isEditor)
        {
            RockPositions allPositions = LoadAllPositions();
            Vector3 position = transform.position;
            
            int index = allPositions.positions.FindIndex(p => p.name == gameObject.name);
            if (index != -1)
            {
                allPositions.positions[index] = new RockPosition(gameObject.name, position);
            }
            else
            {
                allPositions.positions.Add(new RockPosition(gameObject.name, position));
            }

            SaveAllPositions(allPositions);
        }
    }

    public void LoadPosition()
    {
        if (!Application.isEditor)
        {
            RockPositions allPositions = LoadAllPositions();
            RockPosition myPosition = allPositions.positions.Find(p => p.name == gameObject.name);

            if (myPosition != null)
            {
                transform.position = myPosition.position;
            }
        }
    }

    private RockPositions LoadAllPositions()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            RockPositions positions = JsonUtility.FromJson<RockPositions>(json);
            return positions ?? new RockPositions { positions = new List<RockPosition>() };
        }
        return new RockPositions { positions = new List<RockPosition>() };
    }

    private void SaveAllPositions(RockPositions allPositions)
    {
        string json = JsonUtility.ToJson(allPositions, true);
        File.WriteAllText(savePath, json);
    }

    public void EnterWater()
    {
        if (!_isInWater)
        {
            _isInWater = true;
            this.enabled = false;
            if (!Application.isEditor)
            {
                SavePosition();
            }
            // 移除禁用渲染器和碰撞器的代码
        }
    }

    // 移除 DestroyAfterDelay 协程

    // ... 其他代码保持不变 ...
}

// 保存位置数据的类
[System.Serializable]
public class PositionData
{
    public float x, y, z;

    public PositionData(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

// 用于序列化和反序列化位置信息字典的类
[System.Serializable]
public class PositionDictionary
{
    public List<KeyValuePair> positionList;

    // 转换为字典
    public Dictionary<string, PositionData> ToDictionary()
    {
        Dictionary<string, PositionData> dict = new Dictionary<string, PositionData>();
        foreach (var kvp in positionList)
        {
            dict[kvp.key] = kvp.value;
        }
        return dict;
    }

    // 从字典创建列表
    public void FromDictionary(Dictionary<string, PositionData> positions)
    {
        positionList = new List<KeyValuePair>();
        foreach (var kvp in positions)
        {
            positionList.Add(new KeyValuePair(kvp.Key, kvp.Value));
        }
    }
}

// 关键值对类
[System.Serializable]
public class KeyValuePair
{
    public string key;
    public PositionData value;

    public KeyValuePair(string key, PositionData value)
    {
        this.key = key;
        this.value = value;
    }
}
