using UnityEngine;

public class BlackHoleDamageControl : MonoBehaviour
{
    private float _damage = 0f;
    private float _damageInterval = 1f;
    private float _timer;

    private void Start()
    {
        _damage = 0f;
        _damageInterval = 1f;
        _timer = 0f;
    }

    public void SetDamage(float damage, float damageInterval)
    {
        _damage = damage;
        _damageInterval = damageInterval;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _timer += Time.deltaTime;
            TryGiveDamage(other);
        }
    }

    private void TryGiveDamage(Collider2D other)
    {
        if (_timer >= _damageInterval && other.TryGetComponent(out NormalMonster monster))
        {
            _timer = 0f;
            monster.TakeDamage(_damage);
        }
    }
}