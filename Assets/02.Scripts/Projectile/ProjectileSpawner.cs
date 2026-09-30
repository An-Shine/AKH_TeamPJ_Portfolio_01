using System;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    private static ProjectileSpawner _instance;
    public static ProjectileSpawner Instance => _instance;

    private void Awake()
    {
        if (_instance != null) Destroy(gameObject);

        _instance = this;
    }

    public Projectile Spawn(ProjectileType type, Vector2 position, Vector2 direction, float range, int damage)
    {
        var projectile = ProjectilePoolManager.Instance.Get(type);
        projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
        projectile.OnSpawn(position, direction, range, damage);

        return projectile;
    }
}