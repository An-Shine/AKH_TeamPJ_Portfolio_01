using UnityEngine;

public class FireContext
{
    public readonly ProjectileType Type;
    public readonly Vector2 Position;
    public readonly Vector2 Direction;

    public readonly int ProjectileCount;
    public readonly float SpreadAngle;

    public FireContext(ProjectileType type, Vector2 position, Vector2 direction,
        int projectileCount, float spreadAngle)
    {
        Type = type;
        Position = position;
        Direction = direction;
        ProjectileCount = projectileCount;
        SpreadAngle = spreadAngle;
    }
}