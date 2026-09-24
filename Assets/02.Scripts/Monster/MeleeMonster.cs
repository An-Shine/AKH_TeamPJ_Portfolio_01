using UnityEngine;

public class MeleeMonster : NormalMonster
{
    [Header("근접 공격")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackCooldown = 1f;

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

    private void TryAttack()
    {
        if (_attackCooldownTimer > 0f)
        {
            return;
        }

        _attackCooldownTimer = _attackCooldown;

        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(
            transform.position,
            _attackRange
        );

        foreach (Collider2D target in hitTargets)
        {
            if (!target.CompareTag("Player"))
            {
                continue;
            }

            Debug.Log(
                $"{gameObject.name}: 플레이어 근접 공격 적중! 데미지: {_contactDamage}"
            );

            return;
        }

        Debug.Log(
            $"{gameObject.name}: 근접 공격 실행 - 플레이어가 공격 범위에 없음"
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            _attackRange
        );
    }
}