using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal struct BossMotionPose
    {
        public Vector2 Offset;
        public Vector2 Scale;
        public float Roll;
    }

    // Small accents on the sprite child, independent of navigation and hitboxes.
    internal static class BossPatternChoreography
    {
        internal static BossMotionPose Evaluate(BossEncounterPattern pattern, int stage, bool holding,
            float age, float warning, Vector2 facing, int strike, float strikeAge)
        {
            BossMotionPose pose = new BossMotionPose { Scale = Vector2.one };
            if (stage < 0 || stage > 2)
                return pose;
            float build = stage == 0 ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / Mathf.Max(0.1f, Mathf.Min(warning, 0.85f)))) : 0f;
            float release = stage == 1 && !holding ? Mathf.Exp(-Mathf.Max(0f, strikeAge) * 12f) : 0f;
            float cycle = Mathf.Sin(Mathf.Max(0f, age - warning) * 5.5f);
            float side = (strike & 1) == 0 ? -1f : 1f;
            float settle = stage == 2 ? 0.25f : 1f;
            Vector2 lateral = new Vector2(-facing.y, facing.x);
            switch (pattern)
            {
                case BossEncounterPattern.CrescentCleave:
                    pose.Roll = -7f * build + 8f * release;
                    pose.Offset = lateral * (-0.04f * build + 0.1f * release);
                    break;
                case BossEncounterPattern.DoubleClaw:
                    pose.Roll = -6f * build + side * 9f * release;
                    pose.Offset = lateral * side * 0.13f * release;
                    break;
                case BossEncounterPattern.GroundSlam:
                    pose.Offset.y = 0.08f * build - 0.16f * release;
                    pose.Scale += new Vector2(0.065f, -0.09f) * release;
                    break;
                case BossEncounterPattern.RiftLanes:
                    pose.Roll = 5f * build - 6f * release;
                    pose.Offset = facing * 0.09f * release;
                    pose.Scale.y -= 0.045f * release;
                    break;
                case BossEncounterPattern.SerpentWave:
                    pose.Roll = stage == 1 ? cycle * 4f : -3f * build;
                    pose.Offset = lateral * (stage == 1 ? cycle * 0.07f : -0.04f * build);
                    break;
                case BossEncounterPattern.ThornFan:
                    pose.Scale.x += 0.035f * build + 0.05f * release;
                    pose.Offset = -facing * 0.07f * release;
                    break;
                case BossEncounterPattern.RadialSpikes:
                    pose.Offset.y = -0.08f * build + 0.13f * release;
                    pose.Scale += new Vector2(0.025f, -0.055f) * build;
                    break;
                case BossEncounterPattern.ExpandingNova:
                    pose.Scale += Vector2.one * (0.025f * build + 0.055f * release);
                    pose.Offset.y = 0.045f * build;
                    break;
                case BossEncounterPattern.TwinNova:
                    pose.Offset = lateral * side * 0.09f * release;
                    pose.Scale += new Vector2(0.045f, 0.025f) * release;
                    pose.Roll = side * 4f * release;
                    break;
                case BossEncounterPattern.TargetedVolley:
                    pose.Offset = -facing * (0.04f * build + 0.14f * release);
                    pose.Roll = -3f * build + 4f * release;
                    break;
                case BossEncounterPattern.HomingOrbs:
                    pose.Offset.y = 0.04f * build + (stage == 1 ? cycle * 0.025f : 0f);
                    pose.Scale.y += 0.04f * release;
                    break;
                case BossEncounterPattern.EchoTurrets:
                    pose.Roll = side * 6f * release - 3f * build;
                    pose.Offset = lateral * side * 0.08f * release;
                    break;
                case BossEncounterPattern.SpiralBarrage:
                    pose.Roll = stage == 1 ? cycle * 2.5f : -4f * build;
                    pose.Offset = -facing * 0.05f * release;
                    break;
                case BossEncounterPattern.CrossSweep:
                    pose.Scale.x += 0.035f * build;
                    pose.Offset = -facing * (0.07f * build + 0.075f * release);
                    pose.Roll = stage == 1 ? cycle * 2f : 0f;
                    break;
                case BossEncounterPattern.OrbitRelease:
                    pose.Offset = stage == 1 ? lateral * cycle * 0.055f : Vector2.zero;
                    pose.Offset.y += 0.06f * build;
                    pose.Roll = stage == 1 ? cycle * 3f : 0f;
                    break;
                case BossEncounterPattern.PerimeterBarrage:
                    pose.Offset = -facing * (0.025f * build + 0.11f * release);
                    pose.Scale.x += 0.045f * build;
                    break;
                case BossEncounterPattern.DashRend:
                    pose.Offset = facing * (-0.09f * build + 0.18f * release);
                    pose.Roll = -6f * build + 5f * release;
                    pose.Scale.y -= 0.03f * build;
                    break;
                case BossEncounterPattern.BurrowAmbush:
                    pose.Offset.y = -0.17f * build + 0.15f * release;
                    pose.Scale += new Vector2(0.06f, -0.14f) * build;
                    break;
                case BossEncounterPattern.RotatingScythes:
                    pose.Roll = stage == 1 ? cycle * 2f : -7f * build;
                    pose.Scale.x += 0.025f * build;
                    break;
                case BossEncounterPattern.GravityCollapse:
                    pose.Offset.y = 0.11f * build - 0.08f * release;
                    pose.Scale += new Vector2(-0.035f, 0.04f) * build;
                    pose.Scale += new Vector2(0.065f, -0.055f) * release;
                    break;
                case BossEncounterPattern.HellfireRain:
                    pose.Offset.y = 0.07f * build - 0.045f * release;
                    pose.Roll = stage == 1 ? cycle * 1.5f : 2f * build;
                    break;
                case BossEncounterPattern.MeteorLattice:
                    pose.Offset.y = 0.1f * build - 0.09f * release;
                    pose.Scale.x += 0.03f * build;
                    break;
                case BossEncounterPattern.TrackingEruption:
                    pose.Offset = facing * (0.045f * build + 0.085f * release);
                    pose.Roll = -4f * build + 5f * release;
                    break;
                case BossEncounterPattern.BloodPools:
                    pose.Offset.y = -0.04f * build - 0.075f * release;
                    pose.Scale.x += 0.04f * release;
                    break;
                case BossEncounterPattern.StormPillars:
                    pose.Roll = -3f * build + side * 5f * release;
                    pose.Offset.y = 0.085f * build - 0.065f * release;
                    break;
                case BossEncounterPattern.SanguineLances:
                    pose.Offset = facing * (-0.045f * build + 0.12f * release);
                    pose.Roll = -4f * build + 3f * release;
                    break;
                case BossEncounterPattern.AlternatingMaze:
                    pose.Offset = lateral * side * 0.1f * release;
                    pose.Roll = side * 4.5f * release;
                    pose.Offset.y -= 0.035f * build;
                    break;
                case BossEncounterPattern.ExecutionNova:
                    pose.Scale += new Vector2(-0.035f, 0.02f) * build;
                    pose.Scale += Vector2.one * 0.07f * release;
                    pose.Offset.y = 0.065f * build;
                    break;
                case BossEncounterPattern.Cataclysm:
                    pose.Offset.y = 0.12f * build - 0.1f * release;
                    pose.Scale.y += 0.045f * build;
                    pose.Roll = stage == 1 ? cycle * 1.6f : 0f;
                    break;
                case BossEncounterPattern.ApocalypseSequence:
                    pose.Offset.y = 0.135f * build - 0.115f * release;
                    pose.Scale += Vector2.one * (0.035f * build + 0.045f * release);
                    pose.Roll = stage == 1 ? cycle * 2.2f : -2f * build;
                    break;
            }
            pose.Offset *= settle;
            pose.Scale = Vector2.Lerp(Vector2.one, pose.Scale, settle);
            pose.Roll *= settle;
            return pose;
        }
    }
}
