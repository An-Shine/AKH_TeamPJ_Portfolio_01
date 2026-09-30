public static class ImmediateEffectFactory
{
    public static IItemImmediateEffect Create(EffectType type)
    {
        IItemImmediateEffect immediateEffect = null;
        switch (type)
        {
            case EffectType.HealHealth:
            {
                immediateEffect = new HealEffect();
                break;
            }
            case EffectType.DamageHealth:
            {
                immediateEffect = new DamageEffect();
                break;
            }
        }

        return immediateEffect;
    }
}