using UnityEngine;

public class NormalProjectile : Projectile
{
    protected override void Move()
    {
        transform.position += _direction * (_speedScalar * Time.deltaTime);
    }
}