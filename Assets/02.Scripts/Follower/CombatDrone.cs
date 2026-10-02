using UnityEngine;

public class CombatDrone : Follower
{
    [SerializeField] private ProjectileType _projectileType;
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private float _projectileRangeBonus;
    [SerializeField] private int _projectileDamageBonus;
    [SerializeField] private float _fireCoolTime;
    [SerializeField] private Vector3 _adjustPosition;

    private float _timer;

    private void Start()
    {
        _timer = 0f;
    }

    protected override void Update()
    {
        _timer += Time.deltaTime;
        Rotation();
        base.Update();
        Fire();
    }

    protected override void Move()
    {
        transform.position = _playerTransform.position + _adjustPosition;
    }

    private void Rotation()
    {
        float angle = Mathf.Atan2(_playerAim.AimDirection.y, _playerAim.AimDirection.x) * Mathf.Rad2Deg;
        Vector3 targetAngle = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angle);
        transform.eulerAngles = targetAngle;
    }

    private void Fire()
    {
        if (_timer < _fireCoolTime) return;
        _timer = 0f;
        ProjectileSpawner.Instance.Spawn(
            _projectileType,
            transform.position,
            _playerAim.AimDirection,
            _projectileRangeBonus,
            _projectileDamageBonus);
    }
}