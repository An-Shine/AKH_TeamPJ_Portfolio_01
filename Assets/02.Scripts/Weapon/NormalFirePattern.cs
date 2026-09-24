public class NormalFirePattern : IFirePattern
{
    public void Fire(FireContext context)
    {
        //spawner에게 spawn 요청 by context.Position, context.Direction
    }
}