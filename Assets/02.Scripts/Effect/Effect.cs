using UnityEngine;

public class Effect : MonoBehaviour
{
    private EffectType _type;
    public EffectType Type => _type;

    public void Initialize(EffectType type)
    {
        _type = type;
    }

    protected void DestroyEffect()
    {
        Destroy(gameObject);
    }
}