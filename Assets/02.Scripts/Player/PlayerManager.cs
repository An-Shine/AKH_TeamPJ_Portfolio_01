using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private const float WeaponCooldownUnits = 10f;
    private static PlayerManager _instance;
    public static PlayerManager Instance => _instance;

    public PlayerStatus PlayerStatusData { get; private set; }
    private float _hitInvulnerableUntil;
    private Weapon _modifiedWeapon;
    private float _appliedFireFlat;
    private float _appliedFireRate;
    private int _appliedMagazine;
    private int _appliedProjectileCount;
    private float _appliedAngle;
    private float _appliedRange;

    public PlayerController PlayerControllerObj;
    public PlayerAim PlayerAimObj;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        RefreshFromManagers();
    }

    private void Update()
    {
        RefreshFromManagers();
    }

    private void OnDestroy()
    {
        ApplyModifierDelta(_modifiedWeapon, 0f, 0f, 0, 0, 0f, 0f);
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void EnsureStatus()
    {
        if (PlayerStatusData != null || DataManager.Instance == null || DataManager.Instance.PlayerStatusDatas == null)
        {
            return;
        }

        SavePlayerStatus saved = DataManager.Instance.PlayerStatusDatas;
        PlayerStatusData = new PlayerStatus(saved.MaxHP, saved.MoveSpeed, saved.DashDistance,
            saved.DashDuration, saved.DashInvulnerableDuration, saved.DashCooldown,
            saved.HitInvulnerableDuration, 0);
    }

    private void RefreshFromManagers()
    {
        EnsureStatus();
        ItemManager items = ItemManager.Instance;
        Weapon weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;

        if (PlayerStatusData != null)
        {
            PlayerStatusData.ApplyItemStats(items != null ? items.PlayerStat : null);
        }

        ApplyWeaponBonuses(weapon, items != null ? items.WeaponStat : null);
        if (PlayerStatusData != null)
        {
            RefreshWeaponStatus(weapon, items != null ? items.ProjectileStat : null);
        }
    }

    private void ApplyWeaponBonuses(Weapon weapon, ItemWeaponStat stat)
    {
        if (_modifiedWeapon != weapon)
        {
            ApplyModifierDelta(_modifiedWeapon, 0f, 0f, 0, 0, 0f, 0f);
            _modifiedWeapon = weapon;
        }

        if (weapon == null || weapon.Data == null || weapon.Modifier == null)
        {
            return;
        }

        float fireFlat = stat != null ? stat.FireSpeedModifier : 0f;
        float fireRate = stat != null ? stat.FireSpeedMultiplier : 0f;
        int magazine = stat != null ? Mathf.RoundToInt(weapon.Data.MagazineSize * stat.MagazineSizeMultiplier) : 0;
        int projectileCount = stat != null ? stat.ProjectileCountModifier : 0;
        float angle = stat != null
            ? (weapon.Data.Angle + stat.SpreadAngleModifier) * (1f + stat.SpreadAngleMultiplier) - weapon.Data.Angle
            : 0f;
        float range = stat != null
            ? (weapon.Data.Range + stat.RangeModifier) * (1f + stat.RangeMultiplier) - weapon.Data.Range
            : 0f;

        ApplyModifierDelta(weapon, fireFlat, fireRate, magazine, projectileCount, angle, range);
    }

    private void ApplyModifierDelta(Weapon weapon, float fireFlat, float fireRate, int magazine,
        int projectileCount, float angle, float range)
    {
        if (weapon != null && weapon.Modifier != null)
        {
            WeaponModifier modifier = weapon.Modifier;
            modifier.FireRate.Flat += fireFlat - _appliedFireFlat;
            modifier.FireRate.Rate += fireRate - _appliedFireRate;
            modifier.MagazineSize += magazine - _appliedMagazine;
            modifier.ProjectileCount += projectileCount - _appliedProjectileCount;
            modifier.Angle += angle - _appliedAngle;
            modifier.Range += range - _appliedRange;
        }

        _appliedFireFlat = fireFlat;
        _appliedFireRate = fireRate;
        _appliedMagazine = magazine;
        _appliedProjectileCount = projectileCount;
        _appliedAngle = angle;
        _appliedRange = range;
    }

    private void RefreshWeaponStatus(Weapon weapon, ItemProjectileStat projectileStat)
    {
        if (weapon == null || weapon.Data == null)
        {
            PlayerStatusData.Damage = 0f;
            PlayerStatusData.FireInterval = 0f;
            PlayerStatusData.isChargeWeapon = false;
            PlayerStatusData.ChargeCooldown = 0f;
            return;
        }

        float fireRate = (weapon.Data.FireRate + weapon.Modifier.FireRate.Flat)
                         * (1f + weapon.Modifier.FireRate.Rate);
        PlayerStatusData.FireInterval = fireRate > 0f ? WeaponCooldownUnits / fireRate : float.PositiveInfinity;
        PlayerStatusData.isChargeWeapon = weapon.Data.BehaviourType == FireBehaviourType.Charge;
        PlayerStatusData.ChargeCooldown = PlayerStatusData.isChargeWeapon
            ? weapon.Data.ChargeTime / Mathf.Max(0.0001f, weapon.ChargeSpeed)
            : 0f;

        PlayerStatusData.Damage = 0f;
        if (DataManager.Instance == null || DataManager.Instance.ProjectileDatas == null ||
            !DataManager.Instance.ProjectileDatas.TryGetValue(weapon.Data.ProjectileType,
                out ProjectileData projectileData))
        {
            return;
        }

        int flat = projectileStat != null ? projectileStat.DamageModifier : 0;
        float rate = projectileStat != null ? projectileStat.DamageMultiplier : 0f;
        PlayerStatusData.Damage = Mathf.Max(0, Mathf.RoundToInt((projectileData.Damage + flat) * (1f + rate)));
    }

    public void GetWeapon(WeaponData weaponData)
    {
        if (weaponData == null || DataManager.Instance == null || DataManager.Instance.WeaponDatas == null)
            return;

        foreach (var entry in DataManager.Instance.WeaponDatas)
        {
            if (ReferenceEquals(entry.Value, weaponData))
            {
                GetWeapon(entry.Key);
                return;
            }
        }
    }

    public void GetWeapon(string weaponId)
    {
        // if (WeaponManager.Instance == null || !WeaponManager.Instance.GainWeapon(weaponId))
        //     return;

        if (WeaponManager.Instance.CurrentWeapon == null)
            WeaponManager.Instance.EquipByIndex(0);
        RefreshFromManagers();
    }

    public void GetItem()
    {
        RefreshFromManagers();
    }

    public void ModifyHealth(float amount)
    {
        EnsureStatus();
        if (PlayerStatusData != null)
            PlayerStatusData.ModifyHealth(amount);
    }

    public bool TryTakeDamage(float damage)
    {
        EnsureStatus();
        if (PlayerStatusData == null || !PlayerStatusData.IsVulnerable ||
            PlayerStatusData.CurrentHP <= 0f || Time.timeScale <= 0f ||
            Time.time < _hitInvulnerableUntil || damage <= 0f ||
            float.IsNaN(damage) || float.IsInfinity(damage))
        {
            return false;
        }

        PlayerStatusData.ModifyHealth(-damage);
        _hitInvulnerableUntil = Time.time + Mathf.Max(0f, PlayerStatusData.HitInvulnerableDuration);
        return true;
    }

    public void MonsterHit()
    {
        // TODO: Damage is supplied by the monster combat system.
    }

    public void BulletHit()
    {
        // TODO: Damage is supplied by the projectile combat system.
    }

    private void Save()
    {
    }

    private void Load()
    {
    }
}