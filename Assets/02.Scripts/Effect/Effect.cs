using UnityEngine;

public class Effect : MonoBehaviour
{
    private EffectType _type;
    public EffectType Type => _type;

    public void Initialize(EffectType type)
    {
        _type = type;
    }

    public virtual void OnSpawn()
    {
        Debug.Log("Require Override");
    }

    protected void DestroyEffect()
    {
        EffectSpawner.Instance.Release(this);
    }
}