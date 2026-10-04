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

    [System.Serializable]
    private class ProjectileIconEntry
    {
        public ProjectileType Type;
        public Projectile ProjectilePrefab;
    }

    [SerializeField] private List<ProjectileIconEntry> _projectileIcons;
    [SerializeField] private List<WeaponIconEntry> _weaponIcons;
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private RectTransform _projectileIconContainer;
    [SerializeField] private Image _projectileIconPrefab;
    [SerializeField] private Color _availableAmmoColor = Color.white;
    [SerializeField] private Color _usedAmmoColor = Color.gray;

    private Sprite _lastWeaponIcon;
    private int _lastRemainingAmmo = -1;
    private int _lastMagazineSize = -1;
    private ProjectileType? _lastProjectileType;

    private readonly List<Image> _projectileIconImages = new();

    private const int ProjectileIconCount = 10;

    private void Awake()
    {
        CreateProjectileIcons();
    }

    private void CreateProjectileIcons()
    {
        if (_projectileIconPrefab == null || _projectileIconContainer == null) return;

        for (var i = 0; i < ProjectileIconCount; i++)
        {
            var icon = Instantiate(_projectileIconPrefab, _projectileIconContainer);
            icon.enabled = false;
            _projectileIconImages.Add(icon);
        }
    }

    private Sprite FindWeaponIcon(string weaponName)
    {
        if (_weaponIcons == null) return null;

        foreach (var entry in _weaponIcons)
        {
            if (entry != null && entry.WeaponName == weaponName)
                return entry.Icon;
        }

        return null;
    }

    private Sprite FindProjectileIcon(ProjectileType projectileType)
    {
        if (_projectileIcons == null) return null;

        foreach (var entry in _projectileIcons)
        {
            if (entry == null || entry.Type != projectileType || entry.ProjectilePrefab == null) continue;

            var spriteRenderer = entry.ProjectilePrefab.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null) return spriteRenderer.sprite;
        }

        return null;
    }

    public void SetWeaponData(string weaponName, ProjectileType projectileType, int remainingAmmo, int magazineSize)
    {
        var weaponIcon = FindWeaponIcon(weaponName);

        if (_lastWeaponIcon != weaponIcon)
        {
            _lastWeaponIcon = weaponIcon;
            _weaponIcon.sprite = weaponIcon;
            _weaponIcon.enabled = weaponIcon != null;
        }

        RefreshProjectileIcons(projectileType);

        if (_lastRemainingAmmo == remainingAmmo && _lastMagazineSize == magazineSize) return;
        _lastRemainingAmmo = remainingAmmo;
        _lastMagazineSize = magazineSize;
        _ammoText.text = $"{remainingAmmo} / {magazineSize}";
        RefreshAmmoIcons(remainingAmmo, magazineSize);
    }

    private void RefreshProjectileIcons(ProjectileType projectileType)
    {
        if (_lastProjectileType == projectileType) return;

        _lastProjectileType = projectileType;
        var projectileSprite = FindProjectileIcon(projectileType);

        foreach (var icon in _projectileIconImages)
        {
            if (icon == null) continue;
            icon.sprite = projectileSprite;
            icon.enabled = projectileSprite != null;
        }
    }

    private void RefreshAmmoIcons(int remainingAmmo, int magazineSize)
    {
        if (magazineSize <= 0) return;

        var clampedRemainingAmmo = Mathf.Clamp(remainingAmmo, 0, magazineSize);
        var usedAmmo = magazineSize - clampedRemainingAmmo;

        var usedIconCount = usedAmmo * ProjectileIconCount / magazineSize;

        for (var i = 0; i < _projectileIconImages.Count; i++)
        {
            var icon = _projectileIconImages[i];

            if (icon == null) continue;

            var isUsed = i < usedIconCount;
            icon.color = isUsed ? _usedAmmoColor : _availableAmmoColor;
        }
    }
}