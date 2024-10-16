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
        Load();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_buttonData.isPressed)
        {
            ButtonDown();
            //Save();
        }
    }

    public virtual void ButtonDown()
    {
        _buttonData.isPressed = true;
        _spriteRenderer.sprite = ButtonDownSprite;
        OnButtonDown.Invoke();
    }

    public void Save()
    {
        string json = JsonConvert.SerializeObject(_buttonData, Formatting.Indented);
        File.WriteAllText(GetSavePath(), json);
    }

    public void Load()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _buttonData = JsonConvert.DeserializeObject<ButtonData>(json);
            if (_buttonData.isPressed)
            {
                ButtonDown();
            }
        }
    }

    private string GetSavePath()
    {
        string directory = Path.Combine(Application.persistentDataPath, "ButtonData");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return Path.Combine(directory, $"{gameObject.name}_data.json");
    }
}
