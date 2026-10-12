public class ItemWeaponStat
{
    public float FireSpeedModifier;
    public float FireSpeedMultiplier;
    public int RangeModifier;
    public float RangeMultiplier;
    public float MagazineSizeMultiplier;
    public int ProjectileCountModifier;
    public float SpreadAngleModifier;
    public float SpreadAngleMultiplier;

    public bool HasLastBulletEffect;
    public float LastBulletMultiplier;

    public float ChargeSpeedMultiplier;

    public ItemWeaponStat()
    {
        FireSpeedModifier = 0;
        FireSpeedMultiplier = 0;
        RangeModifier = 0;
        RangeMultiplier = 0;
        MagazineSizeMultiplier = 0;
        ProjectileCountModifier = 0;
        SpreadAngleModifier = 0;
        SpreadAngleMultiplier = 0;

        HasLastBulletEffect = false;
        LastBulletMultiplier = 0;

        ChargeSpeedMultiplier = 0;
    }
}