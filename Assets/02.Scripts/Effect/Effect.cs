using UnityEngine;

public class Effect : MonoBehaviour
{
    private EffectType _type;
    public EffectType Type => _type;

    [SerializeField] protected float _lifeDuration;

    protected float _lifeTimer;
    protected float _damage;

    public void Initialize(EffectType type)
    {
        _type = type;
    }

    public virtual void OnSpawn()
    {
        Debug.Log("Require Override");
    }

    public void SetDamage(float damage)
    {
        _damage = damage;
    }

    protected void DestroyEffect()
    {
        EffectSpawner.Instance.Release(this);
    }
}