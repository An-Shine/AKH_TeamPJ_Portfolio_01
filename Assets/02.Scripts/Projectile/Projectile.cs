using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    protected ProjectileType _type;
    public ProjectileType Type => _type;
    protected int _damage;
    private int _bonusDamage;

    public int Damage =>
        (int)((_damage + _bonusDamage + ItemManager.Instance.ProjectileStat.DamageModifier)
              * (1f + ItemManager.Instance.ProjectileStat.DamageMultiplier));

    protected float _range;
    private int _initialDurability;
    private int _durability;
    protected Vector3 _direction;
    protected float _speedScalar;
    public float SpeedScalar => _speedScalar * (1f + ItemManager.Instance.ProjectileStat.MoveSpeedMultiplier);
    protected Vector3 _initialPosition;

    protected virtual void Update()
    {
        Move();
        CheckRange();
    }

    public void Initialize(ProjectileType type, int damage, float speedScalar, int durability)
    {
        _type = type;
        _damage = damage;
        _bonusDamage = 0;
        _initialDurability = durability;
        _durability = _initialDurability;
        _direction = Vector3.zero;
        _speedScalar = speedScalar;
    }

    public virtual void OnSpawn(Vector3 position, Vector3 direction, float range, int damage)
    {
        _durability = _initialDurability;
        _direction = direction;
        _range = range;
        gameObject.transform.position = position;
        _initialPosition = position;
        _bonusDamage = damage;

        Resize();
        Rotation();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        GiveDamage(other);
        DecreaseDurability(other);
    }

    protected virtual void Rotation()
    {
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        Vector3 targetAngle = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angle);
        transform.eulerAngles = targetAngle;
    }

    protected virtual void Resize()
    {
        Vector3 scale = Vector3.one * (1f + ItemManager.Instance.ProjectileStat.SizeMultiplier);
        transform.localScale = scale;
    }

    protected abstract void Move();

    protected virtual void CheckRange()
    {
        var distanceVector = transform.position - _initialPosition;
        var distance = distanceVector.magnitude;
        if (distance > _range) ProjectilePoolManager.Instance.Release(this);
    }

    protected virtual void GiveDamage(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out NormalMonster monster))
            {
                monster.TakeDamage(Damage);
            }
        }
    }

    private void DecreaseDurability(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _durability -= 1;
        }

        if (_durability <= 0)
        {
            OnDestroySelf();
        }
    }

    protected virtual void OnDestroySelf()
    {
        ProjectilePoolManager.Instance.Release(this);
    }
}