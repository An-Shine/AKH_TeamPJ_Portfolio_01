using System;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private float _damage;

    public void Initialize(float damage)
    {
        _damage = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            //GetComponent
            //damage Process
        }
    }

    private void DestroyEffect()
    {
        Destroy(gameObject);
    }
}