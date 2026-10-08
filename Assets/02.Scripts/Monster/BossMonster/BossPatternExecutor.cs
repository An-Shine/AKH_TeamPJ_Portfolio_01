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

        public float Age => _age;
        public float PlannedEnd => _plannedEnd;
        public bool HasPendingEvents => _eventIndex < _events.Count;
        private int Waves => Mathf.Clamp(_move.Waves + _phase, 1, 8);
        private float ArenaRadius => Mathf.Min(_owner.Arena.width, _owner.Arena.height) * 0.45f;

        public BossPatternExecutor(global::BossMonster owner, BossEncounterMove move, BossBattleWorld world,
            BossBattleEffects effects, BossPatternAudio audio, int phase, float warning, float damage)
        {
            _owner = owner;
            _move = move;
            _world = world;
            _effects = effects;
            _audio = audio;
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
            if (_moving)
            {
                float available = Mathf.Max(0f, Mathf.Min(_age, _motionEnd) - Mathf.Max(_age - deltaTime, _motionStart));
                _owner.MoveForPattern(_motionTarget, _motionSpeed * available);
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
            _events.Add(new ScheduledEvent
            {
                Time = Mathf.Max(0f, time),
                Action = action
            });
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
                    ScheduleWaves(0f, wave => Sector(_origin, AimFrom(_origin), 115f, _move.Radius, _warning));
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
                    ScheduleWaves(0f, wave => Sector(_origin, _aim + (wave % 2 == 0 ? -38f : 38f),
                        105f, _move.Radius, _warning));
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
                    ScheduleWaves(0f, wave => Ring(_origin, 0.7f, ArenaRadius * 1.7f,
                        AimFrom(_origin), 86f, _warning, _move.Duration));
                    break;
                case BossEncounterPattern.TwinNova:
                    ScheduleWaves(0f, TwinNova);
                    break;
                case BossEncounterPattern.TrackingEruption:
                    ScheduleWaves(0f, wave => Circle(PlayerPoint(), _move.Radius, _warning, _move.Duration));
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
            }
        }

        private Vector2 PlayerPoint()
        {
            return _owner.Player != null ? (Vector2)_owner.Player.position : _origin;
        }

        private float AimFrom(Vector2 point)
        {
            return BossBattleGeometry.Angle(PlayerPoint() - point);
        }

        private void Circle(Vector2 point, float radius, float warning, float duration,
            bool spikes = false, bool once = true, bool clampToRoom = true)
        {
            if (clampToRoom)
            {
                point = BossBattleGeometry.ClampToRoom(point, _owner.Arena, radius + 0.1f);
            }
            _world.AddZone(new BossDamageZone
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
            });
        }

        private void Sector(Vector2 point, float angle, float span, float radius, float warning)
        {
            _world.AddZone(new BossDamageZone
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
            });
        }

        private void Ring(Vector2 point, float start, float end, float gapAngle, float gapWidth,
            float warning, float duration, float turn = 0f)
        {
            _world.AddZone(new BossDamageZone
            {
                Shape = BossZoneShape.ClosingRing,
                Move = _move,
                Center = point,
                Radius = start,
                EndRadius = end,
                Angle = gapAngle,
                GapWidth = gapWidth,
                SweepAngle = turn,
                Width = _move.Width,
                Warning = warning,
                Duration = duration,
                Damage = _damage,
                Once = true,
                Spikes = true
            });
        }

        private void ProjectileHint(Vector2 origin, float angle, float span, float warning)
        {
            _world.AddZone(new BossDamageZone
            {
                Shape = BossZoneShape.Sector,
                Move = _move,
                Center = origin,
                Angle = angle,
                Span = span,
                Radius = 5f,
                Warning = warning,
                Duration = 0.1f,
                WarningOnly = true
            });
        }

        private void ReleaseBurst(Vector2 point, float radius)
        {
            _effects.Burst(point, radius * _move.EffectScale, _move.Color, 28);
            _audio.Impact();
            _owner.PulsePatternAnimation(_move);
            _owner.ShakeCamera(_move.ImpactShake, 0.3f);
        }

        private void RiftLanes()
        {
            int count = Mathf.Clamp(_move.Count + _phase, 3, 8);
            int gap = Random.Range(0, count);
            Vector2 side = BossBattleGeometry.Direction(_aim + 90f);
            float spacing = Mathf.Max(1.9f, _move.Width * 2.8f);
            for (int i = 0; i < count; i++)
            {
                if (i == gap)
                {
                    continue;
                }
                _world.AddZone(new BossDamageZone
                {
                    Shape = BossZoneShape.Beam,
                    Move = _move,
                    Center = _owner.Arena.center + side * ((i - (count - 1) * 0.5f) * spacing),
                    Angle = _aim,
                    Width = _move.Width,
                    Length = _owner.Arena.size.magnitude,
                    Warning = _warning + i * 0.08f,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = true,
                    Spikes = true
                });
            }
        }

        private void Rain(int count)
        {
            Vector2 target = PlayerPoint();
            float rotation = Random.Range(0f, 360f);
            count = Mathf.Clamp(count, 2, 8);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = i == 0 ? Vector2.zero
                    : BossBattleGeometry.Direction(rotation + (i - 1) * 360f / (count - 1)) * (_move.Radius * 3.3f);
                Circle(target + offset, _move.Radius, _warning + i * 0.06f, _move.Duration, false, false);
            }
        }

        private void Spiral(int wave)
        {
            int count = Mathf.Clamp(_move.Count + _phase * 2, 6, 20);
            float angle = _aim + wave * 17f;
            for (int i = 1; i < count; i++)
            {
                Shoot(_origin, angle + i * 360f / count);
            }
            ReleaseBurst(_origin, 0.6f);
        }

        private void CrossSweep()
        {
            for (int i = 0; i < 2; i++)
            {
                _world.AddZone(new BossDamageZone
                {
                    Shape = BossZoneShape.Beam,
                    Move = _move,
                    Center = _origin,
                    Angle = _aim + i * 90f,
                    SweepAngle = 32f + _phase * 10f,
                    Width = _move.Width,
                    Length = _owner.Arena.size.magnitude * 2f,
                    Warning = _warning,
                    Duration = _move.Duration,
                    Damage = _damage,
                    Once = false
                });
            }
        }

        private void Cataclysm()
        {
            Vector2 center = _owner.Arena.center;
            Ring(center, ArenaRadius, 0.75f, AimFrom(center), 96f,
                _warning, _move.Duration, _phase * 12f);
        }

        private void GroundSlam(int wave)
        {
            Circle(_origin, _move.Radius, _warning, _move.Duration, true, true, false);
            Ring(_origin, _move.Radius + 0.5f, ArenaRadius * 1.4f, _aim,
                100f, _warning + 0.22f, 1.6f);
        }

        private void SerpentWave(int wave)
        {
            float angle = AimFrom(_origin);
            int count = Mathf.Clamp(_move.Count, 1, 3);
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * 65f;
                float turn = (wave % 2 == 0 ? 1f : -1f) * 45f;
                Ring(_origin, _move.Radius, ArenaRadius * 1.8f, angle + offset + 180f,
                    282f, _warning + i * 0.16f, _move.Duration, turn);
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
                    Vector2 point = _origin + BossBattleGeometry.Direction(heading) * (1.3f + step * 1.2f);
                    if (_owner.Arena.Contains(point))
                    {
                        Circle(point, _move.Radius, _warning + step * 0.12f, _move.Duration, true);
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
                    Vector2 point = _origin + direction * (1.5f + step * 1.15f);
                    if (_owner.Arena.Contains(point))
                    {
                        Circle(point, _move.Radius, _warning + step * 0.16f, _move.Duration, true);
                    }
                }
            }
        }

        private void TwinNova(int wave)
        {
            float gap = AimFrom(_origin);
            Ring(_origin, 0.7f, ArenaRadius * 1.5f, gap, 110f, _warning, _move.Duration);
            Ring(_origin, ArenaRadius * 1.5f, 0.7f, gap, 110f,
                _warning + 0.65f, _move.Duration);
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
                    Vector2 point = new Vector2(area.xMin + (x + 0.5f) * area.width / cells,
                        area.yMin + (y + 0.5f) * area.height / cells);
                    Circle(point, _move.Radius, _warning + x * 0.05f, _move.Duration);
                }
            }
        }

        private void BloodPools(int wave)
        {
            Rect area = _owner.Arena;
            int count = Mathf.Clamp(_move.Count + _phase, 2, 6);
            for (int i = 0; i < count; i++)
            {
                Vector2 point = i == 0 ? PlayerPoint()
                    : new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                Circle(point, _move.Radius, _warning, _move.Duration, false, false);
            }
        }

        private void AimedVolleys(bool homing)
        {
            for (int i = 0; i < Waves; i++)
            {
                float angle = _aim;
                Schedule(i * _move.Interval, () =>
                {
                    angle = AimFrom(_origin);
                    ProjectileHint(_origin, angle, homing ? 65f : 52f, _warning);
                });
                Schedule(i * _move.Interval + _warning, () =>
                {
                    int count = Mathf.Clamp(_move.Count + (homing ? 0 : _phase), 2, 8);
                    float span = homing ? 50f : 40f;
                    for (int shot = 0; shot < count; shot++)
                    {
                        float heading = angle - span * 0.5f + shot * span / (count - 1);
                        Shoot(_origin, heading, homing ? 36f : 0f);
                    }
                    ReleaseBurst(_origin, 0.5f);
                });
            }
        }

        private void Shoot(Vector2 point, float angle, float homing = 0f)
        {
            Vector2 origin = point + BossBattleGeometry.Direction(angle) * Mathf.Max(0.5f, _owner.BodyWidth * 0.4f);
            origin = BossBattleGeometry.ClampToRoom(origin, _owner.Arena, _move.Radius + 0.1f);
            _world.Shoot(origin, angle, _move, _damage, _move.Speed * (1f + _phase * 0.06f), homing, 1.15f);
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
                float[] angles = new float[count];
                Schedule(i * _move.Interval, () =>
                {
                    for (int head = 0; head < count; head++)
                    {
                        angles[head] = AimFrom(positions[head]);
                        ProjectileHint(positions[head], angles[head], 26f, _warning);
                    }
                });
                Schedule(i * _move.Interval + _warning, () =>
                {
                    for (int head = 0; head < count; head++)
                    {
                        Shoot(positions[head], angles[head] - 7f);
                        Shoot(positions[head], angles[head] + 7f);
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
                _world.ShootOrbit(_origin, _aim + i * 360f / count + wave * 22f, _move, _damage,
                    _move.Speed, Mathf.Max(1.2f, _owner.BodyWidth * 0.65f), 1.05f,
                    wave % 2 == 0 ? 150f : -150f);
            }
            ReleaseBurst(_origin, 0.6f);
        }

        private void PerimeterBarrage()
        {
            Rect area = _owner.Arena;
            int count = Mathf.Clamp(_move.Count + _phase, 4, 10);
            Vector2 target = PlayerPoint();
            for (int wave = 0; wave < Waves; wave++)
            {
                int edge = wave % 4;
                bool vertical = edge % 2 == 0;
                float coordinate = vertical ? (target.y - area.yMin) / area.height : (target.x - area.xMin) / area.width;
                int gap = Mathf.Clamp(Mathf.FloorToInt(coordinate * count), 0, count - 1);
                for (int i = 0; i < count; i++)
                {
                    if (Mathf.Abs(i - gap) <= 1)
                    {
                        continue;
                    }
                    float fraction = (i + 0.5f) / count;
                    Vector2 point = vertical
                        ? new Vector2(edge == 0 ? area.xMin + 0.35f : area.xMax - 0.35f, area.yMin + area.height * fraction)
                        : new Vector2(area.xMin + area.width * fraction, edge == 1 ? area.yMax - 0.35f : area.yMin + 0.35f);
                    float angle = edge == 0 ? 0f : edge == 1 ? -90f : edge == 2 ? 180f : 90f;
                    Schedule(wave * _move.Interval, () => ProjectileHint(point, angle, 9f, _warning));
                    Schedule(wave * _move.Interval + _warning, () =>
                    {
                        _world.Shoot(point, angle, _move, _damage, _move.Speed);
                        _effects.Burst(point, 0.4f, _move.Color, 6);
                        _audio.Impact(0.7f);
                    });
                }
            }
        }

        private void DashRend()
        {
            Vector2 destination = _origin + BossBattleGeometry.Direction(_aim) * _move.Speed * _move.Duration;
            _motionTarget = _owner.FindPatternDestination(destination);
            Vector2 delta = _motionTarget - _origin;
            _motionStart = _warning;
            _motionEnd = _warning + _move.Duration;
            _motionSpeed = _move.Speed;
            _moving = true;
            Schedule(0f, () => _world.AddZone(new BossDamageZone
            {
                Shape = BossZoneShape.Beam,
                Move = _move,
                Center = (_origin + _motionTarget) * 0.5f,
                Angle = BossBattleGeometry.Angle(delta),
                Length = Mathf.Max(0.5f, delta.magnitude),
                Width = _move.Width,
                Warning = _warning,
                Duration = _move.Duration,
                Damage = _damage,
                Once = true
            }));
            Schedule(_motionEnd, () => ReleaseBurst(_owner.transform.position, 1.2f));
        }

        private void BurrowAmbush()
        {
            Vector2 target = PlayerPoint() - BossBattleGeometry.Direction(_aim) * 1.2f;
            float moveTime = _warning - 0.4f;
            Vector2 delta = target - _origin;
            float distance = Mathf.Min(delta.magnitude, _move.Speed * moveTime);
            _motionTarget = _owner.FindPatternDestination(_origin + delta.normalized * distance, _move.Radius + 0.1f);
            _motionStart = 0.3f;
            _motionEnd = _warning;
            _motionSpeed = _move.Speed;
            _moving = true;
            Schedule(0f, () => Circle(_motionTarget, _move.Radius, _warning, _move.Duration, true));
            Schedule(0.3f, () =>
            {
                _effects.Burst(_origin, 1.5f, _move.Color, 24);
                _owner.SetPatternHidden(true);
            });
            Schedule(_warning, () =>
            {
                _owner.SetPatternHidden(false);
                ReleaseBurst(_owner.transform.position, 1.8f);
                Ring(_motionTarget, _move.Radius + 0.2f, ArenaRadius * 1.4f,
                    AimFrom(_motionTarget), 125f, 0.65f, 1.4f);
            });
        }

        private void Apocalypse()
        {
            ScheduleWaves(0f, wave =>
            {
                Vector2 target = PlayerPoint();
                float gap = AimFrom(_origin);
                Circle(target, _move.Radius, _warning, 0.28f);
                Ring(_origin, 0.8f, ArenaRadius * 1.6f, gap, 150f, _warning, _move.Duration);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 point = _origin + BossBattleGeometry.Direction(gap + 110f + i * 70f) * (2.2f + wave * 0.35f);
                    Circle(point, _move.Radius, _warning + 0.15f, 0.3f, true);
                }
            });
            float finale = (Waves - 1) * _move.Interval;
            Schedule(finale, () => Circle(_origin, Mathf.Min(2.8f, ArenaRadius * 0.5f),
                _warning + _move.Duration, 0.25f, true, true, false));
            Schedule(finale + _warning + _move.Duration, () => ReleaseBurst(_origin, 3.2f));
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
