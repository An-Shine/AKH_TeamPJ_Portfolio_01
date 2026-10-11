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

    public static void ApplyImmediateEffect(ItemEffectData effect, UnityEngine.Transform playerTransform)
    {
        IItemImmediateEffect immediateEffect = ImmediateEffectFactory.Create(effect.itemEffectType);
        ImmediateEffectContext immediateEffectContext = new ImmediateEffectContext
        {
            PlayerTransform = playerTransform,
            Id = string.Empty,
            Value = effect.Value,
        };
        immediateEffect.Apply(immediateEffectContext);
    }

    public static void ApplyToPlayer(ItemPlayerStat stat, IEnumerable<Item> items)
    {
        foreach (var item in items)
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
        switch (effect.itemEffectType)
        {
            case ItemEffectType.MaxHealthModifier:
            {
                stat.MaxHealthModifier += (int)effect.Value;
                break;
            }
            case ItemEffectType.MoveSpeedMultiplier:
            {
                stat.MoveSpeedMultiplier += effect.Value;
                break;
            }
        }
    }

    public static void ApplyToWeapon(ItemWeaponStat stat, IEnumerable<Item> items)
    {
        foreach (var item in items)
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
        switch (effect.itemEffectType)
        {
            case ItemEffectType.FireSpeedModifier:
            {
                stat.FireSpeedModifier += (int)effect.Value;
                break;
            }
            case ItemEffectType.FireSpeedMultiplier:
            {
                stat.FireSpeedMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.RangeModifier:
            {
                stat.RangeModifier += (int)effect.Value;
                break;
            }
            case ItemEffectType.RangeMultiplier:
            {
                stat.RangeMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.MagazineSizeMultiplier:
            {
                stat.MagazineSizeMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.ProjectileCountModifier:
            {
                stat.ProjectileCountModifier += (int)effect.Value;
                break;
            }
            case ItemEffectType.SpreadAngleModifier:
            {
                stat.SpreadAngleModifier += (int)effect.Value;
                break;
            }
            case ItemEffectType.SpreadAngleMultiplier:
            {
                stat.SpreadAngleMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.LastBullet:
            {
                stat.HasLastBulletEffect = true;
                stat.LastBulletMultiplier = effect.Value;
                break;
            }
            case ItemEffectType.ChargeSpeedMultiplier:
            {
                stat.ChargeSpeedMultiplier += effect.Value;
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
        switch (effect.itemEffectType)
        {
            case ItemEffectType.DamageModifier:
            {
                stat.DamageModifier += (int)effect.Value;
                break;
            }
            case ItemEffectType.DamageMultiplier:
            {
                stat.DamageMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.MoveSpeedMultiplier:
            {
                stat.MoveSpeedMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.ProjectileSizeMultiplier:
            {
                stat.SizeMultiplier += effect.Value;
                break;
            }
            case ItemEffectType.DamageBonusByPenetration:
            {
                stat.DamageBonusByPenetration += effect.Value;
                break;
            }
            case ItemEffectType.AddBlackHoleEffect:
            {
                stat.IsBlackHole = true;
                stat.BlackHoleDamage = effect.Value;
                break;
            }
            case ItemEffectType.AddGammaFieldEffect:
            {
                stat.IsGammaField = true;
                stat.GammaDamage = effect.Value;
                break;
            }
        }
    }
}