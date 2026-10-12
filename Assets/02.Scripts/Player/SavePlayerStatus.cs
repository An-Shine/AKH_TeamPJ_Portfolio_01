public class SavePlayerStatus
{
    public float MaxHP;
    public float MoveSpeed;
    public float DashDistance;
    public float DashDuration;
    public float DashInvulnerableDuration;
    public float DashCooldown;
    public float HitInvulnerableDuration;
    public float ShootingMoveSpeed;
    public int Upgrade1Level; // todo: 업그레이드1, 업그레이드2 말고 업그레이드 종류별로 네이밍하기
    public int Upgrade2Level;
    public int UpgradeCost;

    public SavePlayerStatus(float maxHP, float moveSpeed, float dashDistance, float dashDuration,
        float dashInvulnerableDuration, float dashCooldown, float hitInvulnerableDuration,
        int upgrade1Level, int upgrade2Level, int upgradeCost, float shootingMoveSpeed)
    {
        MaxHP = maxHP;
        MoveSpeed = moveSpeed;
        ShootingMoveSpeed = shootingMoveSpeed;
        DashDistance = dashDistance;
        DashDuration = dashDuration;
        DashInvulnerableDuration = dashInvulnerableDuration;
        DashCooldown = dashCooldown;
        HitInvulnerableDuration = hitInvulnerableDuration;
        Upgrade1Level = upgrade1Level;
        Upgrade2Level = upgrade2Level;
        UpgradeCost = upgradeCost;
    }
}
