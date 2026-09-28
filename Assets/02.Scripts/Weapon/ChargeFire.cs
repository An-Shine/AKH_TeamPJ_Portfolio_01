using UnityEngine;

public class ChargeFire : FireBehaviour
{
    private float _chargeTimer;

    public override void OnMouseDown(Weapon weapon)
    {
        _chargeTimer = 0f;
    }

    public override void OnMouseHeld(Weapon weapon)
    {
        _chargeTimer += Time.deltaTime * weapon.ChargeSpeed;
    }

    public override void OnMouseUp(Weapon weapon)
    {
        if (_chargeTimer >= weapon.Data.ChargeTime)
        {
            weapon.TryFire();
        }
    }
}