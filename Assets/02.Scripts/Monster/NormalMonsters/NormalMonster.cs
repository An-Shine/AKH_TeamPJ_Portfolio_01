using _02.Scripts.Monster.NormalMonsters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class NormalMonster : MonoBehaviour
{
    private enum PlayerDetectionMode
    {
        Room,
        Distance,
        RoomAndDistance
    }
    private const float NormalContactTolerance = 0.02f;
    private const float MonsterSpacingTolerance = 0.002f;
    private static readonly List<NormalMonster> ActiveMonsters =
        new List<NormalMonster>(64);
    #region Inspector 설정
    [Header("기본 능력치")]
    [SerializeField] protected float _maxHealth = 100f;
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected int _contactDamage = 10;
    [Header("소속 방")]
    [Tooltip("스포너가 생성한 몬스터는 자동으로 연결됩니다. 직접 배치한 몬스터는 방을 지정하세요.")]
    [SerializeField] private MonsterRoomBounds _homeRoom;
    [Header("플레이어 감지")]
    [Tooltip("Room: 같은 방에 있을 때 추적. Distance: 거리로 감지. Room And Distance: 같은 방에서 거리로 감지. Home Room이 없으면 거리 감지를 사용합니다.")]
    [SerializeField] private PlayerDetectionMode _detectionMode = PlayerDetectionMode.Room;
    [Tooltip("거리 감지 모드에서 추적을 시작하는 거리입니다. 월드 단위입니다.")]
    [SerializeField, Min(0.1f)] private float _detectionRange = 5f;
    [Tooltip("거리 감지 모드에서 추적을 멈추는 거리입니다. Detection Range 이상으로 적용됩니다.")]
    [SerializeField, Min(0.1f)] private float _loseTargetRange = 7f;
    [Header("플레이어 접촉")]
    [SerializeField, Min(0.05f)] private float _contactDamageCooldown = 0.5f;
    [SerializeField, Range(0f, 0.5f)] private float _contactKnockbackDistance = 0.2f;
    [SerializeField, Range(0.05f, 0.3f)] private float _contactKnockbackDuration = 0.12f;
    [Header("몬스터 간 거리")]
    [SerializeField] private float _separationRadius = 0.8f;
    [SerializeField] private float _separationStrength = 1.5f;
    [Header("몬스터 몸통 겹침 방지")]
    [SerializeField] private bool _preventMonsterOverlap = true;
    [Tooltip("같은 크기의 몬스터 두 마리가 유지할 최소 중심 간격입니다. 월드 단위입니다.")]
    [SerializeField, Min(0.1f)] private float _minimumMonsterDistance = 1.1f;
    [Tooltip("PPU나 Transform Scale로 커진 몬스터의 실제 스프라이트 크기를 간격에 반영합니다.")]
    [SerializeField] private bool _useSpriteSizeForSpacing = true;
    [SerializeField, Range(0.1f, 1f)] private float _spriteSpacingFactor = 0.8f;
    [Tooltip("이미 겹쳐서 생성된 몬스터가 서로 떨어지는 최대 속도입니다.")]
    [SerializeField, Min(0f)] private float _overlapSeparationSpeed = 2.5f;
    [Header("이동 충돌")]
    [SerializeField] private float _collisionCheckRadius = 0.25f;
    [SerializeField] private LayerMask _obstacleLayer;
    [Tooltip("기존 벽의 Layer를 변경하지 않고 모든 일반 Collider 2D를 장애물로 검사합니다.")]
    [SerializeField] private bool _detectSolidCollidersAutomatically = true;
    [Tooltip("충돌용이 아닌 바닥 등의 Layer를 자동 감지에서 제외합니다.")]
    [SerializeField] private LayerMask _ignoredObstacleLayers;
    [Tooltip("Obstacle Layer에 지정한 Trigger도 장애물로 검사합니다. 방 전체 감지 Trigger는 지정하지 않습니다.")]
    [SerializeField] private bool _blockObstacleTriggers;
    [Header("장애물 우회")]
    [SerializeField] private bool _useObstaclePathfinding = true;
    [SerializeField, Min(0.1f)] private float _pathCellSize = 0.5f;
    [SerializeField, Min(0.1f)] private float _pathRepathInterval = 0.5f;
    [SerializeField, Min(0.5f)] private float _pathSearchMargin = 12f;
    [SerializeField, Range(64, 8192)] private int _pathMaxVisitedNodes = 2048;
    [SerializeField, Min(0.02f)] private float _pathWaypointTolerance = 0.1f;
    [Header("피격 설정")]
    [SerializeField] private float _hitCooldown = 0.1f;
    [SerializeField] private float _hitFlashDuration = 0.08f;
    [Header("사망 설정")]
    [SerializeField] private float _deathDelay = 0.2f;
    #endregion
    #region 실행 상태와 캐시
    protected float _currentHealth;
    protected Transform _player;
    private Transform _normalTrackingPlayer;
    private bool _normalHasTrackingTarget;
    // 컴포넌트와 이 스크립트가 관리하는 위치
    private SpriteRenderer[] _normalSpriteRenderers;
    private Color[] _normalOriginalColors;
    private Collider2D[] _normalBodyColliders;
    private Rigidbody2D[] _normalBodies;
    private Rigidbody2D _normalRootBody;
    private Vector3 _normalControlledPosition;
    // 피격
    private float _normalHitCooldownTimer;
    private Coroutine _normalHitFlashRoutine;
    // 플레이어 접촉
    private Transform _normalContactPlayer;
    private Collider2D[] _normalContactPlayerColliders;
    private float _normalNextContactTime;
    // 접촉 넉백
    private bool _normalContactKnockbackActive;
    private Vector2 _normalKnockbackStartPosition;
    private Vector2 _normalKnockbackDirection;
    private float _normalKnockbackStartTime;
    private int _normalKnockbackMoveFrame = -1;
    // 몬스터 간격과 장애물 경로
    private float _normalSpriteSpacingRadius;
    private MonsterPathfinder _normalPathfinder;
    private bool _normalReportedObstacleConfiguration;
    private readonly List<Collider2D> _normalMovementHits =
        new List<Collider2D>(16);
    private readonly List<RaycastHit2D> _normalMovementCastHits =
        new List<RaycastHit2D>(16);
    // 사망 위치 고정
    private bool _normalIsDead;
    private bool _normalDeathPositionLocked;
    private Vector3 _normalDeathPosition;
    private Transform[] _normalDeathTransforms;
    private Vector3[] _normalDeathLocalPositions;
    #endregion
    public bool IsDead => _normalIsDead;
    public MonsterRoomBounds HomeRoom => _homeRoom;
    private float RoomClearance => Mathf.Max(0.01f, _collisionCheckRadius);
    protected bool IsContactKnockbackActive =>
        _normalContactKnockbackActive ||
        _normalKnockbackMoveFrame == Time.frameCount;
    #region Unity 생명주기
    protected virtual void Awake()
    {
        _currentHealth = Mathf.Max(1f, _maxHealth);
        CacheComponents();
        ConfigurePhysicsBodies();
        CacheOriginalColors();
        CacheSpriteSpacingRadius();
    }
    protected virtual void OnEnable()
    {
        if (!ActiveMonsters.Contains(this))
        {
            ActiveMonsters.Add(this);
        }
    }
    protected virtual void OnDisable()
    {
        ActiveMonsters.Remove(this);
        ResetPlayerTracking();
    }
    protected virtual void Start()
    {
        _normalControlledPosition = transform.position;
        FindTaggedPlayer();
    }
    protected virtual void Update()
    {
        if (_normalIsDead)
        {
            return;
        }
        if (_normalHitCooldownTimer > 0f)
        {
            _normalHitCooldownTimer = Mathf.Max(
                0f,
                _normalHitCooldownTimer - Time.deltaTime);
        }
        CheckPlayerContact();
        UpdateContactKnockback();
    }
    protected virtual void FixedUpdate()
    {
        if (_normalIsDead)
        {
            return;
        }
        foreach (Rigidbody2D body in _normalBodies)
        {
            if (body != null)
            {
                ClearBodyVelocity(body);
            }
        }
        ApplyControlledPosition();
        CheckPlayerContact();
    }
    protected virtual void LateUpdate()
    {
        if (!_normalDeathPositionLocked)
        {
            // 공격이나 자폭 준비 중에도 살아 있는 몬스터의 몸통 간격을 유지합니다.
            ResolveMonsterOverlap();
            // Animator나 접촉 처리에서 루트 위치를 바꾸어도 이동 거리만 유지합니다.
            ApplyControlledPosition();
            return;
        }
        RestoreDeathPosition();
    }
    #endregion
    #region 초기화
    private void CacheComponents()
    {
        _normalSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _normalOriginalColors = new Color[_normalSpriteRenderers.Length];
        _normalControlledPosition = transform.position;
        _normalBodyColliders = GetComponentsInChildren<Collider2D>(true);
        _normalBodies = GetComponentsInChildren<Rigidbody2D>(true);
        _normalRootBody = GetComponent<Rigidbody2D>();
    }
    private void ConfigurePhysicsBodies()
    {
        // 접촉 시 이동량은 물리 힘 대신 이 스크립트에서 관리합니다.
        foreach (Rigidbody2D body in _normalBodies)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.useFullKinematicContacts = true;
            body.interpolation = RigidbodyInterpolation2D.None;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            ClearBodyVelocity(body);
        }
    }
    private void CacheOriginalColors()
    {
        for (int i = 0; i < _normalSpriteRenderers.Length; i++)
        {
            _normalOriginalColors[i] = _normalSpriteRenderers[i].color;
        }
    }
    private void FindTaggedPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObject in players)
        {
            if (playerObject.GetComponentInParent<NormalMonster>() != null)
            {
                continue;
            }
            Transform candidate = playerObject.transform;
            if (candidate == transform || candidate.IsChildOf(transform))
            {
                continue;
            }
            _player = candidate;
            break;
        }
        if (_player == null)
        {
            Debug.LogWarning(
                $"{name}: Player 태그가 붙은 실제 플레이어를 찾을 수 없습니다.",
                this);
        }
    }
    protected bool CanTrackPlayer()
    {
        if (_normalIsDead || _player == null || !_player.gameObject.activeInHierarchy)
        {
            ResetPlayerTracking();
            return false;
        }
        if (_normalTrackingPlayer != _player)
        {
            _normalTrackingPlayer = _player;
            _normalHasTrackingTarget = false;
        }
        bool hasRoom = _homeRoom != null;
        bool requiresRoom = _detectionMode != PlayerDetectionMode.Distance;
        if (requiresRoom && hasRoom &&
            (!_homeRoom.isActiveAndEnabled || !_homeRoom.ContainsRoomPosition(_player.position)))
        {
            _normalHasTrackingTarget = false;
            return false;
        }
        bool requiresDistance = _detectionMode != PlayerDetectionMode.Room || !hasRoom;
        if (!requiresDistance)
        {
            _normalHasTrackingTarget = true;
            return true;
        }
        float detectionRange = Mathf.Max(0.1f, _detectionRange);
        float range = _normalHasTrackingTarget
            ? Mathf.Max(detectionRange, _loseTargetRange)
            : detectionRange;
        Vector2 difference = (Vector2)_player.position - (Vector2)transform.position;
        _normalHasTrackingTarget = difference.sqrMagnitude <= range * range;
        return _normalHasTrackingTarget;
    }
    public void ConfigureRoomDetection(float detectionRange, float loseTargetRange)
    {
        _detectionMode = PlayerDetectionMode.RoomAndDistance;
        _detectionRange = Mathf.Max(0.1f, detectionRange);
        _loseTargetRange = Mathf.Max(_detectionRange, loseTargetRange);
        ResetPlayerTracking();
    }
    private void ResetPlayerTracking()
    {
        _normalTrackingPlayer = null;
        _normalHasTrackingTarget = false;
    }
    #endregion
    #region 플레이어 접촉
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        TryPlayerContact(collision.collider);
    }
    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        TryPlayerContact(collision.collider);
    }
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        TryPlayerContact(other);
    }
    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        TryPlayerContact(other);
    }
    private bool CanProcessPlayerContact()
    {
        return !_normalIsDead &&
            Time.timeScale > 0f &&
            Time.time >= _normalNextContactTime;
    }
    private void CacheContactPlayer(Transform player)
    {
        if (_normalContactPlayer == player && _normalContactPlayerColliders != null)
        {
            return;
        }
        _normalContactPlayer = player;
        _normalContactPlayerColliders = player != null
            ? player.GetComponentsInChildren<Collider2D>(true)
            : null;
    }
    private void CheckPlayerContact()
    {
        if (!CanProcessPlayerContact() ||
            _player == null ||
            !_player.gameObject.activeInHierarchy)
        {
            return;
        }
        CacheContactPlayer(_player);
        foreach (Collider2D playerCollider in _normalContactPlayerColliders)
        {
            if (TryPlayerContact(playerCollider))
            {
                return;
            }
        }
    }
    private bool TryPlayerContact(Collider2D other)
    {
        if (!CanProcessPlayerContact() || !IsActiveContactCollider(other))
        {
            return false;
        }
        Transform player = MonsterCombat.ResolvePlayerCollider(other, _player);
        if (player == null || player.GetComponentInParent<NormalMonster>() != null)
        {
            return false;
        }
        CacheContactPlayer(player);
        // 몸의 콜라이더가 있으면 넓은 플레이어 감지 Trigger는 제외합니다.
        if (other.isTrigger &&
            MonsterCombat.HasEnabledBodyCollider(_normalContactPlayerColliders))
        {
            return false;
        }
        bool preferBody = MonsterCombat.HasEnabledBodyCollider(_normalBodyColliders);
        Bounds playerBounds = other.bounds;
        foreach (Collider2D bodyCollider in _normalBodyColliders)
        {
            if (!IsActiveContactCollider(bodyCollider) ||
                (preferBody && bodyCollider.isTrigger))
            {
                continue;
            }
            Bounds bodyBounds = bodyCollider.bounds;
            if (AreContactBoundsSeparated(bodyBounds, playerBounds))
            {
                continue;
            }
            ColliderDistance2D contact = bodyCollider.Distance(other);
            if (!contact.isValid || contact.distance > NormalContactTolerance)
            {
                continue;
            }
            HandlePlayerContact(player, contact);
            return true;
        }
        return false;
    }
    private static bool AreContactBoundsSeparated(Bounds bodyBounds, Bounds playerBounds)
    {
        return Mathf.Abs(bodyBounds.center.x - playerBounds.center.x) >
            bodyBounds.extents.x + playerBounds.extents.x + NormalContactTolerance ||
            Mathf.Abs(bodyBounds.center.y - playerBounds.center.y) >
            bodyBounds.extents.y + playerBounds.extents.y + NormalContactTolerance;
    }
    private void HandlePlayerContact(Transform player, ColliderDistance2D contact)
    {
        // 플레이어가 무적이어도 접촉 쿨다운과 몬스터 넉백은 적용합니다.
        _player = player;
        _normalNextContactTime = Time.time + Mathf.Max(0.05f, _contactDamageCooldown);
        BeginContactKnockback(player);
        Vector2 hitDirection = (Vector2)player.position - (Vector2)_normalControlledPosition;
        Vector3 hitPosition = new Vector3(
            contact.pointB.x,
            contact.pointB.y,
            _normalControlledPosition.z);
        MonsterCombat.TryTakePlayerDamage(
            player,
            _contactDamage,
            hitPosition,
            hitDirection.normalized,
            new Color(1f, 0.5f, 0.2f, 1f));
    }
    private static bool IsActiveContactCollider(Collider2D target)
    {
        if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
        {
            return false;
        }
        Rigidbody2D body = target.attachedRigidbody;
        return body == null || body.simulated;
    }
    #endregion
    #region 접촉 넉백
    private void BeginContactKnockback(Transform player)
    {
        if (_normalContactKnockbackActive || _contactKnockbackDistance <= 0f)
        {
            return;
        }
        _normalKnockbackDirection =
            (Vector2)_normalControlledPosition - (Vector2)player.position;
        if (_normalKnockbackDirection.sqrMagnitude <= 0.000001f)
        {
            _normalKnockbackDirection = Vector2.right;
        }
        _normalKnockbackDirection.Normalize();
        _normalKnockbackStartPosition = _normalControlledPosition;
        _normalKnockbackStartTime = Time.time;
        _normalContactKnockbackActive = true;
    }
    private void UpdateContactKnockback()
    {
        if (!_normalContactKnockbackActive || Time.timeScale <= 0f)
        {
            return;
        }
        float duration = Mathf.Clamp(_contactKnockbackDuration, 0.05f, 0.3f);
        float progress = Mathf.Clamp01((Time.time - _normalKnockbackStartTime) / duration);
        float easedProgress = 1f - (1f - progress) * (1f - progress);
        float distance = Mathf.Clamp(_contactKnockbackDistance, 0f, 0.5f);
        Vector2 desiredPosition = _normalKnockbackStartPosition +
            _normalKnockbackDirection * (distance * easedProgress);
        _normalKnockbackMoveFrame = Time.frameCount;
        if (!TryMoveControlled(desiredPosition - (Vector2)_normalControlledPosition) ||
            progress >= 1f)
        {
            _normalContactKnockbackActive = false;
        }
    }
    #endregion
    #region 몬스터 간격과 겹침 방지
    private void CacheSpriteSpacingRadius()
    {
        SpriteRenderer bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null &&
            _normalSpriteRenderers != null &&
            _normalSpriteRenderers.Length > 0)
        {
            bodyRenderer = _normalSpriteRenderers[0];
        }
        if (bodyRenderer == null || bodyRenderer.sprite == null)
        {
            _normalSpriteSpacingRadius = 0f;
            return;
        }
        Vector3 size = bodyRenderer.bounds.size;
        _normalSpriteSpacingRadius = Mathf.Max(size.x, size.y) * 0.5f;
    }
    private float GetMonsterSpacingRadius()
    {
        float radius = Mathf.Max(0.05f, _minimumMonsterDistance * 0.5f);
        if (_useSpriteSizeForSpacing)
        {
            if (_normalSpriteSpacingRadius <= 0f)
            {
                CacheSpriteSpacingRadius();
            }
            radius = Mathf.Max(
                radius,
                _normalSpriteSpacingRadius * Mathf.Clamp(_spriteSpacingFactor, 0.1f, 1f));
        }
        return radius;
    }
    private bool IsSpacingTarget(NormalMonster other)
    {
        return other != null &&
            other != this &&
            other.isActiveAndEnabled &&
            !other._normalIsDead &&
            other._preventMonsterOverlap &&
            other.gameObject.scene == gameObject.scene &&
            (_homeRoom == null || other._homeRoom == null || _homeRoom == other._homeRoom);
    }
    private static Vector2 GetCoincidentSeparationDirection(
        NormalMonster first,
        NormalMonster second)
    {
        int firstId = first.GetInstanceID();
        int secondId = second.GetInstanceID();
        int pairHash = unchecked((firstId ^ secondId) * 73856093);
        float angle = (pairHash & 1023) * (Mathf.PI * 2f / 1024f);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return firstId < secondId ? direction : -direction;
    }
    protected Vector2 GetSeparationDirection()
    {
        if (_normalIsDead || !_preventMonsterOverlap || _separationRadius <= 0f)
        {
            return Vector2.zero;
        }
        Vector2 origin = _normalControlledPosition;
        Vector2 separationDirection = Vector2.zero;
        float ownRadius = GetMonsterSpacingRadius();
        foreach (NormalMonster other in ActiveMonsters)
        {
            if (!IsSpacingTarget(other))
            {
                continue;
            }
            float radius = Mathf.Max(
                _separationRadius,
                ownRadius + other.GetMonsterSpacingRadius() + 0.2f);
            Vector2 difference = origin - (Vector2)other._normalControlledPosition;
            float distance = difference.magnitude;
            if (distance >= radius)
            {
                continue;
            }
            Vector2 direction = distance > 0.001f
                ? difference / distance
                : GetCoincidentSeparationDirection(this, other);
            float strength = 1f - Mathf.Clamp01(distance / radius);
            separationDirection += direction * strength;
        }
        return separationDirection.normalized;
    }
    protected void ApplySeparation()
    {
        Vector2 separationDirection = GetSeparationDirection();
        if (separationDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }
        MoveWithCollisionCheck(
            separationDirection * _separationStrength * Time.deltaTime);
    }
    private void ResolveMonsterOverlap()
    {
        if (_normalIsDead ||
            !_preventMonsterOverlap ||
            IsContactKnockbackActive ||
            Time.deltaTime <= 0f ||
            _overlapSeparationSpeed <= 0f)
        {
            return;
        }
        Vector2 origin = _normalControlledPosition;
        Vector2 correction = Vector2.zero;
        float ownRadius = GetMonsterSpacingRadius();
        foreach (NormalMonster other in ActiveMonsters)
        {
            if (!IsSpacingTarget(other))
            {
                continue;
            }
            Vector2 difference = origin - (Vector2)other._normalControlledPosition;
            float distance = difference.magnitude;
            float minimumDistance = ownRadius + other.GetMonsterSpacingRadius();
            float overlap = minimumDistance - distance;
            if (overlap <= MonsterSpacingTolerance)
            {
                continue;
            }
            Vector2 direction = distance > 0.001f
                ? difference / distance
                : GetCoincidentSeparationDirection(this, other);
            correction += direction * (overlap * 0.5f);
        }
        correction = Vector2.ClampMagnitude(
            correction,
            Mathf.Max(0f, _overlapSeparationSpeed) * Time.deltaTime);
        // 겹침을 푸는 이동도 기존 벽 충돌 검사로 제한합니다.
        TryMoveControlled(correction);
    }
    private bool IsMonsterSpacingStepClear(Vector2 origin, Vector2 target)
    {
        if (!_preventMonsterOverlap)
        {
            return true;
        }
        Vector2 movement = target - origin;
        float movementSquared = movement.sqrMagnitude;
        float ownRadius = GetMonsterSpacingRadius();
        if (movementSquared <= Mathf.Epsilon)
        {
            return true;
        }
        foreach (NormalMonster other in ActiveMonsters)
        {
            if (!IsSpacingTarget(other))
            {
                continue;
            }
            Vector2 otherPosition = other._normalControlledPosition;
            Vector2 difference = origin - otherPosition;
            float currentSquared = difference.sqrMagnitude;
            float minimumDistance = Mathf.Max(
                0.01f,
                ownRadius + other.GetMonsterSpacingRadius() - MonsterSpacingTolerance);
            float minimumSquared = minimumDistance * minimumDistance;
            if (currentSquared < minimumSquared)
            {
                // 이미 겹친 상태에서는 상대에게서 멀어지는 이동을 허용합니다.
                if (Vector2.Dot(difference, movement) < -0.000001f ||
                    (target - otherPosition).sqrMagnitude + 0.000001f < currentSquared)
                {
                    return false;
                }
                continue;
            }
            // 목적지뿐 아니라 이동 도중의 최소 거리도 검사합니다.
            float progress = Mathf.Clamp01(
                Vector2.Dot(otherPosition - origin, movement) / movementSquared);
            Vector2 closestPoint = origin + movement * progress;
            if ((closestPoint - otherPosition).sqrMagnitude < minimumSquared)
            {
                return false;
            }
        }
        return true;
    }
    #endregion
    #region 이동과 장애물 경로
    protected void FacePlayer()
    {
        if (_normalIsDead || _player == null)
        {
            return;
        }
        Vector2 direction = _player.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    public bool TryAssignHomeRoom(MonsterRoomBounds room)
    {
        if (room == null || !room.Contains(transform.position, RoomClearance))
        {
            return false;
        }
        _homeRoom = room;
        _normalPathfinder = null;
        ResetPlayerTracking();
        return true;
    }
    public bool IsSpawnPositionClear(Vector2 position, MonsterRoomBounds room)
    {
        return room != null && room.Contains(position, RoomClearance) &&
            IsObstaclePositionClear(position);
    }
    public bool IsSpawnPositionClear(Vector2 position)
    {
        return CanMoveTo(position);
    }
    protected Vector2 GetNavigationMovement(Vector2 target, float maxDistance)
    {
        if (_normalIsDead || IsContactKnockbackActive || maxDistance <= 0f)
        {
            return Vector2.zero;
        }
        if (_homeRoom != null)
        {
            target = _homeRoom.ClampPosition(target, RoomClearance);
        }
        Vector2 origin = _normalControlledPosition;
        if (!TryCreateMovementFilter(out _))
        {
            return Vector2.zero;
        }
        if (!_useObstaclePathfinding)
        {
            Vector2 direct = target - origin;
            return direct.normalized * Mathf.Min(maxDistance, direct.magnitude);
        }
        if (_normalPathfinder == null)
        {
            _normalPathfinder = new MonsterPathfinder(
                CanMoveTo,
                CanTravelBetween,
                HasObstacleLineOfSight);
        }
        Vector2 direction = _normalPathfinder.GetDirection(
            origin,
            target,
            _pathCellSize,
            _pathRepathInterval,
            _pathSearchMargin,
            _pathMaxVisitedNodes,
            _pathWaypointTolerance);
        float distance = _normalPathfinder.GetRemainingStepDistance(origin, target);
        return direction * Mathf.Min(maxDistance, distance);
    }
    protected bool HasObstacleLineOfSight(Vector2 origin, Vector2 target)
    {
        return IsObstacleCastClear(origin, target, 0f);
    }
    protected bool CanMoveTo(Vector2 targetPosition)
    {
        return (_homeRoom == null || _homeRoom.Contains(targetPosition, RoomClearance)) &&
            IsObstaclePositionClear(targetPosition);
    }
    private bool IsObstaclePositionClear(Vector2 targetPosition)
    {
        if (!TryCreateMovementFilter(out ContactFilter2D filter))
        {
            return false;
        }
        _normalMovementHits.Clear();
        int count = Physics2D.OverlapCircle(
            targetPosition,
            Mathf.Max(0f, _collisionCheckRadius),
            filter,
            _normalMovementHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _normalMovementHits[i];
            if (!IsMovementObstacle(hit))
            {
                continue;
            }
            return false;
        }
        return true;
    }
    private bool CanTravelBetween(Vector2 origin, Vector2 target)
    {
        return CanMoveTo(target) &&
            IsObstacleCastClear(origin, target, Mathf.Max(0f, _collisionCheckRadius));
    }
    private int GetMovementObstacleMask()
    {
        int layers = _detectSolidCollidersAutomatically ? ~0 : _obstacleLayer.value;
        return layers & ~_ignoredObstacleLayers.value;
    }
    private bool TryCreateMovementFilter(out ContactFilter2D filter)
    {
        filter = new ContactFilter2D();
        int layers = GetMovementObstacleMask();
        int triggerLayers = _obstacleLayer.value & ~_ignoredObstacleLayers.value;
        if (layers == 0 || (_blockObstacleTriggers && triggerLayers == 0))
        {
            if (!_normalReportedObstacleConfiguration)
            {
                Debug.LogError(
                    $"{name}: 장애물 설정이 비어 있어 이동을 멈춥니다. " +
                    "Detect Solid Colliders Automatically를 켜거나 Obstacle Layer를 지정하세요. " +
                    "Block Obstacle Triggers를 켰다면 Trigger 벽의 Layer도 지정해야 합니다.",
                    this);
                _normalReportedObstacleConfiguration = true;
            }
            return false;
        }
        _normalReportedObstacleConfiguration = false;
        filter.SetLayerMask(layers);
        filter.useTriggers = _blockObstacleTriggers;
        return true;
    }
    private bool IsMovementObstacle(Collider2D hit)
    {
        if (hit == null ||
            !hit.enabled ||
            hit.transform == transform ||
            hit.transform.IsChildOf(transform))
        {
            return false;
        }
        int layer = 1 << hit.gameObject.layer;
        if ((GetMovementObstacleMask() & layer) == 0 ||
            (hit.isTrigger &&
                (!_blockObstacleTriggers || (_obstacleLayer.value & layer) == 0)))
        {
            return false;
        }
        return hit.GetComponentInParent<NormalMonster>() == null && !IsPlayerCollider(hit);
    }
    private bool IsPlayerCollider(Collider2D target)
    {
        if (_player != null &&
            (target.transform == _player || target.transform.IsChildOf(_player)))
        {
            return true;
        }
        for (Transform current = target.transform; current != null; current = current.parent)
        {
            if (current.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }
    private bool IsObstacleCastClear(Vector2 origin, Vector2 target, float radius)
    {
        if (!TryCreateMovementFilter(out ContactFilter2D filter))
        {
            return false;
        }
        Vector2 difference = target - origin;
        float distance = difference.magnitude;
        if (distance <= 0.000001f)
        {
            return true;
        }
        _normalMovementCastHits.Clear();
        int count = radius > 0f
            ? Physics2D.CircleCast(
                origin, radius, difference / distance,
                filter, _normalMovementCastHits, distance)
            : Physics2D.Raycast(
                origin, difference / distance,
                filter, _normalMovementCastHits, distance);
        for (int i = 0; i < count; i++)
        {
            if (IsMovementObstacle(_normalMovementCastHits[i].collider))
            {
                return false;
            }
        }
        return true;
    }
    protected void MoveWithCollisionCheck(Vector2 movement)
    {
        // 프레임률이 높아도 작은 이동량을 취소하지 않습니다.
        if (_normalIsDead ||
            IsContactKnockbackActive ||
            movement.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }
        TryMoveControlled(movement);
    }
    private bool TryMoveControlled(Vector2 movement)
    {
        if (movement.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }
        Vector2 origin = _normalControlledPosition;
        if (TryApplyControlledStep(origin, movement))
        {
            return true;
        }
        // 군집 회피 때문에 대각선이 막혀도 벽과 평행한 축으로는 움직입니다.
        Vector2 horizontal = new Vector2(movement.x, 0f);
        Vector2 vertical = new Vector2(0f, movement.y);
        Vector2 first = Mathf.Abs(movement.x) >= Mathf.Abs(movement.y)
            ? horizontal
            : vertical;
        Vector2 second = Mathf.Abs(movement.x) >= Mathf.Abs(movement.y)
            ? vertical
            : horizontal;
        return TryApplyControlledStep(origin, first) ||
            TryApplyControlledStep(origin, second);
    }
    private bool TryApplyControlledStep(Vector2 origin, Vector2 movement)
    {
        if (movement.sqrMagnitude <= Mathf.Epsilon ||
            !IsMonsterSpacingStepClear(origin, origin + movement) ||
            !CanTravelBetween(origin, origin + movement))
        {
            return false;
        }
        _normalControlledPosition += (Vector3)movement;
        ApplyControlledPosition();
        return true;
    }
    private void ApplyControlledPosition()
    {
        transform.position = _normalControlledPosition;
        if (_normalRootBody != null && _normalRootBody.simulated)
        {
            _normalRootBody.position = (Vector2)_normalControlledPosition;
        }
    }
    private static void ClearBodyVelocity(Rigidbody2D body)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = Vector2.zero;
#else
        body.velocity = Vector2.zero;
#endif
        body.angularVelocity = 0f;
    }
    #endregion
    #region 피격과 사망
    public virtual void TakeDamage(float damage)
    {
        if (_normalIsDead ||
            _normalHitCooldownTimer > 0f ||
            damage <= 0f ||
            float.IsNaN(damage) ||
            float.IsInfinity(damage))
        {
            return;
        }
        _normalHitCooldownTimer = Mathf.Max(0f, _hitCooldown);
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        Debug.Log(
            $"{name} 피격! 받은 데미지: {damage}, 현재 체력: {_currentHealth}",
            this);
        if (_currentHealth <= 0f)
        {
            Die();
            return;
        }
        PlayHitFlash();
        OnHit();
    }
    protected virtual void OnHit()
    {
    }
    private void PlayHitFlash()
    {
        if (_normalHitFlashRoutine != null)
        {
            StopCoroutine(_normalHitFlashRoutine);
        }
        _normalHitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }
    private IEnumerator HitFlashRoutine()
    {
        for (int i = 0; i < _normalSpriteRenderers.Length; i++)
        {
            if (_normalSpriteRenderers[i] != null)
            {
                _normalSpriteRenderers[i].color = Color.red;
            }
        }
        yield return new WaitForSeconds(Mathf.Max(0f, _hitFlashDuration));
        if (!_normalIsDead)
        {
            RestoreOriginalColors();
        }
        _normalHitFlashRoutine = null;
    }
    private void RestoreOriginalColors()
    {
        for (int i = 0; i < _normalSpriteRenderers.Length; i++)
        {
            if (_normalSpriteRenderers[i] != null)
            {
                _normalSpriteRenderers[i].color = _normalOriginalColors[i];
            }
        }
    }
    protected virtual void Die()
    {
        if (_normalIsDead)
        {
            return;
        }
        StopMovementForDeath();
        _currentHealth = 0f;
        StopAllCoroutines();
        _normalHitFlashRoutine = null;
        RestoreOriginalColors();
        DisableDeathPhysics();
        Debug.Log($"{name} 사망!", this);
        StartCoroutine(DeathRoutine());
    }
    protected void StopMovementForDeath()
    {
        _normalIsDead = true;
        _normalContactKnockbackActive = false;
        _normalKnockbackMoveFrame = -1;
        if (_normalDeathPositionLocked)
        {
            return;
        }
        _normalDeathPositionLocked = true;
        _normalDeathPosition = _normalControlledPosition;
        _normalDeathTransforms = GetComponentsInChildren<Transform>(true);
        _normalDeathLocalPositions = new Vector3[_normalDeathTransforms.Length];
        for (int i = 0; i < _normalDeathTransforms.Length; i++)
        {
            _normalDeathLocalPositions[i] = _normalDeathTransforms[i].localPosition;
        }
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
            ClearBodyVelocity(body);
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }
    private void RestoreDeathPosition()
    {
        // Animator가 위치를 변경해도 사망을 시작한 자리에 고정합니다.
        _normalControlledPosition = _normalDeathPosition;
        ApplyControlledPosition();
        for (int i = 0; i < _normalDeathTransforms.Length; i++)
        {
            Transform target = _normalDeathTransforms[i];
            if (target != null && target != transform)
            {
                target.localPosition = _normalDeathLocalPositions[i];
            }
        }
    }
    private void DisableDeathPhysics()
    {
        foreach (Collider2D bodyCollider in GetComponentsInChildren<Collider2D>(true))
        {
            bodyCollider.enabled = false;
        }
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
            body.simulated = false;
        }
    }
    private IEnumerator DeathRoutine()
    {
        // 사망할 때 몬스터의 크기를 변경하지 않습니다.
        if (_deathDelay > 0f)
        {
            yield return new WaitForSeconds(_deathDelay);
        }
        Destroy(gameObject);
    }
    #endregion
}
