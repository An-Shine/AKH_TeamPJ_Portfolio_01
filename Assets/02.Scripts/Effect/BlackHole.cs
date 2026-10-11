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

    private BlackHoleDamageControl _blackHoleDamageControl;

    private float _lifeDuration;
    private float _lifeTimer;

    private float _timer;

    private void Awake()
    {
        if (!gameObject.TryGetComponent(out _animator))
        {
            Destroy(gameObject);
            return;
        }

        GameObject child = transform.GetChild(0).gameObject;
        if (!child.TryGetComponent(out _blackHoleDamageControl))
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _lifeDuration = 10000f;
        _aleadyEnd = false;
        _timer = 0f;
        _lifeTimer = _lifeDuration;

        _blackHoleDamageControl.SetDamage(_damage, _damageInterval);
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

    private void CheckLifeTime()
    {
        if (!_aleadyEnd && _lifeTimer <= 0f)
        {
            _animator.SetTrigger(EndBlackHoleAnimationKey);
        }
    }
}