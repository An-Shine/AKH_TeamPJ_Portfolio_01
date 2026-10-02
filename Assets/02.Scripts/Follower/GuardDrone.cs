using UnityEngine;

public class GuardDrone : Follower
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _radius;
    [SerializeField] private float _rotaionSpeedScalar;
    [SerializeField] private float _startAngle;

    private int _health;
    private float _angle;

    private void Start()
    {
        _health = _maxHealth;
        _angle = _startAngle;
    }

    protected override void Move()
    {
        if (_playerTransform == null)
            return;

        _angle += _rotaionSpeedScalar * Time.deltaTime;
        float radian = _angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * _radius;
        transform.position = _playerTransform.position + offset;
    }

    public override void Initiaize(Transform playerTransform)
    {
        base.Initiaize(playerTransform);
        _health = _maxHealth;
        _angle = _startAngle;
    }

    protected override void SetInitialPosition()
    {
        float radian = _angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * _radius;
        transform.position = _playerTransform.position + offset;
    }

    public void TakeDamage(int damage)
    {
        _health -= damage;
        if (_health <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}