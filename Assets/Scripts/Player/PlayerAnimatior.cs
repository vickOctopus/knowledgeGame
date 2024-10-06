using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatior : MonoBehaviour
{
    private Rigidbody2D _rb2d;
    private Animator _animator;

    private void Awake()
    {
        
        _rb2d = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        UpdateAnimation();   
    }

   private void UpdateAnimation()
    {
        _animator.SetFloat("velocityX",Mathf.Abs(_rb2d.velocity.x));
        _animator.SetFloat("velocityY",_rb2d.velocity.y);
    }
}
