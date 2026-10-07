using _02.Scripts.Monster.Presentation;
using UnityEngine;

public class MeleeMonster : NormalMonster
{

    [Header("방향 애니메이션")]
    [SerializeField] private Animator _directionAnimator;
    [Tooltip("현재 8방향의 중심에서 이 각도 이상 벗어나야 방향을 바꿉니다.")]
    [SerializeField, Range(22.5f, 45f)] private float _directionChangeAngle = 30f;
    [SerializeField, Min(0.1f)] private float _facingLockDistance = 1.7f;

    [Header("근접 공격")]
    [SerializeField, Min(0.1f)] private float _attackRange = 1.5f;
    [SerializeField, Range(1f, 360f)] private float _attackAngle = 90f;
    [SerializeField, Min(0f)] private float _attackCooldown = 0.8f;
    [SerializeField, Range(0f, 0.95f)] private float _attackHitNormalizedTime = 0.45f;

    [Header("애니메이션 속도")]
    [SerializeField, Min(0.1f)] private float _walkAnimationSpeed = 1.6f;
    [SerializeField, Min(0.1f)] private float _attackAnimationSpeed = 1.8f;
    [Tooltip("공격 모션과 실제 공격 빈도를 같은 배율로 변경합니다. 2이면 원래의 2배입니다.")]
    [SerializeField, Min(0.1f)] private float _attackSpeedMultiplier = 2f;

    [Header("근접 공격 효과")]
    [Tooltip("비워 두면 기본 픽셀 베기 효과를 표시합니다. 프리팹의 앞 방향은 +X입니다.")]
    [SerializeField] private GameObject _attackEffectPrefab;
    [SerializeField, Min(0.1f)] private float _attackEffectLifetime = 0.4f;
    [SerializeField] private Color _attackEffectColor = new Color(1f, 0.8f, 0.25f, 1f);

    [Header("동작 대기 제한")]
    [SerializeField, Min(0.1f)] private float _maximumAttackTime = 3f;
    [SerializeField, Min(0.1f)] private float _maximumHitTime = 1.5f;
    [SerializeField, Min(0.1f)] private float _maximumDeathTime = 3f;
    private enum ActionState
    {
        None,
        Attack,
        Hit,
        Death
    }

    private static readonly int DirectionId = Animator.StringToHash("Direction");
    private static readonly int MovingId = Animator.StringToHash("IsMoving");
    private static readonly int AttackingId = Animator.StringToHash("IsAttacking");
    private static readonly int HitId = Animator.StringToHash("IsHit");
    private static readonly int DeadId = Animator.StringToHash("IsDead");
    private readonly int[,] _states = new int[5, 8];
    private SpriteRenderer[] _renderers;
    private Color[] _colors;
    private Collider2D[] _colliders;
    private Rigidbody2D[] _bodies;
    private Collider2D[] _playerColliders;
    private MonsterVisualLock _visualLock;
    private NormalMonsterGameFeel _gameFeel;
    private ActionState _action;
    private bool _ready;
    private Transform _roomPlayer;
    private bool _deathStarted;
    private bool _directionInitialized;
    private bool _attackHitApplied;
    private bool _wasMoving;
    private int _direction;
    private int _actionHash;
    private int _actionStartFrame;
    private float _actionStartTime;
    private float _nextAnimationRetry;
    private float _attackCooldownTimer;
    private float _nextPlayerSearch;
    private Vector2 _attackDirection;
    protected override void Awake()
    {
        base.Awake();
        ClampSettings();
        _gameFeel = GetComponent<NormalMonsterGameFeel>();
        _visualLock = new MonsterVisualLock(transform);
        if (_directionAnimator == null)
        {
            _directionAnimator = GetComponent<Animator>();
        }

        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _bodies = GetComponentsInChildren<Rigidbody2D>(true);
        _colors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _colors[i] = _renderers[i].color;
            _renderers[i].flipX = false;
            _renderers[i].flipY = false;
        }

        transform.rotation = Quaternion.identity;
        if (_directionAnimator != null)
        {
            _directionAnimator.applyRootMotion = false;
            _directionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    protected override void Start()
    {
        base.Start();
        CachePlayer();
        _ready = MonsterCombat.ValidateAnimatorParameters(_directionAnimator);
        if (!_ready)
        {
            return;
        }

        string[] motions = { "idle", "walk", "attack", "hit", "death" };
        for (int row = 0; row < motions.Length; row++)
        {
            for (int direction = 0; direction < 8; direction++)
            {
                MonsterCombat.TryGetDirectionalState(_directionAnimator, "MeleeMonster", motions[row], direction, out _states[row, direction]);
            }
        }

        // 상태 이름이 다른 기존 컨트롤러는 Attack / Hit / Death 태그로도 동작합니다.
        _directionAnimator.SetInteger(DirectionId, 0);
        PlayLocomotion(false);
    }

    protected override void Update()
    {
        if (_deathStarted)
        {
            if (_ready)
            {
                UpdateAction();
            }

            return;
        }

        base.Update();
        if (!_ready)
        {
            return;
        }

        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
        if (_action != ActionState.None)
        {
            UpdateAction();
            return;
        }

        if (!ResolvePlayer() || !CanTrackPlayer())
        {
            PlayLocomotion(false);
            return;
        }

        Vector2 toPlayer = (Vector2)_player.position - (Vector2)transform.position;
        float distance = Mathf.Sqrt(MonsterCombat.GetPlayerDistanceSquared(_player, _playerColliders, transform.position, out Vector2 closestPoint));
        bool clearAttackPath = MonsterCombat.HasClearAttackPath(transform.position, closestPoint, transform, _player);
        if (!_directionInitialized || distance > _facingLockDistance)
        {
            SetDirection(toPlayer);
        }

        if (distance <= _attackRange && clearAttackPath)
        {
            // 붙은 상태에서는 밀어내기와 매 프레임 방향 갱신을 멈춥니다.
            PlayLocomotion(false);
            TryAttack(toPlayer, closestPoint);
            return;
        }

        float moveDistance = Mathf.Max(0f, _moveSpeed) * Time.deltaTime;
        if (clearAttackPath)
        {
            moveDistance = Mathf.Min(moveDistance, Mathf.Max(0f, distance - _attackRange));
        }

        Vector2 movement = GetNavigationMovement(_player.position, moveDistance);
        if (distance > _facingLockDistance)
        {
            movement += GetSeparationDirection() * (1.5f * Time.deltaTime);
        }

        Vector2 previous = transform.position;
        MoveWithCollisionCheck(movement);
        PlayLocomotion(((Vector2)transform.position - previous).sqrMagnitude > 0.000001f);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        _visualLock.Restore();
    }

    public override void TakeDamage(float damage)
    {
        if (_deathStarted)
        {
            return;
        }

        base.TakeDamage(damage);
    }

    protected override void OnHit()
    {
        if (_deathStarted || !_ready || _action == ActionState.Hit)
        {
            return;
        }

        // 피해를 받으면 아직 판정하지 않은 근접 공격을 취소합니다.
        BeginAction(ActionState.Hit, 3);
    }

    protected override void Die()
    {
        if (_deathStarted)
        {
            return;
        }

        _deathStarted = true;
        StopMovementForDeath();
        _currentHealth = 0f;
        StopAllCoroutines();
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].color = _colors[i];
            }
        }

        foreach (Collider2D bodyCollider in _colliders)
        {
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }
        }

        foreach (Rigidbody2D body in _bodies)
        {
            if (body != null)
            {
                body.simulated = false;
            }
        }

        // 이동은 멈추고 기존 Death 애니메이션을 재생합니다.
        if (!_ready)
        {
            Destroy(gameObject, 0.2f);
            return;
        }

        BeginAction(ActionState.Death, 4);
    }

    private void CachePlayer()
    {
        if (_player != null && (_player == transform || _player.IsChildOf(transform)))
        {
            _player = null;
        }

        _playerColliders = _player != null ? _player.GetComponentsInChildren<Collider2D>(true) : null;
    }

    private bool ResolvePlayer()
    {
        if (RequiresRoomEntry)
        {
            if (!TryGetRoomPlayer(out Transform roomPlayer))
            {
                return false;
            }

            if (_roomPlayer != roomPlayer)
            {
                _roomPlayer = roomPlayer;
                CachePlayer();
            }

            return _player != null;
        }

        if (_player != null && _player.gameObject.activeInHierarchy)
        {
            return true;
        }

        if (Time.time < _nextPlayerSearch)
        {
            return false;
        }

        _nextPlayerSearch = Time.time + 1f;
        _player = MonsterCombat.FindPlayer(transform);
        CachePlayer();
        return _player != null;
    }

    private void SetDirection(Vector2 direction, bool force = false)
    {
        if (direction.sqrMagnitude < 0.000001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int candidate = MonsterCombat.GetDirectionIndex(direction);
        if (!force && _directionInitialized && candidate != _direction && Mathf.Abs(Mathf.DeltaAngle(_direction * 45f - 90f, angle)) <= _directionChangeAngle)
        {
            return;
        }

        _direction = candidate;
        _directionInitialized = true;
        _directionAnimator.SetInteger(DirectionId, _direction);
    }

    private void TryAttack(Vector2 toPlayer, Vector2 closestPoint)
    {
        if (_attackCooldownTimer > 0f || _player == null)
        {
            return;
        }

        Vector2 facing = MonsterCombat.GetDirectionVector(_direction);
        Vector2 targetDirection = closestPoint - (Vector2)transform.position;
        if (targetDirection.sqrMagnitude < 0.000001f)
        {
            targetDirection = toPlayer;
        }

        // 플레이어가 확실히 뒤로 이동했을 때만 다음 공격 시작 시 방향을 바꿉니다.
        if (toPlayer.sqrMagnitude > 0.0625f && Vector2.Angle(facing, targetDirection) > _attackAngle * 0.5f)
        {
            SetDirection(targetDirection, true);
        }

        _attackDirection = MonsterCombat.GetDirectionVector(_direction);
        if (targetDirection.sqrMagnitude > 0.000001f && Vector2.Angle(_attackDirection, targetDirection) > _attackAngle * 0.5f)
        {
            return;
        }

        if (!MonsterCombat.HasClearAttackPath(transform.position, closestPoint, transform, _player))
        {
            return;
        }

        _attackCooldownTimer = _attackCooldown / _attackSpeedMultiplier;
        _attackHitApplied = false;
        BeginAction(ActionState.Attack, 2);
    }

    private void ApplyAttackHit()
    {
        SpawnAttackEffect();
        Vector2 origin = transform.position;
        float squaredDistance = MonsterCombat.GetPlayerDistanceSquared(_player, _playerColliders, origin, out Vector2 closestPoint);
        if (squaredDistance > _attackRange * _attackRange)
        {
            return;
        }

        Vector2 targetDirection = closestPoint - origin;
        if (targetDirection.sqrMagnitude < 0.000001f && _player != null)
        {
            targetDirection = (Vector2)_player.position - origin;
        }

        if (targetDirection.sqrMagnitude > 0.000001f && Vector2.Angle(_attackDirection, targetDirection) > _attackAngle * 0.5f)
        {
            return;
        }

        if (!MonsterCombat.HasClearAttackPath(origin, closestPoint, transform, _player))
        {
            return;
        }

        Vector3 hitPosition = new Vector3(closestPoint.x, closestPoint.y, transform.position.z);
        bool enhanced = _gameFeel != null && _gameFeel.IsConfigured;
        bool damaged = MonsterCombat.TryTakePlayerDamage(_player, _contactDamage, hitPosition, _attackDirection, _attackEffectColor, !enhanced);
        if (damaged && enhanced)
        {
            _gameFeel.MeleeImpact(hitPosition, _attackDirection);
        }
    }

    private void SpawnAttackEffect()
    {
        if (_gameFeel != null && _gameFeel.IsConfigured)
        {
            _gameFeel.MeleeRelease(_attackDirection, _attackRange, _attackAngle);
            return;
        }

        if (_attackEffectPrefab != null)
        {
            float angle = Mathf.Atan2(_attackDirection.y, _attackDirection.x) * Mathf.Rad2Deg;
            GameObject effect = Instantiate(_attackEffectPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
            Destroy(effect, _attackEffectLifetime);
            return;
        }

        MonsterCombatEffect.SpawnSlash(transform.position, _attackDirection, _attackRange, _attackAngle, _attackEffectColor, transform);
    }

    private void SetAnimationFlags(bool moving)
    {
        _directionAnimator.SetBool(MovingId, moving && _action == ActionState.None && !_deathStarted);
        _directionAnimator.SetBool(AttackingId, _action == ActionState.Attack);
        _directionAnimator.SetBool(HitId, _action == ActionState.Hit);
        _directionAnimator.SetBool(DeadId, _deathStarted);
    }

    private void PlayLocomotion(bool moving)
    {
        SetAnimationFlags(moving);
        _directionAnimator.speed = moving ? _walkAnimationSpeed : 1f;
        int desiredHash = _states[moving ? 1 : 0, _direction];
        AnimatorStateInfo info = _directionAnimator.GetCurrentAnimatorStateInfo(0);
        if (desiredHash != 0 && info.fullPathHash != desiredHash)
        {
            _directionAnimator.Play(desiredHash, 0, moving && _wasMoving ? Mathf.Repeat(info.normalizedTime, 1f) : 0f);
        }

        _wasMoving = moving;
    }

    private void BeginAction(ActionState action, int row)
    {
        _action = action;
        _actionHash = _states[row, _direction];
        _actionStartFrame = Time.frameCount;
        _actionStartTime = Time.time;
        _nextAnimationRetry = Time.time + 0.2f;
        _wasMoving = false;
        _directionAnimator.speed = action == ActionState.Attack ? _attackAnimationSpeed * _attackSpeedMultiplier : 1f;
        if (_gameFeel != null && _gameFeel.IsConfigured)
        {
            if (action == ActionState.Attack)
            {
                _gameFeel.BeginMeleeAttack(_attackDirection, _attackRange);
            }
            else
            {
                _gameFeel.CancelAttackVisual();
            }
        }

        SetAnimationFlags(false);
        if (_actionHash != 0)
        {
            _directionAnimator.Play(_actionHash, 0, 0f);
        }
    }

    private void UpdateAction()
    {
        if (_action == ActionState.None || Time.frameCount == _actionStartFrame)
        {
            return;
        }

        float elapsed = Time.time - _actionStartTime;
        string tag = _action.ToString();
        AnimatorStateInfo info = _directionAnimator.GetCurrentAnimatorStateInfo(0);
        bool matching = MonsterCombat.IsActionState(info, _actionHash, tag);
        float maximum = _action == ActionState.Attack ? _maximumAttackTime / _attackSpeedMultiplier : (_action == ActionState.Hit ? _maximumHitTime : _maximumDeathTime);
        if (matching && _action == ActionState.Attack && !_attackHitApplied && info.normalizedTime >= _attackHitNormalizedTime)
        {
            _attackHitApplied = true;
            ApplyAttackHit();
        }

        if ((matching && info.normalizedTime >= 1f) || elapsed >= maximum)
        {
            FinishAction();
            return;
        }

        if (!matching && _actionHash != 0 && Time.time >= _nextAnimationRetry)
        {
            SetAnimationFlags(false);
            _directionAnimator.Play(_actionHash, 0, Mathf.Clamp(elapsed / maximum, 0f, 0.95f));
            _nextAnimationRetry = Time.time + 0.2f;
        }
        else if (!matching && _actionHash == 0 && elapsed >= 0.75f)
        {
            Debug.LogWarning($"{name}: {_action} 상태의 이름 또는 {tag} 태그와 전환 조건을 확인하세요.", this);
            FinishAction();
        }
    }

    private void FinishAction()
    {
        if (_action == ActionState.Death)
        {
            Destroy(gameObject);
            return;
        }

        _action = ActionState.None;
        if (_gameFeel != null)
        {
            _gameFeel.CancelAttackVisual();
        }

        PlayLocomotion(false);
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    private void ClampSettings()
    {
        _attackRange = Mathf.Max(0.1f, _attackRange);
        _attackAngle = Mathf.Clamp(_attackAngle, 1f, 360f);
        _attackCooldown = Mathf.Max(0f, _attackCooldown);
        _attackHitNormalizedTime = Mathf.Clamp(_attackHitNormalizedTime, 0f, 0.95f);
        _directionChangeAngle = Mathf.Clamp(_directionChangeAngle, 22.5f, 45f);
        _facingLockDistance = Mathf.Max(_attackRange, _facingLockDistance);
        _walkAnimationSpeed = Mathf.Max(0.1f, _walkAnimationSpeed);
        _attackAnimationSpeed = Mathf.Max(0.1f, _attackAnimationSpeed);
        _attackSpeedMultiplier = Mathf.Max(0.1f, _attackSpeedMultiplier);
        _attackEffectLifetime = Mathf.Max(0.1f, _attackEffectLifetime);
        _maximumAttackTime = Mathf.Max(0.1f, _maximumAttackTime);
        _maximumHitTime = Mathf.Max(0.1f, _maximumHitTime);
        _maximumDeathTime = Mathf.Max(0.1f, _maximumDeathTime);
    }
}
