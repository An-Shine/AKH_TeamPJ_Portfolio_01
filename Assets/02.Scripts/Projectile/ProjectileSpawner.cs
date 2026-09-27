using System;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    private static ProjectileSpawner _instance;
    public static ProjectileSpawner Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }

        _instance = this;
    }

    public Projectile Spawn(ProjectileType type, Vector2 position, Vector2 direction, float range)
    {
        Projectile projectile = ProjectilePoolManager.Instance.Get(type);
        projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
        projectile.OnSpawn(direction, direction, range);
        transform.right = direction;

        return projectile;
    }
}