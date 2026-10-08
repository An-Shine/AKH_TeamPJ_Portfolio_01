using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    // The player owns movement. This observer only predicts boss targets.
    internal sealed class BossPlayerTracker
    {
        private Vector2 _lastPosition;
        private Vector2 _velocity;
        private bool _initialized;
        public void Reset(Vector2 position)
        {
            _lastPosition = position;
            _velocity = Vector2.zero;
            _initialized = true;
        }

        public void Sample(Vector2 position, float deltaTime)
        {
            Vector2 displacement = position - _lastPosition;
            // A blink, re-entry or long paused frame is not a movement velocity.
            if (!_initialized || deltaTime <= 0f || deltaTime > 0.5f || displacement.magnitude > Mathf.Max(0.65f, deltaTime * 14f))
            {
                Reset(position);
                return;
            }

            Vector2 measured = Vector2.ClampMagnitude(displacement / deltaTime, 12f);
            _velocity = Vector2.Lerp(_velocity, measured, 1f - Mathf.Exp(-deltaTime / 0.1f));
            _lastPosition = position;
        }

        public Vector2 Predict(Vector2 position, float leadTime, Rect arena)
        {
            Vector2 lead = Vector2.ClampMagnitude(_velocity * Mathf.Max(0f, leadTime), 1.6f);
            return BossBattleGeometry.ClampToRoom(position + lead, arena, 0.15f);
        }
    }

    internal static class BossAttackTracking
    {
        public static void Advance(BossDamageZone zone, Vector2 target, float deltaTime, Rect arena)
        {
            if (zone.Shape == BossZoneShape.ArenaOutsideSafeCircle || deltaTime <= 0f)
                return;
            float until = Mathf.Max(0f, zone.Warning - zone.LockBeforeImpact);
            float trackingTime = Mathf.Max(0f, Mathf.Min(deltaTime, until - zone.Age));
            if (trackingTime <= 0f)
                return;
            if (zone.TrackPosition)
            {
                Vector2 destination = BossBattleGeometry.ClampToRoom(target + zone.TargetOffset, arena, zone.TrackingPadding);
                zone.Center = Vector2.MoveTowards(zone.Center, destination, zone.TrackingSpeed * trackingTime);
            }

            if (zone.TrackDirection)
            {
                Vector2 origin = zone.DirectionFromCenter ? zone.Center : zone.TrackingOrigin;
                Vector2 delta = target - origin;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    float desired = BossBattleGeometry.Angle(delta) + zone.AimOffset;
                    zone.Angle = Mathf.MoveTowardsAngle(zone.Angle, desired, zone.TrackingTurnRate * trackingTime);
                    if (zone.FollowAimRay)
                    {
                        zone.Center = origin + BossBattleGeometry.Direction(zone.Angle) * zone.AimDistance;
                        if (zone.Shape == BossZoneShape.Circle)
                            zone.Center = BossBattleGeometry.ClampToRoom(zone.Center, arena, zone.Radius + 0.1f);
                    }
                }
            }
        }

        public static Vector2 Steer(Vector2 direction, Vector2 targetDelta, float turnRate, float deltaTime)
        {
            if (targetDelta.sqrMagnitude <= 0.0001f || turnRate <= 0f || deltaTime <= 0f)
                return direction;
            float angle = Mathf.MoveTowardsAngle(BossBattleGeometry.Angle(direction), BossBattleGeometry.Angle(targetDelta), turnRate * deltaTime);
            return BossBattleGeometry.Direction(angle);
        }
    }
}
