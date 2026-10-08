using _02.Scripts.Monster.BossMonster;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 기존 투사체와 보스방에서 참조하는 타입 이름을 유지합니다.
[DisallowMultipleComponent]
public class BossMonster : NormalMonster
{
    [Header("새 보스 전투")]
    [SerializeField] private BossEncounterProfile _encounterProfile;
    [SerializeField] private BossRoom _room;
    [SerializeField] private Animator _directionAnimator;
    [SerializeField, Min(0f)] private float _initialAttackDelay = 1.5f;
    [Tooltip("BossRoom이 없을 때만 사용하는 임시 전투 범위입니다. 중심은 보스의 시작 위치입니다.")]
    [SerializeField] private Vector2 _fallbackArenaSize = new Vector2(18f, 12f);
    [SerializeField] private Camera _shakeCamera = null;

    [Header("패턴 사이 이동")]
    [SerializeField] private bool _enableSlowChase = true;
    [SerializeField, Min(0f)] private float _slowMoveSpeed = 0.8f;
    [SerializeField, Min(0f)] private float _stopDistance = 3f;
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

    private Transform _targetPlayer;
    private Collider2D[] _playerColliders = new Collider2D[0];
    private Collider2D[] _bodyColliders;
    private Rigidbody2D[] _bodies;
    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;
    private bool[] _originalRendererEnabled;
    private MonsterVisualLock _visualLock;
    private BossEncounterDirector _director;
    private BossAnimationDriver _animations;
    private BossCameraImpulse _cameraImpulse;
    private Transform _attackRoot;
    private Vector2 _spawnCenter;
    private float _bodyWidth = 2f;
    private float _nextPlayerSearch;
    private float _nextHitReaction;
    private float _nextHitSound;
    private float _nextStrikePulse;
    private float _deathStartTime;
    private bool _deathStarted;
    private bool _started;
    private bool _ownsProfile;
    private AudioSource _hitAudioSource;

    private GameObject _healthBarRoot;
    private RectTransform _healthFill;
    private RectTransform _healthTrail;
    private Image _healthFillImage;
    private Text _healthLabel;
    private float _healthTrailRatio = 1f;
    private float _previousHealthRatio = 1f;
    private float _healthTrailHoldUntil;
    private int _displayedHealth = -1;
    private int _displayedMaximumHealth = -1;
    private int _displayedPhase = -1;
    private string _displayedPattern;

    public BossRoom Room => _room;
    public Transform Player => _targetPlayer;
    public Collider2D[] PlayerColliders => _playerColliders;
    public float CurrentHealth => _currentHealth;
    public float MaximumHealth => Mathf.Max(1f, _maxHealth);
    public float BodyWidth => _bodyWidth;
    public int Phase => _director != null ? _director.Phase : 1;
    public string CurrentPattern => _director != null ? _director.PatternName : string.Empty;
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

    public bool CanFight => !_deathStarted && isActiveAndEnabled && _targetPlayer != null &&
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
        // Player 소스의 접촉 처리와 별도로, 보스의 패턴만 피해를 전달합니다.
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
        _director.Tick(Time.deltaTime);
        if (_director.IsCasting)
        {
            return;
        }
        if (_animations.IsAttacking)
        {
            _animations.EndPattern();
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
        if (_room != null && (_room.transform == transform || _room.transform.IsChildOf(transform) || !_room.ValidateSetup()))
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
        _playerColliders = _targetPlayer != null ? _targetPlayer.GetComponentsInChildren<Collider2D>(true) : new Collider2D[0];
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

    public Vector2 FindPatternDestination(Vector2 target, float padding = 0f)
    {
        Vector2 origin = transform.position;
        target = BossBattleGeometry.ClampToRoom(target, Arena, Mathf.Max(_roomEdgePadding, padding));
        // 착지 지점과 이동 경로를 함께 확인해 벽 반대편으로 돌진하지 않습니다.
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
        }
    }

    // 이전 보스 공격 보조 스크립트가 컴파일될 수 있도록 공개 진입점을 유지합니다.
    public void HitArea(Vector2 center, float radius, float damage, Color color)
    {
        if (!CanFight)
        {
            return;
        }
        float squared = MonsterCombat.GetPlayerDistanceSquared(_targetPlayer, _playerColliders, center, out Vector2 point);
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
        Vector2 source = _targetPlayer != null ? (Vector2)_targetPlayer.position : (Vector2)transform.position + Vector2.down;
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
        if (_hitSound != null && Time.time >= _nextHitSound)
        {
            _nextHitSound = Time.time + _hitSoundCooldown;
            if (_hitAudioSource == null)
            {
                _hitAudioSource = gameObject.AddComponent<AudioSource>();
                _hitAudioSource.playOnAwake = false;
                _hitAudioSource.spatialBlend = 0f;
            }
            _hitAudioSource.PlayOneShot(_hitSound, _hitSoundVolume);
        }
    }

    protected override void OnHit()
    {
        if (!_started || _deathStarted || _director.IsCasting || Time.time < _nextHitReaction)
        {
            return;
        }
        _nextHitReaction = Time.time + _hitReactionCooldown;
        _animations.Hit();
    }

    protected override void Die()
    {
        if (_deathStarted)
        {
            return;
        }
        _deathStarted = true;
        if (AudioManager.Instance != null) AudioManager.Instance.SetBossBattle(this, false);
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
    }

    public void StartPatternAnimation(BossEncounterMove move, float warning)
    {
        _animations.Face((Vector2)_targetPlayer.position - (Vector2)transform.position);
        _animations.BeginPattern(move, warning);
    }

    public void UpdatePatternAnimation(BossEncounterMove move, float age, float warning, float finishAt, bool ending)
    {
        _animations.UpdatePattern(move, age, warning, finishAt, ending);
    }

    public void EndPatternAnimation()
    {
        _animations.EndPattern();
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
        if (AudioManager.Instance != null) AudioManager.Instance.SetBossBattle(this, false);
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
        _healthBarRoot = new GameObject(name + "_HealthBar", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler));
        Canvas canvas = _healthBarRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = _healthBarRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        Image panel = CreateBarImage("Panel", _healthBarRoot.transform, new Color(0.04f, 0.04f, 0.06f, 0.92f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(580f, 58f);
        panelRect.anchoredPosition = new Vector2(0f, -24f);
        Image background = CreateBarImage("Background", panelRect, new Color(0.12f, 0.08f, 0.1f, 1f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.offsetMin = new Vector2(12f, 10f);
        backgroundRect.offsetMax = new Vector2(-12f, 28f);
        _healthTrail = CreateBarImage("DamageTrail", backgroundRect,
            new Color(1f, 0.72f, 0.3f, 1f)).rectTransform;
        _healthFillImage = CreateBarImage("Health", backgroundRect, _healthBarColor);
        _healthFill = _healthFillImage.rectTransform;
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(panelRect, false);
        _healthLabel = label.GetComponent<Text>();
#if UNITY_2022_2_OR_NEWER
        _healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        _healthLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        _healthLabel.fontSize = 18;
        _healthLabel.alignment = TextAnchor.MiddleCenter;
        _healthLabel.color = Color.white;
        _healthLabel.raycastTarget = false;
        RectTransform labelRect = _healthLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.offsetMin = new Vector2(12f, -29f);
        labelRect.offsetMax = new Vector2(-12f, -3f);
        _healthTrailRatio = Mathf.Clamp01(CurrentHealth / MaximumHealth);
        _previousHealthRatio = _healthTrailRatio;
        UpdateHealthBar();
    }
    private static Image CreateBarImage(string objectName, Transform parent, Color color)
    {
        GameObject bar = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(parent, false);
        Image image = bar.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    private void UpdateHealthBar()
    {
        if (_healthBarRoot == null)
        {
            return;
        }
        bool playerInside = _targetPlayer != null && _targetPlayer.gameObject.activeInHierarchy &&
            (_room == null || _room.Contains(_targetPlayer.position, 0f));
        bool visible = _showHealthBar && isActiveAndEnabled && playerInside &&
            (!_deathStarted || Time.time - _deathStartTime < 2f);
        if (_healthBarRoot.activeSelf != visible)
        {
            _healthBarRoot.SetActive(visible);
        }
        float ratio = Mathf.Clamp01(CurrentHealth / MaximumHealth);
        if (ratio < _previousHealthRatio)
        {
            _healthTrailHoldUntil = Time.time + 0.2f;
        }
        _previousHealthRatio = ratio;
        if (ratio > _healthTrailRatio)
        {
            _healthTrailRatio = ratio;
        }
        else if (Time.time >= _healthTrailHoldUntil)
        {
            _healthTrailRatio = Mathf.MoveTowards(_healthTrailRatio, ratio, Time.deltaTime * 0.8f);
        }
        SetBarWidth(_healthFill, ratio);
        SetBarWidth(_healthTrail, _healthTrailRatio);
        _healthFillImage.color = Time.time < _healthTrailHoldUntil ? Color.white : _healthBarColor;
        int health = Mathf.CeilToInt(CurrentHealth);
        int maximum = Mathf.CeilToInt(MaximumHealth);
        if (health != _displayedHealth || maximum != _displayedMaximumHealth ||
            _displayedPhase != Phase || _displayedPattern != CurrentPattern)
        {
            _displayedHealth = health;
            _displayedMaximumHealth = maximum;
            _displayedPhase = Phase;
            _displayedPattern = CurrentPattern;
            _healthLabel.text = $"{_bossDisplayName}  {health} / {maximum}   P{Phase}  {CurrentPattern}";
        }
    }
    private static void SetBarWidth(RectTransform bar, float ratio)
    {
        bar.anchorMin = Vector2.zero;
        bar.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        bar.offsetMin = Vector2.zero;
        bar.offsetMax = Vector2.zero;
    }
}
