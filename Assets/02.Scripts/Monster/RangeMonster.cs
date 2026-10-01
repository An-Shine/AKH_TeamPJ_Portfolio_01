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
            if (_ready) UpdateAction();
            return;
        }

        base.Update();
        if (!_ready) return;
        _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);

        if (_action != ActionState.None)
        {
            UpdateAction();
            return;
        }

        if (_player == null)
        {
            SetAnimationFlags(false);
            return;
        }

        Vector2 toPlayer = _player.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 movement = Vector2.zero;
        if (distance > _attackRange)
        {
            movement = toPlayer.normalized * Mathf.Min(_moveSpeed * Time.deltaTime, distance - _attackRange);
        }
        else if (distance < _keepDistance)
        {
            movement = -toPlayer.normalized * Mathf.Min(_moveSpeed * Time.deltaTime, _keepDistance - distance);
        }
        else if (_cooldown <= 0f && _projectilePrefab != null && distance > 0.001f)
        {
            _shotDirection = toPlayer.normalized;
            SetDirection(_shotDirection);
            _cooldown = Mathf.Max(0f, _attackCooldown);
            _shotFired = false;
            BeginAction(ActionState.Attack, 2);
            return;
        }

        movement += GetSeparationDirection() * (_crowdAvoidanceSpeed * Time.deltaTime);
        Vector2 previous = transform.position;
        MoveWithCollisionCheck(movement);
        Vector2 actualMovement = (Vector2)transform.position - previous;
        bool moving = actualMovement.sqrMagnitude > 0.000001f;
        SetDirection(moving ? actualMovement : toPlayer);
        SetAnimationFlags(moving);
    }

    // 부모의 피해 쿨다운, 체력 감소, OnHit 호출을 그대로 사용합니다.
    public override void TakeDamage(float damage)
    {
        if (_hasDied) return;
        base.TakeDamage(damage);
    }

    protected override void OnHit()
    {
        if (_hasDied || !_ready) return;
        // Attack을 교체하므로 아직 발사하지 않은 투사체도 취소됩니다.
        BeginAction(ActionState.Hit, 3);
    }

    protected override void Die()
    {
        if (_hasDied) return;
        _hasDied = true;
        _currentHealth = 0f;
        StopAllCoroutines();
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = _colors[i];
        foreach (Collider2D bodyCollider in _colliders)
            if (bodyCollider != null) bodyCollider.enabled = false;
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>())
            body.simulated = false;

        if (!_ready)
        {
            // 컨트롤러가 잘못 연결된 경우에도 기존 사망 처리는 가능하게 합니다.
            base.Die();
            return;
        }
        BeginAction(ActionState.Death, 4);
    }

    private void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _direction = (Mathf.RoundToInt((angle + 90f) / 45f) + 8) % 8;
        _animator.SetInteger(DirectionId, _direction);
    }

    private void SetAnimationFlags(bool moving)
    {
        _animator.SetBool(MovingId, moving && _action == ActionState.None && !_hasDied);
        _animator.SetBool(AttackingId, _action == ActionState.Attack);
        _animator.SetBool(HitId, _action == ActionState.Hit);
        _animator.SetBool(DeadId, _hasDied);
    }

    private void BeginAction(ActionState action, int row)
    {
        _action = action;
        _actionHash = _states[row, _direction];
        _actionStartFrame = Time.frameCount;
        _actionStartTime = Time.time;
        SetAnimationFlags(false);
        // 연속 피격도 현재 방향의 Hit을 처음부터 재생합니다.
        _animator.Play(_actionHash, 0, 0f);
    }

    private void UpdateAction()
    {
        if (_action == ActionState.None || Time.frameCount == _actionStartFrame) return;
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != _actionHash)
        {
            if (Time.time - _actionStartTime > 1f)
            {
                Debug.LogError($"{name}: 동작 상태로 진입하지 못했습니다. Animator 전환 조건과 상태 이름을 확인하세요.", this);
                FinishAction();
            }
            return;
        }

        if (_action == ActionState.Attack && !_shotFired && state.normalizedTime >= _fireNormalizedTime)
        {
            _shotFired = true;
            FireProjectile();
        }
        if (state.normalizedTime >= 1f) FinishAction();
    }

    private void FinishAction()
    {
        if (_action == ActionState.Death)
        {
            Destroy(gameObject);
            return;
        }
        _action = ActionState.None;
        SetAnimationFlags(false);
        _animator.Play(_states[0, _direction], 0, 0f);
    }

    private void FireProjectile()
    {
        if (_hasDied || _action != ActionState.Attack || _projectilePrefab == null || _player == null) return;
        Vector2 reference = _firePointReferenceDirection.sqrMagnitude > 0.0001f
            ? _firePointReferenceDirection.normalized : Vector2.down;
        float rotation = Vector2.SignedAngle(reference, _shotDirection);
        Vector3 position = transform.TransformPoint(Quaternion.Euler(0f, 0f, rotation) * _fireOffset);
        RangeProjectile projectile = Instantiate(_projectilePrefab, position, Quaternion.identity);
        projectile.SetDirection(_shotDirection);

        // 생성 직후 발사자 자신의 콜라이더에 닿아 사라지는 것을 방지합니다.
        foreach (Collider2D projectileCollider in projectile.GetComponentsInChildren<Collider2D>())
        {
            foreach (Collider2D ownerCollider in _colliders)
            {
                if (ownerCollider != null && ownerCollider.enabled)
                    Physics2D.IgnoreCollision(projectileCollider, ownerCollider);
            }
        }
    }

    private bool ValidateAnimator()
    {
        if (_animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"{name}: RangeMonsterController를 Animator에 연결하세요.", this);
            return false;
        }
        int[] ids = { DirectionId, MovingId, AttackingId, HitId, DeadId };
        string[] names = { "Direction", "IsMoving", "IsAttacking", "IsHit", "IsDead" };
        for (int i = 0; i < ids.Length; i++)
        {
            AnimatorControllerParameterType type = i == 0
                ? AnimatorControllerParameterType.Int : AnimatorControllerParameterType.Bool;
            bool found = false;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
                if (parameter.nameHash == ids[i] && parameter.type == type) found = true;
            if (!found)
            {
                Debug.LogError($"{name}: Animator에 {names[i]} ({type}) 파라미터가 필요합니다.", this);
                return false;
            }
        }

        string[] motions = { "idle", "walk", "attack", "hit", "death" };
        for (int row = 0; row < motions.Length; row++)
        {
            for (int direction = 0; direction < Directions.Length; direction++)
            {
                string path = $"Base Layer.RangeMonster_{motions[row]}_{Directions[direction]}";
                _states[row, direction] = Animator.StringToHash(path);
                if (!_animator.HasState(0, _states[row, direction]))
                {
                    Debug.LogError($"{name}: Animator 상태를 찾을 수 없습니다: {path}", this);
                    return false;
                }
            }
        }

        return true;
    }
}
