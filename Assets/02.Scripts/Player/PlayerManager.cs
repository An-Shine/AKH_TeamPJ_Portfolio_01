using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Michsky.UI.Shift;

public class PlayerManager : MonoBehaviour
{
    private const float WeaponCooldownUnits = 10f;
    private static PlayerManager _instance;
    public static PlayerManager Instance => _instance;
    public bool IsGameOver { get; private set; }
    public static bool IsInputBlocked => Instance != null &&
                                         (Instance.IsGameOver || (Instance.PlayerStatusData != null &&
                                                                  Instance.PlayerStatusData.CurrentHP <= 0f));
    private readonly List<EventSystem> _blockedEventSystems = new List<EventSystem>();
    private readonly List<MonoBehaviour> _blockedUIInputs = new List<MonoBehaviour>();

    public PlayerStatus PlayerStatusData { get; private set; }
    private float _hitInvulnerableUntil;
    private float _dashInvulnerableUntil;
    private Weapon _modifiedWeapon;
    private float _appliedFireFlat;
    private float _appliedFireRate;
    private int _appliedMagazine;
    private int _appliedProjectileCount;
    private float _appliedAngle;
    private float _appliedRange;

    public PlayerController PlayerControllerObj;
    public PlayerAim PlayerAimObj;
    [SerializeField] private PlayerHitFlash _hitFlash;

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
        foreach (MonoBehaviour input in _blockedUIInputs)
        {
            if (input != null) input.enabled = true;
        }

        foreach (EventSystem eventSystem in _blockedEventSystems)
        {
            if (eventSystem != null) eventSystem.enabled = true;
        }

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

    public void BeginDashInvulnerability()
    {
        EnsureStatus();
        if (PlayerStatusData == null || Time.timeScale <= 0f) return;

        float duration = PlayerStatusData.DashInvulnerableDuration;
        if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration)) return;

        // 기존 무적 시간을 줄이지 않고, 대시 상태가 끝나도 지정 시간 동안 유지합니다.
        _dashInvulnerableUntil = Mathf.Max(_dashInvulnerableUntil, Time.time + duration);
    }

    public bool TryTakeDamage(float damage)
    {
        EnsureStatus();
        if (IsGameOver || PlayerStatusData == null || !PlayerStatusData.IsVulnerable ||
            PlayerStatusData.CurrentHP <= 0f || Time.timeScale <= 0f ||
            Time.time < _hitInvulnerableUntil || Time.time < _dashInvulnerableUntil || damage <= 0f ||
            float.IsNaN(damage) || float.IsInfinity(damage))
        {
            return false;
        }

        PlayerStatusData.ModifyHealth(-damage);

        if (PlayerStatusData != null && PlayerStatusData.CurrentHP <= 0f)
        {
            GameOver();
        }

        _hitInvulnerableUntil = Time.time + Mathf.Max(0f, PlayerStatusData.HitInvulnerableDuration);
        PlayHitFlash();
        return true;
    }


    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        if (PlayerStatusData != null)
        {
            PlayerStatusData.CurrentHP = 0f;
            PlayerStatusData.IsVulnerable = false;
        }

        PlayerController player = PlayerControllerObj != null
            ? PlayerControllerObj
            : FindFirstObjectByType<PlayerController>();
        if (player != null) player.Die();
    }


    private void PlayHitFlash()
    {
        if (_hitFlash == null)
        {
            PlayerController player = PlayerControllerObj != null
                ? PlayerControllerObj
                : FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _hitFlash = player.GetComponent<PlayerHitFlash>();
            }
        }

        if (_hitFlash != null)
        {
            _hitFlash.Play();
        }
    }
}