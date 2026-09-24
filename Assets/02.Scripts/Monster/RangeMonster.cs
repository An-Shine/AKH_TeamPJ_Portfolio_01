using UnityEngine;

public class RangeMonster : NormalMonster
{
    [Header("원거리 공격")]
    [SerializeField] private float _attackRange = 6f;
    [SerializeField] private float _keepDistance = 4f;
    [SerializeField] private float _attackCooldown = 2f;

    [Header("투사체")]
    [SerializeField] private RangeProjectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    private float _attackCooldownTimer;

    protected override void Update()
    {
        base.Update();

        if (_player == null)
        {
            return;
        }

        _attackCooldownTimer -= Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(
            transform.position,
            _player.position
        );

        if (distanceToPlayer > _attackRange)
        {
            MoveToPlayer();
        }
        else if (distanceToPlayer < _keepDistance)
        {
            MoveAwayFromPlayer();
        }
        else
        {
            TryAttack();
        }
    }

    private void MoveToPlayer()
    {
        Vector2 direction =
            (_player.position - transform.position).normalized;

        transform.position +=
            (Vector3)(direction * _moveSpeed * Time.deltaTime);
    }

    private void MoveAwayFromPlayer()
    {
        Vector2 direction =
            (transform.position - _player.position).normalized;

        transform.position +=
            (Vector3)(direction * _moveSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (_attackCooldownTimer > 0f)
        {
            return;
        }

        _attackCooldownTimer = _attackCooldown;

        if (_projectilePrefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: RangeProjectile 프리팹이 설정되지 않았습니다."
            );

            return;
        }

        Vector2 firePosition = transform.position;

        if (_firePoint != null)
        {
            firePosition = _firePoint.position;
        }

        Vector2 direction =
            (_player.position - (Vector3)firePosition).normalized;

        RangeProjectile projectile = Instantiate(
            _projectilePrefab,
            firePosition,
            Quaternion.identity
        );

        projectile.SetDirection(direction);

        Debug.Log($"{gameObject.name}: 원거리 투사체 발사!");
    }
}

