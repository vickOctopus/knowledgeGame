using UnityEngine;

public class Button : SaveableObject
{
    public Sprite ButtonDownSprite;
    private SpriteRenderer _spriteRenderer;
    private bool _isPressed;

    protected override void Awake()
    {
        base.Awake();
        _spriteRenderer = GetComponent<SpriteRenderer>();
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
    }

    public override void Save(int slotIndex)
    {
        SaveBool(GetPersistentKey(), _isPressed, slotIndex);
    }

    public override void Load(int slotIndex)
    {
        _isPressed = LoadBool(GetPersistentKey(), slotIndex);
        if (_isPressed)
        {
            ButtonDown();
        }
    }
}
