public class ItemProjectileStat
{
    public int DamageModifier;
    public float DamageMultiplier;
    public float MoveSpeedMultiplier;
    public float SizeMultiplier;
    public int PierceCount;
    public float KnockbackDistance;
    public bool IsExplosive;
    public float ExplosionRadius;
    public bool IsHoming;

    public float DamageBonusByPenetration;

    public ItemProjectileStat()
    {
        DamageModifier = 0;
        DamageMultiplier = 0;
        MoveSpeedMultiplier = 0;
        SizeMultiplier = 0;
        PierceCount = 0;
        KnockbackDistance = 0;
        IsExplosive = false;
        IsHoming = false;

        DamageBonusByPenetration = 0;
    }
}