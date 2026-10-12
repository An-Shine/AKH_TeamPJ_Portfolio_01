using System;
using System.Collections.Generic;
using _02.Scripts.Monster.Presentation;
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
            public BossShapeVisual Outline;
            public SpriteRenderer Body;
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

        public BossBattleWorld(global::BossMonster owner, BossEncounterProfile profile, BossBattleEffects effects, BossPatternAudio audio)
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

            zone.Fill = _effects.CreateShape("BossDangerArea", false, MonsterVfxReadability.GroundOrder);
            zone.Boundary = _effects.CreateShape("BossDangerContrast", false, MonsterVfxReadability.WarningContrastOrder);
            zone.Outline = _effects.CreateShape("BossDangerOutline", false, MonsterVfxReadability.WarningEdgeOrder);
            zone.Detail = _effects.CreateShape("BossDangerTicks", false, MonsterVfxReadability.WarningDetailOrder);
            zone.Core = _effects.CreateShape("BossAttackCore", true, 4);
            if (zone.Shape == BossZoneShape.Beam && Mathf.Abs(zone.SweepAngle) > 0.01f)
            {
                zone.SweepHint = _effects.CreateShape("BeamSweepWarning", false, MonsterVfxReadability.GroundOrder);
                zone.OppositeHint = _effects.CreateShape("BeamOppositeWarning", false, MonsterVfxReadability.GroundOrder);
            }

            _zones.Add(zone);
            DrawZone(zone);
        }

        public void Shoot(Vector2 origin, float angle, BossEncounterMove move, float damage, float speed, float homingTurn = 0f, float homingUntil = 0f)
        {
            Bolt bolt = CreateBolt(origin, angle, move, damage, speed);
            if (bolt == null)
            {
                return;
            }

            bolt.HomingTurn = homingTurn;
            bolt.HomingUntil = homingUntil;
        }

        public void ShootOrbit(Vector2 center, float angle, BossEncounterMove move, float damage, float speed, float radius, float orbitTime, float orbitTurn)
        {
            Bolt bolt = CreateBolt(center + BossBattleGeometry.Direction(angle) * radius, angle + 90f, move, damage, speed);
            if (bolt == null)
            {
                return;
            }

            bolt.OrbitRadius = radius;
            bolt.OrbitAngle = angle;
            bolt.OrbitTime = orbitTime;
            bolt.OrbitTurn = orbitTurn;
            bolt.Lifetime += orbitTime;
            bolt.HomingTurn = _profile.ProjectileTurnRate * 0.65f;
            bolt.HomingUntil = orbitTime + Mathf.Min(_profile.ProjectileHomingDuration, Mathf.Max(0f, move.Duration - 0.35f));
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
                Speed = speed * _profile.ProjectileSpeedMultiplier,
                Radius = move.Radius * _profile.AttackRangeMultiplier,
                Damage = damage,
                Released = true,
                Move = move,
                Glow = _effects.CreateShape("BossProjectileGlow", true, 2),
                Core = _effects.CreateShape("BossProjectileCore", false, MonsterVfxReadability.ProjectileCoreOrder),
                Outline = _effects.CreateShape("BossProjectileContrast", false, MonsterVfxReadability.ProjectileContrastOrder),
                Body = _effects.CreateBoltRenderer()
            };
            _bolts.Add(bolt);
            _effects.Muzzle(origin, bolt.Direction, move.Width, move.Color);
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
                BossAttackTracking.Advance(zone, _owner.PredictPlayerPosition(zone.TargetLeadTime), deltaTime, _owner.Arena);
                float previousRadius = zone.CurrentRadius;
                float previousAngle = zone.CurrentAngle;
                float previousAge = zone.Age;
                zone.Age += deltaTime;
                if (!zone.WarningOnly && !zone.PreImpactPlayed && zone.Shape == BossZoneShape.Circle
                    && zone.Age < zone.Warning && zone.Warning - zone.Age <= 0.24f)
                {
                    zone.PreImpactPlayed = true;
                    _effects.PreImpact(zone.Move.Kind, zone.Center, zone.Radius, zone.Move.Color);
                }
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
                    bolt.Outline.Dispose();
                    UnityEngine.Object.Destroy(bolt.Body.gameObject);
                    _bolts.RemoveAt(i);
                    continue;
                }

                DrawBolt(bolt);
                if (bolt.Age >= bolt.NextTrail)
                {
                    bolt.NextTrail = bolt.Age + 0.045f;
                    _effects.ProjectileTrail(bolt.Position, bolt.Direction, bolt.Move.Width, bolt.Move.Color);
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

            float homingTime = Mathf.Max(0f, Mathf.Min(deltaTime, bolt.HomingUntil - (bolt.Age - deltaTime)));
            if (bolt.HomingTurn > 0f && homingTime > 0f && _owner.Player != null)
            {
                Vector2 target = _owner.PredictPlayerPosition(_profile.TargetLeadTime);
                bolt.Direction = BossAttackTracking.Steer(bolt.Direction, target - bolt.Position, bolt.HomingTurn, homingTime);
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
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.transform == _owner.transform || collider.transform.IsChildOf(_owner.transform) || collider.GetComponentInParent<NormalMonster>() != null || (collider.attachedRigidbody != null && !collider.attachedRigidbody.simulated))
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
            if (zone.Shape == BossZoneShape.ArenaOutsideSafeCircle)
            {
                point = _owner.Player.position;
                touches = !BossBattleGeometry.PlayerInsideSafeCircle(_owner.Player, _owner.PlayerColliders, zone.SafeCenter, zone.SafeRadius);
                // Resolve the blast once at release; its fading visual is not a second damage window.
                zone.ContactConsumed = true;
                if (touches)
                {
                    // Arena-wide damage does not originate at the boss and cannot be
                    // accidentally cancelled by a wall between boss and player.
                    _owner.TryDealPlayerDamage(zone.Damage, point, (point - zone.SafeCenter).normalized, zone.Move.Color);
                    zone.NextDamage = zone.Age + zone.Move.DamageInterval;
                }

                return;
            }

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
                        int samples = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(zone.CurrentAngle - previousAngle) / 4f), 1, 24);
                        for (int sample = 0; sample <= samples && !touches; sample++)
                            touches = BossBattleGeometry.SectorTouches(body, zone.Center, zone.Radius, Mathf.Lerp(previousAngle, zone.CurrentAngle, sample / (float)samples), zone.Span, out point);
                    }
                    else
                    {
                        touches = BossBattleGeometry.RingTouches(body, zone.Center, previousRadius, zone.CurrentRadius, zone.Width, zone.CurrentAngle, zone.GapWidth, out point);
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
            int count = Physics2D.OverlapCapsule(zone.Center, new Vector2(zone.Length, zone.Width), CapsuleDirection2D.Horizontal, angle, _filter, _overlaps);
            for (int i = 0; i < count; i++)
            {
                Collider2D body = _overlaps[i];
                if (MonsterCombat.ResolvePlayerCollider(body, _owner.Player) == null || (body.isTrigger && MonsterCombat.HasEnabledBodyCollider(_owner.PlayerColliders)) || (body.attachedRigidbody != null && !body.attachedRigidbody.simulated))
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
                return BossBattleGeometry.InSector(point, zone.Center, zone.Radius, zone.CurrentAngle, zone.Span);
            }

            float radius = (point - zone.Center).magnitude;
            return radius >= Mathf.Min(previousRadius, zone.CurrentRadius) - zone.Width * 0.5f && radius <= Mathf.Max(previousRadius, zone.CurrentRadius) + zone.Width * 0.5f && Mathf.Abs(Mathf.DeltaAngle(zone.CurrentAngle, BossBattleGeometry.Angle(point - zone.Center))) >= zone.GapWidth * 0.5f;
        }

        private void Impact(BossDamageZone zone)
        {
            Color color = zone.Move.Color;
            float size = zone.Move.EffectScale;
            if (zone.Shape == BossZoneShape.ArenaOutsideSafeCircle)
            {
                _effects.ArenaDetonation(_owner.Arena, zone.SafeCenter, zone.SafeRadius, color);
            }
            else if (zone.Shape == BossZoneShape.Sector)
            {
                _effects.Slash(zone.Center, zone.Radius, zone.CurrentAngle, color, 1f, zone.Span);
            }
            else if (zone.Shape == BossZoneShape.ClosingRing)
            {
                _effects.NovaArc(zone.Center, zone.CurrentRadius, zone.CurrentAngle + zone.GapWidth * 0.5f,
                    360f - zone.GapWidth, zone.Width, color, true);
            }
            else if (zone.Shape == BossZoneShape.Beam)
            {
                _effects.Beam(zone.Center, zone.CurrentAngle, zone.Length, zone.Width, color, zone.Spikes, _owner.Arena);
            }
            else if (zone.Move.Kind == BossEncounterPattern.StormPillars)
            {
                _effects.Lightning(zone.Center, zone.Radius * size, color);
            }
            else if (zone.Move.Kind == BossEncounterPattern.GravityCollapse)
            {
                _effects.Collapse(zone.Center, zone.Radius * size, color);
            }
            else if (zone.Move.Kind == BossEncounterPattern.BloodPools)
            {
                _effects.BloodPool(zone.Center, zone.Radius, color, true);
            }
            else if (zone.Spikes)
            {
                _effects.Spikes(zone.Center, zone.Radius * size, color);
            }
            else if (zone.Pillar)
            {
                _effects.Pillar(zone.Center, zone.Radius * size, color);
            }
            else
            {
                _effects.Burst(zone.Center, Mathf.Min(1.4f, zone.Radius) * size, color);
            }

            if (zone.Move.ImpactPrefab != null)
            {
                GameObject custom = UnityEngine.Object.Instantiate(zone.Move.ImpactPrefab, new Vector3(zone.Center.x, zone.Center.y, _owner.transform.position.z), Quaternion.Euler(0f, 0f, zone.Angle), _owner.AttackRoot);
                _customEffects.Add(custom);
                UnityEngine.Object.Destroy(custom, zone.Move.ImpactPrefabLifetime);
            }

            _audio.Impact();
            _owner.PulsePatternAnimation(zone.Move);
            if (Time.time >= _nextShake)
            {
                _nextShake = Time.time + 0.08f;
                float shake = zone.Move.ImpactShake;
                if (shake > 0f)
                {
                    float accent = zone.Shape == BossZoneShape.ArenaOutsideSafeCircle ? 1.5f
                        : zone.Shape == BossZoneShape.Sector || zone.Move.Kind == BossEncounterPattern.StormPillars ? 1.3f : 1.15f;
                    _owner.ShakeCamera(Mathf.Min(0.42f, shake * accent), 0.19f);
                }
            }
        }

        private void EmitZoneParticles(BossDamageZone zone)
        {
            if (zone.Age < zone.NextParticle)
            {
                return;
            }

            zone.NextParticle = zone.Age + 0.12f;
            if (zone.Shape == BossZoneShape.ArenaOutsideSafeCircle)
            {
                return;
            }

            if (zone.Move.Kind == BossEncounterPattern.GravityCollapse)
            {
                _effects.Vortex(zone.Center, zone.Radius, zone.CurrentAngle, zone.Move.Color);
                return;
            }
            if (zone.Move.Kind == BossEncounterPattern.BloodPools)
            {
                _effects.BloodPool(zone.Center, zone.Radius, zone.Move.Color, false);
                return;
            }
            if (zone.Shape == BossZoneShape.Sector)
            {
                _effects.Slash(zone.Center, zone.Radius, zone.CurrentAngle, zone.Move.Color, 0.22f, zone.Span);
                return;
            }
            if (zone.Shape == BossZoneShape.ClosingRing)
            {
                _effects.NovaArc(zone.Center, zone.CurrentRadius, zone.CurrentAngle + zone.GapWidth * 0.5f,
                    360f - zone.GapWidth, zone.Width, zone.Move.Color, false);
                return;
            }
            if (zone.Shape == BossZoneShape.Circle)
            {
                _effects.Trail(zone.Center + Random.insideUnitCircle * zone.Radius, Vector2.up, zone.Radius * 0.25f, zone.Move.Color);
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 point;
                if (zone.Shape == BossZoneShape.Beam)
                {
                    point = zone.Center + BossBattleGeometry.Direction(zone.CurrentAngle) * Random.Range(-0.5f, 0.5f) * zone.Length;
                }
                else
                {
                    float angle = zone.Shape == BossZoneShape.Sector ? zone.CurrentAngle + Random.Range(-zone.Span * 0.5f, zone.Span * 0.5f) : zone.CurrentAngle + zone.GapWidth * 0.5f + Random.value * (360f - zone.GapWidth);
                    point = zone.Center + BossBattleGeometry.Direction(angle) * zone.CurrentRadius;
                }

                if (_owner.Arena.Contains(point))
                {
                    _effects.Trail(point, Vector2.down, 0.35f, zone.Move.Color);
                }
            }
        }

        private void DrawZone(BossDamageZone zone)
        {
            bool warning = zone.Age < zone.Warning;
            float progress = Mathf.Clamp01(zone.Age / Mathf.Max(0.01f, zone.Warning));
            float fade = 1f - Mathf.Clamp01((zone.Age - zone.Warning - zone.Duration) / 0.16f);
            bool locked = warning && (zone.TrackPosition || zone.TrackDirection || zone.MotionTracking)
                && zone.Age >= zone.Warning - zone.LockBeforeImpact;
            float activeAge = Mathf.Max(0f, zone.Age - zone.Warning);
            bool residual = !warning && activeAge > zone.Duration;
            // The outline always shows the full hit area. Only its countdown advances.
            Color fill = warning ? MonsterVfxReadability.WarningFill(progress)
                : MonsterVfxReadability.ActiveFill(zone.Move.Color, activeAge);
            Color edge = warning ? MonsterVfxReadability.WarningEdge(progress, locked) : zone.Move.Color;
            if (!warning)
            {
                edge.a = 0.78f + MonsterVfxReadability.ImpactEnvelope(activeAge) * 0.18f;
            }
            if (residual)
            {
                fade *= 0.24f;
            }
            fill.a *= fade;
            edge.a *= fade;
            Color core = MonsterVfxReadability.ActiveCore(zone.Move.Color, activeAge);
            core.a *= warning ? 0f : fade;
            zone.Core.SetVisible(!warning && !residual);
            Color border = new Color(0.025f, 0.018f, 0.045f, fade * 0.95f);
            Color ticks = warning ? Color.Lerp(edge, Color.white, 0.35f) : Color.Lerp(zone.Move.Color, Color.white, 0.18f);
            ticks.a = fade * (warning ? 0.9f : 0.4f);
            if (zone.Shape == BossZoneShape.ArenaOutsideSafeCircle)
            {
                zone.Fill.SetArenaOutsideCircle(_owner.Arena, zone.SafeCenter, zone.SafeRadius, fill);
                Color safe = new Color(0.12f, 1f, 0.78f, fade * (0.85f + 0.1f * Mathf.Sin(zone.Age * 6f)));
                zone.Boundary.SetArc(zone.SafeCenter, zone.SafeRadius - 0.16f, zone.SafeRadius + 0.14f, 0f, 360f, border);
                zone.Outline.SetArc(zone.SafeCenter, zone.SafeRadius - 0.08f, zone.SafeRadius + 0.04f, 0f, 360f, safe);
                zone.Detail.SetReticle(zone.SafeCenter, zone.SafeRadius, 0f, 360f, warning ? progress : 0f,
                    new Color(0.65f, 1f, 0.9f, fade));
                zone.Core.SetVisible(!residual);
                safe.a = 0.055f * fade;
                zone.Core.SetArc(zone.SafeCenter, 0f, zone.SafeRadius - 0.12f, 0f, 360f, safe);
            }
            else if (zone.Shape == BossZoneShape.Circle)
            {
                zone.Fill.SetArc(zone.Center, 0f, zone.Radius, 0f, 360f, fill);
                zone.Boundary.SetArc(zone.Center, Mathf.Max(0f, zone.Radius - 0.12f), zone.Radius + 0.1f, 0f, 360f, border);
                zone.Outline.SetArc(zone.Center, zone.Radius - 0.055f, zone.Radius + 0.055f, 0f, 360f, edge);
                zone.Detail.SetReticle(zone.Center, zone.Radius, -90f, 360f, warning ? progress : 0f, ticks);
                zone.Core.SetArc(zone.Center, zone.Radius - 0.08f, zone.Radius, 0f, 360f, core);
            }
            else if (zone.Shape == BossZoneShape.Sector)
            {
                float start = zone.CurrentAngle - zone.Span * 0.5f;
                zone.Fill.SetArc(zone.Center, 0f, zone.Radius, start, zone.Span, fill);
                zone.Boundary.SetSectorEdges(zone.Center, zone.Radius, start, zone.Span, 0.17f, border);
                zone.Outline.SetSectorEdges(zone.Center, zone.Radius, start, zone.Span, 0.055f, edge);
                zone.Detail.SetReticle(zone.Center, zone.Radius, start, zone.Span, warning ? progress : 0f, ticks);
                zone.Core.SetArc(zone.Center, zone.Radius - 0.2f, zone.Radius, start, zone.Span, core);
            }
            else if (zone.Shape == BossZoneShape.Beam)
            {
                zone.Fill.SetBeam(zone.Center, zone.CurrentAngle, zone.Length, zone.Width, fill);
                zone.Boundary.SetBeamEdges(zone.Center, zone.CurrentAngle, zone.Length, zone.Width, 0.16f, border);
                zone.Outline.SetBeamEdges(zone.Center, zone.CurrentAngle, zone.Length, zone.Width, 0.055f, edge);
                zone.Detail.SetBeamTicks(zone.Center, zone.CurrentAngle, zone.Length, zone.Width, ticks);
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
                zone.Fill.SetArc(zone.Center, warning ? Mathf.Min(zone.Radius, zone.EndRadius) : Mathf.Max(0f, radius - zone.Width * 0.5f), warning ? Mathf.Max(zone.Radius, zone.EndRadius) : radius + zone.Width * 0.5f, start, span, fill);
                zone.Boundary.SetArc(zone.Center, Mathf.Max(0f, radius - zone.Width * 0.5f - 0.04f), radius + zone.Width * 0.5f + 0.04f, start, span, border);
                zone.Outline.SetArc(zone.Center, Mathf.Max(0f, radius - zone.Width * 0.5f), radius + zone.Width * 0.5f, start, span, edge);
                zone.Detail.SetReticle(zone.Center, radius + zone.Width * 0.45f, start, span, warning ? progress : 0f, ticks);
                zone.Core.SetArc(zone.Center, radius - zone.Width * 0.15f, radius + zone.Width * 0.15f, start, span, core);
            }
        }

        private static void DrawBolt(Bolt bolt)
        {
            float size = bolt.Radius * (bolt.Released ? 1.65f : 1.9f);
            Color glow = bolt.Move.Color;
            glow.a = 0.1f;
            bolt.Glow.SetArc(bolt.Position, 0f, size, 0f, 360f, glow);
            bolt.Outline.SetArc(bolt.Position, 0f, bolt.Radius * 1.22f, 0f, 360f, new Color(0.018f, 0.025f, 0.055f, 0.95f));
            bolt.Core.SetArc(bolt.Position, 0f, bolt.Radius * 0.58f, 0f, 360f, Color.Lerp(bolt.Move.Color, Color.white, 0.72f));
            bolt.Body.transform.position = new Vector3(bolt.Position.x, bolt.Position.y, bolt.Body.transform.position.z);
            bolt.Body.transform.rotation = Quaternion.Euler(0f, 0f, BossBattleGeometry.Angle(bolt.Direction) - 90f);
            float spriteSize = Mathf.Max(bolt.Body.sprite.bounds.size.x, bolt.Body.sprite.bounds.size.y);
            bolt.Body.transform.localScale = Vector3.one * bolt.Radius * 2f
                * (1f + Mathf.Sin(bolt.Age * 12f) * 0.025f) / Mathf.Max(0.001f, spriteSize);
            bolt.Body.color = Color.Lerp(bolt.Move.Color, Color.white, 0.12f);
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
                bolt.Outline.Dispose();
                UnityEngine.Object.Destroy(bolt.Body.gameObject);
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
