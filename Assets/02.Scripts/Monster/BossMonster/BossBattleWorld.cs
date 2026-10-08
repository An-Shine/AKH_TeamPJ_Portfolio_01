using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _02.Scripts.Monster.BossMonster
{
    internal sealed class BossBattleWorld : IDisposable
    {
        private sealed class Bolt
        {
            public Vector2 Position;
            public Vector2 Direction;
            public float Age;
            public float NextTrail;
            public float Lifetime;
            public float Speed;
            public float Radius;
            public float Damage;
            public float HomingTurn;
            public float HomingUntil;
            public float OrbitTime;
            public float OrbitRadius;
            public float OrbitAngle;
            public float OrbitTurn;
            public bool Released;
            public BossEncounterMove Move;
            public BossShapeVisual Glow;
            public BossShapeVisual Core;
        }

        private readonly global::BossMonster _owner;
        private readonly BossEncounterProfile _profile;
        private readonly BossBattleEffects _effects;
        private readonly BossPatternAudio _audio;
        private readonly List<BossDamageZone> _zones = new List<BossDamageZone>(64);
        private readonly List<Bolt> _bolts = new List<Bolt>(240);
        private readonly List<RaycastHit2D> _castHits = new List<RaycastHit2D>(32);
        private readonly List<Collider2D> _overlaps = new List<Collider2D>(16);
        private readonly List<GameObject> _customEffects = new List<GameObject>();
        private ContactFilter2D _filter;
        private float _nextShake;

        public float RemainingDuration
        {
            get
            {
                float remaining = 0f;
                foreach (BossDamageZone zone in _zones)
                {
                    float end = zone.WarningOnly ? zone.Warning : zone.Warning + zone.Duration + 0.16f;
                    remaining = Mathf.Max(remaining, end - zone.Age);
                }
                foreach (Bolt bolt in _bolts)
                {
                    remaining = Mathf.Max(remaining, bolt.Lifetime - bolt.Age);
                }
                return remaining;
            }
        }

        public BossBattleWorld(global::BossMonster owner, BossEncounterProfile profile,
            BossBattleEffects effects, BossPatternAudio audio)
        {
            _owner = owner;
            _profile = profile;
            _effects = effects;
            _audio = audio;
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(owner.Room != null ? owner.Room.ProjectileHitLayers : (LayerMask)~0);
            _filter.useTriggers = true;
        }

        public void AddZone(BossDamageZone zone)
        {
            if (_zones.Count >= _profile.MaximumZones)
            {
                return;
            }
            zone.Fill = _effects.CreateShape("BossDangerArea", false);
            zone.Outline = _effects.CreateShape("BossDangerOutline", true, 1);
            zone.Core = _effects.CreateShape("BossAttackCore", true, 2);
            if (zone.Shape == BossZoneShape.Beam && Mathf.Abs(zone.SweepAngle) > 0.01f)
            {
                zone.SweepHint = _effects.CreateShape("BeamSweepWarning", false);
                zone.OppositeHint = _effects.CreateShape("BeamOppositeWarning", false);
            }
            _zones.Add(zone);
            DrawZone(zone);
        }

        public void Shoot(Vector2 origin, float angle, BossEncounterMove move, float damage, float speed,
            float homingTurn = 0f, float homingUntil = 0f)
        {
            Bolt bolt = CreateBolt(origin, angle, move, damage, speed);
            if (bolt == null)
            {
                return;
            }
            bolt.HomingTurn = homingTurn;
            bolt.HomingUntil = homingUntil;
        }

        public void ShootOrbit(Vector2 center, float angle, BossEncounterMove move, float damage,
            float speed, float radius, float orbitTime, float orbitTurn)
        {
            Bolt bolt = CreateBolt(center + BossBattleGeometry.Direction(angle) * radius,
                angle + 90f, move, damage, speed);
            if (bolt == null)
            {
                return;
            }
            bolt.OrbitRadius = radius;
            bolt.OrbitAngle = angle;
            bolt.OrbitTime = orbitTime;
            bolt.OrbitTurn = orbitTurn;
            bolt.Lifetime += orbitTime;
            bolt.Released = false;
        }

        private Bolt CreateBolt(Vector2 origin, float angle, BossEncounterMove move, float damage, float speed)
        {
            if (_bolts.Count >= _profile.MaximumProjectiles)
            {
                return null;
            }
            Bolt bolt = new Bolt
            {
                Position = origin,
                Direction = BossBattleGeometry.Direction(angle),
                Lifetime = move.Duration,
                Speed = speed,
                Radius = move.Radius,
                Damage = damage,
                Released = true,
                Move = move,
                Glow = _effects.CreateShape("BossProjectileGlow", true, 2),
                Core = _effects.CreateShape("BossProjectileCore", true, 3)
            };
            _bolts.Add(bolt);
            DrawBolt(bolt);
            return bolt;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || !_owner.CanFight)
            {
                return;
            }
            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                BossDamageZone zone = _zones[i];
                float previousRadius = zone.CurrentRadius;
                float previousAngle = zone.CurrentAngle;
                float previousAge = zone.Age;
                zone.Age += deltaTime;
                if (zone.WarningOnly && zone.Age >= zone.Warning)
                {
                    zone.Dispose();
                    _zones.RemoveAt(i);
                    continue;
                }
                if (zone.Age >= zone.Warning && previousAge <= zone.Warning + zone.Duration)
                {
                    if (!zone.Impacted)
                    {
                        zone.Impacted = true;
                        Impact(zone);
                    }
                    TestDamage(zone, previousRadius, previousAngle);
                    EmitZoneParticles(zone);
                }
                if (zone.Age >= zone.Warning + zone.Duration + 0.16f)
                {
                    zone.Dispose();
                    _zones.RemoveAt(i);
                    continue;
                }
                DrawZone(zone);
            }
            for (int i = _bolts.Count - 1; i >= 0; i--)
            {
                Bolt bolt = _bolts[i];
                bolt.Age += deltaTime;
                bool remove = MoveBolt(bolt, deltaTime) || bolt.Age >= bolt.Lifetime;
                if (remove)
                {
                    bolt.Glow.Dispose();
                    bolt.Core.Dispose();
                    _bolts.RemoveAt(i);
                    continue;
                }
                DrawBolt(bolt);
                if (bolt.Age >= bolt.NextTrail)
                {
                    bolt.NextTrail = bolt.Age + 0.045f;
                    _effects.Trail(bolt.Position, bolt.Direction, bolt.Move.Width, bolt.Move.Color);
                }
            }
            for (int i = _customEffects.Count - 1; i >= 0; i--)
            {
                if (_customEffects[i] == null)
                {
                    _customEffects.RemoveAt(i);
                }
            }
        }

        private bool MoveBolt(Bolt bolt, float deltaTime)
        {
            if (!bolt.Released)
            {
                float orbitDelta = Mathf.Max(0f, Mathf.Min(bolt.Age, bolt.OrbitTime) - Mathf.Max(0f, bolt.Age - deltaTime));
                bolt.OrbitAngle += bolt.OrbitTurn * orbitDelta;
                float radius = bolt.OrbitRadius * Mathf.Lerp(0.85f, 1.15f, Mathf.Clamp01(bolt.Age / bolt.OrbitTime));
                bolt.Position = (Vector2)_owner.transform.position + BossBattleGeometry.Direction(bolt.OrbitAngle) * radius;
                if (bolt.Age < bolt.OrbitTime)
                {
                    return false;
                }
                bolt.Released = true;
                bolt.Direction = BossBattleGeometry.Direction(bolt.OrbitAngle + (bolt.OrbitTurn >= 0f ? 90f : -90f));
                deltaTime = Mathf.Min(deltaTime, Mathf.Max(0f, bolt.Age - bolt.OrbitTime));
                _effects.Burst(bolt.Position, bolt.Move.Width * 1.7f, bolt.Move.Color, 5);
                _audio.Impact(0.6f);
            }
            if (bolt.HomingTurn > 0f && bolt.Age < bolt.HomingUntil && _owner.Player != null)
            {
                float current = BossBattleGeometry.Angle(bolt.Direction);
                float desired = BossBattleGeometry.Angle((Vector2)_owner.Player.position - bolt.Position);
                float turn = Mathf.Clamp(Mathf.DeltaAngle(current, desired),
                    -bolt.HomingTurn * deltaTime, bolt.HomingTurn * deltaTime);
                bolt.Direction = BossBattleGeometry.Direction(current + turn);
            }
            float distance = bolt.Speed * deltaTime;
            _castHits.Clear();
            int count = Physics2D.CircleCast(bolt.Position, bolt.Radius, bolt.Direction, _filter, _castHits, distance);
            float nearest = float.PositiveInfinity;
            RaycastHit2D selected = default(RaycastHit2D);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _castHits[i];
                if (!CanHitCollider(hit.collider) || hit.distance >= nearest)
                {
                    continue;
                }
                found = true;
                nearest = hit.distance;
                selected = hit;
            }
            if (found)
            {
                if (MonsterCombat.ResolvePlayerCollider(selected.collider, _owner.Player) != null)
                {
                    _owner.TryDealPlayerDamage(bolt.Damage, selected.point, bolt.Direction, bolt.Move.Color);
                }
                _effects.Burst(selected.point, bolt.Move.Width, bolt.Move.Color, 8);
                _audio.Impact(0.5f);
                return true;
            }
            bolt.Position += bolt.Direction * distance;
            return !_owner.Arena.Contains(bolt.Position);
        }

        private bool CanHitCollider(Collider2D collider)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy ||
                collider.transform == _owner.transform || collider.transform.IsChildOf(_owner.transform) ||
                collider.GetComponentInParent<NormalMonster>() != null ||
                (collider.attachedRigidbody != null && !collider.attachedRigidbody.simulated))
            {
                return false;
            }
            if (MonsterCombat.ResolvePlayerCollider(collider, _owner.Player) != null)
            {
                return !collider.isTrigger || !MonsterCombat.HasEnabledBodyCollider(_owner.PlayerColliders);
            }
            return !collider.isTrigger;
        }

        private void TestDamage(BossDamageZone zone, float previousRadius, float previousAngle)
        {
            if ((zone.Once && zone.ContactConsumed) || zone.Age < zone.NextDamage)
            {
                return;
            }
            Vector2 point = zone.Center;
            bool touches = false;
            if (zone.Shape == BossZoneShape.Beam)
            {
                int samples = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(zone.CurrentAngle - previousAngle) / 4f), 1, 20);
                for (int sample = 0; sample <= samples && !touches; sample++)
                {
                    float angle = Mathf.Lerp(previousAngle, zone.CurrentAngle, (float)sample / samples);
                    touches = BeamTouches(zone, angle, out point);
                }
            }
            else
            {
                bool hasBody = false;
                foreach (Collider2D body in _owner.PlayerColliders)
                {
                    if (!BossBattleGeometry.IsBodyCollider(body))
                    {
                        continue;
                    }
                    hasBody = true;
                    if (zone.Shape == BossZoneShape.Circle)
                    {
                        point = body.ClosestPoint(zone.Center);
                        touches = (point - zone.Center).sqrMagnitude <= zone.Radius * zone.Radius;
                    }
                    else if (zone.Shape == BossZoneShape.Sector)
                    {
                        touches = BossBattleGeometry.SectorTouches(body, zone.Center, zone.Radius,
                            zone.Angle, zone.Span, out point);
                    }
                    else
                    {
                        touches = BossBattleGeometry.RingTouches(body, zone.Center, previousRadius,
                            zone.CurrentRadius, zone.Width, zone.CurrentAngle, zone.GapWidth, out point);
                    }
                    if (touches)
                    {
                        break;
                    }
                }
                if (!hasBody && _owner.Player != null)
                {
                    point = _owner.Player.position;
                    touches = PointTouches(zone, point, previousRadius);
                }
            }
            if (!touches || !MonsterCombat.HasClearAttackPath(zone.Center, point, _owner.transform, _owner.Player))
            {
                return;
            }
            zone.ContactConsumed = true;
            zone.NextDamage = zone.Age + zone.Move.DamageInterval;
            _owner.TryDealPlayerDamage(zone.Damage, point, (point - zone.Center).normalized, zone.Move.Color);
        }

        private bool BeamTouches(BossDamageZone zone, float angle, out Vector2 point)
        {
            point = zone.Center;
            _overlaps.Clear();
            int count = Physics2D.OverlapCapsule(zone.Center, new Vector2(zone.Length, zone.Width),
                CapsuleDirection2D.Horizontal, angle, _filter, _overlaps);
            for (int i = 0; i < count; i++)
            {
                Collider2D body = _overlaps[i];
                if (MonsterCombat.ResolvePlayerCollider(body, _owner.Player) == null ||
                    (body.isTrigger && MonsterCombat.HasEnabledBodyCollider(_owner.PlayerColliders)) ||
                    (body.attachedRigidbody != null && !body.attachedRigidbody.simulated))
                {
                    continue;
                }
                point = body.ClosestPoint(zone.Center);
                return true;
            }
            return false;
        }

        private static bool PointTouches(BossDamageZone zone, Vector2 point, float previousRadius)
        {
            if (zone.Shape == BossZoneShape.Circle)
            {
                return (point - zone.Center).sqrMagnitude <= zone.Radius * zone.Radius;
            }
            if (zone.Shape == BossZoneShape.Sector)
            {
                return BossBattleGeometry.InSector(point, zone.Center, zone.Radius, zone.Angle, zone.Span);
            }
            float radius = (point - zone.Center).magnitude;
            return radius >= Mathf.Min(previousRadius, zone.CurrentRadius) - zone.Width * 0.5f &&
                radius <= Mathf.Max(previousRadius, zone.CurrentRadius) + zone.Width * 0.5f &&
                Mathf.Abs(Mathf.DeltaAngle(zone.CurrentAngle, BossBattleGeometry.Angle(point - zone.Center))) >= zone.GapWidth * 0.5f;
        }

        private void Impact(BossDamageZone zone)
        {
            Color color = zone.Move.Color;
            float size = zone.Move.EffectScale;
            if (zone.Spikes && zone.Shape == BossZoneShape.Circle)
            {
                _effects.Spikes(zone.Center, zone.Radius * size, color);
            }
            else if (zone.Pillar)
            {
                _effects.Pillar(zone.Center, zone.Radius * size, color);
            }
            else if (zone.Shape == BossZoneShape.Beam)
            {
                Vector2 direction = BossBattleGeometry.Direction(zone.Angle);
                int count = Mathf.Clamp(Mathf.CeilToInt(zone.Length / 1.4f), 2, 18);
                for (int i = 0; i < count; i++)
                {
                    Vector2 point = zone.Center + direction * ((float)i / (count - 1) - 0.5f) * zone.Length;
                    if (_owner.Arena.Contains(point))
                    {
                        if (zone.Spikes)
                        {
                            _effects.Spikes(point, zone.Width * 0.8f * size, color);
                        }
                        else
                        {
                            _effects.Pillar(point, zone.Width * 0.6f * size, color);
                        }
                    }
                }
            }
            else
            {
                _effects.Burst(zone.Center, Mathf.Min(1.4f, zone.Radius) * size, color);
            }
            if (zone.Move.ImpactPrefab != null)
            {
                GameObject custom = UnityEngine.Object.Instantiate(zone.Move.ImpactPrefab,
                    new Vector3(zone.Center.x, zone.Center.y, _owner.transform.position.z),
                    Quaternion.Euler(0f, 0f, zone.Angle), _owner.AttackRoot);
                _customEffects.Add(custom);
                UnityEngine.Object.Destroy(custom, zone.Move.ImpactPrefabLifetime);
            }
            _audio.Impact();
            _owner.PulsePatternAnimation(zone.Move);
            if (Time.time >= _nextShake)
            {
                _nextShake = Time.time + 0.08f;
                _owner.ShakeCamera(zone.Move.ImpactShake, 0.3f);
            }
        }

        private void EmitZoneParticles(BossDamageZone zone)
        {
            if (zone.Age < zone.NextParticle)
            {
                return;
            }
            zone.NextParticle = zone.Age + 0.065f;
            if (zone.Shape == BossZoneShape.Circle)
            {
                _effects.Trail(zone.Center + Random.insideUnitCircle * zone.Radius,
                    Vector2.down, zone.Radius * 0.35f, zone.Move.Color);
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 point;
                if (zone.Shape == BossZoneShape.Beam)
                {
                    point = zone.Center + BossBattleGeometry.Direction(zone.CurrentAngle) * Random.Range(-0.5f, 0.5f) * zone.Length;
                }
                else
                {
                    float angle = zone.Shape == BossZoneShape.Sector
                        ? zone.Angle + Random.Range(-zone.Span * 0.5f, zone.Span * 0.5f)
                        : zone.CurrentAngle + zone.GapWidth * 0.5f + Random.value * (360f - zone.GapWidth);
                    point = zone.Center + BossBattleGeometry.Direction(angle) * zone.CurrentRadius;
                }
                if (_owner.Arena.Contains(point))
                {
                    _effects.Trail(point, Vector2.down, 0.35f, zone.Move.Color);
                }
            }
        }

        private static void DrawZone(BossDamageZone zone)
        {
            bool warning = zone.Age < zone.Warning;
            float progress = Mathf.Clamp01(zone.Age / Mathf.Max(0.01f, zone.Warning));
            float fade = 1f - Mathf.Clamp01((zone.Age - zone.Warning - zone.Duration) / 0.16f);
            Color fill = warning ? new Color(1f, 0.04f, 0.09f, 0.07f + progress * 0.15f) : zone.Move.Color;
            Color edge = warning ? new Color(1f, 0.13f, 0.16f, 0.6f + progress * 0.35f) : zone.Move.Color;
            fill.a = (warning ? fill.a : 0.25f) * fade;
            edge.a *= fade;
            Color core = Color.Lerp(zone.Move.Color, Color.white, 0.8f);
            core.a = warning ? 0f : 0.95f * fade;
            zone.Core.SetVisible(!warning);
            if (zone.Shape == BossZoneShape.Circle)
            {
                zone.Fill.SetArc(zone.Center, 0f, zone.Radius * (warning ? progress : 1f), 0f, 360f, fill);
                zone.Outline.SetArc(zone.Center, zone.Radius - 0.045f, zone.Radius + 0.045f, 0f, 360f, edge);
                zone.Core.SetArc(zone.Center, zone.Radius - 0.08f, zone.Radius, 0f, 360f, core);
            }
            else if (zone.Shape == BossZoneShape.Sector)
            {
                float start = zone.Angle - zone.Span * 0.5f;
                zone.Fill.SetArc(zone.Center, 0f, zone.Radius, start, zone.Span, fill);
                zone.Outline.SetArc(zone.Center, zone.Radius - 0.07f, zone.Radius + 0.04f, start, zone.Span, edge);
                zone.Core.SetArc(zone.Center, zone.Radius - 0.2f, zone.Radius, start, zone.Span, core);
            }
            else if (zone.Shape == BossZoneShape.Beam)
            {
                zone.Fill.SetBeam(zone.Center, zone.CurrentAngle, zone.Length, zone.Width, fill);
                zone.Outline.SetBeam(zone.Center, zone.CurrentAngle, zone.Length, zone.Width + 0.045f, edge * 0.45f);
                zone.Core.SetBeam(zone.Center, zone.CurrentAngle, zone.Length, zone.Width * 0.25f, core);
                if (zone.SweepHint != null)
                {
                    Color hint = new Color(1f, 0.04f, 0.09f, warning ? 0.045f : 0f);
                    float start = zone.Angle + Mathf.Min(0f, zone.SweepAngle) - 3f;
                    float span = Mathf.Abs(zone.SweepAngle) + 6f;
                    zone.SweepHint.SetArc(zone.Center, 0f, zone.Length * 0.5f, start, span, hint);
                    zone.OppositeHint.SetArc(zone.Center, 0f, zone.Length * 0.5f, start + 180f, span, hint);
                }
            }
            else
            {
                float radius = zone.CurrentRadius;
                float start = zone.CurrentAngle + zone.GapWidth * 0.5f;
                float span = 360f - zone.GapWidth;
                zone.Fill.SetArc(zone.Center, warning ? Mathf.Min(zone.Radius, zone.EndRadius) : Mathf.Max(0f, radius - zone.Width * 0.5f),
                    warning ? Mathf.Max(zone.Radius, zone.EndRadius) : radius + zone.Width * 0.5f, start, span, fill);
                zone.Outline.SetArc(zone.Center, radius - zone.Width * 0.5f, radius + zone.Width * 0.5f, start, span, edge);
                zone.Core.SetArc(zone.Center, radius - zone.Width * 0.15f, radius + zone.Width * 0.15f, start, span, core);
            }
        }

        private static void DrawBolt(Bolt bolt)
        {
            float size = bolt.Move.Width * (bolt.Released ? 0.8f : 1.2f) * bolt.Move.EffectScale;
            bolt.Glow.SetArc(bolt.Position, 0f, size, 0f, 360f, bolt.Move.Color);
            bolt.Core.SetArc(bolt.Position, 0f, bolt.Radius, 0f, 360f, Color.white);
        }

        public void Clear()
        {
            foreach (BossDamageZone zone in _zones)
            {
                zone.Dispose();
            }
            _zones.Clear();
            foreach (Bolt bolt in _bolts)
            {
                bolt.Glow.Dispose();
                bolt.Core.Dispose();
            }
            _bolts.Clear();
            foreach (GameObject effect in _customEffects)
            {
                if (effect != null)
                {
                    effect.SetActive(false);
                    UnityEngine.Object.Destroy(effect);
                }
            }
            _customEffects.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
