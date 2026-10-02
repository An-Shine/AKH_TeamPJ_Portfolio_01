using UnityEngine;

public enum BossAreaHazardKind
{
    Falling,
    Bomb,
    Shockwave
}

public sealed class BossAreaHazard : MonoBehaviour
{
    private BossMonster _owner;
    private BossAttackPattern _pattern;
    private BossAttackIndicator _indicator;
    private SpriteRenderer _visual;
    private BossAreaHazardKind _kind;
    private Vector2 _center;
    private float _age;
    private float _lastWaveRadius;
    private bool _impacted;
    private bool _waveContactConsumed;

    public Vector2 Center => _center;
    public float Radius => _kind == BossAreaHazardKind.Shockwave
        ? _pattern.ShockwaveMaximumRadius : _pattern.AreaRadius;

    public static BossAreaHazard Spawn(BossMonster owner, BossAttackPattern pattern,
        Vector2 center, BossAreaHazardKind kind)
    {
        GameObject root = new GameObject("Boss" + kind + "Hazard");
        root.transform.SetParent(owner.AttackRoot, false);
        root.transform.position = new Vector3(center.x, center.y, owner.transform.position.z);
        BossAreaHazard hazard = root.AddComponent<BossAreaHazard>();
        hazard._owner = owner;
        hazard._pattern = pattern;
        hazard._center = center;
        hazard._kind = kind;
        hazard._lastWaveRadius = pattern.ShockwaveInitialRadius;
        float radius = kind == BossAreaHazardKind.Shockwave
            ? pattern.ShockwaveInitialRadius : pattern.AreaRadius;
        hazard._indicator = BossAttackIndicator.Spawn(root.transform.position, radius,
            pattern.WarningColor, owner.transform, false, root.transform);

        if (kind != BossAreaHazardKind.Shockwave)
        {
            GameObject visual = new GameObject("ProjectileVisual");
            visual.transform.SetParent(root.transform, false);
            hazard._visual = visual.AddComponent<SpriteRenderer>();
            hazard._visual.sprite = pattern.ProjectileSprite != null
                ? pattern.ProjectileSprite : BossAttackIndicator.DefaultBulletSprite;
            hazard._visual.color = pattern.ProjectileColor;
            MonsterCombat.GetEffectSorting(owner.transform, out int layer, out int sortingLayer, out int order);
            visual.layer = layer;
            hazard._visual.sortingLayerID = sortingLayer;
            hazard._visual.sortingOrder = order;
            float spriteSize = Mathf.Max(hazard._visual.sprite.bounds.size.x, hazard._visual.sprite.bounds.size.y);
            visual.transform.localScale = Vector3.one *
                (pattern.ProjectileSize / Mathf.Max(0.001f, spriteSize));
            hazard._visual.enabled = false;
        }

        return hazard;
    }

    private void Update()
    {
        if (_owner == null || _owner.IsDead || !_owner.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        _age += Time.deltaTime;
        if (_kind == BossAreaHazardKind.Shockwave)
        {
            UpdateShockwave();
            return;
        }

        float delay = _kind == BossAreaHazardKind.Falling ? _pattern.FallDuration : _pattern.BombFuse;
        float impactTime = _pattern.WarningTime + delay;
        if (_age >= _pattern.WarningTime && !_impacted)
        {
            _visual.enabled = true;
            float progress = Mathf.Clamp01((_age - _pattern.WarningTime) / delay);
            if (_kind == BossAreaHazardKind.Falling)
            {
                _visual.transform.localPosition = Vector3.up * (_pattern.FallHeight * (1f - progress * progress));
            }
            else
            {
                _visual.color = Color.Lerp(_pattern.ProjectileColor, Color.white,
                    Mathf.PingPong(progress * 8f, 1f));
            }
        }

        if (!_impacted && _age >= impactTime)
        {
            _impacted = true;
            _visual.enabled = false;
            _indicator.Flash();
            _owner.HitArea(_center, _pattern.AreaRadius, _pattern.AreaDamage, _pattern.WarningColor);
            MonsterCombatEffect.SpawnImpact(transform.position, Vector2.up,
                _pattern.ProjectileColor, _owner.transform);
        }

        if (_age >= impactTime + 0.3f)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateShockwave()
    {
        if (_age < _pattern.WarningTime)
        {
            return;
        }

        float progress = Mathf.Clamp01((_age - _pattern.WarningTime) / _pattern.ShockwaveExpandTime);
        float radius = Mathf.Lerp(_pattern.ShockwaveInitialRadius, _pattern.ShockwaveMaximumRadius, progress);
        if (_indicator != null)
        {
            _indicator.SetRadius(radius);
        }

        if (!_impacted)
        {
            TryWaveHit(_lastWaveRadius, radius);
            _lastWaveRadius = radius;
            if (progress >= 1f)
            {
                _impacted = true;
                _indicator.Flash();
            }
        }

        if (_age >= _pattern.WarningTime + _pattern.ShockwaveExpandTime + 0.3f)
        {
            Destroy(gameObject);
        }
    }

    private void TryWaveHit(float previousRadius, float radius)
    {
        if (_waveContactConsumed || _owner.Player == null || !_owner.Player.gameObject.activeInHierarchy)
        {
            return;
        }

        float nearest = Mathf.Sqrt(MonsterCombat.GetPlayerDistanceSquared(
            _owner.Player, _owner.PlayerColliders, _center, out Vector2 closestPoint));
        float farthest = nearest;
        foreach (Collider2D collider in _owner.PlayerColliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger ||
                !collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            float x = Mathf.Max(Mathf.Abs(bounds.min.x - _center.x), Mathf.Abs(bounds.max.x - _center.x));
            float y = Mathf.Max(Mathf.Abs(bounds.min.y - _center.y), Mathf.Abs(bounds.max.y - _center.y));
            farthest = Mathf.Max(farthest, Mathf.Sqrt(x * x + y * y));
        }

        float halfWidth = _pattern.ShockwaveThickness * 0.5f;
        if (nearest > radius + halfWidth || farthest < previousRadius - halfWidth ||
            !MonsterCombat.HasClearAttackPath(_center, closestPoint, _owner.transform, _owner.Player))
        {
            return;
        }

        _waveContactConsumed = true;
        Vector2 direction = closestPoint - _center;
        MonsterCombat.TryTakePlayerDamage(_owner.Player, _pattern.AreaDamage, closestPoint,
            direction.normalized, _pattern.WarningColor);
    }
}
