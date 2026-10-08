public class HealEffect : IItemImmediateEffect
{
    public void Apply(ImmediateEffectContext context)
    {
        PlayerManager manager = PlayerManager.Instance;
        if (manager != null)
        {
            int healAmount = (int)context.Value;
            //manager.Heal(healAmount);
        }
    }
}