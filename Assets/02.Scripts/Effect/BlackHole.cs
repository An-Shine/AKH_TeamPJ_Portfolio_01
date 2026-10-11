using UnityEngine;

public class BlackHole : Effect
{
    [SerializeField] private SFXType _spawnSfxType;
    [SerializeField] private Vector3 _scale;

    private Animator _animator;
    private const string EndAnimationKey = "End";
    private bool _aleadyEnd;

    private BlackHoleDamageControl _blackHoleDamageControl;

    private void Awake()
    {
        if (!gameObject.TryGetComponent(out _animator))
        {
            EffectSpawner.Instance.Release(this);
            return;
        }

        GameObject child = transform.GetChild(0).gameObject;
        if (!child.TryGetComponent(out _blackHoleDamageControl))
        {
            EffectSpawner.Instance.Release(this);
        }
    }

    private void Start()
    {
        _aleadyEnd = false;
        _lifeTimer = _lifeDuration;

        _blackHoleDamageControl.SetDamage(_damage);
    }

    private void Update()
    {
        _lifeDuration -= Time.deltaTime;
        CheckLifeTime();
    }

    public override void OnSpawn()
    {
        AudioManager.Instance.PlaySFX(_spawnSfxType);
        transform.localScale = _scale;
        _aleadyEnd = false;
        _lifeTimer = _lifeDuration;
    }

    private void CheckLifeTime()
    {
        if (!_aleadyEnd && _lifeTimer <= 0f)
        {
            _animator.SetTrigger(EndAnimationKey);
        }
    }
}