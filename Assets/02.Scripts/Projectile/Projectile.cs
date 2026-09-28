using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    protected ProjectileType _type;
    private int _damage;
    protected float _range;
    private int _initialDurability;
    private int _durability;
    
    protected Vector3 _direction;
    protected float _speedScalar;
    protected Vector3 _initialPosition;

    private void Update()
    {
        Move();
        CheckRange();
    }

    public void Initialize(ProjectileType type, int damage, float speedScalar, int durability)
    {
        _type = type;
        _damage = damage;
        _initialDurability = durability;
        _durability = _initialDurability;
        _direction = Vector3.zero;
        _speedScalar = speedScalar;
    }

    public virtual void OnSpawn(Vector3 position, Vector3 direction, float range)
    {
        _durability = _initialDurability;
        _direction = direction;
        _range = range;
        gameObject.transform.position = position;
        _initialPosition = position;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        GiveDamage(other);
        DecreaseDurability(other);
    }
    
    protected abstract void Move();
    protected virtual void CheckRange()
    {
        Vector3 distanceVector = transform.position - _initialPosition;
        float distance = distanceVector.magnitude;
        if (distance > _range)
        {
            ProjectilePoolManager.Instance.Release(this, _type);
        }
    }
    
    protected virtual void GiveDamage(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            //if(other.TryGetComponent(out Enemy enemy))
            //enemy.TakeDamage(_damage);
        }
    }

    protected void DecreaseDurability(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _durability -= 1;
        }
        
        if (_durability <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}