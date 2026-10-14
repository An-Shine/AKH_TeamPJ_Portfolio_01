using _02.Scripts.Monster.BossMonster;
using _02.Scripts.Monster.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BossMonster : NormalMonster
{
    [Header("새 보스 전투")]
    [SerializeField] private BossEncounterProfile _encounterProfile;
    [SerializeField] private BossRoom _room;
    [SerializeField] private Animator _directionAnimator;
    [SerializeField, Min(0f)] private float _initialAttackDelay = 1.5f;
    [SerializeField] private Vector2 _fallbackArenaSize = new Vector2(18f, 12f);
    [SerializeField] private Camera _shakeCamera = null;

    [Header("보스 처치 엔딩")]
    [SerializeField] private EndingCutscenePlayer _endingCutscene;

    [Header("패턴 사이 이동")]
    [SerializeField] private bool _enableSlowChase = true;
    [SerializeField, Min(0f)] private float _slowMoveSpeed = 1.3f;
    [SerializeField, Min(0f)] private float _stopDistance = 2.3f;
    [SerializeField, Min(0f)] private float _roomEdgePadding = 1.1f;

    [Header("보스 피격")]
    [SerializeField] private Color _hitSparkColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private AudioClip _hitSound = null;
    [SerializeField, Range(0f, 1f)] private float _hitSoundVolume = 0.5f;
    [SerializeField, Min(0f)] private float _hitSoundCooldown = 0.08f;
    [SerializeField, Min(0f)] private float _hitReactionCooldown = 0.8f;

    [Header("체력 표시")]
    [SerializeField] private bool _showHealthBar = true;
    [SerializeField] private string _bossDisplayName = "BOSS";
    [SerializeField] private Color _healthBarColor = new Color(0.85f, 0.12f, 0.2f, 1f);
    [SerializeField] private TMP_FontAsset _healthFont;

    private Transform _targetPlayer;
    private readonly BossPlayerTracker _playerTracker = new BossPlayerTracker();
    private Collider2D[] _playerColliders = new Collider2D[0];
    private Collider2D[] _bodyColliders;
    private Rigidbody2D[] _bodies;
    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;
    private bool[] _originalRendererEnabled;
    private MonsterVisualLock _visualLock;
    private BossEncounterDirector _director;
    private BossAnimationDriver _animations;
    private BossVisualPolish _visualPolish;
    private BossCameraImpulse _cameraImpulse;
    private Transform _attackRoot;
    private Vector2 _spawnCenter;
    private float _bodyWidth = 2f;
    private float _nextPlayerSearch;
    private float _nextHitReaction;
    private float _nextHitSound;
    private int _previousHitSound = -1;
    private float _nextStrikePulse;
    private float _deathStartTime;
    private bool _deathStarted;
    private bool _started;
    private bool _ownsProfile;
    private AudioSource _hitAudioSource;
    private GameObject _healthBarRoot;
    private BossHealthBarView _healthBarView;

    public BossRoom Room => _room;
    public Transform Player => _targetPlayer;
    public Collider2D[] PlayerColliders => _playerColliders;
    public float CurrentHealth => _currentHealth;
    public float MaximumHealth => Mathf.Max(1f, _maxHealth);
    public float BodyWidth => _bodyWidth;
    public int Phase => _director != null ? _director.Phase : 1;
    public string CurrentPattern => _director != null ? _director.PatternName : string.Empty;
    public bool ShowHealthBar => _showHealthBar;
    public float HealthChargeProgress => _director != null && _director.IsCasting ? _director.ChargeProgress : 0f;
    public bool IsExecutionCharging => _director != null && _director.IsExecutionCharging;
    public bool CanStartPattern => _animations == null || !_animations.IsHit;

    public Rect Arena
    {
        get
        {
            if (_room != null)
            {
                return _room.PlayArea;
            }

            Vector2 size = new Vector2(Mathf.Max(8f, _fallbackArenaSize.x), Mathf.Max(8f, _fallbackArenaSize.y));
            return new Rect(_spawnCenter - size * 0.5f, size);
        }
    }

    public bool CanFight => !_deathStarted && _currentHealth > 0f && isActiveAndEnabled && _targetPlayer != null &&
                            _targetPlayer.gameObject.activeInHierarchy && Arena.Contains(_targetPlayer.position) &&
                            (PlayerManager.Instance == null || PlayerManager.Instance.PlayerStatusData == null ||
                             PlayerManager.Instance.PlayerStatusData.CurrentHP > 0f);

    public Transform AttackRoot
    {
        get
        {
            if (_attackRoot == null)
            {
                GameObject root = new GameObject(name + "_BossAttacks");
                SceneManager.MoveGameObjectToScene(root, gameObject.scene);
                _attackRoot = root.transform;
            }

            return _attackRoot;
        }
    }

    protected override void Awake()
    {
        _moveSpeed = Mathf.Max(0f, _slowMoveSpeed);
        _contactDamage = 0;
        base.Awake();
        _visualLock = new MonsterVisualLock(transform);
        _visualPolish = GetComponent<BossVisualPolish>();
        _bodyColliders = GetComponentsInChildren<Collider2D>(true);
        _bodies = GetComponentsInChildren<Rigidbody2D>(true);
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _originalColors = new Color[_renderers.Length];
        _originalRendererEnabled = new bool[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].color;
            _originalRendererEnabled[i] = _renderers[i].enabled;
        }

        SpriteRenderer body = GetComponent<SpriteRenderer>();
        if (body == null)
        {
            body = GetComponentInChildren<SpriteRenderer>();
        }

        if (body != null)
        {
            _bodyWidth = Mathf.Max(0.5f, body.bounds.size.x);
        }

        if (_directionAnimator == null)
        {
            _directionAnimator = GetComponentInChildren<Animator>();
        }

        ConfigureDamageTarget();
    }

    protected override void Start()
    {
        ResolveRoom();
        if (_room != null)
        {
            transform.position = _room.BossCenterPosition;
        }

        _spawnCenter = transform.position;
        base.Start();
        _targetPlayer = _player;
        _player = null;
        CachePlayer();

        if (_encounterProfile == null || _encounterProfile.Moves == null || _encounterProfile.Moves.Length == 0)
        {
            _encounterProfile = ScriptableObject.CreateInstance<BossEncounterProfile>();
            _encounterProfile.InitializeDefaults();
            _ownsProfile = true;
        }

        _animations = new BossAnimationDriver(_directionAnimator);
        _director = new BossEncounterDirector(this, _encounterProfile, _initialAttackDelay);
        _started = true;

        if (_deathStarted)
        {
            _director.Stop(true);
            _animations.Death();
        }

        CreateHealthBar();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_started && !_deathStarted && _director == null)
        {
            _director = new BossEncounterDirector(this, _encounterProfile, _initialAttackDelay);
            _animations.EndPattern();
        }
    }

    protected override void Update()
    {
        if (_deathStarted)
        {
            if (_animations != null)
            {
                _animations.TickDeath(Time.deltaTime);
            }

            return;
        }

        base.Update();
        if (!_started || Time.deltaTime <= 0f)
        {
            return;
        }

        ResolvePlayer();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBossBattle(this, CanFight && !PlayerManager.IsInputBlocked);
        }

        if (_targetPlayer != null)
        {
            _playerTracker.Sample(_targetPlayer.position, Time.deltaTime);
        }

        _director.Tick(Time.deltaTime);
        if (_director.IsCasting)
        {
            return;
        }

        if (_animations.IsAttacking)
        {
            _animations.EndPattern();
            if (_visualPolish != null)
            {
                _visualPolish.EndCast();
            }
        }

        UpdateMovement();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (_visualLock != null)
        {
            _visualLock.Restore();
        }

        foreach (SpriteRenderer renderer in _renderers)
        {
            if (renderer != null)
            {
                renderer.flipX = false;
                renderer.flipY = false;
            }
        }

        UpdateHealthBar();
    }

    private void ResolveRoom()
    {
        if (_room == null)
        {
#if UNITY_2022_2_OR_NEWER
            BossRoom[] rooms = FindObjectsByType<BossRoom>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            BossRoom[] rooms = FindObjectsOfType<BossRoom>();
#endif
            foreach (BossRoom room in rooms)
            {
                if (room.gameObject.scene == gameObject.scene && room.Contains(transform.position, 0f))
                {
                    _room = room;
                    break;
                }
            }
        }

        if (_room != null && (_room.transform == transform || _room.transform.IsChildOf(transform) ||
                              !_room.ValidateSetup()))
        {
            _room = null;
        }

        if (_room == null)
        {
            Debug.LogWarning($"{name}: BossRoom이 없어 시작 위치 기준 임시 전투 범위를 사용합니다. 씬 보스의 Room을 연결하세요.", this);
        }
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

    private void CachePlayer()
    {
        _playerColliders = _targetPlayer != null
            ? _targetPlayer.GetComponentsInChildren<Collider2D>(true)
            : new Collider2D[0];

        if (_targetPlayer != null)
        {
            _playerTracker.Reset(_targetPlayer.position);
        }
    }

    private void UpdateMovement()
    {
        if (!CanFight || !_enableSlowChase || _slowMoveSpeed <= 0f || _animations.IsHit)
        {
            _animations.Locomotion(false);
            return;
        }

        Vector2 origin = transform.position;
        Vector2 delta = (Vector2)_targetPlayer.position - origin;
        _animations.Face(delta);

        if (delta.magnitude <= _stopDistance)
        {
            _animations.Locomotion(false);
            return;
        }

        Vector2 target = (Vector2)_targetPlayer.position - delta.normalized * _stopDistance;
        target = BossBattleGeometry.ClampToRoom(target, Arena, _roomEdgePadding);
        Vector2 movement = GetNavigationMovement(target, _slowMoveSpeed * Time.deltaTime);
        Vector2 destination = BossBattleGeometry.ClampToRoom(origin + movement, Arena, _roomEdgePadding);
        MoveWithCollisionCheck(destination - origin);
        _animations.Locomotion(((Vector2)transform.position - origin).sqrMagnitude > 0.00000001f);
    }

    public bool TryDealPlayerDamage(float damage, Vector2 point, Vector2 direction, Color color)
    {
        return CanFight && MonsterCombat.TryTakePlayerDamage(_targetPlayer, damage,
            new Vector3(point.x, point.y, transform.position.z), direction, color);
    }

    public Vector2 PredictPlayerPosition(float leadTime)
    {
        return _targetPlayer != null
            ? _playerTracker.Predict(_targetPlayer.position, leadTime, Arena)
            : (Vector2)transform.position;
    }

    public Vector2 FindPatternDestination(Vector2 target, float padding = 0f)
    {
        Vector2 origin = transform.position;
        target = BossBattleGeometry.ClampToRoom(target, Arena, Mathf.Max(_roomEdgePadding, padding));

        for (int i = 10; i >= 0; i--)
        {
            Vector2 candidate = origin + (target - origin) * (i / 10f);
            if (CanFollowPatternLine(origin, candidate))
            {
                return candidate;
            }
        }

        return origin;
    }

    private bool CanFollowPatternLine(Vector2 origin, Vector2 target)
    {
        if (!HasObstacleLineOfSight(origin, target))
        {
            return false;
        }

        int steps = Mathf.Max(1, Mathf.CeilToInt((target - origin).magnitude / 0.2f));
        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = origin + (target - origin) * (i / (float)steps);
            if (!CanMoveTo(point))
            {
                return false;
            }
        }

        return true;
    }

    public void MoveForPattern(Vector2 target, float distance)
    {
        if (!CanFight || distance <= 0f)
        {
            return;
        }

        Vector2 delta = BossBattleGeometry.ClampToRoom(target, Arena, _roomEdgePadding) - (Vector2)transform.position;
        MoveWithCollisionCheck(delta.normalized * Mathf.Min(distance, delta.magnitude));
        _animations.Face(delta);
    }

    public void EmitPatternAfterimage(BossBattleEffects effects, Color color)
    {
        foreach (SpriteRenderer renderer in _renderers)
        {
            if (renderer != null)
            {
                effects.Afterimage(renderer, color);
            }
        }
    }

    public void SetPatternHidden(bool hidden)
    {
        if (_renderers == null || _originalRendererEnabled == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = !hidden && _originalRendererEnabled[i];
            }
        }
    }

    public void PulsePatternAnimation(BossEncounterMove move)
    {
        if (_animations != null && !_deathStarted && Time.time >= _nextStrikePulse)
        {
            _nextStrikePulse = Time.time + 0.25f;
            _animations.PulseStrike(move);

            if (_visualPolish != null)
            {
                _visualPolish.Pulse(_targetPlayer != null
                    ? (Vector2)(_targetPlayer.position - transform.position)
                    : Vector2.up);
            }
        }
    }

    public void HitArea(Vector2 center, float radius, float damage, Color color)
    {
        if (!CanFight)
        {
            return;
        }

        float squared =
            MonsterCombat.GetPlayerDistanceSquared(_targetPlayer, _playerColliders, center, out Vector2 point);

        if (squared <= radius * radius && MonsterCombat.HasClearAttackPath(center, point, transform, _targetPlayer))
        {
            TryDealPlayerDamage(damage, point, (point - center).normalized, color);
        }
    }

    public override void TakeDamage(float damage)
    {
        if (_deathStarted || !isActiveAndEnabled)
        {
            return;
        }

        float previous = _currentHealth;
        base.TakeDamage(damage);

        if (_currentHealth >= previous)
        {
            return;
        }

        Vector2 source = _targetPlayer != null
            ? (Vector2)_targetPlayer.position
            : (Vector2)transform.position + Vector2.down;

        Vector2 point = transform.position;
        foreach (Collider2D collider in _bodyColliders)
        {
            if (collider != null && collider.enabled && collider.gameObject == gameObject)
            {
                point = collider.ClosestPoint(source);
                break;
            }
        }

        MonsterCombatEffect.SpawnImpact(new Vector3(point.x, point.y, transform.position.z),
            ((Vector2)transform.position - source).normalized, _hitSparkColor, transform);

        BossBattleEffects effects = BossBattleEffects.ForOwner(gameObject);
        if (effects != null)
        {
            effects.PlayBodyHit();
        }

        if (Time.time >= _nextHitSound)
        {
            AudioClip hitSound = MonsterCombatAudio.Pick("Special/BossHit", _hitSound, ref _previousHitSound);
            if (hitSound == null)
            {
                return;
            }
            _nextHitSound = Time.time + _hitSoundCooldown;
            if (_hitAudioSource == null)
            {
                _hitAudioSource = gameObject.AddComponent<AudioSource>();
                _hitAudioSource.playOnAwake = false;
                _hitAudioSource.spatialBlend = 0f;
            }

            _hitAudioSource.PlayOneShot(hitSound, _hitSoundVolume);
        }
    }

    protected override void OnHit()
    {
        if (!_started || _deathStarted || _director.IsCasting || _animations.IsHit || Time.time < _nextHitReaction)
        {
            return;
        }

        _nextHitReaction = Time.time + _hitReactionCooldown;
        _animations.Hit();

        if (_visualPolish != null)
        {
            _visualPolish.Pulse(Vector2.up, 0.45f);
        }
    }

    protected override void Die()
    {
        if (_deathStarted)
        {
            return;
        }

        _deathStarted = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBossBattle(this, false);
        }

        _deathStartTime = Time.time;
        _currentHealth = 0f;
        StopMovementForDeath();
        StopAllCoroutines();

        if (_director != null)
        {
            _director.Stop(true);
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].color = _originalColors[i];
            }
        }

        foreach (Collider2D collider in _bodyColliders)
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

        if (_animations != null)
        {
            _animations.Death();
        }

        if (_endingCutscene != null)
        {
            _endingCutscene.PlayAfterBossDeath();
        }
        else
        {
            Debug.LogError("BossMonster의 Ending Cutscene을 연결하세요.", this);
        }
    }

    public void StartPatternAnimation(BossEncounterMove move, float warning)
    {
        _animations.Face((Vector2)_targetPlayer.position - (Vector2)transform.position);
        _animations.BeginPattern(move, warning);

        if (_visualPolish != null)
        {
            _visualPolish.Cast(move.Color, 0f);
        }
    }

    public void UpdatePatternAnimation(BossEncounterMove move, float age, float warning, float finishAt, bool ending)
    {
        _animations.UpdatePattern(move, age, warning, finishAt, ending);

        if (_visualPolish != null)
        {
            _visualPolish.Cast(move.Color, ending ? 0f : Mathf.Clamp01(age / warning));
        }
    }

    public void EndPatternAnimation()
    {
        _animations.EndPattern();

        if (_visualPolish != null)
        {
            _visualPolish.EndCast();
        }
    }

    public void StartPhaseAnimation()
    {
        _animations.Phase();
    }

    public void ShakeCamera(float strength, float duration)
    {
        ResolveShakeCamera();

        if (_cameraImpulse != null)
        {
            _cameraImpulse.Hit(strength * _encounterProfile.ShakeMultiplier, duration);
        }
    }

    public void SetCameraSustain(float strength)
    {
        if (strength > 0f)
        {
            ResolveShakeCamera();
        }

        if (_cameraImpulse != null)
        {
            _cameraImpulse.SetSustain(strength * _encounterProfile.ShakeMultiplier);
        }
    }

    private void ResolveShakeCamera()
    {
        if (_cameraImpulse == null)
        {
            _cameraImpulse = BossCameraImpulse.GetOrCreate(_shakeCamera != null ? _shakeCamera : Camera.main);
        }
    }

    private void ConfigureDamageTarget()
    {
        try
        {
            gameObject.tag = "Enemy";
        }
        catch (UnityException)
        {
            Debug.LogError($"{name}: Enemy 태그가 필요합니다.", this);
        }

        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError($"{name}: BossMonster와 같은 오브젝트에 피격 Collider2D를 연결하세요.", this);
        }
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
    }

    protected override void OnCollisionStay2D(Collision2D collision)
    {
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
    }

    protected override void OnTriggerStay2D(Collider2D other)
    {
    }

    protected override void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBossBattle(this, false);
        }

        base.OnDisable();

        if (_director != null)
        {
            _director.Dispose();
            _director = null;
        }

        if (_healthBarRoot != null)
        {
            _healthBarRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_director != null)
        {
            _director.Dispose();
        }

        if (_attackRoot != null)
        {
            Destroy(_attackRoot.gameObject);
        }

        if (_healthBarRoot != null)
        {
            Destroy(_healthBarRoot);
        }

        if (_ownsProfile)
        {
            Destroy(_encounterProfile);
        }
    }

    private void Reset()
    {
        _maxHealth = 1200f;
        _contactDamage = 0;
        _directionAnimator = GetComponentInChildren<Animator>();
    }

    private void CreateHealthBar()
    {
        if (!_showHealthBar || _healthBarRoot != null)
        {
            return;
        }

        _healthBarView = BossHealthBarView.Create(this, _bossDisplayName, _healthFont, _healthBarColor);
        _healthBarRoot = _healthBarView.gameObject;
    }

    private void UpdateHealthBar()
    {
        if (_healthBarView != null)
        {
            if (_healthBarRoot != null && !_healthBarRoot.activeSelf && isActiveAndEnabled)
            {
                _healthBarRoot.SetActive(true);
            }

            _healthBarView.Refresh();
        }
    }
}
