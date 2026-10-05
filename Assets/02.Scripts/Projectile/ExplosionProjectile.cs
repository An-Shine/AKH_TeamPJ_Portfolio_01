using UnityEngine;

public class ExplosionProjectile : Projectile
{
    [SerializeField] private Explosion _explosionEffect;
    [SerializeField] private Vector3 _adjustExplosionPosition;

    protected override void Move()
    {
        transform.position += _direction * (_speedScalar * Time.deltaTime);
    }

    protected override void OnDestroySelf()
    {
        ProjectilePoolManager.Instance.Release(this, _type);
        Explosion explosion = Instantiate(
            _explosionEffect, transform.position + _adjustExplosionPosition, Quaternion.identity);
        explosion.Initialize(_damage);
    }
}