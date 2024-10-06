using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spike : MonoBehaviour
{
    [SerializeField]private int damage=1;
    private void OnTriggerEnter2D(Collider2D other)
    {
        var takeDamage = other.GetComponent<ITakeDamage>();
       
        takeDamage?.TakeDamage(damage);
    }
}
