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
            "idle", "walk", "hit", "death", "attack_melee", "attack_projectile", "attack_explosion"
        };

        private readonly Animator _animator;
        private readonly int[,] _states = new int[7, 8];
        private readonly float[,] _lengths = new float[7, 8];
        private readonly bool _ready;
        private readonly bool _hasAttackType;
        private int _direction;
        private int _stage = -1;
        private int _hash;
        private int _attackType = 1;
        private float _hitUntil;
        private float _deathAge;
        private float _deathLength;
        private bool _moving;
        private bool _attacking;
        private bool _dead;
        private bool _looping;

        public bool IsHit => Time.time < _hitUntil;
        public bool IsAttacking => _attacking;

        public BossAnimationDriver(Animator animator)
        {
            _animator = animator;
            _ready = MonsterCombat.ValidateAnimatorParameters(animator);
            if (!_ready)
            {
                return;
            }
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == AttackTypeId && parameter.type == AnimatorControllerParameterType.Int)
                {
                    _hasAttackType = true;
                }
            }
            for (int row = 0; row < Motions.Length; row++)
            {
                for (int direction = 0; direction < 8; direction++)
                {
                    TryState(Motions[row], direction, out _states[row, direction], out _lengths[row, direction]);
                }
            }
            // 업로드된 SW 근접 클립은 비어 있습니다. 새 SW 프레임을 넣을 때까지 W 상태를 사용합니다.
            if (_states[4, 7] == 0)
            {
                _states[4, 7] = _states[4, 6];
                _lengths[4, 7] = _lengths[4, 6];
            }
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.000001f)
            {
                _direction = MonsterCombat.GetDirectionIndex(direction);
            }
        }

        public void Locomotion(bool moving)
        {
            if (_dead || !_ready || IsHit)
            {
                return;
            }
            _moving = moving;
            _attacking = false;
            SetParameters();
            int hash = _states[moving ? 1 : 0, _direction];
            Play(hash, moving ? 1f : 0.8f, false);
        }

        public void BeginPattern(BossEncounterMove move, float warning)
        {
            _stage = -1;
            _hitUntil = 0f;
            _attacking = true;
            _moving = false;
            _attackType = GetAttackType(move.Kind);
            UpdatePattern(move, 0f, warning, warning + move.Duration, false);
        }

        public void UpdatePattern(BossEncounterMove move, float age, float warning, float finishAt, bool ending)
        {
            if (_dead || !_ready)
            {
                return;
            }
            int stage = ending ? 2 : age < warning ? 0 : 1;
            if (stage != _stage)
            {
                _stage = stage;
                string motion = stage == 2 ? move.EndMotion : stage == 1 ? move.LoopMotion : move.AnimationMotion;
                bool custom = TryState(motion, _direction, out int hash, out float length);
                if (!custom)
                {
                    int row = 3 + _attackType;
                    hash = _states[row, _direction];
                    length = _lengths[row, _direction];
                }
                _looping = stage == 1 && custom;
                if (stage == 0)
                {
                    Play(hash, Mathf.Max(0.05f, length * move.ReleaseNormalizedTime / warning), true);
                }
                else if (custom)
                {
                    float speed = stage == 2 ? length / Mathf.Max(0.15f, finishAt - age) : 1f;
                    Play(hash, speed, true);
                }
                else
                {
                    if (_animator.GetCurrentAnimatorStateInfo(0).fullPathHash != hash)
                    {
                        _hash = hash;
                        _animator.Play(hash, 0, move.ReleaseNormalizedTime);
                    }
                    _animator.speed = Mathf.Max(0.1f, length * (1f - move.ReleaseNormalizedTime) / 0.3f);
                }
                SetParameters();
            }
            if (_looping)
            {
                AnimatorStateInfo loop = _animator.GetCurrentAnimatorStateInfo(0);
                if (loop.fullPathHash == _hash && !loop.loop && loop.normalizedTime >= 1f)
                {
                    _animator.Play(_hash, 0, 0f);
                }
            }
            else if (_stage >= 1)
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == _hash && state.normalizedTime >= 0.97f)
                {
                    _animator.speed = 0f;
                }
            }
            if (_animator.IsInTransition(0))
            {
                // 설치 도구 적용 전의 기존 Controller에서도 공격 자세를 다른 Any State가 덮지 않게 합니다.
                _animator.Play(_hash, 0, Mathf.Clamp01(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime));
            }
        }

        public void EndPattern()
        {
            _stage = -1;
            _attacking = false;
            Locomotion(false);
        }

        public void PulseStrike(BossEncounterMove move)
        {
            if (!_ready || _dead || !_attacking || _looping)
            {
                return;
            }
            if (!TryState(move.AnimationMotion, _direction, out int hash, out float length))
            {
                int row = 3 + GetAttackType(move.Kind);
                hash = _states[row, _direction];
                length = _lengths[row, _direction];
            }
            if (hash != 0)
            {
                _hash = hash;
                _animator.Play(hash, 0, move.ReleaseNormalizedTime);
                _animator.speed = Mathf.Max(0.1f, length * (1f - move.ReleaseNormalizedTime) / 0.3f);
            }
        }

        public void Hit()
        {
            if (!_ready || _dead)
            {
                return;
            }
            _hitUntil = Time.time + Mathf.Clamp(_lengths[2, _direction], 0.15f, 2f);
            _moving = false;
            _attacking = false;
            SetParameters();
            Play(_states[2, _direction], 1f, true);
        }

        public void Phase()
        {
            _hitUntil = 0f;
            _attacking = true;
            _moving = false;
            _attackType = 3;
            SetParameters();
            Play(_states[6, _direction], 1f, true);
        }

        public void Death()
        {
            _dead = true;
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
            {
                return;
            }
            _deathAge += deltaTime;
            if (_deathAge >= _deathLength)
            {
                _animator.Play(_states[3, _direction], 0, 1f);
                _animator.Update(0f);
                _animator.speed = 0f;
                _deathAge = _deathLength + 1f;
            }
        }

        private bool TryState(string motion, int direction, out int hash, out float length)
        {
            hash = 0;
            length = 1f;
            if (!_ready || string.IsNullOrEmpty(motion) || !MonsterCombat.TryGetDirectionalState(
                _animator, "BossMonster", motion, direction, out hash))
            {
                return false;
            }
            string name = "BossMonster_" + motion + "_" + MonsterCombat.DirectionNames[direction];
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (string.Equals(clip.name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (clip.empty)
                    {
                        hash = 0;
                        return false;
                    }
                    length = Mathf.Max(0.05f, clip.length);
                    break;
                }
            }
            return true;
        }

        private void SetParameters()
        {
            if (!_ready)
            {
                return;
            }
            _animator.SetInteger(DirectionId, _direction);
            if (_hasAttackType)
            {
                _animator.SetInteger(AttackTypeId, _attackType);
            }
            _animator.SetBool(MovingId, _moving);
            _animator.SetBool(AttackingId, _attacking && !_dead);
            _animator.SetBool(HitId, IsHit && !_dead);
            _animator.SetBool(DeadId, _dead);
        }

        private void Play(int hash, float speed, bool restart)
        {
            if (!_ready || hash == 0)
            {
                return;
            }
            _animator.speed = speed;
            if (restart || _animator.GetCurrentAnimatorStateInfo(0).fullPathHash != hash)
            {
                _hash = hash;
                _animator.Play(hash, 0, 0f);
            }
        }

        private static int GetAttackType(BossEncounterPattern kind)
        {
            return BossPatternCatalog.Animation(kind);
        }
    }
}
