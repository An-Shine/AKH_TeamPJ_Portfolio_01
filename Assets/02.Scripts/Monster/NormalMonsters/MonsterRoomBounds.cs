using UnityEngine;

namespace _02.Scripts.Monster.NormalMonsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterRoomBounds : MonoBehaviour
    {
        private const float MinimumScale = 0.0001f;

        [Header("몬스터가 머무를 방 범위")]
        [SerializeField] private Vector2 _center;
        [SerializeField] private Vector2 _size = new Vector2(12f, 8f);

        [Tooltip("방 가장자리에서 몬스터를 떨어뜨릴 추가 거리입니다. 월드 단위입니다.")]
        [SerializeField, Min(0f)] private float _edgePadding = 0.35f;

        public bool IsConfigured => TryGetHalfSize(0f, out _);

        public bool Contains(Vector2 worldPosition, float clearance = 0f)
        {
            if (!TryGetHalfSize(clearance, out Vector2 halfSize))
            {
                return false;
            }

            Vector3 worldPoint = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector2 offset = localPoint - _center;
            return Mathf.Abs(offset.x) <= halfSize.x &&
                Mathf.Abs(offset.y) <= halfSize.y;
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

            halfSize = new Vector2(
                _size.x * 0.5f - margin / scaleX,
                _size.y * 0.5f - margin / scaleY);
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
