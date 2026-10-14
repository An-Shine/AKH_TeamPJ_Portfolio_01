using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _02.Scripts.Monster.BossMonster
{
    internal sealed class BossPatternExecutor : IDisposable
    {
        private sealed class ScheduledEvent
        {
            public float Time;
            public Action Action;
        }

        private readonly global::BossMonster _owner;
        private readonly BossEncounterMove _move;
        private readonly BossBattleWorld _world;
        private readonly BossBattleEffects _effects;
        private readonly BossPatternAudio _audio;
        private readonly BossEncounterProfile _profile;
        private readonly List<ScheduledEvent> _events = new List<ScheduledEvent>(48);
        private readonly List<BossShapeVisual> _echoShapes = new List<BossShapeVisual>();
        private readonly Vector2 _origin;
        private readonly int _phase;
        private readonly float _warning;
        private readonly float _damage;
        private readonly float _aim;
        private Vector2 _motionTarget;
        private float _motionStart;
        private float _motionEnd;
        private float _motionSpeed;
        private float _nextAfterimage;
        private float _age;
        private float _plannedEnd;
        private int _eventIndex;
        private bool _moving;
        private BossDamageZone _motionZone;
        public float Age => _age;
        public float PlannedEnd => _plannedEnd;
        public bool HasPendingEvents => _eventIndex < _events.Count;
        private int Waves => Mathf.Clamp(_move.Waves + _phase + _profile.ExtraWaves, 1, 8);
        private float AttackRadius => _move.Radius * _profile.AttackRangeMultiplier;
        private float AttackWidth => _move.Width * _profile.AttackRangeMultiplier;
        private float LeadTime => _profile.TargetLeadTime * (1f + _phase * 0.2f);
        private float ArenaRadius => Mathf.Min(_owner.Arena.width, _owner.Arena.height) * 0.45f;

        public BossPatternExecutor(global::BossMonster owner, BossEncounterMove move, BossBattleWorld world, BossBattleEffects effects, BossPatternAudio audio, BossEncounterProfile profile, int phase, float warning, float damage)
        {
            _owner = owner;
            _move = move;
            _world = world;
            _effects = effects;
            _audio = audio;
            _profile = profile;
            _phase = phase;
            _warning = warning;
            _damage = damage;
            _origin = owner.transform.position;
            _aim = AimFrom(_origin);
            _plannedEnd = warning;
            Build();
            _events.Sort((left, right) => left.Time.CompareTo(right.Time));
            ExecuteDueEvents();
        }

        public void Tick(float deltaTime)
        {
            _age += deltaTime;
            UpdateMotionTarget(deltaTime);
            if (_moving)
            {
                float available = Mathf.Max(0f, Mathf.Min(_age, _motionEnd) - Mathf.Max(_age - deltaTime, _motionStart));
                // Integrate a cosine ease rather than stepping into full speed on release.
                float duration = Mathf.Max(0.01f, _motionEnd - _motionStart);
                float before = Mathf.Clamp01((_age - deltaTime - _motionStart) / duration);
                float after = Mathf.Clamp01((_age - _motionStart) / duration);
                float easedBefore = 0.5f - 0.5f * Mathf.Cos(before * Mathf.PI);
                float easedAfter = 0.5f - 0.5f * Mathf.Cos(after * Mathf.PI);
                _owner.MoveForPattern(_motionTarget, _motionSpeed * duration * (easedAfter - easedBefore));
                if (available > 0f && _age >= _nextAfterimage)
                {
                    _nextAfterimage = _age + 0.055f;
                    _owner.EmitPatternAfterimage(_effects, _move.Color);
                }
            }

            ExecuteDueEvents();
        }

        private void ExecuteDueEvents()
        {
            // 프레임이 늦어져도 예정된 공격을 빠뜨리지 않습니다.
            while (_eventIndex < _events.Count && _events[_eventIndex].Time <= _age)
            {
                _events[_eventIndex++].Action();
            }
        }

        private void Schedule(float time, Action action)
        {
            _events.Add(new ScheduledEvent { Time = Mathf.Max(0f, time), Action = action });
            _plannedEnd = Mathf.Max(_plannedEnd, time + 0.2f);
        }

        private void ScheduleWaves(float firstTime, Action<int> action)
        {
            for (int i = 0; i < Waves; i++)
            {
                int wave = i;
                Schedule(firstTime + i * _move.Interval, () => action(wave));
            }
        }

        private void Build()
        {
            switch (_move.Kind)
            {
                case BossEncounterPattern.CrescentCleave:
                    ScheduleWaves(0f, wave => Sector(_origin, PlayerAimFrom(_origin), 125f, AttackRadius, _warning));
                    break;
                case BossEncounterPattern.RiftLanes:
                    Schedule(0f, RiftLanes);
                    break;
                case BossEncounterPattern.HellfireRain:
                    ScheduleWaves(0f, wave => Rain(_move.Count + _phase));
                    break;
                case BossEncounterPattern.SpiralBarrage:
                    ScheduleWaves(_warning, Spiral);
                    break;
                case BossEncounterPattern.CrossSweep:
                    Schedule(0f, CrossSweep);
                    break;
                case BossEncounterPattern.Cataclysm:
                    Schedule(0f, Cataclysm);
                    Schedule(_warning + _move.Duration, () => ReleaseBurst(_owner.Arena.center, 2.6f));
                    break;
                case BossEncounterPattern.DoubleClaw:
                    ScheduleWaves(0f, wave => Sector(_origin, PlayerAimFrom(_origin) + (wave % 2 == 0 ? -38f : 38f), 115f, AttackRadius, _warning));
                    break;
                case BossEncounterPattern.GroundSlam:
                    ScheduleWaves(0f, GroundSlam);
                    break;
                case BossEncounterPattern.SerpentWave:
                    ScheduleWaves(0f, SerpentWave);
                    break;
                case BossEncounterPattern.ThornFan:
                    ScheduleWaves(0f, ThornFan);
                    break;
                case BossEncounterPattern.RadialSpikes:
                    ScheduleWaves(0f, RadialSpikes);
                    break;
                case BossEncounterPattern.ExpandingNova:
                    ScheduleWaves(0f, wave => Ring(_origin, 0.7f, ArenaRadius * 1.7f, PlayerAimFrom(_origin), 86f, _warning, _move.Duration));
                    break;
                case BossEncounterPattern.TwinNova:
                    ScheduleWaves(0f, TwinNova);
                    break;
                case BossEncounterPattern.TrackingEruption:
                    ScheduleWaves(0f, wave => Circle(PlayerPoint(), AttackRadius, _warning, _move.Duration, trackPlayer: true));
                    break;
                case BossEncounterPattern.MeteorLattice:
                    ScheduleWaves(0f, MeteorLattice);
                    break;
                case BossEncounterPattern.BloodPools:
                    ScheduleWaves(0f, BloodPools);
                    break;
                case BossEncounterPattern.TargetedVolley:
                    AimedVolleys(false);
                    break;
                case BossEncounterPattern.HomingOrbs:
                    AimedVolleys(true);
                    break;
                case BossEncounterPattern.EchoTurrets:
                    EchoTurrets();
                    break;
                case BossEncounterPattern.OrbitRelease:
                    ScheduleWaves(_warning, OrbitRelease);
                    break;
                case BossEncounterPattern.PerimeterBarrage:
                    PerimeterBarrage();
                    break;
                case BossEncounterPattern.DashRend:
                    DashRend();
                    break;
                case BossEncounterPattern.BurrowAmbush:
                    BurrowAmbush();
                    break;
                case BossEncounterPattern.ApocalypseSequence:
                    Apocalypse();
                    break;
                case BossEncounterPattern.SanguineLances:
                    ScheduleWaves(0f, SanguineLances);
                    break;
                case BossEncounterPattern.StormPillars:
                    ScheduleWaves(0f, StormPillars);
                    break;
                case BossEncounterPattern.RotatingScythes:
                    Schedule(0f, RotatingScythes);
                    break;
                case BossEncounterPattern.GravityCollapse:
                    ScheduleWaves(0f, GravityCollapse);
                    break;
                case BossEncounterPattern.AlternatingMaze:
                    ScheduleWaves(0f, AlternatingMaze);
                    break;
                case BossEncounterPattern.ExecutionNova:
                    ExecutionNova();
                    break;
            }
        }

        private Vector2 PlayerPoint()
        {
            return _owner.PredictPlayerPosition(LeadTime);
        }

        private float AimFrom(Vector2 point)
        {
            return BossBattleGeometry.Angle(PlayerPoint() - point);
        }

        private float PlayerAimFrom(Vector2 point)
        {
            // Close-range slices aim at the player, without leading across the boss.
            return BossBattleGeometry.Angle(_owner.PredictPlayerPosition(0f) - point);
        }

        private BossDamageZone Circle(Vector2 point, float radius, float warning, float duration, bool spikes = false, bool once = true, bool clampToRoom = true, bool trackPlayer = false)
        {
            Vector2 offset = point - PlayerPoint();
            if (clampToRoom)
            {
                point = BossBattleGeometry.ClampToRoom(point, _owner.Arena, radius + 0.1f);
            }

            BossDamageZone zone = new BossDamageZone
            {
                Shape = BossZoneShape.Circle,
                Move = _move,
                Center = point,
                Radius = radius,
                Warning = warning,
                Duration = duration,
                Damage = _damage,
                Once = once,
                Pillar = !spikes,
                Spikes = spikes
            };
            if (trackPlayer)
            {
                Track(zone, true, false);
                zone.TargetOffset = offset;
                zone.TrackingPadding = radius + 0.1f;
            }

            _world.AddZone(zone);
            return zone;
        }

        private void Sector(Vector2 point, float angle, float span, float radius, float warning)
        {
            BossDamageZone zone = new BossDamageZone
            {
                Shape = BossZoneShape.Sector,
                Move = _move,
                Center = point,
                Angle = angle,
                Span = span,
                Radius = radius,
                Warning = warning,
                Duration = _move.Duration,
                Damage = _damage,
                Once = true
            };
            Track(zone, false, true, directAim: true);
            _world.AddZone(zone);
        }

        private void Ring(Vector2 point, float start, float end, float attackAngle, float gapWidth, float warning, float duration, float turn = 0f, bool trackPlayer = false)
        {
            float gap = gapWidth > 180f ? 360f - (360f - gapWidth) * 1.15f : Mathf.Max(70f, gapWidth * 0.88f);
            BossDamageZone zone = new BossDamageZone
            {
                Shape = BossZoneShape.ClosingRing,
                Move = _move,
                Center = point,
                Radius = start,
                EndRadius = end,
                // ClosingRing.Angle is the escape gap; the damaging arc faces the other way.
                Angle = attackAngle + 180f,
                GapWidth = gap,
                SweepAngle = turn,
                Width = AttackWidth,
                Warning = warning,
                Duration = duration,
                Damage = _damage,
                Once = true,
                Spikes = true
            };
            Track(zone, trackPlayer, true, directAim: true);
            _world.AddZone(zone);
        }

        private BossDamageZone ProjectileHint(Vector2 origin, float angle, float span, float warning)
        {
            BossDamageZone zone = new BossDamageZone
            {
                Shape = BossZoneShape.Sector,
                Move = _move,
                Center = origin,
                Angle = angle,
                Span = span,
                Radius = 5f * _profile.AttackRangeMultiplier,
                Warning = warning,
                Duration = 0.1f,
                WarningOnly = true
            };
            Track(zone, false, true);
            _world.AddZone(zone);
            return zone;
        }

        private void Track(BossDamageZone zone, bool position, bool direction, Vector2? aimOrigin = null, bool forwardBeam = false, bool directAim = false)
        {
            Vector2 origin = aimOrigin ?? zone.Center;
            zone.TargetLeadTime = directAim ? 0f : LeadTime;
            Vector2 target = _owner.PredictPlayerPosition(zone.TargetLeadTime);
            zone.TrackPosition = position;
            zone.TrackDirection = direction;
            zone.DirectionFromCenter = !aimOrigin.HasValue;
            zone.TrackingOrigin = origin;
            zone.FollowAimRay = forwardBeam;
            zone.AimDistance = zone.Length * 0.5f;
            zone.TargetOffset = zone.Center - target;
            zone.AimOffset = Mathf.DeltaAngle(BossBattleGeometry.Angle(target - origin), zone.Angle);
            zone.TrackingSpeed = _profile.GroundTrackingSpeed * (1f + _phase * 0.08f);
            zone.TrackingTurnRate = _profile.AimTrackingTurnRate * (1f + _phase * 0.08f);
            zone.LockBeforeImpact = _profile.TargetLockTime;
            zone.TrackingPadding = 0.4f;
        }

        private void ReleaseBurst(Vector2 point, float radius, bool emitBurst = true)
        {
            if (emitBurst)
                _effects.Burst(point, radius * _move.EffectScale, _move.Color, 28);
            _audio.Impact();
            _owner.PulsePatternAnimation(_move);
            _owner.ShakeCamera(_move.ImpactShake, 0.3f);
        }

        private void RiftLanes()
        {
            int count = Mathf.Clamp(_move.Count + _phase, 3, 8);
            int gap = Mathf.Clamp(count / 2 + 1, 0, count - 1);
            Vector2 side = BossBattleGeometry.Direction(_aim + 90f);
            float spacing = Mathf.Max(1.9f, AttackWidth * 2.6f);
            Vector2 target = PlayerPoint();
            for (int i = 0; i < count; i++)
            {
                if (i == gap)
                {
                    continue;
                }

                Vector2 offset = side * ((i - count / 2) * spacing);
                BossDamageZone zone = new BossDamageZone
                {
                    Shape = BossZoneShape.Beam,
                    Move = _move,
                    Center = target + offset,
                    Angle = _aim,
                    Width = AttackWidth,
                    Length = _owner.Arena.size.magnitude,
                    Warning = _warning + i * 0.08f,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = true,
                    Spikes = true
                };
                Track(zone, true, false);
                _world.AddZone(zone);
            }
        }

        private void Rain(int count)
        {
            Vector2 target = PlayerPoint();
            float rotation = Random.Range(0f, 360f);
            count = Mathf.Clamp(count, 2, 8);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = i == 0 ? Vector2.zero : BossBattleGeometry.Direction(rotation + (i - 1) * 360f / (count - 1)) * (AttackRadius * 2.8f);
                Circle(target + offset, AttackRadius, _warning + i * 0.06f, _move.Duration, false, false, trackPlayer: true);
            }
        }

        private void Spiral(int wave)
        {
            int count = Mathf.Clamp(_move.Count + _phase * 2, 6, 20);
            float angle = AimFrom(_origin) + wave * 17f;
            for (int i = 1; i < count; i++)
            {
                Shoot(_origin, angle + i * 360f / count, _profile.ProjectileTurnRate * 0.35f);
            }

            Shoot(_origin, AimFrom(_origin));
            ReleaseBurst(_origin, 0.6f);
        }

        private void CrossSweep()
        {
            for (int i = 0; i < 2; i++)
            {
                BossDamageZone zone = new BossDamageZone
                {
                    Shape = BossZoneShape.Beam,
                    Move = _move,
                    Center = _origin,
                    Angle = _aim + i * 90f,
                    SweepAngle = 38f + _phase * 12f,
                    Width = AttackWidth,
                    Length = _owner.Arena.size.magnitude * 2f,
                    Warning = _warning,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = false
                };
                Track(zone, false, true);
                _world.AddZone(zone);
            }
        }

        private void Cataclysm()
        {
            Vector2 center = _owner.Arena.center;
            Ring(center, ArenaRadius, 0.75f, PlayerAimFrom(center), 96f, _warning, _move.Duration, _phase * 12f);
        }

        private void GroundSlam(int wave)
        {
            Circle(_origin, AttackRadius, _warning, _move.Duration, true, true, false);
            Circle(PlayerPoint(), AttackRadius * 0.4f, _warning + 0.18f, _move.Duration, true, trackPlayer: true);
            Ring(_origin, AttackRadius + 0.5f, ArenaRadius * 1.4f, PlayerAimFrom(_origin), 100f, _warning + 0.22f, 1.6f);
        }

        private void SerpentWave(int wave)
        {
            float angle = PlayerAimFrom(_origin);
            int count = Mathf.Clamp(_move.Count, 1, 3);
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * 65f;
                float turn = (wave % 2 == 0 ? 1f : -1f) * 45f;
                Ring(_origin, AttackRadius, ArenaRadius * 1.8f, angle + offset, 282f, _warning + i * 0.16f, _move.Duration, turn);
            }
        }

        private void ThornFan(int wave)
        {
            int count = Mathf.Clamp(_move.Count + _phase, 3, 7);
            float angle = AimFrom(_origin);
            for (int ray = 0; ray < count; ray++)
            {
                float heading = angle - 55f + ray * 110f / (count - 1);
                for (int step = 0; step < 5; step++)
                {
                    Vector2 point = _origin + BossBattleGeometry.Direction(heading) * (1.3f + step * 1.2f) * _profile.AttackRangeMultiplier;
                    if (_owner.Arena.Contains(point))
                    {
                        BossDamageZone zone = Circle(point, AttackRadius, _warning + step * 0.12f, _move.Duration, true);
                        zone.Angle = heading;
                        Track(zone, false, true, _origin);
                        zone.FollowAimRay = true;
                        zone.AimDistance = (point - _origin).magnitude;
                    }
                }
            }
        }

        private void RadialSpikes(int wave)
        {
            int count = Mathf.Clamp(_move.Count + _phase, 6, 10);
            float angle = AimFrom(_origin);
            for (int ray = 1; ray < count - 1; ray++)
            {
                Vector2 direction = BossBattleGeometry.Direction(angle + ray * 360f / count);
                for (int step = 0; step < 4; step++)
                {
                    Vector2 point = _origin + direction * (1.5f + step * 1.15f) * _profile.AttackRangeMultiplier;
                    if (_owner.Arena.Contains(point))
                    {
                        BossDamageZone zone = Circle(point, AttackRadius, _warning + step * 0.16f, _move.Duration, true);
                        zone.Angle = BossBattleGeometry.Angle(direction);
                        Track(zone, false, true, _origin);
                        zone.FollowAimRay = true;
                        zone.AimDistance = (point - _origin).magnitude;
                    }
                }
            }
        }

        private void TwinNova(int wave)
        {
            float angle = PlayerAimFrom(_origin);
            Ring(_origin, 0.7f, ArenaRadius * 1.5f, angle, 110f, _warning, _move.Duration);
            Ring(_origin, ArenaRadius * 1.5f, 0.7f, angle, 110f, _warning + 0.65f, _move.Duration);
        }

        private void MeteorLattice(int wave)
        {
            Rect area = _owner.Arena;
            int cells = Mathf.Clamp(_move.Count + (_phase > 1 ? 1 : 0), 3, 5);
            Vector2 player = PlayerPoint();
            int safeRow = Mathf.Clamp((int)((player.y - area.yMin) / area.height * cells), 0, cells - 1);
            for (int y = 0; y < cells; y++)
            {
                for (int x = 0; x < cells; x++)
                {
                    if (y == safeRow || (x + y + wave) % 2 == 0)
                    {
                        continue;
                    }

                    Vector2 point = new Vector2(area.xMin + (x + 0.5f) * area.width / cells, area.yMin + (y + 0.5f) * area.height / cells);
                    Circle(point, AttackRadius, _warning + x * 0.05f, _move.Duration);
                }
            }

            Circle(player, AttackRadius * 0.85f, _warning, _move.Duration, trackPlayer: true);
        }

        private void BloodPools(int wave)
        {
            Rect area = _owner.Arena;
            int count = Mathf.Clamp(_move.Count + _phase, 2, 6);
            for (int i = 0; i < count; i++)
            {
                Vector2 point = i < 2 ? PlayerPoint() + (i == 0 ? Vector2.zero : Random.insideUnitCircle * AttackRadius * 2.2f) : new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                Circle(point, AttackRadius, _warning, _move.Duration, false, false, trackPlayer: i < 2);
            }
        }

        private void AimedVolleys(bool homing)
        {
            for (int i = 0; i < Waves; i++)
            {
                BossDamageZone hint = null;
                Schedule(i * _move.Interval, () =>
                {
                    hint = ProjectileHint(_origin, AimFrom(_origin), homing ? 65f : 52f, _warning);
                });
                Schedule(i * _move.Interval + _warning, () =>
                {
                    int count = Mathf.Clamp(_move.Count + (homing ? 0 : _phase), 2, 8);
                    float span = homing ? 50f : 40f;
                    float angle = hint != null ? hint.Angle : AimFrom(_origin);
                    for (int shot = 0; shot < count; shot++)
                    {
                        float heading = angle - span * 0.5f + shot * span / (count - 1);
                        Shoot(_origin, heading, _profile.ProjectileTurnRate * (homing ? 1.75f : 1f));
                    }

                    ReleaseBurst(_origin, 0.5f);
                });
            }
        }

        private void Shoot(Vector2 point, float angle, float homing = -1f)
        {
            Vector2 origin = point + BossBattleGeometry.Direction(angle) * Mathf.Max(0.5f, _owner.BodyWidth * 0.4f);
            origin = BossBattleGeometry.ClampToRoom(origin, _owner.Arena, AttackRadius + 0.1f);
            float turn = (homing < 0f ? _profile.ProjectileTurnRate : homing) * (1f + _phase * 0.12f);
            float until = Mathf.Min(_move.Duration - 0.35f, _profile.ProjectileHomingDuration + _phase * 0.15f);
            _world.Shoot(origin, angle, _move, _damage, _move.Speed * (1f + _phase * 0.06f), turn, Mathf.Max(0f, until));
        }

        private void EchoTurrets()
        {
            int count = Mathf.Clamp(_move.Count, 2, 4);
            Vector2[] positions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 point = _owner.Arena.center + BossBattleGeometry.Direction(_aim + i * 360f / count) * ArenaRadius * 0.72f;
                positions[i] = BossBattleGeometry.ClampToRoom(point, _owner.Arena, 1f);
                CreateEchoGlyph(positions[i]);
            }

            for (int i = 0; i < Waves; i++)
            {
                BossDamageZone[] hints = new BossDamageZone[count];
                Schedule(i * _move.Interval, () =>
                {
                    for (int head = 0; head < count; head++)
                    {
                        hints[head] = ProjectileHint(positions[head], AimFrom(positions[head]), 26f, _warning);
                    }
                });
                Schedule(i * _move.Interval + _warning, () =>
                {
                    for (int head = 0; head < count; head++)
                    {
                        float angle = hints[head] != null ? hints[head].Angle : AimFrom(positions[head]);
                        Shoot(positions[head], angle - 7f, _profile.ProjectileTurnRate * 0.7f);
                        Shoot(positions[head], angle + 7f, _profile.ProjectileTurnRate * 0.7f);
                        _effects.Burst(positions[head], 0.65f, _move.Color, 8);
                    }

                    _audio.Impact();
                    _owner.PulsePatternAnimation(_move);
                });
            }
        }

        private void CreateEchoGlyph(Vector2 point)
        {
            BossShapeVisual outer = _effects.CreateShape("BossEchoMask", true, 2);
            outer.SetArc(point, 0.38f, 0.57f, 35f, 290f, _move.Color);
            _echoShapes.Add(outer);
            for (int i = -1; i <= 1; i += 2)
            {
                BossShapeVisual eye = _effects.CreateShape("BossEchoEye", true, 3);
                eye.SetBeam(point + new Vector2(i * 0.2f, 0.05f), i * 25f, 0.2f, 0.065f, Color.white);
                _echoShapes.Add(eye);
            }
        }

        private void OrbitRelease(int wave)
        {
            int count = Mathf.Clamp(_move.Count + _phase, 6, 12);
            for (int i = 1; i < count; i++)
            {
                _world.ShootOrbit(_origin, AimFrom(_origin) + i * 360f / count + wave * 22f, _move, _damage, _move.Speed * (1f + _phase * 0.06f), Mathf.Max(1.2f, _owner.BodyWidth * 0.65f) * _profile.AttackRangeMultiplier, 1.05f, wave % 2 == 0 ? 150f : -150f);
            }

            ReleaseBurst(_origin, 0.6f);
        }

        private void PerimeterBarrage()
        {
            Rect area = _owner.Arena;
            float inset = Mathf.Min(_world.ProjectileSpawnInset(_move), Mathf.Min(area.width, area.height) * 0.4f);
            int count = Mathf.Clamp(_move.Count + _phase, 4, 10);
            for (int wave = 0; wave < Waves; wave++)
            {
                int edge = wave % 4;
                bool vertical = edge % 2 == 0;
                List<BossDamageZone> hints = new List<BossDamageZone>();
                Schedule(wave * _move.Interval, () =>
                {
                    Vector2 target = PlayerPoint();
                    float coordinate = vertical
                        ? (target.y - area.yMin - inset) / Mathf.Max(0.1f, area.height - inset * 2f)
                        : (target.x - area.xMin - inset) / Mathf.Max(0.1f, area.width - inset * 2f);
                    int gap = Mathf.Clamp(Mathf.FloorToInt(coordinate * count), 0, count - 1);
                    for (int i = 0; i < count; i++)
                    {
                        if (Mathf.Abs(i - gap) <= 1)
                            continue;
                        float fraction = (i + 0.5f) / count;
                        Vector2 point = vertical
                            ? new Vector2(edge == 0 ? area.xMin + inset : area.xMax - inset,
                                Mathf.Lerp(area.yMin + inset, area.yMax - inset, fraction))
                            : new Vector2(Mathf.Lerp(area.xMin + inset, area.xMax - inset, fraction),
                                edge == 1 ? area.yMax - inset : area.yMin + inset);
                        if (!_world.TryGetProjectileSpawn(point, AimFrom(point), _move, out Vector2 spawn))
                        {
                            continue;
                        }

                        hints.Add(ProjectileHint(spawn, AimFrom(spawn), 14f, _warning));
                    }
                });
                Schedule(wave * _move.Interval + _warning, () =>
                {
                    foreach (BossDamageZone hint in hints)
                    {
                        float until = Mathf.Min(_profile.ProjectileHomingDuration, Mathf.Max(0f, _move.Duration - 0.35f));
                        _world.Shoot(hint.Center, hint.Angle, _move, _damage, _move.Speed * (1f + _phase * 0.06f), _profile.ProjectileTurnRate * 0.5f, until);
                        _effects.Burst(hint.Center, 0.4f, _move.Color, 6);
                    }

                    _audio.Impact(0.7f);
                });
            }
        }

        private void DashRend()
        {
            Vector2 destination = _origin + BossBattleGeometry.Direction(_aim) * _move.Speed * _profile.ProjectileSpeedMultiplier * _move.Duration;
            _motionTarget = _owner.FindPatternDestination(destination);
            Vector2 delta = _motionTarget - _origin;
            _motionStart = _warning;
            _motionEnd = _warning + _move.Duration;
            _motionSpeed = _move.Speed * _profile.ProjectileSpeedMultiplier;
            _moving = true;
            Schedule(0f, () =>
            {
                _motionZone = new BossDamageZone
                {
                    Shape = BossZoneShape.Beam,
                    Move = _move,
                    Center = (_origin + _motionTarget) * 0.5f,
                    Angle = BossBattleGeometry.Angle(delta),
                    Length = Mathf.Max(0.5f, delta.magnitude),
                    Width = AttackWidth,
                    Warning = _warning,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = true,
                    MotionTracking = true,
                    LockBeforeImpact = _profile.TargetLockTime
                };
                _world.AddZone(_motionZone);
            });
            Schedule(_motionEnd, () => ReleaseBurst(_owner.transform.position, 1.2f));
        }

        private void BurrowAmbush()
        {
            Vector2 target = PlayerPoint() - BossBattleGeometry.Direction(_aim) * 1.2f;
            float moveTime = _warning - 0.4f;
            Vector2 delta = target - _origin;
            float distance = Mathf.Min(delta.magnitude, _move.Speed * _profile.ProjectileSpeedMultiplier * moveTime);
            _motionTarget = _owner.FindPatternDestination(_origin + delta.normalized * distance, AttackRadius + 0.1f);
            _motionStart = 0.3f;
            _motionEnd = _warning;
            _motionSpeed = _move.Speed * _profile.ProjectileSpeedMultiplier;
            _moving = true;
            Schedule(0f, () =>
            {
                _motionZone = Circle(_motionTarget, AttackRadius, _warning, _move.Duration, true, clampToRoom: false);
                _motionZone.MotionTracking = true;
                _motionZone.LockBeforeImpact = _profile.TargetLockTime;
            });
            Schedule(0.3f, () =>
            {
                _effects.Burst(_origin, 1.5f, _move.Color, 24);
                _owner.SetPatternHidden(true);
            });
            Schedule(_warning, () =>
            {
                _owner.SetPatternHidden(false);
                ReleaseBurst(_owner.transform.position, 1.8f);
                Ring(_motionTarget, AttackRadius + 0.2f, ArenaRadius * 1.4f, PlayerAimFrom(_motionTarget), 125f, 0.75f, 1.4f);
            });
        }

        private void UpdateMotionTarget(float deltaTime)
        {
            if (_motionZone == null || _age - deltaTime >= _warning - _profile.TargetLockTime)
                return;
            float trackingTime = Mathf.Max(0f, Mathf.Min(deltaTime, _warning - _profile.TargetLockTime - (_age - deltaTime)));
            if (_move.Kind == BossEncounterPattern.DashRend)
            {
                Vector2 direction = BossAttackTracking.Steer(_motionTarget - _origin, PlayerPoint() - _origin, _profile.AimTrackingTurnRate, trackingTime);
                _motionTarget = _owner.FindPatternDestination(_origin + direction * _motionSpeed * _move.Duration);
                Vector2 delta = _motionTarget - _origin;
                _motionZone.Center = (_origin + _motionTarget) * 0.5f;
                _motionZone.Angle = BossBattleGeometry.Angle(delta);
                _motionZone.Length = Mathf.Max(0.5f, delta.magnitude);
            }
            else if (_move.Kind == BossEncounterPattern.BurrowAmbush)
            {
                Vector2 current = _owner.transform.position;
                Vector2 desired = PlayerPoint() - (PlayerPoint() - current).normalized * 1.1f;
                float duration = Mathf.Max(0.01f, _motionEnd - _motionStart);
                float progress = Mathf.Clamp01((_age - _motionStart) / duration);
                float remaining = _motionSpeed * duration * (0.5f + 0.5f * Mathf.Cos(progress * Mathf.PI));
                Vector2 reachable = current + Vector2.ClampMagnitude(desired - current, remaining);
                Vector2 candidate = Vector2.MoveTowards(_motionTarget, reachable, _profile.GroundTrackingSpeed * trackingTime);
                candidate = current + Vector2.ClampMagnitude(candidate - current, remaining);
                _motionTarget = _owner.FindPatternDestination(candidate, AttackRadius + 0.1f);
                _motionZone.Center = _motionTarget;
            }
        }

        private void Apocalypse()
        {
            ScheduleWaves(0f, wave =>
            {
                Vector2 target = PlayerPoint();
                float gap = PlayerAimFrom(_origin);
                Circle(target, AttackRadius, _warning, 0.28f, trackPlayer: true);
                Ring(_origin, 0.8f, ArenaRadius * 1.6f, gap, 150f, _warning, _move.Duration);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 point = _origin + BossBattleGeometry.Direction(gap + 110f + i * 70f) * (2.2f + wave * 0.35f);
                    Circle(point, AttackRadius, _warning + 0.15f, 0.3f, true);
                }
            });
            float finale = (Waves - 1) * _move.Interval;
            Schedule(finale, () => Circle(_origin, Mathf.Min(2.8f * _profile.AttackRangeMultiplier, ArenaRadius * 0.65f), _warning + _move.Duration, 0.25f, true, true, false));
            Schedule(finale + _warning + _move.Duration, () => ReleaseBurst(_origin, 3.2f));
        }

        private void SanguineLances(int wave)
        {
            float aim = AimFrom(_origin);
            for (int i = -1; i <= 1; i++)
            {
                float angle = aim + i * 28f + (wave % 2 == 0 ? -7f : 7f);
                Vector2 direction = BossBattleGeometry.Direction(angle);
                float length = 8f * _profile.AttackRangeMultiplier;
                BossDamageZone zone = new BossDamageZone
                {
                    Shape = BossZoneShape.Beam,
                    Move = _move,
                    Center = _origin + direction * length * 0.5f,
                    Angle = angle,
                    Length = length,
                    Width = AttackWidth,
                    Warning = _warning + Mathf.Abs(i) * 0.12f,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = true,
                    Spikes = true
                };
                Track(zone, false, true, _origin, true);
                _world.AddZone(zone);
            }
        }

        private void StormPillars(int wave)
        {
            Vector2 center = PlayerPoint();
            Circle(center, AttackRadius, _warning, _move.Duration, trackPlayer: true);
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = BossBattleGeometry.Direction(wave * 35f + i * 90f) * (2.4f * _profile.AttackRangeMultiplier);
                Circle(center + offset, AttackRadius, _warning + i * 0.14f, _move.Duration, trackPlayer: true);
            }
        }

        private void RotatingScythes()
        {
            for (int i = 0; i < 3; i++)
            {
                BossDamageZone zone = new BossDamageZone
                {
                    Shape = BossZoneShape.Sector,
                    Move = _move,
                    Center = _origin,
                    Radius = AttackRadius,
                    Angle = PlayerAimFrom(_origin) + i * 120f,
                    Span = 36f,
                    SweepAngle = 180f,
                    Warning = _warning,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = false
                };
                Track(zone, false, true, directAim: true);
                _world.AddZone(zone);
            }
        }

        private void GravityCollapse(int wave)
        {
            Vector2 point = PlayerPoint() + (wave == 0 ? Vector2.zero : BossBattleGeometry.Direction(_aim + wave * 120f) * AttackRadius * 0.7f);
            point = BossBattleGeometry.ClampToRoom(point, _owner.Arena, AttackRadius + 0.1f);
            Circle(point, AttackRadius, _warning, _move.Duration, trackPlayer: true);
            Ring(point, 3.5f * _profile.AttackRangeMultiplier, AttackRadius + 0.3f, PlayerAimFrom(point), 115f, _warning, 1.2f, trackPlayer: true);
        }

        private void AlternatingMaze(int wave)
        {
            Rect area = _owner.Arena;
            bool horizontal = wave % 2 == 0;
            int count = Mathf.Clamp(_move.Count + (_phase > 1 ? 1 : 0), 4, 6);
            float playerFraction = horizontal ? (PlayerPoint().y - area.yMin) / area.height : (PlayerPoint().x - area.xMin) / area.width;
            int playerLane = Mathf.Clamp(Mathf.FloorToInt(playerFraction * count), 0, count - 1);
            int safe = playerLane + (wave % 2 == 0 ? 1 : -1);
            if (safe < 0 || safe >= count)
                safe = playerLane + (safe < 0 ? 1 : -1);
            for (int i = 0; i < count; i++)
            {
                if (i == safe)
                    continue;
                Vector2 point = horizontal ? new Vector2(area.center.x, area.yMin + (i + 0.5f) * area.height / count) : new Vector2(area.xMin + (i + 0.5f) * area.width / count, area.center.y);
                _world.AddZone(new BossDamageZone { Shape = BossZoneShape.Beam, Move = _move, Center = point, Angle = horizontal ? 0f : 90f, Length = horizontal ? area.width : area.height, Width = AttackWidth, Warning = _warning, Duration = _move.Duration, Damage = _damage, Once = true, Spikes = true });
            }
        }

        private void ExecutionNova()
        {
            // Place a nearby reachable refuge, then lock it for the whole warning.
            // If walls leave no clear route, the current player position is the fallback.
            Vector2 player = _owner.Player != null ? (Vector2)_owner.Player.position : _origin;
            Vector2 safe = BossBattleGeometry.ClampToRoom(player, _owner.Arena, 0.1f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 candidate = BossBattleGeometry.ClampToRoom(player + BossBattleGeometry.Direction(_aim + 130f + i * 45f) * 3.1f, _owner.Arena, _move.Radius + 0.2f);
                float travel = (candidate - player).magnitude;
                if (travel >= 1f && travel <= 3.8f && MonsterCombat.HasClearAttackPath(player, candidate, _owner.transform, _owner.Player))
                {
                    safe = candidate;
                    break;
                }
            }

            float damage = PlayerManager.Instance != null && PlayerManager.Instance.PlayerStatusData != null ? Mathf.Max(_damage, PlayerManager.Instance.PlayerStatusData.MaxHP * 10f) : _damage;
            Schedule(0f, () => _world.AddZone(new BossDamageZone { Shape = BossZoneShape.ArenaOutsideSafeCircle, Move = _move, Center = _owner.Arena.center, SafeCenter = safe, SafeRadius = _move.Radius, Radius = _owner.Arena.size.magnitude, Warning = _warning, Duration = _move.Duration, Damage = damage, Once = true }));
            // ArenaDetonation already draws the impact around the refuge.
            // A second origin explosion would obscure the safe circle.
            Schedule(_warning, () => ReleaseBurst(_origin, 3.4f, false));
            _plannedEnd = _warning + _move.Duration + 0.2f;
        }

        public void Dispose()
        {
            _moving = false;
            _owner.SetPatternHidden(false);
            foreach (BossShapeVisual shape in _echoShapes)
            {
                shape.Dispose();
            }

            _echoShapes.Clear();
            _events.Clear();
        }
    }
}
