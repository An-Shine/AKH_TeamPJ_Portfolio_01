public class DamageEffect : IItemImmediateEffect
{
    public void Apply(ImmediateEffectContext context)
    {
        PlayerManager manager = PlayerManager.Instance;
        if (manager != null)
        {
            int damageAmount = (int)context.Value;
            manager.TryTakeDamage(damageAmount);
        }
    }
}