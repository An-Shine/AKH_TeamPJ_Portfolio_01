using System;
using UnityEngine;

[Serializable]
public class PlayerStatus
{
    private readonly float _baseMaxHP;
    private readonly float _baseMoveSpeed;
    private readonly float _baseShootingMoveSpeed;
    public float CurrentHP;
    public float MaxHP;
    public float MoveSpeed;
    public float ShootingMoveSpeed;
    public float DashDistance;
    public float DashDuration;
    public float DashInvulnerableDuration;
    public float DashCooldown;
    public bool IsVulnerable;
    public float HitInvulnerableDuration;
    public int UpgradeCurrency;

    public float Damage;
    public float FireInterval;
    public bool isChargeWeapon;
    public float ChargeCooldown;

    public PlayerStatus(float maxHP, float moveSpeed, float dashDistance, float dashDuration,
        float dashInvulnerableDuration, float dashCooldown, float hitInvulnerableDuration, int upgradeCurrency,
        float shootingMoveSpeed)
    {
        _baseMaxHP = maxHP;
        _baseMoveSpeed = moveSpeed;
        _baseShootingMoveSpeed = shootingMoveSpeed;
        MaxHP = maxHP;
        CurrentHP = maxHP;
        MoveSpeed = moveSpeed;
        ShootingMoveSpeed = shootingMoveSpeed;
        DashDistance = dashDistance;
        DashDuration = dashDuration;
        DashInvulnerableDuration = dashInvulnerableDuration;
        DashCooldown = dashCooldown;
        HitInvulnerableDuration = hitInvulnerableDuration;
        UpgradeCurrency = upgradeCurrency;
        IsVulnerable = true;
    }

    public void ApplyItemStats(ItemPlayerStat stat)
    {
        MaxHP = Mathf.Max(1f, _baseMaxHP + (stat != null ? stat.MaxHealthModifier : 0));
        float moveSpeedMultiplier = 1f + (stat != null ? stat.MoveSpeedMultiplier : 0f);
        MoveSpeed = Mathf.Max(0f, _baseMoveSpeed * moveSpeedMultiplier);
        ShootingMoveSpeed = Mathf.Max(0f, _baseShootingMoveSpeed * moveSpeedMultiplier);
        CurrentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);
    }

    public void ModifyHealth(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
    }
}
