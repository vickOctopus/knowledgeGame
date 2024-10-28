using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spike : MonoBehaviour
{
    [SerializeField]private int damage=1;

    private void OnCollisionEnter2D(Collision2D other)
    {
        var takeDamage = other.gameObject.GetComponent<ITakeDamage>();
        if (takeDamage != null)
        {
            takeDamage.TakeDamage(damage);
        }
    }
}
