using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private TMP_Text _ammoText;

    private Sprite _lastWeaponIcon;
    private int _lastRemainingAmmo = -1;
    private int _lastMagazineSize = -1;

    public void SetWeaponData(Sprite weaponIcon, int remainingAmmo, int magazineSize)
    {
        if (_lastWeaponIcon != weaponIcon)
        {
            _lastWeaponIcon = weaponIcon;
            _weaponIcon.sprite = weaponIcon;
            _weaponIcon.enabled = weaponIcon != null;
        }

        if (_lastRemainingAmmo == remainingAmmo && _lastMagazineSize == magazineSize) return;
        _lastRemainingAmmo = remainingAmmo;
        _lastMagazineSize = magazineSize;
        _ammoText.text = $"{remainingAmmo} / {magazineSize}";
    }
}