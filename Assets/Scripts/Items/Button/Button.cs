using System;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using Newtonsoft.Json;

[Serializable]
public class ButtonData
{
    public bool isPressed;
}

public class Button : MonoBehaviour, ISaveable
{
    public Sprite ButtonDownSprite;
    public UnityEvent OnButtonDown;
    private SpriteRenderer _spriteRenderer;
    private ButtonData _buttonData;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _buttonData = new ButtonData();
    }

    private void Start()
    {
        Load(0); // 默认加载第一个存档槽，你可以根据需要修改这里
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_buttonData.isPressed)
        {
            ButtonDown();
            Save(0); // 默认保存到第一个存档槽，你可以根据需要修改这里
        }
    }

    public virtual void ButtonDown()
    {
        _buttonData.isPressed = true;
        _spriteRenderer.sprite = ButtonDownSprite;
        OnButtonDown.Invoke();
    }

    public void Save(int slotIndex)
    {
        string json = JsonConvert.SerializeObject(_buttonData, Formatting.Indented);
        string saveFilePath = SaveManager.GetSavePath(slotIndex, $"{gameObject.name}_data.json");
        
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存按钮数据时出错：{e.Message}");
        }
    }

    public void Load(int slotIndex)
    {
        string saveFilePath = SaveManager.GetSavePath(slotIndex, $"{gameObject.name}_data.json");
        
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                _buttonData = JsonConvert.DeserializeObject<ButtonData>(json);
                if (_buttonData.isPressed)
                {
                    ButtonDown();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载按钮数据时出错：{e.Message}");
                _buttonData = new ButtonData();
            }
        }
        else
        {
            _buttonData = new ButtonData();
        }
    }
}
