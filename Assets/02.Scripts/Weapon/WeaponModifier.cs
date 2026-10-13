public class WeaponModifier
{
    public WeaponStatModifier Damage;
    public WeaponStatModifier FireRate;
    public WeaponStatModifier ReloadSpeed;
    public WeaponStatModifier ChargeSpeed;
    public int MagazineSize;
    public int ProjectileCount;
    public float Angle;
    public float Range;

    public float Size;

    public WeaponModifier()
    {
        Damage = new WeaponStatModifier();
        FireRate = new WeaponStatModifier();
        ReloadSpeed = new WeaponStatModifier();
        ChargeSpeed = new WeaponStatModifier();

        MagazineSize = 0;

        ProjectileCount = 0;
        Angle = 0;

        Size = 1;
    }
}