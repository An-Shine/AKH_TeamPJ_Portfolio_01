using System;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal sealed class BossEncounterDirector : IDisposable
    {
        private readonly global::BossMonster _owner;
        private readonly BossEncounterProfile _profile;
        private readonly BossBattleEffects _effects;
        private readonly BossPatternAudio _audio;
        private readonly BossBattleWorld _world;
        private readonly float[] _readyTimes;
        private readonly bool[] _introduced;
        private BossEncounterMove _move;
        private BossPatternExecutor _execution;
        private BossShapeVisual _charge;
        private int _index = -1;
        private int _lastIndex = -1;
        private int _phase;
        private float _warning;
        private float _nextAttack;
        private float _phaseUntil;
        private bool _wasInside;
        private bool _retired;
        private bool _ending;

        public bool IsCasting => _move != null || Time.time < _phaseUntil;
        public int Phase => _phase + 1;
        public string PatternName => Time.time < _phaseUntil ? "AWAKENING"
            : _move == null ? string.Empty : BossPatternCatalog.Name(_move.Kind);

        public BossEncounterDirector(global::BossMonster owner, BossEncounterProfile profile, float initialDelay)
        {
            _owner = owner;
            _profile = profile;
            _effects = BossBattleEffects.Create(owner.gameObject, profile);
            _audio = BossPatternAudio.Create(owner.gameObject, profile);
            _world = new BossBattleWorld(owner, profile, _effects, _audio);
            _readyTimes = new float[profile.Moves.Length];
            _introduced = new bool[profile.Moves.Length];
            _nextAttack = Time.time + Mathf.Max(0f, initialDelay);
        }

        public void Tick(float deltaTime)
        {
            if (_retired || deltaTime <= 0f)
            {
                return;
            }
            if (!_owner.CanFight)
            {
                if (_wasInside)
                {
                    Cancel();
                    _world.Clear();
                    _nextAttack = Time.time + 1f;
                }
                _wasInside = false;
                return;
            }
            _wasInside = true;
            float health = _owner.CurrentHealth / _owner.MaximumHealth;
            int phase = health <= _profile.ThirdPhaseHealth ? 2 : health <= _profile.SecondPhaseHealth ? 1 : 0;
            if (phase > _phase)
            {
                BeginPhase(phase);
            }
            if (Time.time < _phaseUntil)
            {
                return;
            }
            _world.Tick(deltaTime);
            if (_move == null)
            {
                if (Time.time >= _nextAttack && _owner.CanStartPattern && SelectMove(out int index))
                {
                    Begin(index);
                }
                return;
            }
            _execution.Tick(deltaTime);
            float remaining = _world.RemainingDuration;
            float finishAt = Mathf.Max(_execution.PlannedEnd, _execution.Age + remaining);
            _owner.UpdatePatternAnimation(_move, _execution.Age, _warning, finishAt, _ending);
            UpdateCharge();
            if (_execution.Age >= _warning && !_ending)
            {
                _owner.SetCameraSustain(_move.SustainShake);
            }
            // 마지막 낙하나 공전 탄환까지 끝나야 종료음과 다음 패턴으로 넘어갑니다.
            bool releasedAll = !_execution.HasPendingEvents && _execution.Age >= _execution.PlannedEnd;
            if (!_ending && releasedAll && remaining <= 0.22f)
            {
                _ending = true;
                _audio.End(true);
                _owner.SetCameraSustain(0f);
            }
            if (releasedAll && remaining <= 0f)
            {
                Finish();
            }
        }

        private bool SelectMove(out int index)
        {
            index = -1;
            BossEncounterMove[] moves = _profile.Moves;
            if (_profile.IntroducePatterns)
            {
                for (int i = 0; i < moves.Length; i++)
                {
                    if (!_introduced[i] && IsAvailable(i))
                    {
                        index = i;
                        return true;
                    }
                }
            }
            bool alternativeKind = false;
            for (int i = 0; i < moves.Length; i++)
            {
                if (IsAvailable(i) && (_lastIndex < 0 || moves[i].Kind != moves[_lastIndex].Kind))
                {
                    alternativeKind = true;
                    break;
                }
            }
            float weight = 0f;
            for (int i = 0; i < moves.Length; i++)
            {
                if (IsCandidate(i, alternativeKind))
                {
                    weight += moves[i].Weight;
                }
            }
            if (weight <= 0f)
            {
                return false;
            }
            float choice = UnityEngine.Random.value * weight;
            for (int i = 0; i < moves.Length; i++)
            {
                if (!IsCandidate(i, alternativeKind))
                {
                    continue;
                }
                index = i;
                choice -= moves[i].Weight;
                if (choice <= 0f)
                {
                    break;
                }
            }
            return index >= 0;
        }

        private bool IsCandidate(int index, bool alternativeKind)
        {
            return IsAvailable(index) && (!alternativeKind || _lastIndex < 0 ||
                _profile.Moves[index].Kind != _profile.Moves[_lastIndex].Kind);
        }

        private bool IsAvailable(int index)
        {
            if (index < 0 || index >= _profile.Moves.Length)
            {
                return false;
            }
            BossEncounterMove move = _profile.Moves[index];
            return move != null && move.Enabled && BossPatternCatalog.IsValid(move.Kind) &&
                move.MinimumPhase <= Phase && Time.time >= _readyTimes[index];
        }

        private void Begin(int index)
        {
            _index = index;
            _lastIndex = index;
            _introduced[index] = true;
            _move = _profile.Moves[index];
            _warning = Mathf.Max(0.65f, _move.Warning * Mathf.Pow(_profile.PhaseWarningMultiplier, _phase));
            _ending = false;
            _audio.Begin(_move);
            _owner.StartPatternAnimation(_move, _warning);
            _charge = _effects.CreateShape("BossPatternCharge", true, 2);
            float damage = _move.Damage * (1f + _phase * _profile.PhaseDamageBonus);
            _execution = new BossPatternExecutor(_owner, _move, _world, _effects, _audio, _phase, _warning, damage);
        }

        private void UpdateCharge()
        {
            if (_charge == null)
            {
                return;
            }
            if (_execution.Age >= _warning)
            {
                _charge.Dispose();
                _charge = null;
                return;
            }
            float radius = Mathf.Max(0.65f, _owner.BodyWidth * 0.45f);
            Color color = _move.Color;
            color.a = 0.3f + _execution.Age / _warning * 0.7f;
            _charge.SetArc(_owner.transform.position, radius - 0.08f, radius + 0.08f,
                _execution.Age * 180f, 300f, color);
        }

        private void BeginPhase(int phase)
        {
            Cancel();
            _world.Clear();
            _phase = phase;
            _phaseUntil = Time.time + 1.2f;
            _nextAttack = _phaseUntil + 0.4f;
            _effects.Burst(_owner.transform.position, 3.1f, new Color(0.8f, 0.18f, 1f, 1f), 56);
            _audio.Play(_profile.PhaseSound, 1f);
            _owner.ShakeCamera(0.28f, 0.7f);
            _owner.StartPhaseAnimation();
        }

        private void Finish()
        {
            _readyTimes[_index] = Time.time + _move.Cooldown;
            _nextAttack = Time.time + _move.Recovery / (1f + _phase * 0.1f);
            Cancel();
            _owner.EndPatternAnimation();
        }

        public void Cancel()
        {
            if (_move != null && !_ending)
            {
                _audio.End(true);
            }
            if (_execution != null)
            {
                _execution.Dispose();
                _execution = null;
            }
            if (_charge != null)
            {
                _charge.Dispose();
                _charge = null;
            }
            _move = null;
            _index = -1;
            _owner.SetCameraSustain(0f);
        }

        public void Stop(bool death)
        {
            if (_retired)
            {
                return;
            }
            Cancel();
            _world.Clear();
            if (death)
            {
                _effects.Burst(_owner.transform.position, 3.8f, new Color(1f, 0.18f, 0.5f, 1f), 64);
                _audio.Play(_profile.DeathSound, 1f);
                _owner.ShakeCamera(0.35f, 0.75f);
            }
            _retired = true;
            _audio.Retire();
            _effects.Retire();
        }

        public void Dispose()
        {
            Stop(false);
            _world.Dispose();
        }
    }
}
