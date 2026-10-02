public class CombatDroneSpawnEffect : IItemImmediateEffect
{
    public void Apply(ImmediateEffectContext context)
    {
        FollowerSpawner.Instance.Spawn(
            (int)context.Value,
            FollowerType.Attack,
            context.PlayerTransform,
            0f,
            1f);
    }
}