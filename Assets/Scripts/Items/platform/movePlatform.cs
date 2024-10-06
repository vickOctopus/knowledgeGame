using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movePlatform : MonoBehaviour
{
    private float direction=1;
    
    private Vector2 checkBoxSize=new Vector2(0.06f,0.3f);
    
    public float moveSpeed;
    
    private float _widthHalf;

    private bool _isGetPlayer;

    private bool _canMove;
    
    private void Awake()
    {
        _widthHalf=GetComponent<SpriteRenderer>().bounds.size.x/2;
    }


    void Start()
    {
        Physics2D.queriesStartInColliders = false;
    }
    
    private void Update()
    { 
        CheckEndsHitSomething();
        
        
        if (_canMove)
        {
            transform.position=new Vector2(transform.position.x+direction*moveSpeed*Time.deltaTime,transform.position.y);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.parent = transform;
            _isGetPlayer = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            transform.DetachChildren();
            _isGetPlayer = false;
        }
    }

    private void CheckEndsHitSomething()
    {
        var left = false;
        var right = false;
        
        
        if (Physics2D.OverlapBox(new Vector2(transform.position.x+_widthHalf,transform.position.y),checkBoxSize,0.0f,LayerMask.GetMask("Platform")))
        {
            direction = -1;
            left = true;
        }
        if(Physics2D.OverlapBox(new Vector2(transform.position.x-_widthHalf,transform.position.y),checkBoxSize,0.0f,LayerMask.GetMask("Platform")))
        {
            direction = 1;
            right = true;
        }
        

        if (!_isGetPlayer)//不可用金箍棒改变玩家正在乘坐的移动平台
        {
            if (Physics2D.OverlapBox(new Vector2(transform.position.x+_widthHalf,transform.position.y),checkBoxSize,0.0f,LayerMask.GetMask("JinGuBang")))
            {
                direction = -1;
                left = true;
            }
            if(Physics2D.OverlapBox(new Vector2(transform.position.x-_widthHalf,transform.position.y),checkBoxSize,0.0f,LayerMask.GetMask("JinGuBang")))
            {
                direction = 1;
                right = true;
            }
        }

        if (left&&right)//如果两端都发生碰撞，则静止
        {
            _canMove = false;
        }
        else
        {
            _canMove = true;
        }
    }


    // private void OnDrawGizmos()
    // {
    //     var halfWidth=GetComponent<SpriteRenderer>().bounds.size.x/2;
    //     Gizmos.DrawWireCube(new Vector2(transform.position.x+halfWidth,transform.position.y),checkBoxSize);
    //     Gizmos.DrawWireCube(new Vector2(transform.position.x-halfWidth,transform.position.y),checkBoxSize);
    // }
}
