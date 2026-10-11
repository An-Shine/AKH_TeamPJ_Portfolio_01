using UnityEngine;

public class GammaField : Effect
{
    [SerializeField] private SFXType _spawnSfxType;
    [SerializeField] private float _damage;
    [SerializeField] private Vector3 _scale;

    private Animator _animator;
    private const string EndAnimationKey = "End";
    private bool _aleadyEnd;

    private float _lifeDuration;
    private float _lifeTimer;

    private void Awake()
    {
        if (!gameObject.TryGetComponent(out _animator))
        {
            EffectSpawner.Instance.Release(this);
        }
    }

    private void Start()
    {
        _aleadyEnd = false;
        _lifeTimer = _lifeDuration;
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out NormalMonster monster))
            {
                monster.TakeDamage(_damage * Time.deltaTime);
            }
        }
    }
}