using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolManager : MonoBehaviour
{
    private static ProjectilePoolManager _instance;
    public static ProjectilePoolManager Instance => _instance;

    [SerializeField] private List<ProjectileEntry> _entries;
    private Dictionary<ProjectileType, ObjectPool<Projectile>> _projectilePools;

    [SerializeField] private Transform _projectileContainer;

    private void Awake()
    {
        if (_instance != null) Destroy(gameObject);

        _instance = this;
    }

    private void Start()
    {
        _projectilePools = new Dictionary<ProjectileType, ObjectPool<Projectile>>();
        foreach (var projectileEntry in _entries)
        {
            _projectilePools[projectileEntry.Type] =
                new ObjectPool<Projectile>(
                    () =>
                    {
                        var projectile = Instantiate(projectileEntry.ProjectilePrefab, _projectileContainer);

                        var damage = DataManager.Instance.ProjectileDatas[projectileEntry.Type].Damage;
                        var speedScalar = DataManager.Instance.ProjectileDatas[projectileEntry.Type].SpeedScalar;
                        var durability = DataManager.Instance.ProjectileDatas[projectileEntry.Type].Durability;
                        projectile.Initialize(projectileEntry.Type, damage, speedScalar, durability);

                        return projectile;
                    },
                    ActivateProjectile,
                    DisableProjectile,
                    DestroyProjectile,
                    false,
                    projectileEntry.InitialAmount,
                    projectileEntry.MaxAmount);
        }
    }

    public Projectile Get(ProjectileType type)
    {
        return _projectilePools[type].Get();
    }

    public void Release(Projectile projectile, ProjectileType type)
    {
        _projectilePools[type].Release(projectile);
    }

    private void ActivateProjectile(Projectile projectile)
    {
        projectile.gameObject.SetActive(true);
    }

    private void DisableProjectile(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }

    private void DestroyProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        Destroy(projectile.gameObject);
    }
}