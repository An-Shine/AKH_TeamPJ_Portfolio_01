using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Monster/Monster Ground Presentation")]
    public sealed class MonsterGroundPresentation : MonoBehaviour
    {
        private static readonly int MovingId = Animator.StringToHash("IsMoving");
        private static readonly int AttackingId = Animator.StringToHash("IsAttacking");
        private static readonly int HitId = Animator.StringToHash("IsHit");
        private static readonly int DeadId = Animator.StringToHash("IsDead");
        [SerializeField]
        private MonsterGroundProfile _profile;
        [SerializeField]
        private SpriteRenderer _bodyRenderer;
        [SerializeField]
        private Animator _animator;
        [Tooltip("선택 항목입니다. 비워 두면 프로필의 Ground Offset Factor로 발 위치를 계산합니다.")]
        [SerializeField]
        private Transform _feetAnchor;
        private NormalMonster _monster;
        private global::BossMonster _boss;
        private MonsterGroundEffects _effects;
        private SpriteRenderer _shadowRenderer;
        private Vector3 _previousPosition;
        private Vector2 _direction = Vector2.down;
        private Vector2 _referenceSize;
        private Vector3 _referenceCenter;
        private float _distanceSinceStep;
        private float _shadowPhase;
        private int _previousSoundIndex = -1;
        private bool _leftFoot = true;
        private bool _hasMovingParameter;
        private bool _hasAttackingParameter;
        private bool _hasHitParameter;
        private bool _hasDeadParameter;
        private bool _ready;
        private void Start()
        {
            if (_profile == null)
            {
                Debug.LogWarning($"{name}: Monster Ground Profile을 연결하세요.", this);
                return;
            }

            _monster = GetComponent<NormalMonster>();
            _boss = GetComponent<global::BossMonster>();
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if (_bodyRenderer == null || _bodyRenderer.sprite == null)
            {
                Debug.LogWarning($"{name}: 본체 SpriteRenderer와 Sprite가 필요합니다.", this);
                return;
            }

            Vector3 scale = _bodyRenderer.transform.lossyScale;
            Vector3 spriteSize = _bodyRenderer.sprite.bounds.size;
            _referenceCenter = _bodyRenderer.sprite.bounds.center;
            _referenceSize = new Vector2(Mathf.Max(0.1f, spriteSize.x * Mathf.Abs(scale.x)), Mathf.Max(0.1f, spriteSize.y * Mathf.Abs(scale.y)));
            CacheAnimatorParameters();
            _effects = MonsterGroundEffects.ForScene(gameObject.scene);
            _shadowRenderer = _effects.CreateShadow(name);
            _previousPosition = transform.position;
            _ready = true;
            RefreshShadow(IsDead());
        }

        private void OnEnable()
        {
            _previousPosition = transform.position;
            _distanceSinceStep = 0f;
        }

        private void CacheAnimatorParameters()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Bool)
                {
                    continue;
                }

                _hasMovingParameter |= parameter.nameHash == MovingId;
                _hasAttackingParameter |= parameter.nameHash == AttackingId;
                _hasHitParameter |= parameter.nameHash == HitId;
                _hasDeadParameter |= parameter.nameHash == DeadId;
            }
        }

        private void LateUpdate()
        {
            if (!_ready || _profile == null || _bodyRenderer == null || _effects == null)
            {
                return;
            }

            Vector3 position = transform.position;
            if (_bodyRenderer.sprite == null)
            {
                if (_shadowRenderer != null)
                {
                    _shadowRenderer.enabled = false;
                }

                _previousPosition = position;
                return;
            }

            Vector2 movement = (Vector2)(position - _previousPosition);
            bool dead = IsDead();
            RefreshShadow(dead);
            if (dead || Time.timeScale <= 0f || !CanWalk() || movement.sqrMagnitude < 0.000001f)
            {
                if (dead || !CanWalk())
                {
                    _distanceSinceStep = 0f;
                }

                _previousPosition = position;
                return;
            }

            float distance = movement.magnitude;
            if (distance > Mathf.Max(2f, _referenceSize.x * 2f))
            {
                // 방 이동이나 순간 배치 변경은 발걸음으로 처리하지 않습니다.
                _distanceSinceStep = 0f;
                _previousPosition = position;
                return;
            }

            _direction = movement / distance;
            if (!_profile.UseAnimationEvents)
            {
                EmitStepsAlongMovement(position, distance);
            }

            _previousPosition = position;
        }

        private void EmitStepsAlongMovement(Vector3 position, float distance)
        {
            float stepDistance = Mathf.Max(0.05f, _referenceSize.x * _profile.StepDistanceFactor);
            float remaining = distance;
            float travelled = 0f;
            int emitted = 0;
            Vector3 groundOffset = GroundPosition() - position;
            while (remaining > 0f && emitted < 3)
            {
                float toNextStep = Mathf.Max(0.001f, stepDistance - _distanceSinceStep);
                if (remaining < toNextStep)
                {
                    _distanceSinceStep += remaining;
                    break;
                }

                remaining -= toNextStep;
                travelled += toNextStep;
                _distanceSinceStep = 0f;
                Vector3 stepCenter = Vector3.Lerp(_previousPosition, position, travelled / distance) + groundOffset;
                EmitStep(_leftFoot, stepCenter);
                _leftFoot = !_leftFoot;
                emitted++;
            }

            if (emitted == 3 && remaining > 0f)
            {
                _distanceSinceStep = Mathf.Min(stepDistance, remaining);
            }
        }

        // Animator가 붙은 몬스터 루트의 Animation Event에서 선택할 수 있습니다.
        public void FootstepLeft()
        {
            EmitAnimationStep(true);
        }

        public void FootstepRight()
        {
            EmitAnimationStep(false);
        }

        private void EmitAnimationStep(bool left)
        {
            if (!_ready || _profile == null || !_profile.UseAnimationEvents || _effects == null || _bodyRenderer == null || !isActiveAndEnabled || Time.timeScale <= 0f || IsDead() || !CanWalk())
            {
                return;
            }

            Vector2 movement = (Vector2)(transform.position - _previousPosition);
            if (movement.sqrMagnitude > 0.000001f)
            {
                _direction = movement.normalized;
            }

            EmitStep(left, GroundPosition());
        }

        private void EmitStep(bool left, Vector3 center)
        {
            Vector2 side = new Vector2(-_direction.y, _direction.x);
            float separation = _referenceSize.x * _profile.FootSeparationFactor;
            Vector2 offset = side * (left ? separation : -separation);
            Vector3 position = center + new Vector3(offset.x, offset.y, 0f);
            int sortingLayer = _bodyRenderer.sortingLayerID;
            int order = _bodyRenderer.sortingOrder;
            int layer = gameObject.layer;
            if (_profile.ShowFootprints)
            {
                _effects.Footprint(position, _direction, _referenceSize.x * _profile.FootprintWidthFactor, _profile.FootprintColor, _profile.FootprintLifetime, layer, sortingLayer, order + _profile.FootprintOrderOffset);
            }

            if (_profile.ShowDust)
            {
                _effects.Dust(position, _direction, _referenceSize.x * _profile.DustWidthFactor, _profile.DustColor, _profile.DustLifetime, _profile.DustPuffsPerStep, layer, sortingLayer, order + _profile.DustOrderOffset);
            }

            AudioClip sound = ChooseStepSound();
            _effects.PlayStep(sound, position, _profile);
            _shadowPhase = 1f;
        }

        private AudioClip ChooseStepSound()
        {
            AudioClip[] sounds = _profile.StepSounds;
            if (sounds == null || sounds.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, sounds.Length);
            if (sounds.Length > 1 && index == _previousSoundIndex)
            {
                index = (index + Random.Range(1, sounds.Length)) % sounds.Length;
            }

            for (int i = 0; i < sounds.Length; i++)
            {
                int candidate = (index + i) % sounds.Length;
                if (sounds[candidate] != null)
                {
                    _previousSoundIndex = candidate;
                    return sounds[candidate];
                }
            }

            return null;
        }

        private Vector3 GroundPosition()
        {
            if (_feetAnchor != null)
            {
                return _feetAnchor.position + Vector3.forward * 0.01f;
            }

            Vector3 center = _bodyRenderer.transform.TransformPoint(_referenceCenter);
            center.y -= _referenceSize.y * _profile.GroundOffsetFactor;
            center.z = transform.position.z + 0.01f;
            return center;
        }

        private bool CanWalk()
        {
            if (!_bodyRenderer.enabled || _bodyRenderer.sprite == null || !_bodyRenderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            // Boss dashes move during a cast; actual distance still controls footsteps.
            // Burrowing and teleport placement are filtered by visibility/distance above.
            if (_boss != null)
                return _boss.CurrentHealth > 0f;
            if (_animator == null || !_animator.isActiveAndEnabled)
            {
                return true;
            }

            return (!_hasMovingParameter || _animator.GetBool(MovingId)) && (!_hasAttackingParameter || !_animator.GetBool(AttackingId)) && (!_hasHitParameter || !_animator.GetBool(HitId));
        }

        private bool IsDead()
        {
            // 보스는 공통 IsDead 대신 자체 사망 처리와 CurrentHealth를 사용합니다.
            return (_boss != null && _boss.CurrentHealth <= 0f) || (_monster != null && _monster.IsDead) || (_animator != null && _hasDeadParameter && _animator.GetBool(DeadId));
        }

        private void RefreshShadow(bool dead)
        {
            if (_shadowRenderer == null)
            {
                return;
            }

            _shadowRenderer.enabled = _profile.ShowShadow && _bodyRenderer.enabled && _bodyRenderer.gameObject.activeInHierarchy && _bodyRenderer.sprite != null;
            if (!_shadowRenderer.enabled)
            {
                return;
            }

            _shadowPhase = Mathf.MoveTowards(_shadowPhase, 0f, Time.deltaTime * 5f);
            float pulse = 1f + _shadowPhase * _profile.ShadowPulse;
            Color color = _profile.ShadowColor;
            if (dead)
            {
                color.a *= _profile.DeadShadowOpacity;
            }

            _shadowRenderer.gameObject.layer = gameObject.layer;
            _shadowRenderer.sortingLayerID = _bodyRenderer.sortingLayerID;
            _shadowRenderer.sortingOrder = _bodyRenderer.sortingOrder + _profile.ShadowOrderOffset;
            _shadowRenderer.color = color;
            _shadowRenderer.transform.position = GroundPosition();
            _shadowRenderer.transform.localScale = new Vector3(_referenceSize.x * _profile.ShadowWidthFactor * pulse, _referenceSize.x * _profile.ShadowHeightFactor / pulse, 1f);
        }

        private void OnDisable()
        {
            if (_shadowRenderer != null)
            {
                _shadowRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_shadowRenderer != null)
            {
                Destroy(_shadowRenderer.gameObject);
            }
        }
    }
}
