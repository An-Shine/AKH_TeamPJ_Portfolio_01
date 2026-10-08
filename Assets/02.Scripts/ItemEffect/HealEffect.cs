public class HealEffect : IItemImmediateEffect
{
    public void Apply(ImmediateEffectContext context)
    {
        if (context.PlayerTransform.TryGetComponent(out PlayerManager playerManager))
        {
            int healAmount = (-1) * (int)context.Value;
            playerManager.TryTakeDamage(healAmount);
        }
    }
}