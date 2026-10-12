using UnityEngine;

public enum RangeAttackPattern
{
    Fan,
    Burst,
    Sweep,
    Twin,
    Single,
    EliteFan,
    EliteSweep,
    EliteTwin
}

public static class RangeAttackPatterns
{
    public const int Count = 8;
    public const int BasicPatternCount = 3;
    public const int ElitePatternCount = 3;

    public static int PatternCount(bool elite)
    {
        return elite ? ElitePatternCount : BasicPatternCount;
    }

    public static RangeAttackPattern ForTier(bool elite, int index)
    {
        int count = PatternCount(elite);
        int slot = (index % count + count) % count;
        if (elite)
            return (RangeAttackPattern)((int)RangeAttackPattern.EliteFan + slot);
        return slot == 0 ? RangeAttackPattern.Single : slot == 1 ? RangeAttackPattern.Fan : RangeAttackPattern.Burst;
    }

    public static bool IsElite(RangeAttackPattern pattern)
    {
        return pattern >= RangeAttackPattern.EliteFan && pattern <= RangeAttackPattern.EliteTwin;
    }

    public static int WaveCount(RangeAttackPattern pattern)
    {
        switch (pattern)
        {
            case RangeAttackPattern.Burst:
            case RangeAttackPattern.EliteFan:
                return 3;
            case RangeAttackPattern.Sweep:
                return 5;
            case RangeAttackPattern.Twin:
                return 2;
            case RangeAttackPattern.EliteSweep:
                return 7;
            case RangeAttackPattern.EliteTwin:
                return 4;
            default:
                return 1;
        }
    }

    public static int ShotCount(RangeAttackPattern pattern)
    {
        switch (pattern)
        {
            case RangeAttackPattern.Fan:
                return 3;
            case RangeAttackPattern.EliteFan:
                return 5;
            case RangeAttackPattern.Twin:
            case RangeAttackPattern.EliteSweep:
            case RangeAttackPattern.EliteTwin:
                return 2;
            default:
                return 1;
        }
    }

    public static float WaveInterval(RangeAttackPattern pattern, float interval)
    {
        interval = Mathf.Clamp(interval, 0.08f, 0.14f);
        if (pattern == RangeAttackPattern.EliteFan)
            return interval * 1.8f;
        if (pattern == RangeAttackPattern.EliteTwin)
            return interval * 1.4f;
        if (pattern == RangeAttackPattern.Sweep)
            return interval * 0.75f;
        return pattern == RangeAttackPattern.Twin ? interval * 1.5f : interval;
    }

    public static float Angle(RangeAttackPattern pattern, int wave, int shot, int sweepSign)
    {
        switch (pattern)
        {
            case RangeAttackPattern.Fan:
                return (shot - 1) * 14f;
            case RangeAttackPattern.Sweep:
                return (wave - 2) * 11f * (sweepSign < 0 ? -1 : 1);
            case RangeAttackPattern.Twin:
                return (shot == 0 ? -1f : 1f) * (10f + wave * 5f);
            case RangeAttackPattern.EliteFan:
                return (shot - 2) * 15f + (wave % 2 == 0 ? 0f : 7.5f * (sweepSign < 0 ? -1f : 1f));
            case RangeAttackPattern.EliteSweep:
                return (shot == 0 ? -1f : 1f) * ((wave - 3) * 12f + 6f) * (sweepSign < 0 ? -1f : 1f);
            case RangeAttackPattern.EliteTwin:
                return (shot == 0 ? -1f : 1f) * (9f + wave * 7f);
            default:
                return 0f;
        }
    }

    public static float SpeedMultiplier(RangeAttackPattern pattern)
    {
        switch (pattern)
        {
            case RangeAttackPattern.Burst:
                return 1.05f;
            case RangeAttackPattern.Sweep:
                return 0.85f;
            case RangeAttackPattern.EliteFan:
                return 0.92f;
            case RangeAttackPattern.EliteSweep:
                return 0.95f;
            case RangeAttackPattern.EliteTwin:
                return 1.02f;
            case RangeAttackPattern.Single:
                return 1f;
            default:
                return 0.9f;
        }
    }

    public static int CueCount(RangeAttackPattern pattern)
    {
        switch (pattern)
        {
            case RangeAttackPattern.Fan:
                return 3;
            case RangeAttackPattern.Sweep:
                return 5;
            case RangeAttackPattern.Twin:
                return 4;
            case RangeAttackPattern.EliteFan:
            case RangeAttackPattern.EliteSweep:
                return 6;
            case RangeAttackPattern.EliteTwin:
                return 8;
            default:
                return 1;
        }
    }

    public static float CueAngle(RangeAttackPattern pattern, int index, int sweepSign)
    {
        if (pattern == RangeAttackPattern.EliteFan)
        {
            return index < 5 ? Angle(pattern, 0, index, sweepSign)
                : Angle(pattern, 1, sweepSign < 0 ? 0 : 4, sweepSign);
        }

        if (pattern == RangeAttackPattern.EliteSweep)
            return Angle(pattern, index / 2 * 3, index % 2, sweepSign);
        if (pattern == RangeAttackPattern.EliteTwin || pattern == RangeAttackPattern.Twin)
            return Angle(pattern, index / 2, index % 2, sweepSign);
        return Angle(pattern, pattern == RangeAttackPattern.Sweep ? index : 0,
            pattern == RangeAttackPattern.Fan ? index : 0, sweepSign);
    }

    public static Vector2 Direction(Vector2 aim, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sine = Mathf.Sin(radians);
        float cosine = Mathf.Cos(radians);
        return new Vector2(aim.x * cosine - aim.y * sine, aim.x * sine + aim.y * cosine).normalized;
    }

    public static Color Color(RangeAttackPattern pattern)
    {
        switch (pattern)
        {
            case RangeAttackPattern.Fan:
                return new Color(0.2f, 0.85f, 1f, 1f);
            case RangeAttackPattern.Burst:
                return new Color(1f, 0.52f, 0.12f, 1f);
            case RangeAttackPattern.Sweep:
                return new Color(0.85f, 0.35f, 1f, 1f);
            case RangeAttackPattern.EliteFan:
                return new Color(1f, 0.82f, 0.22f, 1f);
            case RangeAttackPattern.EliteSweep:
                return new Color(0.72f, 0.38f, 1f, 1f);
            case RangeAttackPattern.EliteTwin:
                return new Color(1f, 0.28f, 0.55f, 1f);
            default:
                return new Color(0.4f, 1f, 0.35f, 1f);
        }
    }
}
