using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EffectSpawner : MonoBehaviour
{
    private static EffectSpawner _instance;
    public static EffectSpawner Instance => _instance;

    [SerializeField] private List<EffectEntry> _entries;
    private Dictionary<EffectType, ObjectPool<Effect>> _effectPools;
    [SerializeField] private Transform _effectContainer;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }

        _instance = this;
    }

    private void Start()
    {
        _effectPools = new Dictionary<EffectType, ObjectPool<Effect>>();
        foreach (EffectEntry effectEntry in _entries)
        {
            _effectPools[effectEntry.Type] =
                new ObjectPool<Effect>(
                    () =>
                    {
                        Effect effect = Instantiate(effectEntry.EffectPrefab, _effectContainer);
                        effect.Initialize(effectEntry.Type);
                        return effect;
                    },
                    ActivateEffect,
                    DisableProjectile,
                    DestroyProjectile,
                    false,
                    effectEntry.InitialAmount,
                    effectEntry.MaxAmount);
        }
    }

    public Effect Get(EffectType type)
    {
        return _effectPools[type].Get();
    }

    public void Release(Effect effect)
    {
        _effectPools[effect.Type].Release(effect);
    }

    private void ActivateEffect(Effect effect)
    {
        effect.gameObject.SetActive(true);
    }

    private void DisableProjectile(Effect effect)
    {
        effect.gameObject.SetActive(false);
    }

    private void DestroyProjectile(Effect effect)
    {
        if (effect == null) return;
        Destroy(effect.gameObject);
    }

    public void Spawn(EffectType type, Vector3 position, float damage)
    {
        Effect effect = Get(type);
        effect.transform.position = position;
        effect.OnSpawn();
        effect.SetDamage(damage);
    }
}