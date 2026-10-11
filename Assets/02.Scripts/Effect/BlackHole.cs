using System;
using UnityEngine;

public class BlackHole : Effect
{
    [SerializeField] private SFXType _spawnSfxType;
    [SerializeField] private float _damage;
    [SerializeField] private float _damageInterval;
    [SerializeField] private Vector3 _scale;

    private Animator _animator;
    private const string EndBlackHoleAnimationKey = "End";
    private bool _aleadyEnd;

    private float _lifeDuration;
    private float _lifeTimer;

    private float _timer;

    private void Awake()
    {
        if (!gameObject.TryGetComponent(out _animator))
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _aleadyEnd = false;
        _timer = 0f;
        _lifeTimer = _lifeDuration;
    }

    private void Update()
    {
        _lifeDuration -= Time.deltaTime;
        CheckLifeTime();
    }

    public void OnSpawn()
    {
        AudioManager.Instance.PlaySFX(_spawnSfxType);
        transform.localScale = _scale;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _timer += Time.deltaTime;
            TryGiveDamage(other);
        }
    }

    private void TryGiveDamage(Collider2D other)
    {
        if (_timer >= _damageInterval && other.TryGetComponent(out NormalMonster monster))
        {
            _timer = 0f;
            monster.TakeDamage(_damage);
        }
    }

    private void CheckLifeTime()
    {
        if (!_aleadyEnd && _lifeTimer <= 0f)
        {
            _animator.SetTrigger(EndBlackHoleAnimationKey);
        }
    }
}