using UnityEngine;

public class NormalMonster : MonoBehaviour
{
    [Header("기본 능력치")]
    [SerializeField] protected float _maxHealth = 100f;
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected int _contactDamage = 10;

    protected float _currentHealth;
    protected Transform _player;

    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
    }

    protected virtual void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Player 태그가 붙은 오브젝트를 찾을 수 없습니다.");
        }
    }

    protected virtual void Update()
    {
    }

    public virtual void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        _currentHealth -= damage;

        Debug.Log($"{gameObject.name} 피격! 현재 체력: {_currentHealth}");

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}