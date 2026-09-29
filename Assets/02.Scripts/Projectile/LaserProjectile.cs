using UnityEngine;

public class LaserProjectile : Projectile
{
    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField] private LayerMask _targetLayer;

    protected override void Move()
    {
    }

    protected override void CheckRange()
    {
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
    }

    public override void OnSpawn(Vector3 position, Vector3 direction, float range)
    {
        base.OnSpawn(position, direction, range);
        CreateLaser();
    }

    public void CreateLaser()
    {
        var direction = _direction;
        direction.Normalize();

        var hit = Physics2D.Raycast(transform.position, direction, _range, _targetLayer);

        Vector2 endPosition;
        if (hit.collider != null)
        {
            endPosition = hit.point;
            GiveDamage(hit.collider);
        }
        else
        {
            endPosition = transform.position + direction * _range;
        }
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, endPosition);
    }
}