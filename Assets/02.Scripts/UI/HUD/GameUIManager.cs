using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private HealthUI _healthUI;
    [SerializeField] private WeaponUI _weaponUI;
    [SerializeField] private ItemUI _itemUI;

    private PlayerManager _playerManager;
    private WeaponManager _weaponManager;
    private ItemManager _itemManager;

    private void Start()
    {
        _playerManager = PlayerManager.Instance;
        _weaponManager = WeaponManager.Instance;
        _itemManager = ItemManager.Instance;
    }

    private void Update()
    {
        RefreshHealthUI();
        RefreshWeaponUI();
        RefreshItemUI();
    }

    private void RefreshHealthUI()
    {
        var status = _playerManager.PlayerStatusData;

        _healthUI.SetHealthData(status.CurrentHP, status.MaxHP);
    }

    private void RefreshWeaponUI()
    {
        var weapon = _weaponManager.CurrentWeapon;

        if (weapon == null || weapon.Data == null) return;

        _weaponUI.SetWeaponData(weapon.Data.Name, weapon.Data.ProjectileType, weapon.RemainingAmmo,
            weapon.MagazineSize);
    }

    private void RefreshItemUI()
    {
        if (_itemManager == null || _itemUI == null || _itemManager.GainedItemList == null) return;

        _itemUI.SetItemData(_itemManager.GainedItemList);
    }
}