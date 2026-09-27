public class NormalFirePattern : IFirePattern
{
    public void Fire(FireContext context)
    {
        ProjectileSpawner.Instance.Spawn(context.Type, context.Position, context.Direction, context.Range);
    }
}