using System.Collections;
using UnityEngine;

public class MeleeMonster : NormalMonster
{
    [Header("근접 공격")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackAngle = 90f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("공격 연출")]
    [SerializeField] private float _attackScale = 1.2f;
    [SerializeField] private float _attackAnimationTime = 0.12f;

    private float _attackCooldownTimer;

    private Vector3 _originalScale;
    private bool _isAttacking;

    protected override void Awake()
    {
        base.Awake();

        _originalScale = transform.localScale;
    }

    protected override void Update()
    {
        base.Update();

        if (_player == null)
        {
            return;
        }

        _attackCooldownTimer -= Time.deltaTime;

        FacePlayer();
        ApplySeparation();

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
        if (_isAttacking)
        {
            return;
        }

        Vector2 direction =
            (_player.position - transform.position).normalized;

        transform.position +=
            (Vector3)(
                direction *
                _moveSpeed *
                Time.deltaTime
            );
    }

    private void TryAttack()
    {
        if (_attackCooldownTimer > 0f)
        {
            return;
        }

        if (_isAttacking)
        {
            return;
        }

        Vector2 directionToPlayer =
            (_player.position - transform.position).normalized;

        float angle = Vector2.Angle(
            transform.right,
            directionToPlayer
        );

        if (angle > _attackAngle * 0.5f)
        {
            return;
        }

        _attackCooldownTimer =
            _attackCooldown;

        StartCoroutine(
            AttackRoutine()
        );
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        Debug.Log(
            $"{gameObject.name}: 근접 공격 실행!"
        );

        Vector3 targetScale =
            _originalScale * _attackScale;

        float timer = 0f;

        // 커지는 연출
        while (timer < _attackAnimationTime)
        {
            timer += Time.deltaTime;

            float progress =
                timer / _attackAnimationTime;

            transform.localScale =
                Vector3.Lerp(
                    _originalScale,
                    targetScale,
                    progress
                );

            yield return null;
        }

        // 실제 공격 판정
        Collider2D[] hitTargets =
            Physics2D.OverlapCircleAll(
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

            break;
        }

        // 작아지는 연출
        timer = 0f;

        while (timer < _attackAnimationTime)
        {
            timer += Time.deltaTime;

            float progress =
                timer / _attackAnimationTime;

            transform.localScale =
                Vector3.Lerp(
                    targetScale,
                    _originalScale,
                    progress
                );

            yield return null;
        }

        transform.localScale =
            _originalScale;

        _isAttacking = false;
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