using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal sealed class BossAnimationDriver
    {
        private static readonly int DirectionId = Animator.StringToHash("Direction");
        private static readonly int MovingId = Animator.StringToHash("IsMoving");
        private static readonly int AttackingId = Animator.StringToHash("IsAttacking");
        private static readonly int HitId = Animator.StringToHash("IsHit");
        private static readonly int DeadId = Animator.StringToHash("IsDead");
        private static readonly int AttackTypeId = Animator.StringToHash("AttackType");
        private static readonly string[] Motions =
        {
            "idle",
            "walk",
            "hit",
            "death",
            "attack_melee",
            "attack_projectile",
            "attack_explosion"
        };
        private readonly Animator _animator;
        private readonly BossVisualPolish _visual;
        private readonly BossMotionUpgrade _motionUpgrade;
        private readonly int[,] _states = new int[7, 8];
        private readonly float[,] _lengths = new float[7, 8];
        private readonly Dictionary<string, float> _clipLengths = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
        private readonly bool _ready;
        private readonly bool _hasAttackType;
        private int _direction;
        private int _stage = -1;
        private int _hash;
        private int _attackType = 1;
        private float _hitUntil;
        private int _motionDirection = -1;
        private float _playStartedAt;
        private float _playNormalized;
        private float _playSpeed = 1f;
        private float _nextDirectionAt;
        private float _deathAge;
        private float _deathLength;
        private bool _moving;
        private bool _attacking;
        private bool _dead;
        private bool _looping;
        private bool _chargeHolding;
        private float _chargeBuildTime;
        private bool _attackHolding;
        private bool _pendingStrike;
        private int _strikeCount;
        private int _releaseStage = 2;
        private float _lastStrikeAt = -1000f;
        private float _patternAge;
        private float _patternWarning;
        private BossEncounterMove _activeMove;
        public bool IsHit => Time.time < _hitUntil;
        public bool IsAttacking => _attacking;

        public BossAnimationDriver(Animator animator)
        {
            _animator = animator;
            _visual = animator != null ? animator.GetComponentInParent<BossVisualPolish>() : null;
            _ready = MonsterCombat.ValidateAnimatorParameters(animator);
            if (!_ready)
                return;
            _motionUpgrade = BossMotionUpgrade.Install(_animator);
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == AttackTypeId && parameter.type == AnimatorControllerParameterType.Int)
                    _hasAttackType = true;
            }
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && !clip.empty)
                    _clipLengths[clip.name] = Mathf.Max(0.05f, clip.length);
            }
            if (_motionUpgrade != null)
                _motionUpgrade.CopyClipLengths(_clipLengths);
            for (int row = 0; row < Motions.Length; row++)
            {
                for (int direction = 0; direction < 8; direction++)
                    TryState(Motions[row], direction, out _states[row, direction], out _lengths[row, direction]);
            }
            if (_states[4, 7] == 0)
            {
                _states[4, 7] = _states[4, 6];
                _lengths[4, 7] = _lengths[4, 6];
            }
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.000001f || Time.time < _nextDirectionAt)
                return;
            int next = MonsterCombat.GetDirectionIndex(direction);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float center = _direction * 45f - 90f;
            if (next != _direction && Mathf.Abs(Mathf.DeltaAngle(center, angle)) > 29f)
            {
                _direction = next;
                _nextDirectionAt = Time.time + 0.09f;
            }
        }

        public void Locomotion(bool moving)
        {
            if (_dead || !_ready || IsHit)
                return;
            bool wasMoving = _moving;
            _moving = moving;
            _attacking = false;
            SetParameters();
            int hash = _states[moving ? 1 : 0, _direction];
            float normalized = wasMoving == moving ? Mathf.Repeat(CurrentPhase(), 1f) : 0f;
            _looping = true;
            Play(hash, moving ? 1f : 0.82f, false, normalized);
        }

        public void BeginPattern(BossEncounterMove move, float warning)
        {
            if (_dead || !_ready || move == null)
                return;
            if (_motionUpgrade != null)
            {
                _motionUpgrade.SelectPattern(move.Kind);
                _motionUpgrade.CopyClipLengths(_clipLengths);
            }
            _stage = -1;
            _chargeHolding = false;
            _attackHolding = false;
            _pendingStrike = false;
            _strikeCount = 0;
            _releaseStage = 2;
            _lastStrikeAt = -1000f;
            _activeMove = move;
            _chargeBuildTime = Mathf.Min(warning, 0.85f);
            _hitUntil = 0f;
            _attacking = true;
            _moving = false;
            _attackType = BossPatternCatalog.Animation(move.Kind);
            SetParameters();
            UpdatePattern(move, 0f, warning, warning + move.Duration, false);
        }

        public void UpdatePattern(BossEncounterMove move, float age, float warning, float finishAt, bool ending)
        {
            if (_dead || !_ready || move == null)
                return;
            _patternAge = age;
            _patternWarning = warning;
            int stage = ending ? 2 : age < warning ? 0 : 1;
            if (stage != _stage)
            {
                bool continueChannel = _motionUpgrade == null && _stage == 0 && _chargeHolding && stage == 1;
                _stage = stage;
                _chargeHolding = false;
                _attackHolding = false;
                _releaseStage = 2;
                if (_stage == 1 && _motionUpgrade != null && _motionUpgrade.HasPatternMotions)
                {
                    _motionUpgrade.SetStage(2);
                    _attackHolding = !_motionUpgrade.IsStageLooping;
                }
                if (!continueChannel)
                    PlayStage(move, warning);
                else if (_motionDirection != _direction)
                    PlayStage(move, warning, Mathf.Repeat(CurrentPhase(), 1f));
                if (_stage == 1 && _pendingStrike)
                {
                    _pendingStrike = false;
                    PulseStrike(move);
                }
            }
            else if (_motionDirection != _direction)
            {
                // Follow the player without restarting the current pose cycle.
                PlayStage(move, warning, _looping ? Mathf.Repeat(CurrentPhase(), 1f) : Mathf.Clamp01(CurrentPhase()));
            }

            if (_stage == 0 && !_chargeHolding && warning > _chargeBuildTime + 0.12f && age >= _chargeBuildTime)
            {
                // A five-second warning must not turn the windup into two frames/sec.
                // Finish the windup, then breathe in the ready pose until release.
                _chargeHolding = true;
                PlayStage(move, warning);
            }

            // Sustained casts breathe through a real loop instead of freezing a final frame.
            if ((_stage == 1 || _chargeHolding) && !_animator.IsInTransition(0))
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == _hash && !state.loop && state.normalizedTime >= 0.97f)
                {
                    if (_motionUpgrade != null && _motionUpgrade.HasPatternMotions && _stage == 1 && !_attackHolding)
                    {
                        _attackHolding = true;
                        PlayStage(move, warning);
                    }
                    else if (_looping)
                    {
                        Play(_hash, 1f, true);
                    }
                    else
                    {
                        Play(_states[0, _direction], 0.88f, false);
                    }
                }
            }

            SetParameters();
            UpdateVisualMotion();
        }

        private void PlayStage(BossEncounterMove move, float warning, float normalized = 0f)
        {
            if (_motionUpgrade != null)
            {
                int motionStage = _stage == 2 ? 3 : _chargeHolding || _attackHolding ? 1
                    : _stage == 0 ? 0 : _releaseStage;
                _motionUpgrade.SetStage(motionStage);
                _motionUpgrade.CopyClipLengths(_clipLengths);
            }
            string motion = _stage == 0 && !_chargeHolding ? move.AnimationMotion : _stage == 1 || _chargeHolding ? move.LoopMotion : move.EndMotion;
            bool custom = TryState(motion, _direction, out int hash, out float length);
            if (!custom)
            {
                int row = _stage == 1 || _chargeHolding ? 0 : 3 + _attackType;
                hash = _states[row, _direction];
                length = _lengths[row, _direction];
            }

            _looping = _stage == 1 || _chargeHolding;
            if (_motionUpgrade != null && _motionUpgrade.HasPatternMotions)
                _looping = _motionUpgrade.IsStageLooping;
            _motionDirection = _direction;
            float speed = _stage == 0 && !_chargeHolding ? length / Mathf.Max(0.1f, _chargeBuildTime) : _stage == 2 ? length / 0.34f : 1f;
            Play(hash, Mathf.Clamp(speed, 0.08f, 5f), true, normalized);
        }

        public void EndPattern()
        {
            _stage = -1;
            _chargeHolding = false;
            _attackHolding = false;
            _pendingStrike = false;
            _activeMove = null;
            _attacking = false;
            _looping = false;
            if (_visual != null)
                _visual.ClearPatternMotion();
            Locomotion(false);
        }

        public void PulseStrike(BossEncounterMove move)
        {
            if (_dead || !_ready || !_attacking || move == null || _activeMove == null
                || move.Kind != _activeMove.Kind || _stage == 2)
            {
                return;
            }

            if (_stage == 0)
            {
                _pendingStrike = true;
                return;
            }
            if (_stage != 1 || Time.time < _lastStrikeAt + 0.18f)
                return;
            _lastStrikeAt = Time.time;
            _strikeCount++;
            if (_motionUpgrade != null && _motionUpgrade.HasPatternMotions)
            {
                _motionUpgrade.SetStage(2);
                // A beam or orbit continues its cast; discrete strikes alternate
                // release and follow-through poses at the actual attack events.
                if (!_motionUpgrade.IsStageLooping)
                {
                    _attackHolding = false;
                    _releaseStage = (_strikeCount & 1) == 0 ? 4 : 2;
                    PlayStage(move, _patternWarning);
                }
            }
            UpdateVisualMotion();
        }

        private void UpdateVisualMotion()
        {
            if (_visual == null || _activeMove == null)
                return;
            float angle = (_direction * 45f - 90f) * Mathf.Deg2Rad;
            _visual.PatternMotion(_activeMove.Kind, _stage, _chargeHolding || _attackHolding,
                _patternAge, _patternWarning, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                _strikeCount, _lastStrikeAt);
        }

        public void Hit()
        {
            if (!_ready || _dead || IsHit || _attacking)
                return;
            const float reaction = 0.24f;
            _hitUntil = Time.time + reaction;
            _moving = false;
            SetParameters();
            Play(_states[2, _direction], _lengths[2, _direction] / reaction, true);
        }

        public void Phase()
        {
            _activeMove = null;
            _pendingStrike = false;
            if (_visual != null)
                _visual.ClearPatternMotion();
            if (_motionUpgrade != null)
            {
                _motionUpgrade.SelectPhase();
                _motionUpgrade.CopyClipLengths(_clipLengths);
            }
            _hitUntil = 0f;
            _attacking = true;
            _moving = false;
            _attackType = 3;
            SetParameters();
            Play(_states[6, _direction], _lengths[6, _direction] / 1.2f, true);
        }

        public void Death()
        {
            _dead = true;
            _activeMove = null;
            _pendingStrike = false;
            if (_visual != null)
                _visual.ClearPatternMotion();
            _hitUntil = 0f;
            _moving = false;
            _attacking = false;
            _deathAge = 0f;
            _deathLength = Mathf.Clamp(_lengths[3, _direction], 0.1f, 6f);
            SetParameters();
            Play(_states[3, _direction], 1f, true);
        }

        public void TickDeath(float deltaTime)
        {
            if (!_ready || _deathAge > _deathLength)
                return;
            _deathAge += deltaTime;
            if (_deathAge < _deathLength)
                return;
            _animator.Play(_states[3, _direction], 0, 1f);
            _animator.Update(0f);
            _animator.speed = 0f;
            _deathAge = _deathLength + 1f;
        }

        private bool TryState(string motion, int direction, out int hash, out float length)
        {
            hash = 0;
            length = 1f;
            if (!_ready || string.IsNullOrEmpty(motion))
                return false;
            string name = "BossMonster_" + motion + "_" + MonsterCombat.DirectionNames[direction];
            return _clipLengths.TryGetValue(name, out length) && MonsterCombat.TryGetDirectionalState(_animator, "BossMonster", motion, direction, out hash);
        }

        private void SetParameters()
        {
            if (!_ready)
                return;
            _animator.SetInteger(DirectionId, _direction);
            if (_hasAttackType)
                _animator.SetInteger(AttackTypeId, _attackType);
            _animator.SetBool(MovingId, _moving);
            _animator.SetBool(AttackingId, _attacking && !_dead);
            _animator.SetBool(HitId, IsHit && !_dead);
            _animator.SetBool(DeadId, _dead);
        }

        private void Play(int hash, float speed, bool restart, float normalized = 0f)
        {
            if (!_ready || hash == 0)
                return;
            speed = Mathf.Max(0.05f, speed);
            if (restart || _hash != hash || (!_animator.IsInTransition(0) && _animator.GetCurrentAnimatorStateInfo(0).fullPathHash != hash))
            {
                if (_visual != null)
                    _visual.BlendMotion(_dead ? 0.035f : 0.065f);
                _hash = hash;
                _playNormalized = normalized;
                _playStartedAt = Time.time;
                _playSpeed = speed;
                // Sprite interpolation is performed by BossVisualPolish; this also
                // blends the continuous transform curves in the generated controller.
                _animator.CrossFadeInFixedTime(hash, (_dead ? 0.045f : 0.075f) * speed, 0, normalized * FindLength(hash));
            }
            else if (Mathf.Abs(_playSpeed - speed) > 0.0001f)
            {
                _playNormalized = CurrentPhase();
                _playStartedAt = Time.time;
                _playSpeed = speed;
            }

            _animator.speed = speed;
        }

        private float CurrentPhase()
        {
            return _playNormalized + Mathf.Max(0f, Time.time - _playStartedAt) * _playSpeed / Mathf.Max(0.05f, FindLength(_hash));
        }

        private float FindLength(int hash)
        {
            // Override clip lengths change for every pattern and release pose.
            // Consult the current bindings before the constructor's fallback cache.
            foreach (KeyValuePair<string, float> clip in _clipLengths)
            {
                if (Animator.StringToHash("Base Layer." + clip.Key) == hash)
                    return clip.Value;
            }
            for (int row = 0; row < 7; row++)
            {
                for (int d = 0; d < 8; d++)
                {
                    if (_states[row, d] == hash)
                        return _lengths[row, d];
                }
            }
            return 1f;
        }
    }
}
