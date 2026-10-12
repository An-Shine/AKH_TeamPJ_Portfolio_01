public static class ImmediateEffectFactory
{
    public static IItemImmediateEffect Create(ItemEffectType type)
    {
        IItemImmediateEffect immediateEffect = null;
        switch (type)
        {
            case ItemEffectType.HealHealth:
            {
                immediateEffect = new HealEffect();
                break;
            }
            case ItemEffectType.DamageHealth:
            {
                immediateEffect = new DamageEffect();
                break;
            }
            case ItemEffectType.CombatDrone:
            {
                immediateEffect = new CombatDroneSpawnEffect();
                break;
            }
            case ItemEffectType.GuardDrone:
            {
                immediateEffect = new GuardDroneSpawnEffect();
                break;
            }
        }

        return immediateEffect;
    }
}