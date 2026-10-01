using UnityEngine;

[RequireComponent(typeof(Animator))]
public class KamikazeMonster : NormalMonster
{
    [Header("자폭")]
    [SerializeField, Min(0.1f)]
    private float _explodeRange = 1.2f;

    [Tooltip("Attack 애니메이션 진행률 중 실제 폭발이 발생하는 시점")]
    [SerializeField, Range(0f, 0.95f)]
    private float _explodeNormalizedTime = 0.6f;


    [Header("추격")]
    [Tooltip("이 거리 안으로 들어오면 빠르게 돌진합니다.")]
    [SerializeField, Min(0.1f)]
    private float _chargeRange = 4f;

    [Tooltip("일반 추적 속도 배율")]
    [SerializeField, Min(0.1f)]
    private float _chaseSpeedMultiplier = 1.2f;

    [Tooltip("플레이어 근처에서 돌진할 때 속도 배율")]
    [SerializeField, Min(0.1f)]
    private float _chargeSpeedMultiplier = 2f;

    [Tooltip("일반 추적 중 다른 몬스터와 겹치는 것을 방지하는 강도")]
    [SerializeField, Min(0f)]
    private float _crowdAvoidanceSpeed = 1.5f;


    [Header("폭발 효과 - 선택")]
    [SerializeField]
    private GameObject _explosionEffectPrefab;

    [SerializeField, Min(0f)]
    private float _explosionEffectLifetime = 1f;


    private enum ActionState
    {
        None,
        Attack,
        Hit,
        Death
    }


    private static readonly int DirectionId =
        Animator.StringToHash("Direction");

    private static readonly int MovingId =
        Animator.StringToHash("IsMoving");

    private static readonly int AttackingId =
        Animator.StringToHash("IsAttacking");

    private static readonly int HitId =
        Animator.StringToHash("IsHit");

    private static readonly int DeadId =
        Animator.StringToHash("IsDead");


    private static readonly string[] Directions =
    {
        "S",
        "SE",
        "E",
        "NE",
        "N",
        "NW",
        "W",
        "SW"
    };


    private Animator _animator;

    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;

    private Collider2D[] _colliders;


    // 0 = Idle
    // 1 = Walk
    // 2 = Attack
    // 3 = Hit
    // 4 = Death
    private readonly int[,] _states = new int[5, 8];


    private ActionState _action = ActionState.None;

    private bool _ready;
    private bool _hasDied;
    private bool _explosionTriggered;

    private int _direction;

    private int _actionHash;
    private int _actionStartFrame;

    private float _actionStartTime;


    protected override void Awake()
    {
        base.Awake();

        _animator = GetComponent<Animator>();

        _renderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        _colliders =
            GetComponentsInChildren<Collider2D>(true);


        _originalColors =
            new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] =
                _renderers[i].color;

            // 8방향 이미지를 직접 사용하므로 Flip 사용 안 함
            _renderers[i].flipX = false;
            _renderers[i].flipY = false;
        }


        transform.rotation =
            Quaternion.identity;


        _explodeRange =
            Mathf.Max(0.1f, _explodeRange);

        _chargeRange =
            Mathf.Max(
                _explodeRange + 0.1f,
                _chargeRange);

        _chaseSpeedMultiplier =
            Mathf.Max(
                0.1f,
                _chaseSpeedMultiplier);

        _chargeSpeedMultiplier =
            Mathf.Max(
                _chaseSpeedMultiplier,
                _chargeSpeedMultiplier);

        _explodeNormalizedTime =
            Mathf.Clamp(
                _explodeNormalizedTime,
                0f,
                0.95f);


        _animator.applyRootMotion = false;

        _animator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;
    }


    protected override void Start()
    {
        base.Start();


        // 실수로 몬스터 자신에게 Player 태그가 붙은 경우 방지
        if (_player != null &&
            (_player == transform ||
             _player.IsChildOf(transform)))
        {
            Debug.LogError(
                $"{name}: 몬스터에 Player 태그가 붙어 있습니다. " +
                "실제 플레이어 오브젝트에만 Player 태그를 설정하세요.",
                this);

            _player = null;
        }


        _ready =
            ValidateAnimator();

        if (!_ready)
            return;


        _direction = 0;

        _animator.SetInteger(
            DirectionId,
            _direction);

        SetAnimationFlags(false);


        _animator.Play(
            _states[0, _direction],
            0,
            0f);
    }


    protected override void Update()
    {
        // -------------------------
        // Death 애니메이션 진행
        // -------------------------

        if (_hasDied)
        {
            if (_ready)
                UpdateAction();

            return;
        }


        base.Update();


        if (!_ready)
            return;


        // -------------------------
        // Attack / Hit 진행 중
        // -------------------------

        if (_action != ActionState.None)
        {
            UpdateAction();
            return;
        }


        // -------------------------
        // 플레이어 없음
        // -------------------------

        if (_player == null)
        {
            SetAnimationFlags(false);
            return;
        }


        Vector2 toPlayer =
            (Vector2)_player.position -
            (Vector2)transform.position;

        float distance =
            toPlayer.magnitude;


        // 플레이어와 거의 같은 위치
        if (distance <= 0.001f)
        {
            BeginKamikazeAttack();
            return;
        }


        // ==========================================
        // 자폭 거리 안으로 들어옴
        // ==========================================

        if (distance <= _explodeRange)
        {
            SetDirection(toPlayer);

            BeginKamikazeAttack();

            return;
        }


        // ==========================================
        // 플레이어 추적
        // ==========================================

        Vector2 chaseDirection =
            toPlayer.normalized;


        // 가까워지면 돌진 모드
        bool isCharging =
            distance <= _chargeRange;


        float speedMultiplier =
            isCharging
                ? _chargeSpeedMultiplier
                : _chaseSpeedMultiplier;


        float moveSpeed =
            _moveSpeed *
            speedMultiplier;


        float moveDistance =
            moveSpeed *
            Time.deltaTime;


        // 폭발 거리보다 안쪽으로 너무 깊게 들어가는 것 방지
        float remainingDistance =
            Mathf.Max(
                0f,
                distance - _explodeRange);


        moveDistance =
            Mathf.Min(
                moveDistance,
                remainingDistance);


        Vector2 movement =
            chaseDirection *
            moveDistance;


        // ==========================================
        // 몬스터끼리 겹침 방지
        //
        // 돌진 중에는 Separation을 사용하지 않는다.
        // 플레이어에게 직선으로 달려들게 하기 위함.
        // ==========================================

        if (!isCharging)
        {
            movement +=
                GetSeparationDirection() *
                (_crowdAvoidanceSpeed *
                 Time.deltaTime);
        }


        Vector2 previousPosition =
            transform.position;


        MoveWithCollisionCheck(
            movement);


        Vector2 actualMovement =
            (Vector2)transform.position -
            previousPosition;


        bool moving =
            actualMovement.sqrMagnitude >
            0.000001f;


        // 실제 움직인 방향으로 바라본다.
        // 벽에 막힌 경우에는 플레이어 방향을 바라본다.
        if (moving)
        {
            SetDirection(
                actualMovement);
        }
        else
        {
            SetDirection(
                toPlayer);
        }


        SetAnimationFlags(
            moving);
    }


    // =============================================
    // 피해
    // =============================================

    public override void TakeDamage(float damage)
    {
        if (_hasDied)
            return;

        base.TakeDamage(
            damage);
    }


    protected override void OnHit()
    {
        if (_hasDied ||
            !_ready)
        {
            return;
        }


        // 이미 실제 폭발이 발생했다면
        // Hit으로 공격을 끊지 않는다.
        if (_action ==
                ActionState.Attack &&
            _explosionTriggered)
        {
            return;
        }


        BeginAction(
            ActionState.Hit,
            3);
    }


    // =============================================
    // 사망
    // =============================================

    protected override void Die()
    {
        BeginDeath();
    }


    // =============================================
    // 자폭 공격 시작
    // =============================================

    private void BeginKamikazeAttack()
    {
        if (_hasDied)
            return;


        if (_action != ActionState.None)
            return;


        _explosionTriggered =
            false;


        BeginAction(
            ActionState.Attack,
            2);
    }


    // =============================================
    // 방향 계산
    //
    // 0 = S
    // 1 = SE
    // 2 = E
    // 3 = NE
    // 4 = N
    // 5 = NW
    // 6 = W
    // 7 = SW
    // =============================================

    private void SetDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <
            0.000001f)
        {
            return;
        }


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x) *
            Mathf.Rad2Deg;


        _direction =
            (Mathf.RoundToInt(
                (angle + 90f) /
                45f)
             + 8) % 8;


        _animator.SetInteger(
            DirectionId,
            _direction);
    }


    // =============================================
    // Animator Bool 설정
    // =============================================

    private void SetAnimationFlags(
        bool moving)
    {
        _animator.SetBool(
            MovingId,
            moving &&
            _action ==
                ActionState.None &&
            !_hasDied);


        _animator.SetBool(
            AttackingId,
            _action ==
            ActionState.Attack);


        _animator.SetBool(
            HitId,
            _action ==
            ActionState.Hit);


        _animator.SetBool(
            DeadId,
            _hasDied);
    }


    // =============================================
    // Attack / Hit / Death 시작
    // =============================================

    private void BeginAction(
        ActionState action,
        int row)
    {
        _action =
            action;


        _actionHash =
            _states[
                row,
                _direction];


        _actionStartFrame =
            Time.frameCount;


        _actionStartTime =
            Time.time;


        SetAnimationFlags(
            false);


        // 해당 방향 애니메이션을
        // 첫 프레임부터 강제로 실행
        _animator.Play(
            _actionHash,
            0,
            0f);
    }


    // =============================================
    // Attack / Hit / Death 진행
    // =============================================

    private void UpdateAction()
    {
        if (_action ==
                ActionState.None ||
            Time.frameCount ==
                _actionStartFrame)
        {
            return;
        }


        AnimatorStateInfo state =
            _animator.GetCurrentAnimatorStateInfo(
                0);


        // Animator가 해당 State로 실제 진입했는지 확인
        if (state.fullPathHash !=
            _actionHash)
        {
            if (Time.time -
                _actionStartTime >
                1f)
            {
                Debug.LogError(
                    $"{name}: 동작 상태로 진입하지 못했습니다. " +
                    "Animator Transition 조건과 State 이름을 확인하세요.",
                    this);


                FinishAction();
            }

            return;
        }


        // ==========================================
        // Attack 애니메이션 중 폭발
        // ==========================================

        if (_action ==
                ActionState.Attack &&
            !_explosionTriggered &&
            state.normalizedTime >=
                _explodeNormalizedTime)
        {
            _explosionTriggered =
                true;


            TriggerExplosion();
        }


        // 아직 애니메이션 끝나지 않음
        if (state.normalizedTime <
            1f)
        {
            return;
        }


        // ==========================================
        // Attack 완료 후 사망
        // ==========================================

        if (_action ==
            ActionState.Attack)
        {
            BeginDeath();
            return;
        }


        FinishAction();
    }


    // =============================================
    // 실제 폭발
    // =============================================

    private void TriggerExplosion()
    {
        // 폭발 이펙트
        if (_explosionEffectPrefab !=
            null)
        {
            GameObject effect =
                Instantiate(
                    _explosionEffectPrefab,
                    transform.position,
                    Quaternion.identity);


            if (_explosionEffectLifetime >
                0f)
            {
                Destroy(
                    effect,
                    _explosionEffectLifetime);
            }
        }


        /*
         * =========================================
         * Player 데미지는 아직 연결하지 않는다.
         * =========================================
         *
         * Player 쪽은 팀원이 작업 중.
         *
         * 나중에 Player 구현 완료 후
         * 여기에서 폭발 범위 안의 Player를 찾아
         * 실제 데미지를 주도록 연결하면 된다.
         */
    }


    // =============================================
    // Death 시작
    // =============================================

    private void BeginDeath()
    {
        if (_hasDied)
            return;


        _hasDied =
            true;


        _currentHealth =
            0f;


        StopAllCoroutines();


        // Hit Flash 등이 남아있으면
        // 원래 색상으로 복구
        for (int i = 0;
             i < _renderers.Length;
             i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].color =
                    _originalColors[i];
            }
        }


        // 사망 후 충돌 제거
        foreach (
            Collider2D bodyCollider
            in _colliders)
        {
            if (bodyCollider != null)
            {
                bodyCollider.enabled =
                    false;
            }
        }


        // 물리 이동 정지
        Rigidbody2D[] bodies =
            GetComponentsInChildren<Rigidbody2D>();


        foreach (
            Rigidbody2D body
            in bodies)
        {
            if (body != null)
            {
                body.simulated =
                    false;
            }
        }


        // Animator 설정이 잘못된 경우
        // 부모 사망 처리 사용
        if (!_ready)
        {
            base.Die();
            return;
        }


        BeginAction(
            ActionState.Death,
            4);
    }


    // =============================================
    // 액션 종료
    // =============================================

    private void FinishAction()
    {
        // Death 끝
        if (_action ==
            ActionState.Death)
        {
            Destroy(
                gameObject);

            return;
        }


        _action =
            ActionState.None;


        SetAnimationFlags(
            false);


        // Hit 완료 후 현재 방향 Idle로 복귀
        _animator.Play(
            _states[
                0,
                _direction],
            0,
            0f);
    }


    // =============================================
    // Animator 검사
    // =============================================

    private bool ValidateAnimator()
    {
        if (_animator.runtimeAnimatorController ==
            null)
        {
            Debug.LogError(
                $"{name}: KamikazeMonster.controller를 " +
                "Animator Controller에 연결하세요.",
                this);

            return false;
        }


        // -------------------------
        // Parameter 검사
        // -------------------------

        int[] ids =
        {
            DirectionId,
            MovingId,
            AttackingId,
            HitId,
            DeadId
        };


        string[] names =
        {
            "Direction",
            "IsMoving",
            "IsAttacking",
            "IsHit",
            "IsDead"
        };


        for (int i = 0;
             i < ids.Length;
             i++)
        {
            AnimatorControllerParameterType expectedType =
                i == 0
                    ? AnimatorControllerParameterType.Int
                    : AnimatorControllerParameterType.Bool;


            bool found =
                false;


            foreach (
                AnimatorControllerParameter parameter
                in _animator.parameters)
            {
                if (parameter.nameHash ==
                        ids[i] &&
                    parameter.type ==
                        expectedType)
                {
                    found =
                        true;

                    break;
                }
            }


            if (!found)
            {
                Debug.LogError(
                    $"{name}: Animator에 " +
                    $"{names[i]} ({expectedType}) " +
                    "파라미터가 필요합니다.",
                    this);

                return false;
            }
        }


        // -------------------------
        // State 검사
        // -------------------------

        string[] motions =
        {
            "idle",
            "walk",
            "attack",
            "hit",
            "death"
        };


        for (int row = 0;
             row < motions.Length;
             row++)
        {
            for (int direction = 0;
                 direction <
                 Directions.Length;
                 direction++)
            {
                string path =
                    $"Base Layer.KamikazeMonster_" +
                    $"{motions[row]}_" +
                    $"{Directions[direction]}";


                int stateHash =
                    Animator.StringToHash(
                        path);


                // Hit 파일이 대문자 Hit으로
                // 만들어진 경우도 자동 지원
                if (row == 3 &&
                    !_animator.HasState(
                        0,
                        stateHash))
                {
                    path =
                        $"Base Layer.KamikazeMonster_Hit_" +
                        $"{Directions[direction]}";


                    stateHash =
                        Animator.StringToHash(
                            path);
                }


                _states[
                    row,
                    direction] =
                    stateHash;


                if (!_animator.HasState(
                        0,
                        stateHash))
                {
                    Debug.LogError(
                        $"{name}: Animator 상태를 찾을 수 없습니다: {path}",
                        this);

                    return false;
                }
            }
        }


        return true;
    }
}