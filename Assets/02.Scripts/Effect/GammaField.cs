using UnityEngine;

public class GammaField : Effect
{
    [SerializeField] private SFXType _spawnSfxType;
    [SerializeField] private float _damage;
    [SerializeField] private float _damageInterval;
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
            Destroy(gameObject);
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

    public void OnSpawn()
    {
        AudioManager.Instance.PlaySFX(_spawnSfxType);
        transform.localScale = _scale;
    }

    private void CheckLifeTime()
    {
        if (!_aleadyEnd && _lifeTimer <= 0f)
        {
            _animator.SetTrigger(EndAnimationKey);
        }
    }
}