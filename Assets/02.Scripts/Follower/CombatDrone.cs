using UnityEngine;

public class CombatDrone : Follower
{
    [SerializeField] private ProjectileType _projectileType;
    [SerializeField] private float _projectileRange;
    [SerializeField] private int _projectileDamageBonus;
    [SerializeField] private float _fireCoolTime;

    private Vector3 _adjustPosition;
    private float _timer;

    private Vector3 _fireDirection;
    private Transform _target;

    private float _judgeTimer;
    private float _judgeTerm = 0.1f;
    private bool _doSupportFire;

    private void Start()
    {
        _judgeTimer = 0f;
        _timer = 0f;
        _target = PlayerManager.Instance.PlayerAimObj.FireDirectionPoint.transform;
    }

    protected override void Update()
    {
        _judgeTimer += Time.deltaTime;
        _timer += Time.deltaTime;
        Rotation();
        base.Update();
        JudgeFire();
        Fire();
    }

    private void JudgeFire()
    {
        if (_judgeTimer >= _judgeTerm)
        {
            _judgeTimer = 0f;
            PlayerManager manager = PlayerManager.Instance;
            if (manager != null)
            {
                _doSupportFire = manager.PlayerControllerObj._weapon.IsAttacking;
                return;
            }
        }

        _doSupportFire = false;
    }

    protected override void Move()
    {
        if (_playerTransform == null) return;
        Vector3 direction = _playerTransform.position - transform.position + _adjustPosition;
        Vector3 distance = direction * (_speedScalar * Time.deltaTime);
        float distanceScalar = distance.magnitude;

        if (CanMove(direction, distanceScalar))
        {
            transform.position += distance;
        }

        if (CheckTeleportDistance(distanceScalar))
        {
            transform.position = _playerTransform.position + _adjustPosition;
        }
    }

    protected override void SetInitialPosition()
    {
        float radian = _startAngle * Mathf.Deg2Rad;
        _adjustPosition = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * _radius;
        transform.position = _playerTransform.position + _adjustPosition;
    }

    private void Rotation()
    {
        Vector3 direction = _target.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 targetAngle = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angle);
        transform.eulerAngles = targetAngle;
    }

    private void Fire()
    {
        if (!_doSupportFire) return;
        if (_timer < _fireCoolTime) return;
        _timer = 0f;
        Vector3 direction = _target.position - transform.position;
        direction.Normalize();
        AudioManager.Instance.PlaySFX(SFXType.FollowerAttack);
        ProjectileSpawner.Instance.Spawn(
            _projectileType,
            transform.position,
            direction,
            _projectileRange,
            _projectileDamageBonus);
    }
}