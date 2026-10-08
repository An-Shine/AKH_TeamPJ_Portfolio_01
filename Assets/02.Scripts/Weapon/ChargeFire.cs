using UnityEngine;

public class ChargeFire : FireBehaviour
{
    private float _chargeTimer;
    public float ChargeTime => _chargeTimer;

    private bool _isFiring;
    public bool IsFiring => _isFiring;

    public override void OnMouseDown(Weapon weapon)
    {
        AudioManager.Instance.PlayLoopSFX(SFXType.Charging);
        _chargeTimer = 0f;
        _isFiring = true;
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
        _isFiring = false;
    }
}