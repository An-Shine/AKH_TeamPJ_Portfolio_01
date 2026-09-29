using System.Collections;
using UnityEngine;

public class MeleeMonster : NormalMonster
{
    [Header("방향 애니메이션")]
    [SerializeField] private Animator _directionAnimator;

    [Header("근접 공격")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackAngle = 90f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("공격 연출")]
    [SerializeField] private float _attackScale = 1.2f;
    [SerializeField] private float _attackAnimationTime = 0.12f;

    private static readonly int DirectionHash =
        Animator.StringToHash("Direction");

    // 오른쪽부터 반시계 방향:
    // east, north-east, north, north-west,
    // west, south-west, south, south-east
    private static readonly int[] DirectionNumbers =
    {
        1, 4, 2, 5, 3, 7, 0, 6
    };

    private float _attackCooldownTimer;
    private Vector3 _originalScale;
    private Vector2 _facingDirection = Vector2.down;
    private bool _isAttacking;
    private bool _deathStarted;
    private Coroutine _attackRoutine;

    protected override void Awake()
    {
        base.Awake();

        _originalScale = transform.localScale;

        if (_directionAnimator == null)
        {
            _directionAnimator =
                GetComponentInChildren<Animator>(true);
        }

        // 8방향 스프라이트 자체로 방향을 표현합니다.
        transform.rotation = Quaternion.identity;

        if (_directionAnimator == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Animator를 연결해 주세요.",
                this
            );
        }
    }

    protected override void Update()
    {
        base.Update();

        if (_deathStarted || _player == null)
        {
            return;
        }

        _attackCooldownTimer -= Time.deltaTime;

        ApplySeparation();
        UpdateFacingDirection();

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

    private void UpdateFacingDirection()
    {
        Vector2 direction =
            (Vector2)(_player.position - transform.position);

        // 같은 위치에서는 마지막 방향을 유지합니다.
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        _facingDirection = direction.normalized;

        float angle = Mathf.Atan2(
            _facingDirection.y,
            _facingDirection.x
        ) * Mathf.Rad2Deg;

        if (angle < 0f)
        {
            angle += 360f;
        }

        // 원을 45도씩 나누어 8방향 중 하나를 선택합니다.
        int sector =
            Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;

        if (_directionAnimator != null)
        {
            _directionAnimator.SetInteger(
                DirectionHash,
                DirectionNumbers[sector]
            );
        }
    }

    private void MoveToPlayer()
    {
        if (_isAttacking)
        {
            return;
        }

        Vector2 direction =
            ((Vector2)(_player.position - transform.position))
            .normalized;

        transform.position +=
            (Vector3)(direction * _moveSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (_attackCooldownTimer > 0f || _isAttacking)
        {
            return;
        }

        Vector2 directionToPlayer =
            ((Vector2)(_player.position - transform.position))
            .normalized;

        // transform.right 대신 실제 바라보는 방향을 사용합니다.
        float angle = Vector2.Angle(
            _facingDirection,
            directionToPlayer
        );

        if (angle > _attackAngle * 0.5f)
        {
            return;
        }

        _attackCooldownTimer = _attackCooldown;
        _attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        Debug.Log($"{gameObject.name}: 근접 공격 실행!");

        Vector3 targetScale = _originalScale * _attackScale;
        float timer = 0f;

        // 커지는 연출
        while (timer < _attackAnimationTime)
        {
            timer += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                _originalScale,
                targetScale,
                timer / _attackAnimationTime
            );

            yield return null;
        }

        // 기존 공격 감지
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
                $"{gameObject.name}: 플레이어 근접 공격 적중! " +
                $"데미지: {_contactDamage}"
            );

            break;
        }

        // 작아지는 연출
        timer = 0f;

        while (timer < _attackAnimationTime)
        {
            timer += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                targetScale,
                _originalScale,
                timer / _attackAnimationTime
            );

            yield return null;
        }

        transform.localScale = _originalScale;
        _isAttacking = false;
        _attackRoutine = null;
    }

    protected override void Die()
    {
        if (_deathStarted)
        {
            return;
        }

        _deathStarted = true;

        // 공격 확대 연출이 사망 축소 연출과 겹치지 않게 합니다.
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _isAttacking = false;
        base.Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}