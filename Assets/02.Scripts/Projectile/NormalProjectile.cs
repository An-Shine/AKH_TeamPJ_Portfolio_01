using UnityEngine;

public class NormalProjectile : Projectile
{
    [SerializeField] private GameObject _hitEffectPrefab;
    protected override void Move()
    {
        transform.position += _direction * (SpeedScalar * Time.deltaTime);
    }

    protected override void GiveDamage(Collider2D other)
    {
        if (!other.CompareTag("Enemy") || !other.TryGetComponent(out NormalMonster monster)) return;

        // 사망 처리로 콜라이더가 비활성화되기 전에 명중 위치를 구합니다.
        Vector2 point = other.ClosestPoint(transform.position);
        if (monster.TryTakeDamage(Damage))
        {
            ProjectileHitFeedback.Play(new Vector3(point.x, point.y, transform.position.z),
                _direction, monster, _hitEffectPrefab);
        }
    }
}
