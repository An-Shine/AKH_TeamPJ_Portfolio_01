using UnityEngine;

public class SpreadFirePattern : IFirePattern
{
    public Projectile Fire(FireContext context, int damage)
    {
        var projectileCount = context.ProjectileCount;

        if (projectileCount <= 1)
        {
            ProjectileSpawner.Instance.Spawn(context.Type, context.Position, context.Direction, context.Range, damage);
            return null;
        }

        var intervalOfAngle = context.SpreadAngle / (projectileCount - 1);

        var startAngle = -1 * context.SpreadAngle * 0.5f; //half of spread angle

        for (var i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + intervalOfAngle * i;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector2 direction = rotation * context.Direction;

            ProjectileSpawner.Instance.Spawn(context.Type, context.Position, direction, context.Range, damage);
        }

        return null;
    }
}