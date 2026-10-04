using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponUI : MonoBehaviour
{
    [System.Serializable]
    private class WeaponIconEntry
    {
        public string WeaponName;
        public Sprite Icon;
    }

    [SerializeField] private List<WeaponIconEntry> _weaponIcons;
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private TMP_Text _ammoText;

    private Sprite _lastWeaponIcon;
    private int _lastRemainingAmmo = -1;
    private int _lastMagazineSize = -1;

    private Sprite FindWeaponIcon(string weaponName)
    {
        if (_weaponIcons == null)
        {
            return null;
        }

        foreach (WeaponIconEntry entry in _weaponIcons)
        {
            if (entry != null && entry.WeaponName == weaponName)
            {
                return entry.Icon;
            }
        }
        return null;
    }

    public void SetWeaponData(string weaponName, int remainingAmmo, int magazineSize)
    {
        Sprite weaponIcon = FindWeaponIcon(weaponName);
        
        if (_lastWeaponIcon != weaponIcon)
        {
            _lastWeaponIcon = weaponIcon;
            _weaponIcon.sprite = weaponIcon;
            _weaponIcon.enabled = weaponIcon != null;
        }

        if (_lastRemainingAmmo == remainingAmmo && _lastMagazineSize == magazineSize)
        {
            return;
        }
        _lastRemainingAmmo = remainingAmmo;
        _lastMagazineSize = magazineSize;
        _ammoText.text = $"{remainingAmmo} / {magazineSize}";
    }
}