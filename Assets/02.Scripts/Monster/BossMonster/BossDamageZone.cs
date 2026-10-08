using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal enum BossZoneShape
    {
        Circle,
        Sector,
        Beam,
        ClosingRing
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
        public bool Once;
        public bool Pillar;
        public bool Spikes;
        public bool WarningOnly;
        public bool ContactConsumed;
        public bool Impacted;
        public float Age;
        public float NextDamage;
        public float NextParticle;
        public BossShapeVisual Fill;
        public BossShapeVisual Outline;
        public BossShapeVisual Core;
        public BossShapeVisual SweepHint;
        public BossShapeVisual OppositeHint;

        public float Progress => Mathf.Clamp01((Age - Warning) / Mathf.Max(0.01f, Duration));
        public float CurrentAngle => Angle + SweepAngle * Progress;
        public float CurrentRadius => Shape == BossZoneShape.ClosingRing
            ? Mathf.Lerp(Radius, EndRadius, Progress) : Radius;

        public void Dispose()
        {
            Fill.Dispose();
            Outline.Dispose();
            Core.Dispose();
            if (SweepHint != null)
            {
                SweepHint.Dispose();
                OppositeHint.Dispose();
            }
        }
    }
}
