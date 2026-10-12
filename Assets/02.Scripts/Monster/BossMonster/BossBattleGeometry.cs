using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal static class BossBattleGeometry
    {
        public static Vector2 Direction(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public static float Angle(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        public static Vector2 ClampToRoom(Vector2 point, Rect room, float padding)
        {
            padding = Mathf.Min(Mathf.Max(0f, padding), Mathf.Min(room.width, room.height) * 0.45f);
            return new Vector2(Mathf.Clamp(point.x, room.xMin + padding, room.xMax - padding), Mathf.Clamp(point.y, room.yMin + padding, room.yMax - padding));
        }

        public static bool IsBodyCollider(Collider2D collider)
        {
            return collider != null && collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy && (collider.attachedRigidbody == null || collider.attachedRigidbody.simulated);
        }

        public static bool BoundsInsideCircle(Bounds bounds, Vector2 center, float radius)
        {
            float x = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x));
            float y = Mathf.Max(Mathf.Abs(bounds.min.y - center.y), Mathf.Abs(bounds.max.y - center.y));
            return x * x + y * y <= radius * radius;
        }

        public static bool PlayerInsideSafeCircle(Transform player, Collider2D[] bodies, Vector2 center, float radius)
        {
            bool found = false;
            foreach (Collider2D body in bodies)
            {
                if (!IsBodyCollider(body))
                    continue;
                found = true;
                if (!BoundsInsideCircle(body.bounds, center, radius))
                    return false;
            }

            return found || (player != null && ((Vector2)player.position - center).sqrMagnitude <= radius * radius);
        }

        public static bool InSector(Vector2 point, Vector2 center, float radius, float angle, float span)
        {
            Vector2 delta = point - center;
            return delta.sqrMagnitude <= radius * radius && (delta.sqrMagnitude < 0.000001f || Mathf.Abs(Mathf.DeltaAngle(angle, Angle(delta))) <= span * 0.5f);
        }

        public static bool SectorTouches(Collider2D body, Vector2 center, float radius, float angle, float span, out Vector2 point)
        {
            point = body.ClosestPoint(center);
            if ((point - center).sqrMagnitude > radius * radius)
            {
                return false;
            }

            if (InSector(point, center, radius, angle, span))
            {
                return true;
            }

            // 부채꼴의 두 경계와 중앙선도 검사해 플레이어 몸의 일부가 들어온 경우를 잡습니다.
            for (int i = -1; i <= 1; i++)
            {
                Vector2 direction = Direction(angle + span * 0.5f * i);
                float projected = Mathf.Clamp(Vector2.Dot((Vector2)body.bounds.center - center, direction), 0f, radius);
                Vector2 candidate = body.ClosestPoint(center + direction * projected);
                if (InSector(candidate, center, radius + 0.002f, angle, span + 0.05f))
                {
                    point = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool RingTouches(Collider2D body, Vector2 center, float previousRadius, float radius, float width, float gapAngle, float gapWidth, out Vector2 point)
        {
            point = body.ClosestPoint(center);
            float nearest = (point - center).magnitude;
            Bounds bounds = body.bounds;
            float farX = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x));
            float farY = Mathf.Max(Mathf.Abs(bounds.min.y - center.y), Mathf.Abs(bounds.max.y - center.y));
            float farthest = Mathf.Sqrt(farX * farX + farY * farY);
            float inner = Mathf.Min(previousRadius, radius) - width * 0.5f;
            float outer = Mathf.Max(previousRadius, radius) + width * 0.5f;
            if (nearest > outer || farthest < inner)
            {
                return false;
            }

            Vector2 toBody = (Vector2)bounds.center - center;
            float bodyRadius = ((Vector2)bounds.extents).magnitude;
            float angularRadius = Mathf.Asin(Mathf.Clamp01(bodyRadius / Mathf.Max(0.01f, toBody.magnitude))) * Mathf.Rad2Deg;
            float difference = Mathf.Abs(Mathf.DeltaAngle(gapAngle, Angle(toBody)));
            return difference + angularRadius >= gapWidth * 0.5f;
        }
    }
}
