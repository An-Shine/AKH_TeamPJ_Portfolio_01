using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal enum BossZoneShape
    {
        Circle,
        Sector,
        Beam,
        ClosingRing,
        ArenaOutsideSafeCircle
    }

    internal sealed class BossDamageZone
    {
        public BossZoneShape Shape;
        public BossEncounterMove Move;
        public Vector2 Center;
        public float Angle;
        public float SweepAngle;
        public float Radius;
        public float EndRadius;
        public float Width;
        public float Length;
        public float Span = 120f;
        public float GapWidth = 90f;
        public float Warning;
        public float Duration;
        public float Damage;
        public Vector2 SafeCenter;
        public float SafeRadius;
        public bool Once;
        public bool Pillar;
        public bool Spikes;
        public bool WarningOnly;
        public bool ContactConsumed;
        public bool Impacted;
        public bool PreImpactPlayed;
        public bool TrackPosition;
        public bool TrackDirection;
        public bool DirectionFromCenter;
        public bool FollowAimRay;
        public float AimDistance;
        public bool MotionTracking;
        public Vector2 TrackingOrigin;
        public Vector2 TargetOffset;
        public float AimOffset;
        public float TrackingSpeed;
        public float TrackingTurnRate;
        public float TrackingPadding;
        public float LockBeforeImpact;
        public float TargetLeadTime;
        public float Age;
        public float NextDamage;
        public float NextParticle;
        public BossShapeVisual Fill;
        public BossShapeVisual Outline;
        public BossShapeVisual Core;
        public BossShapeVisual Boundary;
        public BossShapeVisual Detail;
        public BossShapeVisual SweepHint;
        public BossShapeVisual OppositeHint;
        public float Progress => Mathf.Clamp01((Age - Warning) / Mathf.Max(0.01f, Duration));
        public float CurrentAngle => Angle + SweepAngle * Progress;
        public float CurrentRadius => Shape == BossZoneShape.ClosingRing ? Mathf.Lerp(Radius, EndRadius, Progress) : Radius;

        public void Dispose()
        {
            Fill.Dispose();
            Outline.Dispose();
            Core.Dispose();
            Boundary.Dispose();
            Detail.Dispose();
            if (SweepHint != null)
            {
                SweepHint.Dispose();
                OppositeHint.Dispose();
            }
        }
    }
}
