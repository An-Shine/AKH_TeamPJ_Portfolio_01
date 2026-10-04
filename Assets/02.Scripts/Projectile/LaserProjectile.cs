using System.Collections.Generic;
using UnityEngine;

public class LaserProjectile : Projectile
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private List<LayerMask> _targetLayers;
    private int _combinedLayerMask;

    private void Start()
    {
        _combinedLayerMask = 0;
        foreach (LayerMask mask in _targetLayers)
        {
            _combinedLayerMask |= mask.value;
        }
    }

    protected override void Update()
    {
        CreateLaser();
    }

    protected override void Move()
    {
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
    }

    public void UpdateDirection(Vector3 direction)
    {
        _direction = direction;
    }

    public override void OnSpawn(Vector3 position, Vector3 direction, float range, int damage)
    {
        base.OnSpawn(position, direction, range, damage);
        CreateLaser();
    }

    public void CreateLaser()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            _direction,
            _range,
            _combinedLayerMask);

        Vector2 endPosition;
        if (hit.collider != null)
        {
            endPosition = hit.point;
            GiveDamage(hit.collider);
        }
        else
        {
            endPosition = transform.position + _direction * _range;
        }

        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, endPosition);
    }

    protected override void GiveDamage(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out NormalMonster monster))
            {
                monster.TakeDamage(Damage * Time.deltaTime);
            }
        }
    }

    protected override void Rotation()
    {
    }

    protected override void Resize()
    {
    }
}