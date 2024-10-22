using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour, ISaveable
{
    public Sprite ButtonDownSprite;
    // public UnityEvent OnButtonDown;
    private SpriteRenderer _spriteRenderer;
    private bool _isPressed;
    private string _parentChunkName;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _parentChunkName = GetParentChunkName();
    }

    private void Start()
    {
        Load(SaveManager.CurrentSlotIndex);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isPressed)
        {
            ButtonDown();
            Save(SaveManager.CurrentSlotIndex);
        }
    }

    public virtual void ButtonDown()
    {
        _isPressed = true;
        _spriteRenderer.sprite = ButtonDownSprite;
        SendMessageUpwards("OnButtonDown", SendMessageOptions.DontRequireReceiver);
        // OnButtonDown.Invoke();
    }

    public void Save(int slotIndex)
    {
        string key = GetPersistentKey();
        PlayerPrefs.SetInt($"{key}_Slot{slotIndex}", _isPressed ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Load(int slotIndex)
    {
        string key = GetPersistentKey();
        _isPressed = PlayerPrefs.GetInt($"{key}_Slot{slotIndex}", 0) == 1;
        if (_isPressed)
        {
            ButtonDown();
        }
    }

    private string GetPersistentKey()
    {
        return $"Button_{_parentChunkName}_{gameObject.name}";
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
}
