using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LadderTopCollider : MonoBehaviour
{
    private Collider2D _platformCollider; // 代表当前平台的 Collider
    private bool _isSubscribed = false;

    void Start()
    {
        // 获取当前平台的 Collider2D 组件
        _platformCollider = GetComponent<Collider2D>();

        _platformCollider.enabled = false;

        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (!_isSubscribed)
        {
            EventManager.instance.OnClimbLadder += EnableCollider;
            EventManager.instance.OnLeftLadder += DisableCollider;
            _isSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_isSubscribed)
        {
            EventManager.instance.OnClimbLadder -= EnableCollider;
            EventManager.instance.OnLeftLadder -= DisableCollider;
            _isSubscribed = false;
        }
    }

    void EnableCollider()
    {
        _platformCollider.enabled = true;
    }

    void DisableCollider()
    {
        _platformCollider.enabled = false;
    }
}
