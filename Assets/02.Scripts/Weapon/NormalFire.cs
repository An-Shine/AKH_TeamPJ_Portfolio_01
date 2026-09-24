public class NormalFire : FireBehaviour
{
    public override void OnMouseDown(Weapon weapon)
    {
        weapon.TryFire();
    }
}