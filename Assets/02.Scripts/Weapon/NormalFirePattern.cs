public class NormalFirePattern : IFirePattern
{
    public Projectile Fire(FireContext context, int damage)
    {
        Projectile projectile =
            ProjectileSpawner.Instance.Spawn(context.Type, context.Position, context.Direction, context.Range, damage);
        return projectile;
    }
}