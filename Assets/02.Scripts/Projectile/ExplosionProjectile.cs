using UnityEngine;

public class ExplosionProjectile : Projectile
{
    [SerializeField] private EffectType _effectType;
    [SerializeField] private Vector3 _adjustExplosionPosition;

    protected override void Move()
    {
        transform.position += _direction * (_speedScalar * Time.deltaTime);
    }

    protected override void OnDestroySelf()
    {
        base.OnDestroySelf();
        EffectSpawner.Instance.Spawn(_effectType, transform.position + _adjustExplosionPosition, _damage);
    }
}