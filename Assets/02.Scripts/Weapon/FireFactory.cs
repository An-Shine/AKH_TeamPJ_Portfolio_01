public static class FireFactory
{
    public static FireBehaviour CreateBehaviour(FireBehaviourType fireBehaviourType)
    {
        FireBehaviour fireBehaviour =new NormalFire();
        switch (fireBehaviourType)
        {
            case FireBehaviourType.Auto:
            {
                //fireBehaviour = new AutoFire();
                break;
            }
            case FireBehaviourType.Charge:
            {
                //fireBehaviour = new ChargeFire();
                break;
            }
        }

        return fireBehaviour;
    }

    public static IFirePattern CreatePattern(FirePatternType patternType)
    {
        IFirePattern firePattern =new NormalFirePattern();
        switch (patternType)
        {
            case FirePatternType.Spread:
            {
                break;
            }
            case FirePatternType.RandomSpread:
            {
                break;
            }
            case FirePatternType.Parallel:
            {
                break;
            }
        }

        return firePattern;
    }
}