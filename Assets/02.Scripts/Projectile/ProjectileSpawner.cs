using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public Projectile Spawn(ProjectileType type, Vector2 position, Vector2 direction)
    {
        Projectile projectile = ProjectilePoolManager.Instance.Get(type);
        projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
        projectile.OnSpawn(direction, direction);

        return projectile;
    }
}