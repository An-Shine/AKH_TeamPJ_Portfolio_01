using System.Collections.Generic;

public static class ItemEffectApplier
{
    public static void ApplyStatEffect(ItemPlayerStat itemPlayerStat, ItemWeaponStat itemWeaponStat,
        ItemProjectileStat itemProjectileStat, ItemEffectData effect)
    {
        switch (effect.TargetType)
        {
            case EffectTarget.Player:
            {
                ApplyPlayerEffect(itemPlayerStat, effect);
                break;
            }
            case EffectTarget.Weapon:
            {
                ApplyWeaponEffect(itemWeaponStat, effect);
                break;
            }
            case EffectTarget.Projectile:
            {
                ApplyProjectileEffect(itemProjectileStat, effect);
                break;
            }
        }
    }

    public static void ApplyImmediateEffect(ItemEffectData effect)
    {
        switch (effect.EffectType)
        {
            case EffectType.HealHealth:
            {
                break;
            }
            case EffectType.DamageHealth:
            {
                break;
            }
        }
    }

    public static void ApplyToPlayer(ItemPlayerStat stat, IEnumerable<Item> items)
    {
        foreach (Item item in items)
        {
            foreach (ItemEffectData effect in item.Effects)
            {
                if (effect.TargetType != EffectTarget.Player)
                    continue;

                ApplyPlayerEffect(stat, effect);
            }
        }
    }

    private static void ApplyPlayerEffect(ItemPlayerStat stat, ItemEffectData effect)
    {
        switch (effect.EffectType)
        {
            case EffectType.MaxHealthModifier:
            {
                stat.MaxHealthModifier += (int)effect.Value;
                break;
            }
            case EffectType.MoveSpeedMultiplier:
            {
                stat.MoveSpeedMultiplier += effect.Value;
                break;
            }
        }
    }

    public static void ApplyToWeapon(ItemWeaponStat stat, IEnumerable<Item> items)
    {
        foreach (Item item in items)
        {
            foreach (ItemEffectData effect in item.Effects)
            {
                if (effect.TargetType != EffectTarget.Weapon)
                    continue;

                ApplyWeaponEffect(stat, effect);
            }
        }
    }

    private static void ApplyWeaponEffect(ItemWeaponStat stat, ItemEffectData effect)
    {
        switch (effect.EffectType)
        {
            case EffectType.FireSpeedModifier:
            {
                stat.FireSpeedModifier += (int)effect.Value;
                break;
            }
            case EffectType.FireSpeedMultiplier:
            {
                stat.FireSpeedMultiplier += effect.Value;
                break;
            }
            case EffectType.RangeModifier:
            {
                stat.RangeModifier += (int)effect.Value;
                break;
            }
            case EffectType.RangeMultiplier:
            {
                stat.RangeMultiplier += effect.Value;
                break;
            }
            case EffectType.MagazineSizeMultiplier:
            {
                stat.MagazineSizeMultiplier += effect.Value;
                break;
            }
            case EffectType.ProjectileCountModifier:
            {
                stat.ProjectileCountModifier += (int)effect.Value;
                break;
            }
            case EffectType.SpreadAngleModifier:
            {
                stat.SpreadAngleModifier += (int)effect.Value;
                break;
            }
            case EffectType.SpreadAngleMultiplier:
            {
                stat.SpreadAngleMultiplier += effect.Value;
                break;
            }
        }
    }

    public static void ApplyToProjectile(ItemProjectileStat stat, IEnumerable<Item> items)
    {
        foreach (Item item in items)
        {
            foreach (ItemEffectData effect in item.Effects)
            {
                if (effect.TargetType != EffectTarget.Projectile)
                    continue;

                ApplyProjectileEffect(stat, effect);
            }
        }
    }

    private static void ApplyProjectileEffect(ItemProjectileStat stat, ItemEffectData effect)
    {
        switch (effect.EffectType)
        {
            case EffectType.DamageModifier:
            {
                stat.DamageModifier += (int)effect.Value;
                break;
            }
            case EffectType.DamageMultiplier:
            {
                stat.DamageMultiplier += effect.Value;
                break;
            }
            case EffectType.MoveSpeedMultiplier:
            {
                stat.MoveSpeedMultiplier += effect.Value;
                break;
            }
            case EffectType.ProjectileSizeMultiplier:
            {
                stat.SizeMultiplier += effect.Value;
                break;
            }
        }
    }
}