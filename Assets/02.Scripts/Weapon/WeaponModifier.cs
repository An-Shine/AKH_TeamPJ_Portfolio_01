public class WeaponModifier
{
    public StatModifier Damage;
    public StatModifier FireRate;
    public StatModifier ReloadSpeed;
    public StatModifier ChargeSpeed;

    public int MagazineSize;

    public int ProjectileCount;
    public float Angle;
    public float Range;

    public WeaponModifier()
    {
        Damage = new StatModifier();
        FireRate = new StatModifier();
        ReloadSpeed = new StatModifier();
        ChargeSpeed = new  StatModifier();
        
        MagazineSize = 0;

        ProjectileCount = 0;
        Angle = 0;
    }
}
