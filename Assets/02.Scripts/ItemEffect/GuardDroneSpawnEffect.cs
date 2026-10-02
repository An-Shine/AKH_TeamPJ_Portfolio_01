public class GuardDroneSpawnEffect : IItemImmediateEffect
{
    public void Apply(ImmediateEffectContext context)
    {
        FollowerSpawner.Instance.Spawn(
            (int)context.Value,
            FollowerType.Defense,
            context.PlayerTransform,
            0f,
            1f);
    }
}