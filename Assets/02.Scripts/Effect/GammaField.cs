using UnityEngine;

public class GammaField : Effect
{
    [SerializeField] private SFXType _spawnSfxType;
    [SerializeField] private Vector3 _scale;

    private Animator _animator;
    private bool _aleadyEnd;

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
        _lifeTimer -= Time.deltaTime;
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
            _aleadyEnd = true;
            _animator.Play(EndAnimHash);
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