public static class FireFactory
{
    public static FireBehaviour CreateBehaviour(FireType fireType)
    {
        FireBehaviour fireBehaviour =new NormalFire();
        switch (fireType)
        {
            case FireType.Auto:
            {
                //fireBehaviour = new AutoFire();
                break;
            }
            case FireType.Charge:
            {
                //fireBehaviour = new ChargeFire();
                break;
            }
        }

        return fireBehaviour;
    }

    public static IFirePattern CreatePattern(ProjectileType projectileType)
    {
        IFirePattern firePattern =new NormalFirePattern();
        switch (projectileType)
        {
            case ProjectileType.Explosion:
            {
                break;
            }
            case ProjectileType.BigExplosion:
            {
                break;
            }
            case ProjectileType.Laser:
            {
                break;
            }
            case ProjectileType.Shot:
            {
                break;
            }
            case ProjectileType.Penetration:
            {
                break;
            }
        }

        return firePattern;
    }
}