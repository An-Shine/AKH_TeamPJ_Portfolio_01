using UnityEngine;

public class ChargeFire : FireBehaviour
{
    private float _chargeTimer;

    public override void OnMouseDown(Weapon weapon)
    {
        AudioManager.Instance.PlayLoopSFX(SFXType.Charging);
        _chargeTimer = 0f;
    }

    public override void OnMouseHeld(Weapon weapon)
    {
        _chargeTimer += Time.deltaTime * weapon.ChargeSpeed;
    }

    public override void OnMouseUp(Weapon weapon)
    {
        AudioManager.Instance.StopLoopSFX();
        if (_chargeTimer >= weapon.Data.ChargeTime)
        {
            weapon.TryFire();
        }
    }
}