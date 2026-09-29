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

    [Header("공격 애니메이션")]
    // Attack 8프레임 / Samples 12 기준 약 0.67초
    [SerializeField] private float _attackDuration = 0.67f;

    [Header("공격 연출")]
    [SerializeField] private float _attackScale = 1.2f;
    [SerializeField] private float _attackAnimationTime = 0.12f;

    // Animator Parameters
    private static readonly int DirectionHash =
        Animator.StringToHash("Direction");

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int IsAttackingHash =
        Animator.StringToHash("IsAttacking");

    /*
     * Animator Direction
     *
     * 0 = South
     * 1 = South-East
     * 2 = East
     * 3 = North-East
     * 4 = North
     * 5 = North-West
     * 6 = West
     * 7 = South-West
     *
     * Atan2 sector 순서
     *
     * East
     * North-East
     * North
     * North-West
     * West
     * South-West
     * South
     * South-East
     */
    private static readonly int[] DirectionNumbers =
    {
        2, 3, 4, 5, 6, 7, 0, 1
    };

    private float _attackCooldownTimer;

    private Vector3 _originalScale;

    // 처음에는 아래쪽을 바라봄
    private Vector2 _facingDirection = Vector2.down;

    private bool _isAttacking;
    private bool _deathStarted;

    private Coroutine _attackRoutine;


    protected override void Awake()
    {
        base.Awake();

        _originalScale = transform.localScale;

        // MeleeMonster 자기 자신에게 붙어 있는 Animator 사용
        _directionAnimator = GetComponent<Animator>();

        // 8방향 Sprite를 사용하므로
        // 오브젝트 자체는 회전시키지 않음
        transform.rotation = Quaternion.identity;

        if (_directionAnimator == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Animator 컴포넌트가 없습니다.",
                this
            );

            return;
        }

        if (_directionAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Animator Controller가 연결되어 있지 않습니다.",
                this
            );

            return;
        }

        // 초기 Animator 상태
        _directionAnimator.SetInteger(
            DirectionHash,
            0
        );

        _directionAnimator.SetBool(
            IsMovingHash,
            false
        );

        _directionAnimator.SetBool(
            IsAttackingHash,
            false
        );
    }


    protected override void Update()
    {
        base.Update();

        // 사망 중
        if (_deathStarted)
        {
            SetMoving(false);
            SetAttacking(false);
            return;
        }

        // 플레이어 없음
        if (_player == null)
        {
            SetMoving(false);
            return;
        }

        // 공격 쿨타임 감소
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        ApplySeparation();

        /*
         * 공격 중에는 방향을 바꾸지 않습니다.
         *
         * 공격 시작 순간의 방향으로
         * Attack 애니메이션 끝까지 유지.
         */
        if (!_isAttacking)
        {
            UpdateFacingDirection();
        }

        float distanceToPlayer =
            Vector2.Distance(
                transform.position,
                _player.position
            );

        // 공격 중에는 이동하지 않음
        if (_isAttacking)
        {
            SetMoving(false);
            return;
        }

        // 플레이어가 공격 범위 밖
        if (distanceToPlayer > _attackRange)
        {
            MoveToPlayer();
        }
        // 플레이어가 공격 범위 안
        else
        {
            SetMoving(false);
            TryAttack();
        }
    }


    private bool IsAnimatorReady()
    {
        return
            _directionAnimator != null &&
            _directionAnimator.isActiveAndEnabled &&
            _directionAnimator.runtimeAnimatorController != null;
    }


    // =========================================================
    // 방향 처리
    // =========================================================

    private void UpdateFacingDirection()
    {
        if (_player == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)(
                _player.position -
                transform.position
            );

        // 거의 같은 위치라면 마지막 방향 유지
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        _facingDirection =
            direction.normalized;

        float angle =
            Mathf.Atan2(
                _facingDirection.y,
                _facingDirection.x
            ) * Mathf.Rad2Deg;

        if (angle < 0f)
        {
            angle += 360f;
        }

        // 45도씩 8구역
        int sector =
            Mathf.FloorToInt(
                (angle + 22.5f) / 45f
            ) % 8;

        int directionNumber =
            DirectionNumbers[sector];

        if (!IsAnimatorReady())
        {
            return;
        }

        _directionAnimator.SetInteger(
            DirectionHash,
            directionNumber
        );
    }


    // =========================================================
    // 이동
    // =========================================================

    private void MoveToPlayer()
    {
        if (_isAttacking)
        {
            SetMoving(false);
            return;
        }

        if (_player == null)
        {
            SetMoving(false);
            return;
        }

        Vector2 direction =
            (Vector2)(
                _player.position -
                transform.position
            );

        if (direction.sqrMagnitude <= 0.0001f)
        {
            SetMoving(false);
            return;
        }

        direction.Normalize();

        SetMoving(true);

        transform.position +=
            (Vector3)(
                direction *
                _moveSpeed *
                Time.deltaTime
            );
    }


    private void SetMoving(bool isMoving)
    {
        if (!IsAnimatorReady())
        {
            return;
        }

        _directionAnimator.SetBool(
            IsMovingHash,
            isMoving
        );
    }


    // =========================================================
    // 공격
    // =========================================================

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

        if (_player == null)
        {
            return;
        }

        Vector2 directionToPlayer =
            (Vector2)(
                _player.position -
                transform.position
            );

        if (directionToPlayer.sqrMagnitude <= 0.0001f)
        {
            directionToPlayer =
                _facingDirection;
        }
        else
        {
            directionToPlayer.Normalize();
        }

        float angle =
            Vector2.Angle(
                _facingDirection,
                directionToPlayer
            );

        // 공격 가능 각도 확인
        if (angle > _attackAngle * 0.5f)
        {
            return;
        }

        _attackCooldownTimer =
            _attackCooldown;

        _attackRoutine =
            StartCoroutine(
                AttackRoutine()
            );
    }


    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        SetMoving(false);
        SetAttacking(true);

        Debug.Log(
            $"{gameObject.name}: 근접 공격 시작!"
        );

        /*
         * Attack 애니메이션:
         *
         * IsAttacking = true
         *
         * 현재 Direction에 따라서
         *
         * attack_S
         * attack_SE
         * attack_E
         * ...
         *
         * 중 하나가 Animator에서 재생됩니다.
         */

        float elapsedTime = 0f;

        Vector3 targetScale =
            _originalScale * _attackScale;


        // =====================================================
        // 공격 전진 느낌 - 확대
        // =====================================================

        float timer = 0f;

        while (timer < _attackAnimationTime)
        {
            timer += Time.deltaTime;
            elapsedTime += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / _attackAnimationTime
                );

            transform.localScale =
                Vector3.Lerp(
                    _originalScale,
                    targetScale,
                    t
                );

            yield return null;
        }

        transform.localScale =
            targetScale;


        // =====================================================
        // 공격 판정
        // =====================================================

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
                $"{gameObject.name}: " +
                $"플레이어 근접 공격 적중! " +
                $"데미지: {_contactDamage}"
            );

            /*
             * 현재 기존 코드에는 실제 PlayerHealth 데미지 호출이
             * 연결되어 있지 않으므로 우선 기존 구조를 유지합니다.
             */

            break;
        }


        // =====================================================
        // 원래 크기로 복귀
        // =====================================================

        timer = 0f;

        while (timer < _attackAnimationTime)
        {
            timer += Time.deltaTime;
            elapsedTime += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / _attackAnimationTime
                );

            transform.localScale =
                Vector3.Lerp(
                    targetScale,
                    _originalScale,
                    t
                );

            yield return null;
        }

        transform.localScale =
            _originalScale;


        // =====================================================
        // Attack Clip이 끝날 때까지 기다림
        // =====================================================

        float remainingTime =
            _attackDuration - elapsedTime;

        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingTime
            );
        }


        // =====================================================
        // 공격 종료
        // =====================================================

        SetAttacking(false);

        _isAttacking = false;
        _attackRoutine = null;

        Debug.Log(
            $"{gameObject.name}: 근접 공격 종료!"
        );
    }


    private void SetAttacking(bool isAttacking)
    {
        if (!IsAnimatorReady())
        {
            return;
        }

        _directionAnimator.SetBool(
            IsAttackingHash,
            isAttacking
        );
    }


    // =========================================================
    // 사망
    // =========================================================

    protected override void Die()
    {
        if (_deathStarted)
        {
            return;
        }

        _deathStarted = true;

        SetMoving(false);
        SetAttacking(false);

        if (_attackRoutine != null)
        {
            StopCoroutine(
                _attackRoutine
            );

            _attackRoutine = null;
        }

        _isAttacking = false;

        transform.localScale =
            _originalScale;

        base.Die();
    }


    // =========================================================
    // Gizmo
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            _attackRange
        );
    }
}