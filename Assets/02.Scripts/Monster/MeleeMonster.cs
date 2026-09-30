using System.Collections;
using UnityEngine;

public class MeleeMonster : NormalMonster
{
    [Header("방향 애니메이션")]
    [SerializeField] private Animator _directionAnimator;

    [Header("방향 전환 안정화")]
    [SerializeField] private float _directionChangeAngle = 30f;

    [Header("근접 공격")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackAngle = 90f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("공격 판정")]
    [Range(0f, 1f)]
    [SerializeField] private float _attackHitNormalizedTime = 0.5f;


    // =========================================================
    // Animator Parameters
    // =========================================================

    private static readonly int DirectionHash =
        Animator.StringToHash("Direction");

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int IsAttackingHash =
        Animator.StringToHash("IsAttacking");

    private static readonly int IsHitHash =
        Animator.StringToHash("IsHit");

    private static readonly int IsDeadHash =
        Animator.StringToHash("IsDead");


    // =========================================================
    // 방향 번호
    // =========================================================

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
     *
     * Atan2 Sector
     *
     * 0 = East
     * 1 = North-East
     * 2 = North
     * 3 = North-West
     * 4 = West
     * 5 = South-West
     * 6 = South
     * 7 = South-East
     */

    private static readonly int[] DirectionNumbers =
    {
        2, // East
        3, // North-East
        4, // North
        5, // North-West
        6, // West
        7, // South-West
        0, // South
        1  // South-East
    };


    // =========================================================
    // 내부 상태
    // =========================================================

    private float _attackCooldownTimer;

    private Vector2 _facingDirection =
        Vector2.down;

    // Atan2 기준 South = 6
    private int _currentSector = 6;

    private bool _isAttacking;
    private bool _isHit;
    private bool _deathStarted;

    private Coroutine _attackRoutine;
    private Coroutine _hitRoutine;


    // =========================================================
    // Awake
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        // MeleeMonster 자기 자신의 Animator 사용
        _directionAnimator =
            GetComponent<Animator>();

        // 8방향 Sprite를 사용하므로
        // Transform 자체를 회전시키지 않음
        transform.rotation =
            Quaternion.identity;


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


        // 처음에는 South Idle
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

        _directionAnimator.SetBool(
            IsHitHash,
            false
        );

        _directionAnimator.SetBool(
            IsDeadHash,
            false
        );
    }


    // =========================================================
    // Update
    // =========================================================

    protected override void Update()
    {
        base.Update();


        // 사망 시작 후 행동 중지
        if (_deathStarted)
        {
            SetMoving(false);
            return;
        }


        // Player를 찾지 못했다면 행동 중지
        if (_player == null)
        {
            SetMoving(false);
            return;
        }


        // 공격 쿨타임
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -=
                Time.deltaTime;
        }


        // =====================================================
        // Hit 중
        // =====================================================

        /*
         * 피격 중에는:
         *
         * 이동 X
         * 공격 X
         * 방향전환 X
         */
        if (_isHit)
        {
            SetMoving(false);
            return;
        }


        // =====================================================
        // Attack 중
        // =====================================================

        /*
         * 공격 중에는:
         *
         * 이동 X
         * 방향전환 X
         */
        if (_isAttacking)
        {
            SetMoving(false);
            return;
        }


        // 공격/피격 상태가 아닐 때만 방향 갱신
        UpdateFacingDirection();


        // 몬스터끼리 겹치는 현상 완화
        ApplySeparation();


        float distanceToPlayer =
            Vector2.Distance(
                transform.position,
                _player.position
            );


        // 공격 범위보다 멀면 추적
        if (distanceToPlayer > _attackRange)
        {
            MoveToPlayer();
        }
        // 공격 범위 안이면 멈추고 공격
        else
        {
            SetMoving(false);

            TryAttack();
        }
    }


    // =========================================================
    // Animator 상태 확인
    // =========================================================

    private bool IsAnimatorReady()
    {
        return
            _directionAnimator != null &&
            _directionAnimator.isActiveAndEnabled &&
            _directionAnimator.runtimeAnimatorController != null;
    }


    // =========================================================
    // 8방향 처리
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


        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }


        _facingDirection =
            direction.normalized;


        /*
         * East  = 0도
         * North = 90도
         * West  = 180도
         * South = 270도
         */
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;


        if (angle < 0f)
        {
            angle += 360f;
        }


        // 가장 가까운 45도 방향
        int candidateSector =
            Mathf.RoundToInt(
                angle / 45f
            ) % 8;


        // 현재 바라보는 방향의 중심 각도
        float currentSectorAngle =
            _currentSector * 45f;


        float difference =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    currentSectorAngle,
                    angle
                )
            );


        /*
         * 방향 경계에서
         *
         * E → NE → E → NE
         *
         * 식으로 계속 튀는 현상 방지
         */
        if (
            candidateSector != _currentSector &&
            difference >= _directionChangeAngle
        )
        {
            _currentSector =
                candidateSector;
        }


        if (!IsAnimatorReady())
        {
            return;
        }


        int animatorDirection =
            DirectionNumbers[_currentSector];


        _directionAnimator.SetInteger(
            DirectionHash,
            animatorDirection
        );
    }


    // =========================================================
    // 이동
    // =========================================================

    private void MoveToPlayer()
    {
        /*
         * 공격 / 피격 / 사망 중이면
         * 절대 이동하지 않음
         */
        if (
            _isAttacking ||
            _isHit ||
            _deathStarted
        )
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


        // Walk Animation
        SetMoving(true);


        /*
         * 중요:
         *
         * MoveWithCollisionCheck() 사용하지 않음.
         *
         * 이전에 잘 움직이던 방식으로
         * 직접 Transform 이동.
         *
         * Player 코드는 전혀 건드리지 않습니다.
         */
        transform.position +=
            (Vector3)(
                direction *
                _moveSpeed *
                Time.deltaTime
            );
    }


    private void SetMoving(bool value)
    {
        if (!IsAnimatorReady())
        {
            return;
        }


        _directionAnimator.SetBool(
            IsMovingHash,
            value
        );
    }


    // =========================================================
    // 공격 시작 판정
    // =========================================================

    private void TryAttack()
    {
        if (_attackCooldownTimer > 0f)
        {
            return;
        }


        if (
            _isAttacking ||
            _isHit ||
            _deathStarted
        )
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


        // 공격 가능 각도 밖
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


    // =========================================================
    // 공격 Animation
    // =========================================================

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;


        SetMoving(false);

        SetAttacking(true);


        /*
         * Animator가 Any State → Attack
         * Transition을 처리할 시간
         */
        yield return null;


        float timeout = 0f;


        // 실제 Attack State 진입 대기
        while (IsAnimatorReady())
        {
            AnimatorStateInfo info =
                _directionAnimator
                    .GetCurrentAnimatorStateInfo(0);


            if (info.IsTag("Attack"))
            {
                break;
            }


            timeout +=
                Time.deltaTime;


            if (timeout >= 0.5f)
            {
                Debug.LogError(
                    $"{gameObject.name}: " +
                    "Attack 상태 진입 실패. " +
                    "Attack Tag 또는 Transition을 확인하세요.",
                    this
                );


                SetAttacking(false);

                _isAttacking = false;
                _attackRoutine = null;

                yield break;
            }


            yield return null;
        }


        bool damageApplied =
            false;


        // =====================================================
        // 실제 Attack Clip 끝까지 재생
        // =====================================================

        while (IsAnimatorReady())
        {
            AnimatorStateInfo info =
                _directionAnimator
                    .GetCurrentAnimatorStateInfo(0);


            if (!info.IsTag("Attack"))
            {
                yield return null;
                continue;
            }


            /*
             * 기본값 0.5
             *
             * 애니메이션 절반쯤에서
             * 실제 공격 판정
             */
            if (
                !damageApplied &&
                info.normalizedTime >=
                _attackHitNormalizedTime
            )
            {
                damageApplied = true;

                ApplyAttackHit();
            }


            /*
             * normalizedTime >= 1
             *
             * Attack Animation 100% 완료
             */
            if (info.normalizedTime >= 1f)
            {
                break;
            }


            yield return null;
        }


        // 공격 종료
        SetAttacking(false);


        _isAttacking = false;
        _attackRoutine = null;
    }


    private void ApplyAttackHit()
    {
        Collider2D[] targets =
            Physics2D.OverlapCircleAll(
                transform.position,
                _attackRange
            );


        foreach (Collider2D target in targets)
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
             * Player 쪽 코드는 팀원 작업물이므로
             * 여기서 수정하거나 직접 호출하지 않습니다.
             */


            break;
        }
    }


    private void SetAttacking(bool value)
    {
        if (!IsAnimatorReady())
        {
            return;
        }


        _directionAnimator.SetBool(
            IsAttackingHash,
            value
        );
    }


    // =========================================================
    // HIT
    // =========================================================

    protected override void OnHit()
    {
        if (_deathStarted)
        {
            return;
        }


        /*
         * 이미 Hit 중이면
         * 현재 Hit 애니메이션을 그대로 끝까지 재생.
         */
        if (_isHit)
        {
            return;
        }


        /*
         * Attack 중 총알 맞으면
         * 공격 Coroutine 즉시 중단
         */
        if (_attackRoutine != null)
        {
            StopCoroutine(
                _attackRoutine
            );

            _attackRoutine = null;
        }


        _isAttacking =
            false;


        SetAttacking(false);

        SetMoving(false);


        _hitRoutine =
            StartCoroutine(
                HitRoutine()
            );
    }


    private IEnumerator HitRoutine()
    {
        _isHit = true;


        SetMoving(false);

        SetHit(true);


        /*
         * Any State → Hit으로 넘어갈
         * 한 프레임의 시간
         */
        yield return null;


        float timeout = 0f;


        // =====================================================
        // 실제 Hit State 진입 확인
        // =====================================================

        while (IsAnimatorReady())
        {
            AnimatorStateInfo info =
                _directionAnimator
                    .GetCurrentAnimatorStateInfo(0);


            if (info.IsTag("Hit"))
            {
                break;
            }


            timeout +=
                Time.deltaTime;


            if (timeout >= 0.5f)
            {
                Debug.LogError(
                    $"{gameObject.name}: " +
                    "Hit 상태 진입 실패. " +
                    "Hit Tag / Direction / Transition을 확인하세요.",
                    this
                );


                SetHit(false);

                _isHit = false;
                _hitRoutine = null;

                yield break;
            }


            yield return null;
        }


        // =====================================================
        // Hit Animation 실제 끝까지 대기
        // =====================================================

        while (IsAnimatorReady())
        {
            AnimatorStateInfo info =
                _directionAnimator
                    .GetCurrentAnimatorStateInfo(0);


            if (!info.IsTag("Hit"))
            {
                yield return null;
                continue;
            }


            /*
             * Hit Clip 100% 완료
             */
            if (info.normalizedTime >= 1f)
            {
                break;
            }


            yield return null;
        }


        // =====================================================
        // Hit 종료
        // =====================================================

        SetHit(false);


        _isHit = false;
        _hitRoutine = null;


        /*
         * 여기서 Update가 다시 실행됨.
         *
         * 플레이어가 멀다
         * → 다시 Walk
         *
         * 플레이어가 공격 범위 안
         * → 다시 Attack
         */
    }


    private void SetHit(bool value)
    {
        if (!IsAnimatorReady())
        {
            return;
        }


        _directionAnimator.SetBool(
            IsHitHash,
            value
        );
    }


    // =========================================================
    // DEATH
    // 현재 Death Animation은 아직 다음 단계
    // =========================================================

    protected override void Die()
    {
        if (_deathStarted)
        {
            return;
        }


        _deathStarted =
            true;


        // 이동 중지
        SetMoving(false);


        // =====================================================
        // Attack 중이면 취소
        // =====================================================

        if (_attackRoutine != null)
        {
            StopCoroutine(
                _attackRoutine
            );

            _attackRoutine = null;
        }


        // =====================================================
        // Hit 중이면 취소
        // =====================================================

        if (_hitRoutine != null)
        {
            StopCoroutine(
                _hitRoutine
            );

            _hitRoutine = null;
        }


        _isAttacking =
            false;

        _isHit =
            false;


        SetAttacking(false);

        SetHit(false);


        /*
         * 기존 Idle / Walk / Attack / Hit Transition에
         * IsDead = false를 넣어놨기 때문에
         * true가 되면 그쪽으로 다시 못 들어갑니다.
         */
        if (IsAnimatorReady())
        {
            _directionAnimator.SetBool(
                IsDeadHash,
                true
            );
        }


        /*
         * Death Animation은 아직 연결 전이므로
         * 현재는 NormalMonster의 기존 사망 처리 실행.
         *
         * 다음 단계에서
         * Death 8방향 Animation으로 교체합니다.
         */
        base.Die();
    }


    // =========================================================
    // 공격 범위 Gizmo
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.red;


        Gizmos.DrawWireSphere(
            transform.position,
            _attackRange
        );
    }
}