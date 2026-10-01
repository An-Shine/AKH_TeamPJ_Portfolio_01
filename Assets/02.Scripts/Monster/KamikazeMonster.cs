using UnityEngine;

[RequireComponent(typeof(Animator))]
public class KamikazeMonster : NormalMonster
{
    [Header("자폭 시작")]
    [Tooltip("플레이어 몸의 Collider2D와 이 거리 안에 들어오면 자폭 준비를 시작합니다.")]
    [SerializeField, Min(0.1f)] private float _explodeRange = 1.2f;
    [SerializeField, Min(0f)] private float _attackTriggerTolerance = 0.05f;

    [Tooltip("Attack 애니메이션에서 폭발하는 시점입니다. 0.8이면 80% 지점입니다.")]
    [SerializeField, Range(0f, 0.95f)] private float _explodeNormalizedTime = 0.8f;

    [Tooltip("Animator가 공격 상태를 반복하거나 벗어나도 이 시간이 지나면 폭발합니다.")]
    [SerializeField, Min(0.1f)] private float _maximumFuseTime = 1.5f;

    [Tooltip("폭발 후 Attack 애니메이션이 끝나지 않을 때 Death로 넘어가는 대기 시간입니다.")]
    [SerializeField, Min(0.1f)] private float _postExplosionDelay = 0.5f;

    [Header("폭발 데미지")]
    [SerializeField, Min(0f)] private float _explosionDamage = 30f;
    [SerializeField, Min(0.1f)] private float _explosionDamageRadius = 2f;

    [Header("추격")]
    [SerializeField, Min(0.1f)] private float _chargeRange = 4f;
    [SerializeField, Min(0.1f)] private float _chaseSpeedMultiplier = 1.2f;
    [SerializeField, Min(0.1f)] private float _chargeSpeedMultiplier = 2f;
    [SerializeField, Min(0f)] private float _crowdAvoidanceSpeed = 1.5f;

    [Header("방향 안정화")]
    [Tooltip("이 거리 안에서는 마지막 접근 방향을 유지합니다. 공격 중에는 항상 방향을 고정합니다.")]
    [SerializeField, Min(0.1f)] private float _facingLockDistance = 1.5f;

    [Tooltip("8방향 경계에서 작은 각도 변화 때문에 자세가 왕복하는 것을 줄입니다.")]
    [SerializeField, Range(0f, 20f)] private float _directionHysteresis = 7f;

    [Header("걷기 애니메이션")]
    [Tooltip("Walk 재생 배율입니다. 실제 이동 속도와 별개입니다.")]
    [SerializeField, Min(0.1f)] private float _walkAnimationSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float _chargeAnimationSpeed = 2.2f;

    [Header("폭발 효과")]
    [Tooltip("연결하면 이 프리팹을 사용합니다. 비워 두면 기본 픽셀 폭발 효과를 생성합니다.")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField, Min(0f)] private float _explosionEffectLifetime = 1f;
    [SerializeField] private Color _explosionColor = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("애니메이션 종료 보정")]
    [SerializeField, Min(0.1f)] private float _maximumHitTime = 1.2f;
    [SerializeField, Min(0.1f)] private float _maximumDeathTime = 3f;

    private enum ActionState { None, Attack, Hit, Death }

    private static readonly int DirectionId = Animator.StringToHash("Direction");
    private static readonly int MovingId = Animator.StringToHash("IsMoving");
    private static readonly int AttackingId = Animator.StringToHash("IsAttacking");
    private static readonly int HitId = Animator.StringToHash("IsHit");
    private static readonly int DeadId = Animator.StringToHash("IsDead");

    private static readonly string[] Directions =
        { "S", "SE", "E", "NE", "N", "NW", "W", "SW" };

    // Idle / Walk / Attack / Hit / Death, 각 8방향.
    private readonly int[,] _states = new int[5, 8];

    private Animator _animator;
    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;
    private Collider2D[] _colliders;
    private Collider2D[] _playerColliders;
    private Rigidbody2D[] _bodies;
    private MonsterVisualLock _visualLock;

    private ActionState _action;
    private bool _ready;
    private bool _hasDied;
    private bool _explosionTriggered;
    private bool _directionInitialized;
    private bool _locomotionWasMoving;
    private int _direction;
    private int _actionHash;
    private int _actionStartFrame;
    private float _actionStartTime;
    private float _nextAnimationRetryTime;
    private float _nextPlayerSearchTime;
    private float _explosionTime;

    protected override void Awake()
    {
        base.Awake();
        _visualLock = new MonsterVisualLock(transform);

        ClampSettings();
        _animator = GetComponent<Animator>();
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _bodies = GetComponentsInChildren<Rigidbody2D>(true);
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].color;
            _renderers[i].flipX = false;
            _renderers[i].flipY = false;
        }

        // 8방향 스프라이트는 이미지로 방향을 표현합니다.
        // 충돌 때문에 Rigidbody2D 자체가 회전하지 않게 합니다.
        foreach (Rigidbody2D body in _bodies)
        {
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        transform.rotation = Quaternion.identity;
        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    protected override void Start()
    {
        base.Start();
        CachePlayerColliders();
        _ready = ValidateAnimator();

        if (!_ready)
            return;

        _direction = 0;
        _animator.SetInteger(DirectionId, _direction);
        PlayLocomotion(false, false);
    }

    protected override void Update()
    {
        if (_hasDied)
        {
            if (_ready)
                UpdateAction();
            return;
        }

        base.Update();

        if (!_ready)
            return;

        if (_action != ActionState.None)
        {
            UpdateAction();
            return;
        }

        if (!ResolvePlayer())
        {
            PlayLocomotion(false, false);
            return;
        }

        Vector2 toPlayer = (Vector2)_player.position - (Vector2)transform.position;
        float distance = Mathf.Sqrt(GetPlayerDistanceSquared(transform.position));

        if (distance <= _explodeRange + _attackTriggerTolerance)
        {
            // 가까운 거리에서 처음 생성된 경우에만 방향을 정합니다.
            // 보통은 직전까지의 접근 방향을 유지합니다.
            if (!_directionInitialized)
                SetDirection(toPlayer);

            BeginKamikazeAttack();
            return;
        }

        bool charging = distance <= _chargeRange;
        float multiplier = charging ? _chargeSpeedMultiplier : _chaseSpeedMultiplier;
        float moveDistance = Mathf.Min(
            Mathf.Max(0f, _moveSpeed) * multiplier * Time.deltaTime,
            Mathf.Max(0f, distance - _explodeRange));

        Vector2 movement = toPlayer.normalized * moveDistance;

        if (!charging)
            movement += GetSeparationDirection() * (_crowdAvoidanceSpeed * Time.deltaTime);

        Vector2 previousPosition = transform.position;
        MoveWithCollisionCheck(movement);
        Vector2 actualMovement = (Vector2)transform.position - previousPosition;

        // 군집 회피나 접촉에 의한 작은 이동량으로 바라보는 방향을 바꾸지 않습니다.
        if (!_directionInitialized || distance > _facingLockDistance)
            SetDirection(toPlayer);

        PlayLocomotion(actualMovement.sqrMagnitude > 0.000001f, charging);
    }

    private void LateUpdate()
    {
        _visualLock.Restore();
    }

    public override void TakeDamage(float damage)
    {
        if (_hasDied || _explosionTriggered)
            return;

        base.TakeDamage(damage);
    }

    protected override void OnHit()
    {
        if (_hasDied || !_ready || _action == ActionState.Attack)
            return;

        // 자폭 준비 중에는 체력 피해는 받되 Hit으로 준비 시간을 다시 시작하지 않습니다.
        // 폭발 전에 체력이 0이 되면 부모 TakeDamage가 Die를 호출합니다.
        BeginAction(ActionState.Hit, 3);
    }

    protected override void Die()
    {
        BeginDeath();
    }

    private bool ResolvePlayer()
    {
        if (_player != null && _player.gameObject.activeInHierarchy)
            return true;

        if (Time.time < _nextPlayerSearchTime)
            return false;

        _nextPlayerSearchTime = Time.time + 1f;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        _player = playerObject != null ? playerObject.transform : null;
        CachePlayerColliders();
        return _player != null && _player.gameObject.activeInHierarchy;
    }

    private void CachePlayerColliders()
    {
        if (_player != null && (_player == transform || _player.IsChildOf(transform)))
        {
            Debug.LogError($"{name}: 실제 플레이어 오브젝트에만 Player 태그를 설정하세요.", this);
            _player = null;
        }

        _playerColliders = _player != null
            ? _player.GetComponentsInChildren<Collider2D>(true)
            : null;
    }

    private float GetPlayerDistanceSquared(Vector2 position)
    {
        if (_player == null || !_player.gameObject.activeInHierarchy)
            return float.PositiveInfinity;

        float nearest = float.PositiveInfinity;

        if (_playerColliders != null)
        {
            foreach (Collider2D playerCollider in _playerColliders)
            {
                // 넓은 감지용 Trigger는 몸의 거리 판정에서 제외합니다.
                if (playerCollider == null || !playerCollider.enabled ||
                    playerCollider.isTrigger || !playerCollider.gameObject.activeInHierarchy)
                    continue;

                Rigidbody2D body = playerCollider.attachedRigidbody;
                if (body != null && !body.simulated)
                    continue;

                Vector2 closestPoint = playerCollider.ClosestPoint(position);
                nearest = Mathf.Min(nearest, (closestPoint - position).sqrMagnitude);
            }
        }

        // 몸의 Collider가 없거나 비활성화된 경우 Transform 위치를 사용합니다.
        if (float.IsPositiveInfinity(nearest))
            nearest = ((Vector2)_player.position - position).sqrMagnitude;

        return nearest;
    }

    private void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int candidate = (Mathf.RoundToInt((angle + 90f) / 45f) + 8) % 8;

        if (_directionInitialized && candidate != _direction)
        {
            float currentAngle = _direction * 45f - 90f;
            if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, angle)) <= 22.5f + _directionHysteresis)
                return;
        }

        _direction = candidate;
        _directionInitialized = true;
        _animator.SetInteger(DirectionId, _direction);
    }

    private void SetAnimationFlags(bool moving)
    {
        _animator.SetBool(MovingId, moving && _action == ActionState.None && !_hasDied);
        _animator.SetBool(AttackingId, _action == ActionState.Attack);
        _animator.SetBool(HitId, _action == ActionState.Hit);
        _animator.SetBool(DeadId, _hasDied);
    }

    private void PlayLocomotion(bool moving, bool charging)
    {
        SetAnimationFlags(moving);
        _animator.speed = moving
            ? (charging ? _chargeAnimationSpeed : _walkAnimationSpeed)
            : 1f;

        int desiredHash = _states[moving ? 1 : 0, _direction];
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

        if (state.fullPathHash != desiredHash)
        {
            // 방향이 바뀌어도 진행 중이던 걸음의 위상을 유지합니다.
            float phase = moving && _locomotionWasMoving
                ? Mathf.Repeat(state.normalizedTime, 1f)
                : 0f;
            _animator.Play(desiredHash, 0, phase);
        }

        _locomotionWasMoving = moving;
    }

    private void BeginKamikazeAttack()
    {
        if (_hasDied || _action != ActionState.None)
            return;

        _explosionTriggered = false;
        BeginAction(ActionState.Attack, 2);
    }

    private void BeginAction(ActionState action, int row)
    {
        _action = action;
        _actionHash = _states[row, _direction];
        _actionStartFrame = Time.frameCount;
        _actionStartTime = Time.time;
        _nextAnimationRetryTime = Time.time + 0.15f;
        _locomotionWasMoving = false;
        _animator.speed = 1f;
        SetAnimationFlags(false);
        _animator.Play(_actionHash, 0, 0f);
    }

    private void UpdateAction()
    {
        if (_action == ActionState.None || Time.frameCount == _actionStartFrame)
            return;

        float elapsed = Time.time - _actionStartTime;
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        bool matching = state.fullPathHash == _actionHash;

        if (_action == ActionState.Attack)
        {
            bool animationReachedExplosion = matching &&
                state.normalizedTime >= _explodeNormalizedTime;

            if (!_explosionTriggered && (animationReachedExplosion || elapsed >= _maximumFuseTime))
                TriggerExplosion();

            if (_explosionTriggered &&
                ((matching && state.normalizedTime >= 1f) ||
                 Time.time - _explosionTime >= _postExplosionDelay))
            {
                BeginDeath();
                return;
            }
        }
        else if (_action == ActionState.Hit)
        {
            if ((matching && state.normalizedTime >= 1f) || elapsed >= _maximumHitTime)
            {
                FinishAction();
                return;
            }
        }
        else if (_action == ActionState.Death)
        {
            if ((matching && state.normalizedTime >= 1f) || elapsed >= _maximumDeathTime)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (!matching && Time.time >= _nextAnimationRetryTime)
        {
            // 잘못된 전환으로 빠져나온 경우 같은 방향의 액션으로 복귀합니다.
            float duration = _action == ActionState.Attack
                ? _maximumFuseTime
                : (_action == ActionState.Hit ? _maximumHitTime : _maximumDeathTime);
            float progress = Mathf.Clamp(elapsed / duration, 0f, 0.95f);
            SetAnimationFlags(false);
            _animator.Play(_actionHash, 0, progress);
            _nextAnimationRetryTime = Time.time + 0.15f;
        }
    }

    private void TriggerExplosion()
    {
        if (_hasDied || _explosionTriggered)
            return;

        _explosionTriggered = true;
        _explosionTime = Time.time;
        Vector3 explosionPosition = transform.position;

        // 폭발한 몸이 다시 밀리거나 추가 충돌을 만들지 않게 합니다.
        DisablePhysics();
        SpawnExplosionEffect(explosionPosition);

        float squaredDistance = MonsterCombat.GetPlayerDistanceSquared(
            _player, _playerColliders, explosionPosition, out Vector2 hitPoint);
        if (squaredDistance > _explosionDamageRadius * _explosionDamageRadius)
            return;

        // 데미지 판정은 폭발당 한 번입니다.
        // 대시/피격 무적과 HP 감소는 기존 PlayerManager가 처리합니다.
        Vector2 hitDirection = _player != null
            ? ((Vector2)_player.position - (Vector2)explosionPosition).normalized : Vector2.zero;
        MonsterCombat.TryTakePlayerDamage(_player, _explosionDamage,
            new Vector3(hitPoint.x, hitPoint.y, explosionPosition.z), hitDirection, _explosionColor);
    }

    private void SpawnExplosionEffect(Vector3 position)
    {
        if (_explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(_explosionEffectPrefab, position, Quaternion.identity);
            if (_explosionEffectLifetime > 0f)
                Destroy(effect, _explosionEffectLifetime);
            return;
        }

        int sortingLayer = 0;
        int sortingOrder = 1;
        if (_renderers.Length > 0 && _renderers[0] != null)
        {
            sortingLayer = _renderers[0].sortingLayerID;
            sortingOrder = _renderers[0].sortingOrder + 1;
        }

        KamikazeExplosionEffect.Spawn(
            position, _explosionDamageRadius, _explosionColor,
            gameObject.layer, sortingLayer, sortingOrder);
    }

    private void BeginDeath()
    {
        if (_hasDied)
            return;

        _hasDied = true;
        _currentHealth = 0f;
        StopAllCoroutines();

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].color = _originalColors[i];
        }

        DisablePhysics();

        if (!_ready)
        {
            // 부모의 크기 축소 사망 처리는 호출하지 않습니다.
            Destroy(gameObject, 0.2f);
            return;
        }

        BeginAction(ActionState.Death, 4);
    }

    private void DisablePhysics()
    {
        foreach (Collider2D bodyCollider in _colliders)
        {
            if (bodyCollider != null)
                bodyCollider.enabled = false;
        }

        foreach (Rigidbody2D body in _bodies)
        {
            if (body != null)
                body.simulated = false;
        }
    }

    private void FinishAction()
    {
        _action = ActionState.None;
        PlayLocomotion(false, false);
    }

    private bool ValidateAnimator()
    {
        if (_animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"{name}: Animator에 KamikazeMonster.controller를 연결하세요.", this);
            return false;
        }

        int[] ids = { DirectionId, MovingId, AttackingId, HitId, DeadId };
        string[] names = { "Direction", "IsMoving", "IsAttacking", "IsHit", "IsDead" };
        AnimatorControllerParameter[] parameters = _animator.parameters;

        for (int i = 0; i < ids.Length; i++)
        {
            AnimatorControllerParameterType expected = i == 0
                ? AnimatorControllerParameterType.Int
                : AnimatorControllerParameterType.Bool;
            bool found = false;

            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (parameter.nameHash == ids[i] && parameter.type == expected)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"{name}: Animator에 {names[i]} ({expected}) 파라미터가 필요합니다.", this);
                return false;
            }
        }

        string[] motions = { "idle", "walk", "attack", "hit", "death" };
        for (int row = 0; row < motions.Length; row++)
        {
            for (int direction = 0; direction < Directions.Length; direction++)
            {
                string path = $"Base Layer.KamikazeMonster_{motions[row]}_{Directions[direction]}";
                if (!MonsterCombat.TryGetDirectionalState(_animator, "KamikazeMonster",
                    motions[row], direction, out int hash))
                {
                    Debug.LogError($"{name}: Animator 상태를 찾을 수 없습니다: {path}", this);
                    return false;
                }

                _states[row, direction] = hash;
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
        _explodeRange = Mathf.Max(0.1f, _explodeRange);
        _attackTriggerTolerance = Mathf.Max(0f, _attackTriggerTolerance);
        _explodeNormalizedTime = Mathf.Clamp(_explodeNormalizedTime, 0f, 0.95f);
        _maximumFuseTime = Mathf.Max(0.1f, _maximumFuseTime);
        _postExplosionDelay = Mathf.Max(0.1f, _postExplosionDelay);
        _explosionDamage = Mathf.Max(0f, _explosionDamage);
        _explosionDamageRadius = Mathf.Max(0.1f, _explosionDamageRadius);
        _chargeRange = Mathf.Max(_explodeRange + _attackTriggerTolerance + 0.1f, _chargeRange);
        _chaseSpeedMultiplier = Mathf.Max(0.1f, _chaseSpeedMultiplier);
        _chargeSpeedMultiplier = Mathf.Max(_chaseSpeedMultiplier, _chargeSpeedMultiplier);
        _crowdAvoidanceSpeed = Mathf.Max(0f, _crowdAvoidanceSpeed);
        _facingLockDistance = Mathf.Max(_explodeRange + _attackTriggerTolerance, _facingLockDistance);
        _directionHysteresis = Mathf.Clamp(_directionHysteresis, 0f, 20f);
        _walkAnimationSpeed = Mathf.Max(0.1f, _walkAnimationSpeed);
        _chargeAnimationSpeed = Mathf.Max(0.1f, _chargeAnimationSpeed);
        _explosionEffectLifetime = Mathf.Max(0f, _explosionEffectLifetime);
        _maximumHitTime = Mathf.Max(0.1f, _maximumHitTime);
        _maximumDeathTime = Mathf.Max(0.1f, _maximumDeathTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 1f);
        Gizmos.DrawWireSphere(transform.position, _explodeRange);
        Gizmos.color = new Color(1f, 0.2f, 0.1f, 1f);
        Gizmos.DrawWireSphere(transform.position, _explosionDamageRadius);
    }
}
