public class NormalFirePattern : IFirePattern
{
    public void Fire(FireContext context, int damage)
    {
        ProjectileSpawner.Instance.Spawn(context.Type, context.Position, context.Direction, context.Range, damage);
    }
}