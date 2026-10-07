using UnityEngine;

namespace _02.Scripts.Monster.NormalMonsters
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class MonsterRoomBounds : MonoBehaviour
    {
        private const float MinimumScale = 0.0001f;

        [Header("몬스터가 머무를 방 범위")]
        [SerializeField] private Vector2 _center;
        [SerializeField] private Vector2 _size = new Vector2(12f, 8f);
        [Tooltip("방 가장자리에서 몬스터를 떨어뜨릴 추가 거리입니다. 월드 단위입니다.")]
        [SerializeField, Min(0f)] private float _edgePadding = 0.35f;

        [Header("방 진입 감지")]
        [Tooltip("아이템 방 스포너가 켭니다. 연결한 Area 안에 플레이어가 있을 때 추적을 시작합니다.")]
        [SerializeField] private bool _waitForPlayerEntry;
        [Tooltip("아이템 방 전체를 감싸는 감지용 Collider2D입니다. 이동용 벽 콜라이더를 연결하지 마세요.")]
        [SerializeField] private Collider2D _activationArea;
        private Transform _cachedPlayer;
        private Collider2D[] _playerColliders;
        private bool _playerInside;
        private int _detectionFrame = -1;
        public bool IsConfigured => TryGetHalfSize(0f, out _);
        public bool RequiresPlayerEntry => _waitForPlayerEntry;
        public Collider2D ActivationArea => _activationArea;
        public Vector2 LocalCenter => _center;
        public Vector2 LocalSize => _size;
        private void OnEnable()
        {
            _detectionFrame = -1;
        }

        private void OnDisable()
        {
            _playerInside = false;
            _detectionFrame = -1;
        }

        private void Update()
        {
            if (_waitForPlayerEntry)
            {
                RefreshPlayerDetection();
            }
        }

        public void EnablePlayerEntryDetection()
        {
            if (_waitForPlayerEntry)
            {
                return;
            }

            _waitForPlayerEntry = true;
            _detectionFrame = -1;
        }

        public void SetActivationArea(Collider2D area)
        {
            _activationArea = area;
            _detectionFrame = -1;
        }

        public bool TryGetDetectedPlayer(out Transform player)
        {
            player = null;
            if (!isActiveAndEnabled)
            {
                return false;
            }

            RefreshPlayerDetection();
            if (!_playerInside)
            {
                return false;
            }

            player = _cachedPlayer;
            return player != null && player.gameObject.activeInHierarchy;
        }

        private void RefreshPlayerDetection()
        {
            if (_detectionFrame == Time.frameCount)
            {
                return;
            }

            _detectionFrame = Time.frameCount;
            if (_cachedPlayer == null || !_cachedPlayer.gameObject.activeInHierarchy)
            {
                Transform candidate = MonsterCombat.FindPlayer(transform);
                if (candidate != null && candidate.GetComponentInParent<NormalMonster>() != null)
                {
                    candidate = null;
                }

                _cachedPlayer = candidate;
                _playerColliders = candidate != null ? candidate.GetComponentsInChildren<Collider2D>(true) : null;
            }

            _playerInside = _cachedPlayer != null && ContainsPlayerBody();
        }

        private bool ContainsPlayerBody()
        {
            if (_activationArea == null)
            {
                // 감지에는 이동용 가장자리 여백을 적용하지 않습니다.
                Vector3 scale = transform.lossyScale;
                if (Mathf.Abs(scale.x) <= MinimumScale || Mathf.Abs(scale.y) <= MinimumScale || _size.x <= 0f || _size.y <= 0f)
                {
                    return false;
                }

                Vector2 localPoint = transform.InverseTransformPoint(_cachedPlayer.position);
                Vector2 offset = localPoint - _center;
                return Mathf.Abs(offset.x) <= _size.x * 0.5f && Mathf.Abs(offset.y) <= _size.y * 0.5f;
            }

            if (!_activationArea.enabled || !_activationArea.gameObject.activeInHierarchy)
            {
                return false;
            }

            bool preferBody = MonsterCombat.HasEnabledBodyCollider(_playerColliders);
            bool hasActiveCollider = false;
            if (_playerColliders != null)
            {
                foreach (Collider2D bodyCollider in _playerColliders)
                {
                    if (bodyCollider == null || !bodyCollider.enabled || !bodyCollider.gameObject.activeInHierarchy || (preferBody && bodyCollider.isTrigger))
                    {
                        continue;
                    }

                    Rigidbody2D body = bodyCollider.attachedRigidbody;
                    if (body != null && !body.simulated)
                    {
                        continue;
                    }

                    hasActiveCollider = true;
                    ColliderDistance2D distance = _activationArea.Distance(bodyCollider);
                    if (distance.isValid && distance.distance <= 0f)
                    {
                        return true;
                    }
                }
            }

            return !hasActiveCollider && _activationArea.OverlapPoint(_cachedPlayer.position);
        }

        public bool Contains(Vector2 worldPosition, float clearance = 0f)
        {
            if (!TryGetHalfSize(clearance, out Vector2 halfSize))
            {
                return false;
            }

            Vector3 worldPoint = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector2 offset = localPoint - _center;
            return Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.y) <= halfSize.y;
        }

        public Vector2 ClampPosition(Vector2 worldPosition, float clearance = 0f)
        {
            if (!TryGetHalfSize(clearance, out Vector2 halfSize))
            {
                return transform.TransformPoint(_center);
            }

            Vector3 worldPoint = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            localPoint.x = Mathf.Clamp(localPoint.x, _center.x - halfSize.x, _center.x + halfSize.x);
            localPoint.y = Mathf.Clamp(localPoint.y, _center.y - halfSize.y, _center.y + halfSize.y);
            return transform.TransformPoint(localPoint);
        }

        private bool TryGetHalfSize(float clearance, out Vector2 halfSize)
        {
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Abs(scale.x);
            float scaleY = Mathf.Abs(scale.y);
            float margin = Mathf.Max(0f, _edgePadding) + Mathf.Max(0f, clearance);
            halfSize = Vector2.zero;
            if (scaleX <= MinimumScale || scaleY <= MinimumScale)
            {
                return false;
            }

            halfSize = new Vector2(_size.x * 0.5f - margin / scaleX, _size.y * 0.5f - margin / scaleY);
            return halfSize.x > 0f && halfSize.y > 0f;
        }

        private void OnValidate()
        {
            _size.x = Mathf.Max(0.1f, _size.x);
            _size.y = Mathf.Max(0.1f, _size.y);
            _edgePadding = Mathf.Max(0f, _edgePadding);
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(_center, new Vector3(_size.x, _size.y, 0f));
            if (TryGetHalfSize(0f, out Vector2 halfSize))
            {
                Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
                Gizmos.DrawWireCube(_center, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0f));
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
