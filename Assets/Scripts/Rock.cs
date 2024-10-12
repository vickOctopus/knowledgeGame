using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Rock : MonoBehaviour
{
    private string savePath;

    void Start()
    {
        // 保存路径为所有 Rocks 公共的一个 JSON 文件
        savePath = Path.Combine(Application.persistentDataPath, "rockPositions.json");

        // 关卡加载时，自动加载自身位置信息
        LoadPosition();
    }

    // 保存自身位置信息
    public void SavePosition()
    {
        // Debug.Log("保存位置被调用");

        Dictionary<string, PositionData> allPositions = LoadAllPositions();
        Vector3 position = transform.position;
        PositionData data = new PositionData(position.x, position.y, position.z);
    
        allPositions[gameObject.name] = data;

        // Debug.Log("当前所有位置: " + JsonUtility.ToJson(new PositionDictionary { positionList = ToList(allPositions) }, true));

        SaveAllPositions(allPositions);
    }

    // 加载位置信息
    public void LoadPosition()
    {
        // Debug.Log("尝试加载位置: " + gameObject.name);
        Dictionary<string, PositionData> allPositions = LoadAllPositions();

        if (allPositions != null && allPositions.ContainsKey(gameObject.name))
        {
            PositionData data = allPositions[gameObject.name];
            transform.position = new Vector3(data.x, data.y, data.z);
            // Debug.Log(gameObject.name + " 的位置已恢复: " + transform.position);
        }
        // else
        // {
        //     Debug.LogWarning("未找到 " + gameObject.name + " 的位置数据");
        // }
    }

    // 加载所有 Rocks 的位置信息
    private Dictionary<string, PositionData> LoadAllPositions()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                PositionDictionary positionDict = JsonUtility.FromJson<PositionDictionary>(json);
                return positionDict?.ToDictionary() ?? new Dictionary<string, PositionData>(); // 处理可能为 null 的情况
            }
            catch (System.Exception e)
            {
                Debug.LogError("加载位置时发生错误: " + e.Message);
                return new Dictionary<string, PositionData>(); // 返回空字典以防止崩溃
            }
        }
        return new Dictionary<string, PositionData>(); // 如果文件不存在，返回空字典
    }

    // 保存所有 Rocks 的位置信息
    private void SaveAllPositions(Dictionary<string, PositionData> allPositions)
    {
        PositionDictionary positionDict = new PositionDictionary();
        positionDict.FromDictionary(allPositions);
        string json = JsonUtility.ToJson(positionDict, true);
    
        // Debug.Log("准备写入的 JSON: " + json);
    
        try
        {
            File.WriteAllText(savePath, json);
            // Debug.Log("保存成功: " + savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("保存时发生错误: " + e.Message);
        }
    }

    // 将字典转换为列表
    private List<KeyValuePair> ToList(Dictionary<string, PositionData> positions)
    {
        List<KeyValuePair> list = new List<KeyValuePair>();
        foreach (var kvp in positions)
        {
            list.Add(new KeyValuePair(kvp.Key, kvp.Value));
        }
        return list;
    }
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