using _02.Scripts.Monster.Presentation;
using UnityEngine;

public class RangeMonster : NormalMonster
{

    [Header("원거리 공격")]
    [SerializeField, Min(0.1f)] private float _attackRange = 6f;
    [SerializeField, Min(0f)] private float _keepDistance = 4f;
    [SerializeField, Min(0f)] private float _attackCooldown = 2f;

    [Header("투사체")]
    [SerializeField] private RangeProjectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [Header("애니메이션 발사 시점")]
    [Tooltip("0.5는 공격 모션의 절반에서 한 번 발사합니다.")]
    [SerializeField, Range(0f, 0.95f)] private float _fireNormalizedTime = 0.5f;
    [Tooltip("Attack 모션 길이를 알아내지 못할 때 사용하는 기본 시간입니다.")]
    [SerializeField, Min(0.1f)] private float _attackFallbackDuration = 0.8f;

    [Header("총구 섬광")]
    [SerializeField] private bool _showMuzzleFlash = true;
    [SerializeField] private Color _muzzleFlashColor = new Color(0.25f, 0.85f, 1f, 1f);

    [Header("발사 위치 기준")]
    [SerializeField] private Vector2 _firePointReferenceDirection = Vector2.down;

    [Header("이동")]
    [SerializeField, Min(0f)] private float _crowdAvoidanceSpeed = 1.5f;
    [SerializeField, Min(0f)] private float _distanceTolerance = 0.1f;

    [Header("방향 안정화")]
    [SerializeField, Range(0f, 20f)] private float _directionHysteresis = 7f;
    [SerializeField, Min(0.1f)] private float _facingLockDistance = 1.5f;

    [Header("걷기 애니메이션")]
    [SerializeField, Min(0.1f)] private float _walkAnimationSpeed = 1.6f;
    [SerializeField, Min(0.1f)] private float _attackAnimationSpeed = 1f;
    [Tooltip("공격 모션과 실제 공격 빈도를 같은 배율로 변경합니다. 2이면 원래의 2배입니다.")]
    [SerializeField, Min(0.1f)] private float _attackSpeedMultiplier = 2f;

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
    private Animator _animator;
    private SpriteRenderer[] _renderers;
    private Color[] _colors;
    private Collider2D[] _colliders;
    private Rigidbody2D[] _bodies;
    private MonsterVisualLock _visualLock;
    private NormalMonsterGameFeel _gameFeel;
    private ActionState _action;
    private bool _ready;
    private bool _hasDied;
    private bool _shotFired;
    private bool _directionInitialized;
    private bool _wasMoving;
    private int _direction;
    private int _actionHash;
    private int _actionStartFrame;
    private float _actionStartTime;
    private float _nextAnimationRetry;
    private float _nextPlayerSearch;
    private float _cooldown;
    private float _attackDuration;
    private bool _attackDurationResolved;
    private Vector2 _shotDirection;
    private Vector3 _fireOffset;
    protected override void Awake()
    {
        base.Awake();
        ClampSettings();
        _gameFeel = GetComponent<NormalMonsterGameFeel>();
        _visualLock = new MonsterVisualLock(transform);
        _animator = GetComponent<Animator>();
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
        _fireOffset = _firePoint != null ? transform.InverseTransformPoint(_firePoint.position) : Vector3.zero;
        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    protected override void Start()
    {
        base.Start();
        if (_player != null && (_player == transform || _player.IsChildOf(transform)))
        {
            _player = null;
        }

        _ready = ValidateAnimator();
        if (_projectilePrefab == null)
        {
            Debug.LogError($"{name}: Projectile Prefab이 비어 있습니다. RangeProjectile 프리팹을 연결하세요.", this);
        }

        if (!_ready)
        {
            return;
        }

        _animator.SetInteger(DirectionId, 0);
        PlayLocomotion(false);
    }

    protected override void Update()
    {
        if (_hasDied)
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

        _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
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
        float distance = toPlayer.magnitude;
        bool clearAttackPath = HasObstacleLineOfSight(transform.position, _player.position);
        if (!_directionInitialized || distance > _facingLockDistance)
        {
            SetDirection(toPlayer);
        }

        Vector2 movement = Vector2.zero;
        if (!clearAttackPath)
        {
            // 사거리 안이어도 벽 너머라면 공격하지 않고 통로를 찾습니다.
            movement = GetNavigationMovement(_player.position, Mathf.Max(0f, _moveSpeed) * Time.deltaTime);
        }
        else if (distance > _attackRange + _distanceTolerance)
        {
            movement = GetNavigationMovement(_player.position, Mathf.Min(Mathf.Max(0f, _moveSpeed) * Time.deltaTime, distance - _attackRange));
        }
        else if (distance < _keepDistance - _distanceTolerance)
        {
            movement = -toPlayer.normalized * Mathf.Min(Mathf.Max(0f, _moveSpeed) * Time.deltaTime, _keepDistance - distance);
        }
        else if (TryAttack(toPlayer, distance))
        {
            return;
        }

        if (distance > _facingLockDistance)
        {
            movement += GetSeparationDirection() * (_crowdAvoidanceSpeed * Time.deltaTime);
        }

        Vector2 previous = transform.position;
        MoveWithCollisionCheck(movement);
        bool moving = ((Vector2)transform.position - previous).sqrMagnitude > 0.000001f;
        // 벽에 막혀 후퇴하지 못하는 경우에도 사거리 안이면 공격할 수 있습니다.
        if (!moving && TryAttack(toPlayer, distance))
        {
            return;
        }

        // 후퇴하거나 옆으로 밀려도 시선은 플레이어 쪽을 유지합니다.
        PlayLocomotion(moving);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        _visualLock.Restore();
    }

    public override void TakeDamage(float damage)
    {
        if (_hasDied)
        {
            return;
        }

        base.TakeDamage(damage);
    }

    protected override void OnHit()
    {
        if (_hasDied || !_ready || _action == ActionState.Hit)
        {
            return;
        }

        BeginAction(ActionState.Hit, 3);
    }

    protected override void Die()
    {
        if (_hasDied)
        {
            return;
        }

        _hasDied = true;
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

    private bool ResolvePlayer()
    {
        if (RequiresRoomEntry)
        {
            return TryGetRoomPlayer(out _);
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
        if (!force && _directionInitialized && candidate != _direction && Mathf.Abs(Mathf.DeltaAngle(_direction * 45f - 90f, angle)) <= 22.5f + _directionHysteresis)
        {
            return;
        }

        _direction = candidate;
        _directionInitialized = true;
        _animator.SetInteger(DirectionId, _direction);
    }

    private bool TryAttack(Vector2 toPlayer, float distance)
    {
        if (_cooldown > 0f || _projectilePrefab == null || _player == null || distance > _attackRange + _distanceTolerance || toPlayer.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        if (!HasObstacleLineOfSight(transform.position, _player.position))
        {
            return false;
        }

        _shotDirection = toPlayer.normalized;
        // 실제 발사 방향을 공격 시작 시에만 확정하고 모션 중에는 유지합니다.
        SetDirection(_shotDirection);
        _cooldown = _attackCooldown / _attackSpeedMultiplier;
        _shotFired = false;
        BeginAction(ActionState.Attack, 2);
        return true;
    }

    private void SetAnimationFlags(bool moving)
    {
        _animator.SetBool(MovingId, moving && _action == ActionState.None && !_hasDied);
        _animator.SetBool(AttackingId, _action == ActionState.Attack);
        _animator.SetBool(HitId, _action == ActionState.Hit);
        _animator.SetBool(DeadId, _hasDied);
    }

    private void PlayLocomotion(bool moving)
    {
        SetAnimationFlags(moving);
        _animator.speed = moving ? _walkAnimationSpeed : 1f;
        int desiredHash = _states[moving ? 1 : 0, _direction];
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.fullPathHash != desiredHash)
        {
            _animator.Play(desiredHash, 0, moving && _wasMoving ? Mathf.Repeat(info.normalizedTime, 1f) : 0f);
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
        if (action == ActionState.Attack)
        {
            _attackDuration = _attackFallbackDuration / (_attackAnimationSpeed * _attackSpeedMultiplier);
            _attackDurationResolved = false;
        }

        _wasMoving = false;
        _animator.speed = action == ActionState.Attack ? _attackAnimationSpeed * _attackSpeedMultiplier : 1f;
        if (_gameFeel != null && _gameFeel.IsConfigured)
        {
            if (action == ActionState.Attack)
            {
                _gameFeel.BeginRangeAttack(_shotDirection, GetMuzzlePosition());
            }
            else
            {
                _gameFeel.CancelAttackVisual();
            }
        }

        SetAnimationFlags(false);
        _animator.Play(_actionHash, 0, 0f);
    }

    private void UpdateAction()
    {
        if (_action == ActionState.None || Time.frameCount == _actionStartFrame)
        {
            return;
        }

        float elapsed = Time.time - _actionStartTime;
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        bool matching = info.fullPathHash == _actionHash;
        bool useNextState = false;
        if (!matching && _animator.IsInTransition(0))
        {
            AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
            if (next.fullPathHash == _actionHash)
            {
                info = next;
                matching = true;
                useNextState = true;
            }
        }

        float maximum = _action == ActionState.Attack ? _maximumAttackTime / _attackSpeedMultiplier : (_action == ActionState.Hit ? _maximumHitTime : _maximumDeathTime);
        float fallbackDuration = maximum;
        if (_action == ActionState.Attack)
        {
            ResolveAttackDuration(info, matching, useNextState);
            fallbackDuration = _attackDuration;
            float progress = Mathf.Max(matching ? info.normalizedTime : 0f, elapsed / fallbackDuration);
            // 전환이나 자기 재진입으로 모션이 초기화되어도 한 공격에서 한 번만 발사합니다.
            if (!_shotFired && (progress >= _fireNormalizedTime || elapsed >= maximum))
            {
                FireProjectile();
            }
        }

        bool timedAttackFinished = _action == ActionState.Attack && elapsed >= fallbackDuration;
        if ((matching && info.normalizedTime >= 1f) || timedAttackFinished || elapsed >= maximum)
        {
            FinishAction();
            return;
        }

        if (!matching && Time.time >= _nextAnimationRetry)
        {
            SetAnimationFlags(false);
            _animator.Play(_actionHash, 0, Mathf.Clamp(elapsed / fallbackDuration, 0f, 0.95f));
            _nextAnimationRetry = Time.time + 0.2f;
        }
    }

    private void ResolveAttackDuration(AnimatorStateInfo info, bool matching, bool useNextState)
    {
        if (_attackDurationResolved || !matching)
        {
            return;
        }

        float speed = Mathf.Abs(info.speed * info.speedMultiplier * _animator.speed);
        if (speed <= 0.000001f)
        {
            return;
        }

        AnimatorClipInfo[] clips = useNextState ? _animator.GetNextAnimatorClipInfo(0) : _animator.GetCurrentAnimatorClipInfo(0);
        float duration = 0f;
        foreach (AnimatorClipInfo clipInfo in clips)
        {
            if (clipInfo.clip != null && clipInfo.weight > 0f)
            {
                duration = Mathf.Max(duration, clipInfo.clip.length / speed);
            }
        }

        if (duration <= 0f)
        {
            return;
        }

        _attackDuration = Mathf.Clamp(duration, 0.025f, _maximumAttackTime / _attackSpeedMultiplier);
        _attackDurationResolved = true;
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

    private Vector3 GetMuzzlePosition()
    {
        Vector2 reference = _firePointReferenceDirection.sqrMagnitude > 0.0001f ? _firePointReferenceDirection.normalized : Vector2.down;
        float rotation = Vector2.SignedAngle(reference, _shotDirection);
        return transform.TransformPoint(Quaternion.Euler(0f, 0f, rotation) * _fireOffset);
    }

    private void FireProjectile()
    {
        if (_shotFired || _hasDied || _action != ActionState.Attack || _projectilePrefab == null || _player == null)
        {
            return;
        }

        Vector3 position = GetMuzzlePosition();
        _shotFired = true;
        RangeProjectile projectile = Instantiate(_projectilePrefab, position, Quaternion.identity);
        projectile.gameObject.SetActive(true);
        projectile.Initialize(_shotDirection, transform, _player);
        if (_gameFeel != null && _gameFeel.IsConfigured)
        {
            _gameFeel.RangeRelease(position, _shotDirection);
        }
        else if (_showMuzzleFlash)
        {
            MonsterCombatEffect.SpawnImpact(position, _shotDirection, _muzzleFlashColor, transform);
        }
    }

    private bool ValidateAnimator()
    {
        if (!MonsterCombat.ValidateAnimatorParameters(_animator))
        {
            return false;
        }

        string[] motions = { "idle", "walk", "attack", "hit", "death" };
        for (int row = 0; row < motions.Length; row++)
        {
            for (int direction = 0; direction < 8; direction++)
            {
                if (!MonsterCombat.TryGetDirectionalState(_animator, "RangeMonster", motions[row], direction, out _states[row, direction]))
                {
                    Debug.LogError($"{name}: RangeMonster_{motions[row]}_" + $"{MonsterCombat.DirectionNames[direction]} 상태를 찾을 수 없습니다.", this);
                    return false;
                }
            }
        }

        return true;
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    private void ClampSettings()
    {
        _attackRange = Mathf.Max(0.1f, _attackRange);
        _keepDistance = Mathf.Clamp(_keepDistance, 0f, _attackRange);
        _attackCooldown = Mathf.Max(0f, _attackCooldown);
        _fireNormalizedTime = Mathf.Clamp(_fireNormalizedTime, 0f, 0.95f);
        _crowdAvoidanceSpeed = Mathf.Max(0f, _crowdAvoidanceSpeed);
        _distanceTolerance = Mathf.Clamp(_distanceTolerance, 0f, _attackRange * 0.25f);
        _directionHysteresis = Mathf.Clamp(_directionHysteresis, 0f, 20f);
        _facingLockDistance = Mathf.Max(0.1f, _facingLockDistance);
        _walkAnimationSpeed = Mathf.Max(0.1f, _walkAnimationSpeed);
        _attackAnimationSpeed = Mathf.Max(0.1f, _attackAnimationSpeed);
        _attackSpeedMultiplier = Mathf.Max(0.1f, _attackSpeedMultiplier);
        _maximumAttackTime = Mathf.Max(0.1f, _maximumAttackTime);
        _attackFallbackDuration = Mathf.Clamp(_attackFallbackDuration, 0.1f, _maximumAttackTime);
        _maximumHitTime = Mathf.Max(0.1f, _maximumHitTime);
        _maximumDeathTime = Mathf.Max(0.1f, _maximumDeathTime);
    }
}
