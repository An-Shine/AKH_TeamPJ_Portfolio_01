using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RangeMonster : NormalMonster
{
    [Header("원거리 공격")]
    [SerializeField] private float _attackRange = 6f;
    [SerializeField] private float _keepDistance = 4f;
    [SerializeField] private float _attackCooldown = 2f;

    [Header("투사체")]
    [SerializeField] private RangeProjectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [Header("애니메이션 발사 시점")]
    [Tooltip("공격 모션의 진행률. 0.5는 모션 절반에서 발사합니다.")]
    [SerializeField, Range(0f, 0.95f)] private float _fireNormalizedTime = 0.5f;

    [Header("발사 위치 기준")]
    [Tooltip("편집기의 FirePoint를 배치할 때 기준으로 삼은 방향. 기본값은 남쪽입니다.")]
    [SerializeField] private Vector2 _firePointReferenceDirection = Vector2.down;

    [Header("이동")]
    [SerializeField, Min(0f)] private float _crowdAvoidanceSpeed = 1.5f;

    private enum ActionState { None, Attack, Hit, Death }

    private static readonly int DirectionId = Animator.StringToHash("Direction");
    private static readonly int MovingId = Animator.StringToHash("IsMoving");
    private static readonly int AttackingId = Animator.StringToHash("IsAttacking");
    private static readonly int HitId = Animator.StringToHash("IsHit");
    private static readonly int DeadId = Animator.StringToHash("IsDead");
    private static readonly string[] Directions = { "S", "SE", "E", "NE", "N", "NW", "W", "SW" };

    private Animator _animator;
    private SpriteRenderer[] _renderers;
    private Color[] _colors;
    private Collider2D[] _colliders;
    private readonly int[,] _states = new int[5, 8];
    private ActionState _action;
    private bool _ready;
    private bool _hasDied;
    private bool _shotFired;
    private int _direction;
    private int _actionHash;
    private int _actionStartFrame;
    private float _actionStartTime;
    private float _cooldown;
    private Vector2 _shotDirection;
    private Vector3 _fireOffset;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _colors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _colors[i] = _renderers[i].color;
            _renderers[i].flipX = false;
            _renderers[i].flipY = false;
        }

        transform.rotation = Quaternion.identity;
        _fireOffset = _firePoint != null
            ? transform.InverseTransformPoint(_firePoint.position)
            : Vector3.zero;
        _attackRange = Mathf.Max(0.1f, _attackRange);
        _keepDistance = Mathf.Clamp(_keepDistance, 0f, _attackRange);
        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    protected override void Start()
    {
        base.Start();
        if (_player != null && (_player == transform || _player.IsChildOf(transform)))
        {
            Debug.LogError($"{name}: 몬스터에 Player 태그가 붙어 있습니다. 실제 플레이어에만 Player 태그를 설정하세요.", this);
            _player = null;
        }

        _ready = ValidateAnimator();
        if (!_ready) return;
        SetAnimationFlags(false);
        _animator.SetInteger(DirectionId, 0);
        _animator.Play(_states[0, 0], 0, 0f);
    }

    protected override void Update()
    {
        if (_hasDied)
        {
            if (_ready)