using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    // Rendering priorities are relative to the monster's existing sorting layer.
    // Damage, collision, animation speed and attack clocks never use these values.
    internal static class MonsterVfxReadability
    {
        public const int GroundOrder = -3;
        public const int DecorationOrder = 3;
        public const int ImpactOrder = 7;
        public const int WarningContrastOrder = 12;
        public const int WarningEdgeOrder = 13;
        public const int WarningDetailOrder = 14;
        public const int ProjectileContrastOrder = 16;
        public const int ProjectileBodyOrder = 17;
        public const int ProjectileCoreOrder = 18;
        public const int DecorativeBudget = 96;
        public const int ImpactBudget = 64;

        public static Color WarningFill(float progress)
        {
            return new Color(1f, 0.065f, 0.12f, Mathf.Lerp(0.055f, 0.12f, progress));
        }

        public static Color WarningEdge(float progress, bool locked)
        {
            Color color = new Color(1f, 0.16f, 0.22f, Mathf.Lerp(0.78f, 1f, progress));
            return locked ? Color.Lerp(color, new Color(1f, 0.72f, 0.24f, 1f), 0.75f) : color;
        }

        public static float ImpactEnvelope(float age)
        {
            float progress = Mathf.Clamp01(age / 0.11f);
            return (1f - progress) * (1f - progress);
        }

        public static Color ActiveFill(Color color, float age)
        {
            color.a = 0.12f + ImpactEnvelope(age) * 0.08f;
            return color;
        }

        public static Color ActiveCore(Color color, float age)
        {
            float impact = ImpactEnvelope(age);
            color = Color.Lerp(color, Color.white, 0.18f + impact * 0.62f);
            color.a = 0.28f + impact * 0.62f;
            return color;
        }

        public static Color WithOpacity(Color color, float opacity)
        {
            color.a = Mathf.Min(color.a, opacity);
            return color;
        }

        public static Color Trail(Color color)
        {
            return WithOpacity(color, 0.3f);
        }

        public static float TransientFade(float age, float lifetime)
        {
            float progress = Mathf.Clamp01(age / Mathf.Max(0.02f, lifetime));
            float remaining = 1f - progress;
            return remaining * remaining;
        }

        public static bool IsGroundParticle(string key)
        {
            return key == "smoke" || key == "black_smoke" || key == "dust" || key == "debris"
                || key == "blood_pool" || key == "earth_ring" || key == "sigil"
                || key == "charge_rune" || key == "orbit_rune";
        }

        public static int ParticleOrder(string key, int requestedOrder)
        {
            return IsGroundParticle(key) ? GroundOrder : Mathf.Clamp(requestedOrder, 0, ImpactOrder);
        }

        public static Color ParticleColor(string key, Color color, bool impact)
        {
            float opacity = IsGroundParticle(key) ? 0.24f : impact ? 0.9f : 0.48f;
            if (key == "glow" || key == "electric_aura" || key == "arcane_orb")
            {
                opacity = 0.2f;
            }

            return WithOpacity(color, opacity);
        }
    }
}
