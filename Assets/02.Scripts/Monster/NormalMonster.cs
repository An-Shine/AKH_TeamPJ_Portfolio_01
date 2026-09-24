using System.Collections;
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

    [Header("피격 설정")]
    [SerializeField] private float _hitCooldown = 0.1f;

    [Header("사망 설정")]
    [SerializeField] private float _deathDelay = 0.2f;
    [SerializeField] private float _deathScale = 0f;

    protected float _currentHealth;
    protected Transform _player;

    private float _hitCooldownTimer;
    private bool _isDead;

    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
    }

    protected virtual void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning(
                $"{gameObject.name}: Player 태그가 붙은 오브젝트를 찾을 수 없습니다."
            );
        }
    }

    protected virtual void Update()
    {
        if (_isDead)
        {
            return;
        }

        if (_hitCooldownTimer > 0f)
        {
            _hitCooldownTimer -= Time.deltaTime;
        }
    }

    protected void FacePlayer()
    {
        if (_player == null)
        {
            return;
        }

        Vector2 direction =
            _player.position - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    protected Vector2 GetSeparationDirection()
    {
        Collider2D[] monsters =
            Physics2D.OverlapCircleAll(
                transform.position,
                _separationRadius
            );

        Vector2 separationDirection = Vector2.zero;

        foreach (Collider2D target in monsters)
        {
            if (target.gameObject == gameObject)
            {
                continue;
            }

            NormalMonster otherMonster =
                target.GetComponent<NormalMonster>();

            if (otherMonster == null)
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

            float strength =
                1f - Mathf.Clamp01(
                    distance / _separationRadius
                );

            separationDirection +=
                direction.normalized * strength;
        }

        return separationDirection.normalized;
    }

    protected void ApplySeparation()
    {
        Vector2 separationDirection =
            GetSeparationDirection();

        if (separationDirection == Vector2.zero)
        {
            return;
        }

        transform.position +=
            (Vector3)(
                separationDirection *
                _separationStrength *
                Time.deltaTime
            );
    }

    public virtual void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }

        if (_hitCooldownTimer > 0f)
        {
            return;
        }

        _hitCooldownTimer = _hitCooldown;

        _currentHealth -= damage;

        Debug.Log(
            $"{gameObject.name} 피격! 받은 데미지: {damage}, 현재 체력: {_currentHealth}"
        );

        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;
            Die();
        }
    }

    protected virtual void Die()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;

        Debug.Log($"{gameObject.name} 사망!");

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * _deathScale;

        float timer = 0f;

        while (timer < _deathDelay)
        {
            timer += Time.deltaTime;

            float progress =
                timer / _deathDelay;

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    progress
                );

            yield return null;
        }

        transform.localScale = targetScale;

        Destroy(gameObject);
    }
}