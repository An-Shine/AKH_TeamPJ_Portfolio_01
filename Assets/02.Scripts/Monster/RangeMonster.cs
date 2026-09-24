using System.Collections;
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

    [Header("발사 연출")]
    [SerializeField] private float _attackScale = 1.15f;
    [SerializeField] private float _attackAnimationTime = 0.1f;

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

    private void MoveAwayFromPlayer()
    {
        if (_isAttacking)
        {
            return;
        }

        Vector2 direction =
            (transform.position - _player.position).normalized;

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

        if (_projectilePrefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: RangeProjectile 프리팹이 설정되지 않았습니다."
            );

            return;
        }

        _attackCooldownTimer = _attackCooldown;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        Debug.Log(
            $"{gameObject.name}: 원거리 공격 준비!"
        );

        Vector3 targetScale =
            _originalScale * _attackScale;

        float timer = 0f;

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

        FireProjectile();

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

    private void FireProjectile()
    {
        Vector2 firePosition =
            transform.position;

        if (_firePoint != null)
        {
            firePosition =
                _firePoint.position;
        }

        Vector2 direction =
            (_player.position - (Vector3)firePosition).normalized;

        RangeProjectile projectile =
            Instantiate(
                _projectilePrefab,
                firePosition,
                Quaternion.identity
            );

        projectile.SetDirection(direction);

        Debug.Log(
            $"{gameObject.name}: 원거리 투사체 발사!"
        );
    }
}