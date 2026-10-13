using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    private AimDisplayMode _aimDisplayMode = AimDisplayMode.Crosshair;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private ParticleSystem _reloadParticle;
    [SerializeField] private GameObject _reloadText;
    private float _attackVisualDuration = 0.7f; // 공격 애니메이션 길이로 맞추기

    private Weapon _activeWeapon;
    private bool _wasFireHeld;
    private float _chargeTime;
    private float _attackVisualUntil;

    public bool IsAttacking => Time.time < _attackVisualUntil;
    public bool IsReloading => WeaponManager.Instance != null &&
                               WeaponManager.Instance.CurrentWeapon != null &&
                               WeaponManager.Instance.CurrentWeapon.IsReloading;
    public int ShotSequence { get; private set; }
    public AimDisplayMode AimMode => _aimDisplayMode;
    public Transform Muzzle => _muzzle;
    [SerializeField] private Transform _origin;
    public Transform Origin => _origin;

    private void LateUpdate()
    {
        RefreshReloadText();
    }

    private void RefreshReloadText()
    {
        if (_reloadText == null) return;

        Weapon weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;
        bool showReloadText = weapon != null && weapon.RemainingAmmo <= 0;
        if (_reloadText.activeSelf != showReloadText)
        {
            _reloadText.SetActive(showReloadText);
        }
    }

    public void Tick(bool fireHeld, bool isDashing, Vector2 aimDirection)
    {
        if (PlayerManager.IsInputBlocked) return;
        WeaponManager weaponManager = WeaponManager.Instance;
        Weapon equippedWeapon = weaponManager.CurrentWeapon;
        if (_activeWeapon != equippedWeapon)
        {
            _activeWeapon = equippedWeapon;
            CancelAttackVisual();
        }

        if (_activeWeapon == null)
        {
            return;
        }

        if (isDashing || Time.timeScale <= 0f)
        {
            if (_activeWeapon.Data.BehaviourType == FireBehaviourType.Continuous)
            {
                TickCharge(false);
                _wasFireHeld = false;
            }

            CancelAttackVisual();
            return;
        }

        Vector2 fireOrigin = _muzzle != null ? _muzzle.position : transform.position;
        _activeWeapon.SetFirePose(fireOrigin, aimDirection);

        if (_activeWeapon.Data.BehaviourType == FireBehaviourType.Charge ||
            _activeWeapon.Data.BehaviourType == FireBehaviourType.Continuous)
        {
            TickCharge(fireHeld);
        }
        else if (fireHeld && _activeWeapon.TryFire())
        {
            ShowAttack();
        }

        _wasFireHeld = fireHeld;
    }

    private void TickCharge(bool fireHeld)
    {
        if (fireHeld)
        {
            if (!_wasFireHeld)
            {
                _chargeTime = 0f;
                _activeWeapon.OnMouseDown();
            }

            _chargeTime += Time.deltaTime * _activeWeapon.ChargeSpeed;
            _activeWeapon.OnMouseHeld();
        }
        else if (_wasFireHeld)
        {
            bool charged = _chargeTime >= _activeWeapon.Data.ChargeTime && _activeWeapon.CheckRemainingAmmo();
            _activeWeapon.OnMouseUp();
            _chargeTime = 0f;
            if (charged)
                ShowAttack();
        }
    }

    private void ShowAttack()
    {
        _attackVisualUntil = Time.time + _attackVisualDuration;
        ShotSequence++;
    }

    public float Reload()
    {
        if (PlayerManager.IsInputBlocked) return 0f;
        if (_activeWeapon == null)
        {
            return 0f;
        }

        _activeWeapon.Reload();
        if (_reloadParticle != null)
        {
            _reloadParticle.transform.position = transform.position;
            _reloadParticle.gameObject.SetActive(true);
            _reloadParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _reloadParticle.Play(true);
        }

        WeaponStatModifier modifier = _activeWeapon.Modifier.ReloadSpeed;
        float reloadSpeed = (1f + modifier.Flat) * (1f + modifier.Rate);
        return reloadSpeed > 0f ? _activeWeapon.Data.ReloadTime / reloadSpeed : 0f;
    }

    public void CancelActions()
    {
        StopReloadParticle();
        CancelAttackVisual();
        Weapon weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : _activeWeapon;
        if (weapon != null) weapon.CancelActions();
    }

    public void CancelAttackVisual()
    {
        _attackVisualUntil = 0f;
        //_wasFireHeld = false;
        _chargeTime = 0f;
    }

    public void SetAimPresentation(AimDisplayMode mode, Transform muzzle)
    {
        _aimDisplayMode = mode;
        _muzzle = muzzle;
    }

    private void OnDisable()
    {
        StopReloadParticle();
    }

    private void StopReloadParticle()
    {
        if (_reloadParticle != null)
            _reloadParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
