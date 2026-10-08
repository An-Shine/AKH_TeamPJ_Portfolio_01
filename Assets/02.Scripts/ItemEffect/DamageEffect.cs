public class DamageEffect : IItemImmediateEffect
{
    public void Apply(ImmediateEffectContext context)
    {
        if (context.PlayerTransform.TryGetComponent(out PlayerManager playerManager))
        {
            int damageAmount = (int)context.Value;
            playerManager.TryTakeDamage(damageAmount);
        }
    }
}