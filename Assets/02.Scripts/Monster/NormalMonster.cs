using UnityEngine;

public class NormalMonster : MonoBehaviour
{
    [Header("기본 능력치")]
    [SerializeField] protected float _maxHealth = 100f;
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected int _contactDamage = 10;

    [Header("피격 설정")]
    [SerializeField] private float _hitCooldown = 0.1f;

    protected float _currentHealth;
    protected Transform _player;

    private float _hitCooldownTimer;

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
        if (_hitCooldownTimer > 0f)
        {
            _hitCooldownTimer -= Time.deltaTime;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        if (_currentHealth <= 0f)
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
        Debug.Log($"{gameObject.name} 사망!");

        Destroy(gameObject);
    }
}