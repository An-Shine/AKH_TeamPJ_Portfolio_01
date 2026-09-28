public class PlayerStatus
{
    public float CurrentHP;
    public float MaxHP;
    public float MoveSpeed;
    public float DashDistance;
    public float DashDuration;
    public float DashInvulnerableDuration;
    public float DashCooldown;
    public bool IsVulnerable;
    public float HitInvulnerableDuration;
    public int KeyCount;
    public int UpgradeCurrency;

    public float Damage;
    public float FireInterval;
    public bool isChargeWeapon;
    public float ChargeCooldown;

    public PlayerStatus(float maxHP, float moveSpeed, float dashDistance, float dashDuration,
        float dashInvulnerableDuration, float dashCooldown, float hitInvulnerableDuration, int upgradeCurrency)
    {
    }
}