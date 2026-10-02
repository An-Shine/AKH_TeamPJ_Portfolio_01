using System.Collections.Generic;
using UnityEngine;

public sealed class BossPatternProjectile : MonoBehaviour
{
    private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(16);
    private BossMonster _owner;
    private BossAttackPattern _pattern;
    private ContactFilter2D _filter;
    private Vector2 _direction;
    private Vector2 _orbitCenter;
    private float _orbitStartAngle;
    private float _orbitStartRadius;
    private float _age;
    private bool _orbit;
    private bool _released;
    private bool _consumed;

    public static BossPatternProjectile Spawn(BossMonster owner, BossAttackPattern pattern,
        Vector3 position, Vector2 direction, bool orbit)
    {
        GameObject root = new GameObject(orbit ? "BossOrbitBullet" : "BossBarrageBullet");
        root.transform.SetParent(owner.AttackRoot, false);
        root.transform.position = position;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = pattern.ProjectileSprite != null
            ? pattern.ProjectileSprite : BossAttackIndicator.DefaultBulletSprite;
        renderer.color = pattern.ProjectileColor;
        MonsterCombat.GetEffectSorting(owner.transform, out int layer, out int sortingLayer, out int order);
        root.layer = layer;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = order;
        float spriteSize = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
        root.transform.localScale = Vector3.one * (pattern.ProjectileSize / Mathf.Max(0.001f, spriteSize));

        BossPatternProjectile projectile = root.AddComponent<BossPatternProjectile>();
        projectile._owner = owner;
        projectile._pattern = pattern;
        projectile._direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector2.down;
        projectile._orbit = orbit;
        projectile._orbitCenter = owner.transform.position;
        Vector2 offset = (Vector2)position - projectile._orbitCenter;
        projectile._orbitStartAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        projectile._orbitStartRadius = offset.magnitude;
        projectile._filter = new ContactFilter2D();
        projectile._filter.SetLayerMask(owner.Room.ProjectileHitLayers);
        projectile._filter.useTriggers = true;
        return projectile;
    }

    private void Update()
    {
        if (_consumed || Time.deltaTime <= 0f)
        {
            return;
        }

        if (_owner == null || _owner.IsDead || !_owner.gameObject.activeInHierarchy ||
            !_owner.Room.Contains(transform.position, -0.75f))
        {
            Consume();
            return;
        }

        _age += Time.deltaTime;
        float lifetime = _orbit ? Mathf.Max(_pattern.ProjectileLifetime, _pattern.OrbitDuration + 0.2f)
            : _pattern.ProjectileLifetime;
        if (_age >= lifetime)
        {
            Consume();
            return;
        }

        if (_orbit && !_released)
        {
            float orbitAge = Mathf.Min(_age, _pattern.OrbitDuration);
            float angle = (_orbitStartAngle + _pattern.OrbitAngularSpeed * orbitAge) * Mathf.Deg2Rad;
            float radius = Mathf.Lerp(_orbitStartRadius, _pattern.OrbitRadius,
                Mathf.Clamp01(orbitAge / 0.3f));
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SweepTo(_orbitCenter + radial * radius);
            if (_consumed || _age < _pattern.OrbitDuration)
            {
                return;
            }

            _released = true;
            _direction = radial;
            SweepTo((Vector2)transform.position + _direction *
                (_pattern.ProjectileSpeed * (_age - _pattern.OrbitDuration)));
            return;
        }

        SweepTo((Vector2)transform.position + _direction * (_pattern.ProjectileSpeed * Time.deltaTime));
    }

    private void SweepTo(Vector2 destination)
    {
        Vector2 origin = transform.position;
        Vector2 movement = destination - origin;
        float distance = movement.magnitude;
        Vector2 direction = distance > 0.000001f ? movement / distance : _direction;
        int count = Physics2D.CircleCast(origin, _pattern.ProjectileCollisionRadius,
            direction, _filter, _hits, distance);
        bool found = false;
        RaycastHit2D nearest = default(RaycastHit2D);
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _hits[i];
            if (!CanImpact(hit.collider) || hit.distance >= nearestDistance)
            {
                continue;
            }

            found = true;
            nearest = hit;
            nearestDistance = hit.distance;
        }

        if (!found)
        {
            transform.position = new Vector3(destination.x, destination.y, transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return;
        }

        _consumed = true;
        Transform player = MonsterCombat.ResolvePlayerCollider(nearest.collider, _owner.Player);
        if (player != null)
        {
            MonsterCombat.TryTakePlayerDamage(player, _pattern.ProjectileDamage,
                nearest.point, direction, _pattern.ProjectileColor);
        }
        else
        {
            MonsterCombatEffect.SpawnImpact(nearest.point, direction, _pattern.ProjectileColor, _owner.transform);
        }

        Consume();
    }

    private bool CanImpact(Collider2D collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy ||
            collider.GetComponentInParent<NormalMonster>() != null)
        {
            return false;
        }

        if (MonsterCombat.ResolvePlayerCollider(collider, _owner.Player) != null)
        {
            return !collider.isTrigger || !MonsterCombat.HasEnabledBodyCollider(_owner.PlayerColliders);
        }

        return !collider.isTrigger;
    }

    private void Consume()
    {
        _consumed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
