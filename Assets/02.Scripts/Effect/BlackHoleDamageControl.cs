using UnityEngine;

public class BlackHoleDamageControl : MonoBehaviour
{
    private float _damage = 0f;

    private void Start()
    {
        _damage = 0f;
    }

    public void SetDamage(float damage)
    {
        _damage = damage;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out NormalMonster monster))
            {
                monster.TakeDamage(_damage * Time.deltaTime);
            }
        }
    }
}