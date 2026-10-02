using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossMonster : NormalMonster
{
    [Header("보스방과 애니메이션")]
    [SerializeField] private BossRoom _room;
    [SerializeField] private Animator _directionAnimator;
    [SerializeField, Range(22.5f, 45f)] private float _directionChangeAngle = 30f;
    [Tooltip("순서: S, SE, E, NE, N, NW, W, SW. 실제 오른쪽 위 총구 끝의 자식입니다.")]
    [SerializeField] private Transform[] _gunMuzzles = new Transform[8];

    [Header("패턴과 소환몹")]
    [SerializeField] private BossAttackPattern[] _patterns = new BossAttackPattern[0];
    [SerializeField] private RangeMonster _rangeMonsterPrefab;
    [SerializeField, Min(1)] private int _maxLiveSummons = 4;
    [SerializeField, Min(1)] private int _maxActiveBullets = 240;
    [SerializeField, Min(1)] private int _maxActiveHazards = 32;
    [SerializeField, Min(0f)] private float _initialAttackDelay = 1.5f;

    [Header("피격과 동작 제한")]
    [SerializeField] private bool _interruptAttacksOnHit;
    [SerializeField, Min(0f)] private float _hitReactionCooldown = 0.6f;
    [SerializeField, Min(0.1f)] private float _maximumAttackTime = 30f;
    [SerializeField, Min(0.1f)] private float _maximumHitTime = 3f;
    [SerializeField, Min(0.1f)] private float _maximumDeathTime = 6f;

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
    private static readonly int AttackTypeId = Animator.StringToHash("AttackType");
    private static readonly string[] Motions =
    {
        "idle", "hit", "death", "attack_melee", "attack_projectile", "attack_explosion"
    };

    private readonly int[,] _states = new int[6, 8];
    private readonly List<int> _availablePatterns = new List<int>(6);
    private readonly List<RangeMonster> _summons = new List<RangeMonster>();
    private readonly HashSet<Transform> _summonOwners = new HashSet<Transform>();
    private readonly List<BossPatternProjectile> _projectiles = new List<BossPatternProjectile>();
    private readonly List<BossAreaHazard> _hazards = new List<BossAreaHazard>();
    private readonly List<int> _edgeChoices = new List<int>();
    private Transform _targetPlayer;
    private Collider2D[] _playerColliders = new Collider2D[0];
    private SpriteRenderer[] _renderers;
    private Color[] _colors;
    private Collider2D[] _colliders;
    private Rigidbody2D[] _bodies;
    private MonsterVisualLock _visualLock;
    private Transform _attackRoot;
    private SpriteRenderer _gunGlow;
    private BossAttackPattern _currentPattern;
    private ActionState _action;
    private float[] _readyTimes;
    private int _direction;
    private int _actionHash;
    private int _actionStartFrame;
    private int _patternIndex = -1;
    private int _lastPatternIndex = -1;
    private int _emittedWaves;
    private float _actionStartTime;
    private float _effectEndTime;
    private float _nextEmitTime;
    private float _nextAttackTime;
    private float _nextHitReactionTime;
    private float _nextPlayerSearch;
    private bool _ready;
    private bool _animatorReady;
    private bool _directionInitialized;
    private bool _released;
    private bool _animationFinished;
    private bool _deathStarted;
    private bool _deathPoseHeld;

    public BossRoom Room => _room;
    public Transform Player => _targetPlayer;
    public Collider2D[] PlayerColliders => _playerColliders;
    public float CurrentHealth => _currentHealth;
    public float MaximumHealth => Mathf.Max(1f, _maxHealth);

    public Transform AttackRoot
    {
        get
        {
            if (_attackRoot == null)
            {
                _attackRoot = new GameObject(name + "_BossAttacks").transform;
            }

            return _attackRoot;
        }
    }

    protected override void Awake()
    {
        _moveSpeed = 0f;
        _contactDamage = 0;
        base.Awake();
        _visualLock = new MonsterVisualLock(transform);
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _colors = new Color[_renderers.Length];
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _bodies = GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < _renderers.Length; i++)
        {
            _colors[i] = _renderers[i].color;
            _renderers[i].flipX = false;
            _renderers[i].flipY = false;
        }

        if (_directionAnimator == null)
        {
            _directionAnimator = GetComponentInChildren<Animator>();
        }

        if (_directionAnimator != null)
        {
            _directionAnimator.applyRootMotion = false;
            _directionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    protected override void Start()
    {
        if (_room != null)
        {
            transform.position = _room.BossCenterPosition;
        }

        // 고정 위치를 초기화하고 기존 방식으로 플레이어를 찾습니다.
        base.Start();
        _targetPlayer = _player;
        CachePlayer();
        // 본체 접촉 판정은 사용하지 않습니다. 실제 공격 대상은 _targetPlayer로 관리합니다.
        _player = null;
        _animatorReady = ValidateAnimator();
        _ready = ValidateEncounter() && _animatorReady;
        _readyTimes = new float[_patterns == null ? 0 : _patterns.Length];
        _nextAttackTime = Time.time + Mathf.Max(0f, _initialAttackDelay);
        if (_animatorReady && _deathStarted)
        {
            BeginAction(ActionState.Death, _states[2, _direction], 1f);
        }
        else if (_animatorReady)
        {
            PlayIdle();
        }
    }

    protected override void Update()
    {
        if (_deathStarted)
        {
            UpdateDeath();
            return;
        }

        // 공통 피격 쿨다운과 색상 효과를 유지합니다. 추적이나 이동은 호출하지 않습니다.
        base.Update();
        if (!_ready || Time.deltaTime <= 0f)
        {
            return;
        }

        ResolvePlayer();
        if (_action == ActionState.Attack)
        {
            UpdateAttack();
            return;
        }

        if (_action == ActionState.Hit)
        {
            UpdateHit();
            return;
        }

        if (_targetPlayer == null || !_targetPlayer.gameObject.activeInHierarchy ||
            !_room.Contains(_targetPlayer.position, 0f))
        {
            PlayIdle();
            return;
        }

        SetDirection((Vector2)_targetPlayer.position - (Vector2)transform.position, false);
        if (Time.time >= _nextAttackTime && TrySelectPattern(out int index))
        {
            SetDirection((Vector2)_targetPlayer.position - (Vector2)transform.position, true);
            _patternIndex = index;
            _lastPatternIndex = index;
            _currentPattern = _patterns[index];
            _released = false;
            _animationFinished = false;
            _emittedWaves = 0;
            BeginAction(ActionState.Attack, _states[2 + (int)_currentPattern.AttackType, _direction],
                _currentPattern.AnimationSpeed);
            if (_currentPattern.AttackType == BossAttackType.Projectile)
            {
                CreateGunGlow();
            }

            return;
        }

        PlayIdle();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        _visualLock.Restore();
        foreach (SpriteRenderer renderer in _renderers)
        {
            if (renderer != null)
            {
                renderer.flipX = false;
                renderer.flipY = false;
            }
        }
    }

    // 패턴만 피해를 주므로 공통 본체 접촉 피해·접촉 넉백은 호출하지 않습니다.

    protected override void OnHit()
    {
        if (_deathStarted || !_ready || _action == ActionState.Hit || Time.time < _nextHitReactionTime ||
            (_action == ActionState.Attack && !_interruptAttacksOnHit))
        {
            return;
        }

        if (_action == ActionState.Attack)
        {
            ClearOwnedAttacks();
        }

        EndPattern();
        _nextHitReactionTime = Time.time + Mathf.Max(0f, _hitReactionCooldown);
        BeginAction(ActionState.Hit, _states[1, _direction], 1f);
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
        ClearOwnedAttacks();
        ClearSummonsAndTheirBullets();
        _currentPattern = null;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].color = _colors[i];
            }
        }

        foreach (Collider2D collider in _colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        foreach (Rigidbody2D body in _bodies)
        {
            if (body != null)
            {
                body.simulated = false;
            }
        }

        if (_animatorReady)
        {
            BeginAction(ActionState.Death, _states[2, _direction], 1f);
        }
        // base.Die()는 루트를 삭제하므로 호출하지 않습니다.
    }

    public void HitArea(Vector2 center, float radius, float damage, Color color)
    {
        if (_deathStarted || _targetPlayer == null || !_targetPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        float squaredDistance = MonsterCombat.GetPlayerDistanceSquared(
            _targetPlayer, _playerColliders, center, out Vector2 closestPoint);
        if (squaredDistance > radius * radius ||
            !MonsterCombat.HasClearAttackPath(center, closestPoint, transform, _targetPlayer))
        {
            return;
        }

        Vector2 direction = closestPoint - center;
        MonsterCombat.TryTakePlayerDamage(_targetPlayer, damage, closestPoint, direction.normalized, color);
    }

    private bool ValidateAnimator()
    {
        if (!MonsterCombat.ValidateAnimatorParameters(_directionAnimator))
        {
            return false;
        }

        bool valid = false;
        foreach (AnimatorControllerParameter parameter in _directionAnimator.parameters)
        {
            if (parameter.nameHash == AttackTypeId && parameter.type == AnimatorControllerParameterType.Int)
            {
                valid = true;
                break;
            }
        }

        if (!valid)
        {
            Debug.LogError($"{name}: Animator에 Int 타입 AttackType을 추가하세요.", this);
        }

        for (int row = 0; row < Motions.Length; row++)
        {
            for (int direction = 0; direction < 8; direction++)
            {
                if (!MonsterCombat.TryGetDirectionalState(_directionAnimator, "BossMonster",
                    Motions[row], direction, out _states[row, direction]))
                {
                    valid = false;
                    Debug.LogError($"{name}: Base Layer.BossMonster_{Motions[row]}_" +
                        $"{MonsterCombat.DirectionNames[direction]} 상태를 찾을 수 없습니다.", this);
                }
            }
        }

        return valid;
    }

    private bool ValidateEncounter()
    {
        bool valid = true;
        if (_room == null || _room.transform == transform || _room.transform.IsChildOf(transform) ||
            !_room.ValidateSetup())
        {
            Debug.LogError($"{name}: 보스와 별도로 만든 BossRoom을 Room에 연결하세요.", this);
            valid = false;
        }

        if (GetComponents<NormalMonster>().Length != 1)
        {
            Debug.LogError($"{name}: 보스 루트에는 BossMonster만 남기세요.", this);
            valid = false;
        }

        if (_patterns == null || _patterns.Length == 0)
        {
            Debug.LogError($"{name}: Patterns에 새 패턴 6개를 연결하세요.", this);
            return false;
        }

        bool needsGun = false;
        bool needsEdges = false;
        foreach (BossAttackPattern pattern in _patterns)
        {
            if (pattern == null || (int)pattern.PatternKind < 1 || (int)pattern.PatternKind > 6)
            {
                Debug.LogError($"{name}: 비어 있거나 잘못된 패턴 항목이 있습니다.", this);
                valid = false;
                continue;
            }

            needsGun |= pattern.AttackType == BossAttackType.Projectile;
            needsEdges |= pattern.PatternKind == BossPatternKind.RandomEdgeBarrage;
            if (pattern.PatternKind == BossPatternKind.SummonRangeMonsters && _rangeMonsterPrefab == null)
            {
                Debug.LogError($"{name}: 기존 일반 RangeMonster 프리팹을 연결하세요.", this);
                valid = false;
            }
        }

        if (needsGun)
        {
            if (_gunMuzzles == null || _gunMuzzles.Length != 8)
            {
                Debug.LogError($"{name}: Gun Muzzles를 8개 연결하세요.", this);
                valid = false;
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    if (_gunMuzzles[i] == null || _gunMuzzles[i] == transform ||
                        !_gunMuzzles[i].IsChildOf(transform) ||
                        (_gunMuzzles[i].position - transform.position).sqrMagnitude < 0.0001f)
                    {
                        Debug.LogError($"{name}: {MonsterCombat.DirectionNames[i]} 방향 총구 자식을 연결하세요.", this);
                        valid = false;
                    }
                }
            }
        }

        if (needsEdges && _room != null)
        {
            int points = 0;
            foreach (Transform point in _room.BarragePoints)
            {
                if (point != null)
                {
                    points++;
                }
            }

            if (points < 2)
            {
                Debug.LogError($"{name}: BossRoom에 서로 다른 위치의 Barrage Points를 최소 2개 연결하세요.", this);
                valid = false;
            }
        }

        return valid;
    }

    private void CachePlayer()
    {
        _playerColliders = _targetPlayer != null
            ? _targetPlayer.GetComponentsInChildren<Collider2D>(true) : new Collider2D[0];
    }

    private void ResolvePlayer()
    {
        if (_targetPlayer != null && _targetPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.time < _nextPlayerSearch)
        {
            return;
        }

        _nextPlayerSearch = Time.time + 1f;
        _targetPlayer = MonsterCombat.FindPlayer(transform);
        CachePlayer();
    }

    private void SetDirection(Vector2 vector, bool force)
    {
        if (vector.sqrMagnitude < 0.000001f)
        {
            return;
        }

        float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        if (force || !_directionInitialized ||
            Mathf.Abs(Mathf.DeltaAngle(_direction * 45f - 90f, angle)) > _directionChangeAngle)
        {
            _direction = MonsterCombat.GetDirectionIndex(vector);
            _directionInitialized = true;
        }
    }

    private bool TrySelectPattern(out int index)
    {
        index = -1;
        _availablePatterns.Clear();
        for (int i = 0; i < _patterns.Length; i++)
        {
            if (Time.time < _readyTimes[i] || (_patterns[i].PatternKind == BossPatternKind.SummonRangeMonsters &&
                LiveSummonCount() >= Mathf.Max(1, _maxLiveSummons)))
            {
                continue;
            }

            _availablePatterns.Add(i);
        }

        if (_availablePatterns.Count == 0)
        {
            return false;
        }

        if (_availablePatterns.Count > 1)
        {
            _availablePatterns.Remove(_lastPatternIndex);
        }

        float weight = 0f;
        foreach (int candidate in _availablePatterns)
        {
            weight += _patterns[candidate].SelectionWeight;
        }

        float choice = Random.Range(0f, weight);
        foreach (int candidate in _availablePatterns)
        {
            choice -= _patterns[candidate].SelectionWeight;
            if (choice <= 0f)
            {
                index = candidate;
                return true;
            }
        }

        index = _availablePatterns[_availablePatterns.Count - 1];
        return true;
    }

    private void BeginAction(ActionState action, int hash, float speed)
    {
        _action = action;
        _actionHash = hash;
        _actionStartTime = Time.time;
        _actionStartFrame = Time.frameCount;
        SetAnimatorParameters();
        _directionAnimator.speed = speed;
        _directionAnimator.Play(hash, 0, 0f);
    }

    private void UpdateAttack()
    {
        if (Time.frameCount == _actionStartFrame)
        {
            return;
        }

        if (Time.time - _actionStartTime > Mathf.Max(0.1f, _maximumAttackTime))
        {
            Debug.LogError($"{name}: 공격 대기 제한을 넘었습니다. 클립과 패턴 시간을 확인하세요.", this);
            ClearOwnedAttacks();
            FinishAction();
            return;
        }

        UpdateGunGlow();
        AnimatorStateInfo state = _directionAnimator.GetCurrentAnimatorStateInfo(0);
        if (!_released && state.fullPathHash == _actionHash &&
            state.normalizedTime >= _currentPattern.ReleaseNormalizedTime)
        {
            _released = true;
            ReleasePattern();
        }

        if (!_animationFinished && state.fullPathHash == _actionHash && state.normalizedTime >= 1f)
        {
            _animationFinished = true;
            _directionAnimator.speed = 0f;
        }

        if (_released)
        {
            TickPattern();
            bool allWavesEmitted = (_currentPattern.PatternKind != BossPatternKind.FallingProjectiles &&
                _currentPattern.PatternKind != BossPatternKind.ScatteredBombs) ||
                _emittedWaves >= _currentPattern.WaveCount;
            if (_animationFinished && allWavesEmitted && Time.time >= _effectEndTime)
            {
                FinishAction();
            }
        }
    }

    private void ReleasePattern()
    {
        _nextEmitTime = Time.time;
        switch (_currentPattern.PatternKind)
        {
            case BossPatternKind.FallingProjectiles:
                EmitHazardWave(BossAreaHazardKind.Falling);
                break;
            case BossPatternKind.SummonRangeMonsters:
                SpawnSummons();
                _effectEndTime = Time.time + _currentPattern.Duration;
                break;
            case BossPatternKind.RandomEdgeBarrage:
            case BossPatternKind.OrbitBarrage:
                _effectEndTime = Time.time + _currentPattern.Duration;
                break;
            case BossPatternKind.ExpandingShockwave:
                _hazards.RemoveAll(item => item == null);
                if (_hazards.Count < Mathf.Max(1, _maxActiveHazards))
                {
                    _hazards.Add(BossAreaHazard.Spawn(this, _currentPattern, transform.position,
                        BossAreaHazardKind.Shockwave));
                }

                _effectEndTime = Time.time + _currentPattern.WarningTime +
                    _currentPattern.ShockwaveExpandTime + 0.3f;
                break;
            case BossPatternKind.ScatteredBombs:
                BossAttackIndicator cue = BossAttackIndicator.Spawn(transform.position,
                    _currentPattern.BossCueRadius, _currentPattern.WarningColor, transform, false, AttackRoot);
                cue.ExpireAfter(_currentPattern.WarningTime + _currentPattern.BombFuse + 0.3f);
                EmitHazardWave(BossAreaHazardKind.Bomb);
                break;
        }
    }

    private void TickPattern()
    {
        if (Time.time < _nextEmitTime)
        {
            return;
        }

        switch (_currentPattern.PatternKind)
        {
            case BossPatternKind.FallingProjectiles:
                if (_emittedWaves < _currentPattern.WaveCount)
                {
                    EmitHazardWave(BossAreaHazardKind.Falling);
                }

                break;
            case BossPatternKind.ScatteredBombs:
                if (_emittedWaves < _currentPattern.WaveCount)
                {
                    EmitHazardWave(BossAreaHazardKind.Bomb);
                }

                break;
            case BossPatternKind.RandomEdgeBarrage:
                if (Time.time < _effectEndTime)
                {
                    EmitEdgeBullets();
                }

                _nextEmitTime = Time.time + _currentPattern.EmitInterval;
                break;
            case BossPatternKind.OrbitBarrage:
                if (Time.time < _effectEndTime)
                {
                    _projectiles.RemoveAll(item => item == null);
                    for (int i = 0; i < _currentPattern.CountPerWave &&
                        _projectiles.Count < Mathf.Max(1, _maxActiveBullets); i++)
                    {
                        _projectiles.Add(BossPatternProjectile.Spawn(this, _currentPattern,
                            _gunMuzzles[_direction].position, MonsterCombat.GetDirectionVector(_direction), true));
                    }
                }

                _nextEmitTime = Time.time + _currentPattern.EmitInterval;
                break;
        }
    }

    private void EmitHazardWave(BossAreaHazardKind kind)
    {
        _hazards.RemoveAll(item => item == null);
        for (int i = 0; i < _currentPattern.CountPerWave &&
            _hazards.Count < Mathf.Max(1, _maxActiveHazards); i++)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                if (!_room.TryRandomPoint(_currentPattern.AreaRadius, out Vector2 point))
                {
                    break;
                }

                bool overlaps = false;
                foreach (BossAreaHazard hazard in _hazards)
                {
                    float spacing = hazard.Radius + _currentPattern.AreaRadius + 0.1f;
                    if ((hazard.Center - point).sqrMagnitude < spacing * spacing)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    _hazards.Add(BossAreaHazard.Spawn(this, _currentPattern, point, kind));
                    break;
                }
            }
        }

        _emittedWaves++;
        _nextEmitTime = Time.time + _currentPattern.EmitInterval;
        float delay = kind == BossAreaHazardKind.Falling ? _currentPattern.FallDuration : _currentPattern.BombFuse;
        _effectEndTime = Time.time + _currentPattern.WarningTime + delay + 0.3f;
    }

    private void EmitEdgeBullets()
    {
        _projectiles.RemoveAll(item => item == null);
        _edgeChoices.Clear();
        for (int i = 0; i < _room.BarragePoints.Length; i++)
        {
            Transform point = _room.BarragePoints[i];
            if (point != null && _room.Contains(point.position, 0f) &&
                (_targetPlayer == null || MonsterCombat.GetPlayerDistanceSquared(
                    _targetPlayer, _playerColliders, point.position, out Vector2 closestPoint) >= 1f))
            {
                _edgeChoices.Add(i);
            }
        }

        int count = Mathf.Min(_currentPattern.CountPerWave, _edgeChoices.Count);
        for (int i = 0; i < count && _projectiles.Count < Mathf.Max(1, _maxActiveBullets); i++)
        {
            int choice = Random.Range(0, _edgeChoices.Count);
            Transform source = _room.BarragePoints[_edgeChoices[choice]];
            _edgeChoices.RemoveAt(choice);
            Vector2 target;
            if (_targetPlayer != null && Random.value < 0.65f)
            {
                target = _targetPlayer.position;
            }
            else
            {
                target = _room.PlayArea.center + Random.insideUnitCircle *
                    (Mathf.Min(_room.PlayArea.width, _room.PlayArea.height) * 0.25f);
            }

            Vector2 direction = target - (Vector2)source.position;
            direction = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f)) * direction;
            _projectiles.Add(BossPatternProjectile.Spawn(this, _currentPattern, source.position, direction, false));
            MonsterCombatEffect.SpawnImpact(source.position, direction, _currentPattern.ProjectileColor, transform);
        }
    }

    private int LiveSummonCount()
    {
        _summons.RemoveAll(item => item == null);
        int count = 0;
        foreach (RangeMonster summon in _summons)
        {
            if (!summon.IsDead && summon.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private void SpawnSummons()
    {
        int remaining = Mathf.Max(1, _maxLiveSummons) - LiveSummonCount();
        int count = Mathf.Min(_currentPattern.SummonCount, remaining);
        for (int i = 0; i < count; i++)
        {
            if (!TrySummonPosition(out Vector2 point))
            {
                continue;
            }

            RangeMonster summon = Instantiate(_rangeMonsterPrefab,
                new Vector3(point.x, point.y, transform.position.z), Quaternion.identity);
            _summons.Add(summon);
            _summonOwners.Add(summon.transform);
            BossAttackIndicator ring = BossAttackIndicator.Spawn(summon.transform.position,
                0.65f, _currentPattern.WarningColor, transform, true, AttackRoot);
            ring.ExpireAfter(0.3f);
        }
    }

    private bool TrySummonPosition(out Vector2 position)
    {
        position = transform.position;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            if (_room.SummonPoints != null && _room.SummonPoints.Length > 0)
            {
                Transform point = _room.SummonPoints[Random.Range(0, _room.SummonPoints.Length)];
                if (point == null)
                {
                    continue;
                }

                position = point.position;
            }
            else if (!_room.TryRandomPoint(0.65f, out position))
            {
                return false;
            }

            if (!_room.IsClear(position, 0.65f) ||
                (position - (Vector2)transform.position).sqrMagnitude < 4f ||
                (_targetPlayer != null &&
                    (position - (Vector2)_targetPlayer.position).sqrMagnitude < 2.25f))
            {
                continue;
            }

            bool occupied = false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, 0.65f))
            {
                if (!collider.isTrigger && collider.GetComponentInParent<NormalMonster>() != null)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateGunGlow()
    {
        GameObject glow = new GameObject("UpperGunCharge");
        glow.transform.SetParent(AttackRoot, false);
        _gunGlow = glow.AddComponent<SpriteRenderer>();
        _gunGlow.sprite = BossAttackIndicator.DefaultBulletSprite;
        MonsterCombat.GetEffectSorting(transform, out int layer, out int sortingLayer, out int order);
        glow.layer = layer;
        _gunGlow.sortingLayerID = sortingLayer;
        _gunGlow.sortingOrder = order;
        UpdateGunGlow();
    }

    private void UpdateGunGlow()
    {
        if (_gunGlow == null || _currentPattern == null)
        {
            return;
        }

        _gunGlow.transform.position = _gunMuzzles[_direction].position;
        float pulse = Mathf.PingPong(Time.time * 5f, 1f);
        _gunGlow.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.4f, pulse);
        _gunGlow.color = Color.Lerp(_currentPattern.ProjectileColor, Color.white, pulse);
    }

    private void EndPattern()
    {
        if (_currentPattern != null)
        {
            _nextAttackTime = Time.time + _currentPattern.RecoveryTime;
            if (_readyTimes != null && _patternIndex >= 0 && _patternIndex < _readyTimes.Length)
            {
                _readyTimes[_patternIndex] = Time.time + _currentPattern.Cooldown;
            }
        }

        if (_gunGlow != null)
        {
            Destroy(_gunGlow.gameObject);
            _gunGlow = null;
        }

        _currentPattern = null;
        _patternIndex = -1;
    }

    private void FinishAction()
    {
        EndPattern();
        _action = ActionState.None;
        PlayIdle();
    }

    private void UpdateHit()
    {
        if (Time.frameCount == _actionStartFrame)
        {
            return;
        }

        AnimatorStateInfo state = _directionAnimator.GetCurrentAnimatorStateInfo(0);
        if ((state.fullPathHash == _actionHash && state.normalizedTime >= 1f) ||
            Time.time - _actionStartTime > Mathf.Max(0.1f, _maximumHitTime))
        {
            _nextHitReactionTime = Time.time + Mathf.Max(0f, _hitReactionCooldown);
            FinishAction();
        }
    }

    private void UpdateDeath()
    {
        if (!_animatorReady || _deathPoseHeld || Time.frameCount == _actionStartFrame)
        {
            return;
        }

        AnimatorStateInfo state = _directionAnimator.GetCurrentAnimatorStateInfo(0);
        if ((state.fullPathHash != _actionHash || state.normalizedTime < 1f) &&
            Time.time - _actionStartTime <= Mathf.Max(0.1f, _maximumDeathTime))
        {
            return;
        }

        _directionAnimator.Play(_states[2, _direction], 0, 1f);
        _directionAnimator.Update(0f);
        _directionAnimator.speed = 0f;
        _deathPoseHeld = true;
    }

    private void SetAnimatorParameters()
    {
        _directionAnimator.SetInteger(DirectionId, _direction);
        _directionAnimator.SetInteger(AttackTypeId,
            _currentPattern == null ? 1 : (int)_currentPattern.AttackType);
        _directionAnimator.SetBool(MovingId, false);
        _directionAnimator.SetBool(AttackingId, _action == ActionState.Attack);
        _directionAnimator.SetBool(HitId, _action == ActionState.Hit);
        _directionAnimator.SetBool(DeadId, _deathStarted);
    }

    private void PlayIdle()
    {
        if (!_animatorReady || _deathStarted)
        {
            return;
        }

        SetAnimatorParameters();
        _directionAnimator.speed = 1f;
        int hash = _states[0, _direction];
        if (_directionAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash != hash)
        {
            _directionAnimator.Play(hash, 0, 0f);
        }
    }

    private void ClearOwnedAttacks()
    {
        if (_attackRoot != null)
        {
            _attackRoot.gameObject.SetActive(false);
            Destroy(_attackRoot.gameObject);
            _attackRoot = null;
        }

        _gunGlow = null;
        _projectiles.Clear();
        _hazards.Clear();
    }

    private void ClearSummonsAndTheirBullets()
    {
        if (_summonOwners.Count > 0)
        {
#if UNITY_2022_2_OR_NEWER
            RangeProjectile[] bullets = FindObjectsByType<RangeProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            RangeProjectile[] bullets = FindObjectsOfType<RangeProjectile>(true);
#endif
            foreach (RangeProjectile bullet in bullets)
            {
                if (_summonOwners.Contains(bullet.Owner))
                {
                    bullet.gameObject.SetActive(false);
                    Destroy(bullet.gameObject);
                }
            }
        }

        foreach (RangeMonster summon in _summons)
        {
            if (summon != null)
            {
                summon.gameObject.SetActive(false);
                Destroy(summon.gameObject);
            }
        }

        _summons.Clear();
        _summonOwners.Clear();
    }

    private void OnDisable()
    {
        ClearOwnedAttacks();
        ClearSummonsAndTheirBullets();
        if (!_deathStarted)
        {
            EndPattern();
            _action = ActionState.None;
        }
    }

    private void Reset()
    {
        _maxHealth = 1200f;
        _moveSpeed = 0f;
        _contactDamage = 0;
        _directionAnimator = GetComponentInChildren<Animator>();
    }
}
