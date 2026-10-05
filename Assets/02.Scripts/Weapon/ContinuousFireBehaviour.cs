public class ContinuousFireBehaviour : FireBehaviour
{
    private bool _isFiring;
    public bool IsFiring => _isFiring;

    public override void OnMouseDown(Weapon weapon)
    {
        if (weapon.TryFire())
        {
            _isFiring = true;
            AudioManager.Instance.PlayLoopSFX(SFXType.FiringLaser);
        }
    }

    public override void OnMouseHeld(Weapon weapon)
    {
        if (!_isFiring)
            return;

        weapon.RefreshDirection();

        if (!weapon.CanFire())
        {
            StopFire(weapon);
        }
    }

    public override void OnMouseUp(Weapon weapon)
    {
        StopFire(weapon);
    }

    private void StopFire(Weapon weapon)
    {
        if (!_isFiring)
            return;

        _isFiring = false;

        AudioManager.Instance.StopLoopSFX();
        weapon.StopFire();
    }
}