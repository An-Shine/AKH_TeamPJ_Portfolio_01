using UnityEngine;

public class RangeMonster : NormalMonster
{
    [Header("원거리 공격")]
    [SerializeField] private float _attackRange = 6f;
    [SerializeField] private float _keepDistance = 4f;
    [SerializeField] private float _attackCooldown = 2f;

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

        Debug.Log($"{gameObject.name}: 원거리 공격!");
    }
}