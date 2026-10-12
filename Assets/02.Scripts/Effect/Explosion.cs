using UnityEngine;

public class Explosion : Effect
{
    [SerializeField] private SFXType _spawnSfxType;

    public override void OnSpawn()
    {
        AudioManager.Instance.PlaySFX(_spawnSfxType);
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