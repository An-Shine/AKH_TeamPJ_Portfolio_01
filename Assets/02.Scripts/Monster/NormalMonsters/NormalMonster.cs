using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalMonster : MonoBehaviour
{
    [Header("기본 능력치")]
    [SerializeField] protected float _maxHealth = 100f;
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected int _contactDamage = 10;

    [Header("몬스터 간 거리")]
    [SerializeField] private float _separationRadius = 0.8f;
    [SerializeField] private float _separationStrength = 1.5f;

    [Header("이동 충돌")]
    [SerializeField] private float _collisionCheckRadius = 0.25f;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("피격 설정")]
    [SerializeField] private float _hitCooldown = 0.1f;
    [SerializeField] private float _hitFlashDuration = 0.08f;

    [Header("사망 설정")]
    [SerializeField] private float _deathDelay = 0.2f;

    protected float _currentHealth;
    protected Transform _player;

    public bool IsDead => _normalIsDead;

    private float _normalHitCooldownTimer;
    private bool _normalIsDead;
    private Coroutine _normalHitFlashRoutine;
    private SpriteRenderer[] _normalSpriteRenderers;
    private Color[] _normalOriginalColors;
    private bool _normalDeathPositionLocked;
    private Vector3 _normalDeathPosition;
    private Transform[] _normalDeathTransforms;
    private Vector3[] _normalDeathLocalPositions;

    private readonly List<Collider2D> _normalMovementHits =
        new List<Collider2D>(16);

    private readonly HashSet<NormalMonster> _normalSeparationTargets =
        new HashSet<NormalMonster>();

    protected virtual void Awake()
    {
        _currentHealth = Mathf.Max(1f, _maxHealth);
        _normalSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _normalOriginalColors = new Color[_normalSpriteRenderers.Length];

        for (int i = 0; i < _normalSpriteRenderers.Length; i++)
        {
            _normalOriginalColors[i] = _normalSpriteRenderers[i].color;
        }
    }

    protected virtual void Start()
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
                this
            );
        }
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
                _normalHitCooldownTimer - Time.deltaTime
            );
        }
    }

    protected virtual void LateUpdate()
    {
        if (!_normalDeathPositionLocked)
        {
            return;
        }

        // Animator가 위치를 변경해도 사망을 시작한 자리에 고정합니다.
        transform.position = _normalDeathPosition;

        for (int i = 0; i < _normalDeathTransforms.Length; i++)
        {
            Transform target = _normalDeathTransforms[i];

            if (target != null && target != transform)
            {
                target.localPosition = _normalDeathLocalPositions[i];
            }
        }
    }

    protected void StopMovementForDeath()
    {
        _normalIsDead = true;

        if (_normalDeathPositionLocked)
        {
            return;
        }

        _normalDeathPositionLocked = true;
        _normalDeathPosition = transform.position;
        _normalDeathTransforms = GetComponentsInChildren<Transform>(true);
        _normalDeathLocalPositions = new Vector3[_normalDeathTransforms.Length];

        for (int i = 0; i < _normalDeathTransforms.Length; i++)
        {
            _normalDeathLocalPositions[i] = _normalDeathTransforms[i].localPosition;
        }

        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector2.zero;
#else
            body.velocity = Vector2.zero;
#endif
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

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

    protected Vector2 GetSeparationDirection()
    {
        if (_normalIsDead || _separationRadius <= 0f)
        {
            return Vector2.zero;
        }

        Collider2D[] monsters = Physics2D.OverlapCircleAll(
            transform.position,
            _separationRadius
        );

        Vector2 separationDirection = Vector2.zero;
        _normalSeparationTargets.Clear();

        foreach (Collider2D target in monsters)
        {
            NormalMonster otherMonster = target.GetComponentInParent<NormalMonster>();

            if (otherMonster == null || otherMonster == this || otherMonster._normalIsDead ||
                !_normalSeparationTargets.Add(otherMonster))
            {
                continue;
            }

            Vector2 direction =
                (Vector2)transform.position -
                (Vector2)otherMonster.transform.position;

            float distance = direction.magnitude;

            if (distance <= 0.001f)
            {
                direction = Random.insideUnitCircle.normalized;
                distance = 0.001f;
            }

            float strength = 1f - Mathf.Clamp01(distance / _separationRadius);
            separationDirection += direction.normalized * strength;
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
            separationDirection * _separationStrength * Time.deltaTime
        );
    }

    protected bool CanMoveTo(Vector2 targetPosition)
    {
        if (_obstacleLayer.value == 0)
        {
            return true;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(_obstacleLayer);
        filter.useTriggers = false;

        _normalMovementHits.Clear();

        int count = Physics2D.OverlapCircle(
            targetPosition,
            Mathf.Max(0f, _collisionCheckRadius),
            filter,
            _normalMovementHits
        );

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _normalMovementHits[i];

            if (hit == null || !hit.enabled || hit.isTrigger)
            {
                continue;
            }

            // 자기 몸과 자식 콜라이더는 장애물에서 제외합니다.
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.GetComponentInParent<NormalMonster>() != null)
            {
                continue;
            }

            if (IsPlayerCollider(hit))
            {
                continue;
            }

            return false;
        }

        return true;
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

    protected void MoveWithCollisionCheck(Vector2 movement)
    {
        // 프레임률이 높아도 작은 이동량을 취소하지 않습니다.
        if (_normalIsDead || movement.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 targetPosition = transform.position + (Vector3)movement;

        if (CanMoveTo((Vector2)targetPosition))
        {
            transform.position = targetPosition;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (_normalIsDead || _normalHitCooldownTimer > 0f ||
            damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
        {
            return;
        }

        _normalHitCooldownTimer = Mathf.Max(0f, _hitCooldown);
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        Debug.Log(
            $"{name} 피격! 받은 데미지: {damage}, 현재 체력: {_currentHealth}",
            this
        );

        if (_currentHealth <= 0f)
        {
            Die();
            return;
        }

        if (_normalHitFlashRoutine != null)
        {
            StopCoroutine(_normalHitFlashRoutine);
        }

        _normalHitFlashRoutine = StartCoroutine(HitFlashRoutine());
        OnHit();
    }

    protected virtual void OnHit()
    {
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

        foreach (Collider2D bodyCollider in GetComponentsInChildren<Collider2D>(true))
        {
            bodyCollider.enabled = false;
        }

        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
            body.simulated = false;
        }

        Debug.Log($"{name} 사망!", this);
        StartCoroutine(DeathRoutine());
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
}
