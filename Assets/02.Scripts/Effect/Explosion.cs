using UnityEngine;

public class Explosion : Effect
{
    [SerializeField] private SFXType _sfxType;
    private float _damage;

    public void Initialize(float damage)
    {
        AudioManager.Instance.PlaySFX(_sfxType);
        _damage = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out NormalMonster monster))
            {
                monster.TakeDamage(_damage);
            }
        }
    }
}