using UnityEngine;

public class Effect : MonoBehaviour
{
    private EffectType _type;
    public EffectType Type => _type;

    protected float _lifeDuration;
    protected float _lifeTimer;

    public void Initialize(EffectType type)
    {
        _type = type;
    }

    public virtual void OnSpawn(float lifeTime)
    {
        Debug.Log("Require Override");
    }

    protected void DestroyEffect()
    {
        EffectSpawner.Instance.Release(this);
    }
}