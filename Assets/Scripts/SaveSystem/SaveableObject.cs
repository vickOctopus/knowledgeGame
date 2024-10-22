using UnityEngine;

public abstract class SaveableObject : MonoBehaviour, ISaveable
{
    protected string _parentChunkName;

    protected virtual void Awake()
    {
        _parentChunkName = GetParentChunkName();
    }

    protected virtual void Start()
    {
        Load(SaveManager.CurrentSlotIndex);
    }

    public abstract void Save(int slotIndex);
    public abstract void Load(int slotIndex);

    protected string GetPersistentKey()
    {
        return $"{GetType().Name}_{_parentChunkName}_{gameObject.name}";
    }

    private string GetParentChunkName()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.StartsWith("Chunk_"))
            {
                return current.name;
            }
            current = current.parent;
        }
        return "NoChunk"; // 如果没有找到父 Chunk，返回一个默认值
    }

    protected void SaveBool(string key, bool value, int slotIndex)
    {
        PlayerPrefs.SetInt($"{key}_Slot{slotIndex}", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    protected bool LoadBool(string key, int slotIndex, bool defaultValue = false)
    {
        return PlayerPrefs.GetInt($"{key}_Slot{slotIndex}", defaultValue ? 1 : 0) == 1;
    }

    // 可以根据需要添加更多类型的 Save 和 Load 方法，如 SaveInt, LoadInt 等
}
