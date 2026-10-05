using UnityEngine;

public class LaserProjectile : Projectile
{
    [SerializeField] private SpriteRenderer _laserRenderer;
    [SerializeField] private Transform _startEffect;
    [SerializeField] private Transform _bodyEffect;
    [SerializeField] private Transform _endEffect;
    [SerializeField] private LayerMask _targetLayer;

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

    public void UpdateLaserShape(Vector3 position, Vector3 direction)
    {
        transform.position = position;
        _direction = direction.normalized;
    }

    public override void OnSpawn(Vector3 position, Vector3 direction, float range, int damage)
    {
        base.OnSpawn(position, direction, range, damage);
        CreateLaser();
    }

    public void CreateLaser()
    {
        Vector2 origin = transform.position;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            _direction,
            _range,
            _targetLayer);

        Vector2 endPosition;

        if (hit.collider != null)
        {
            endPosition = hit.point;
            GiveDamage(hit.collider);
        }
        else
        {
            endPosition = origin + (Vector2)_direction * _range;
        }

        Debug.Log($"direction : {_direction}");
        Debug.Log($"endPosition : {endPosition}");

        UpdateLaserEffect(origin, endPosition);
    }

    private void UpdateLaserEffect(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        Debug.Log(distance);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.position = start;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Body
        //_laserRenderer.size =  new Vector2(distance, _laserRenderer.size.y);
        _bodyEffect.localScale = new Vector3(distance, _bodyEffect.localScale.y, _bodyEffect.localScale.z);
        float bodyPositionX = distance / 2f + 0.75f;
        _bodyEffect.localPosition = new Vector3(bodyPositionX, 0f, 0f);

        // 충돌 지점
        float endPositionX = distance + 1.25f;
        _endEffect.localPosition = new Vector3(endPositionX, 0f, 0f);
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