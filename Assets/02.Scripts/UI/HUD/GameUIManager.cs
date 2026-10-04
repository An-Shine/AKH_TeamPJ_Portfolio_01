using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private HealthUI _healthUI;
    [SerializeField] private WeaponUI _weaponUI;

    private PlayerManager _playerManager;
    private WeaponManager _weaponManager;

    private void Start()
    {
        _playerManager = PlayerManager.Instance;
        _weaponManager = WeaponManager.Instance;
    }

    private void Update()
    {
        RefreshHealthUI();
        RefreshWeaponUI();
    }

    private void RefreshHealthUI()
    {
        var status = _playerManager.PlayerStatusData;

        _healthUI.SetHealthData(status.CurrentHP, status.MaxHP);
    }

    private void RefreshWeaponUI()
    {
        var weapon = _weaponManager.CurrentWeapon;

        _weaponUI.SetWeaponData(weapon.Data.Name, weapon.RemainingAmmo, weapon.MagazineSize);
    }
}