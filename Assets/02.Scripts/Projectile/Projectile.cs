using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    private ProjectileType _type;
    private int _damage;
    private int _initialDurability;
    private int _durability;
    protected Vector3 _direction;
    protected float _speedScalar;

    private void Update()
    {
        Move();
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

    public void OnSpawn(Vector3 position, Vector3 direction)
    {
        _durability = _initialDurability;
        _direction = direction;
        gameObject.transform.position = position;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        GiveDamage(other);
        DecreaseDurability();
    }
    
    protected abstract void Move();
    
    protected void GiveDamage(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            //if(other.TryGetComponent(out Enemy enemy))
            //enemy.TakeDamage(_damage);
        }
    }

    protected void DecreaseDurability()
    {
        _durability -= 1;
        if (_durability <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}