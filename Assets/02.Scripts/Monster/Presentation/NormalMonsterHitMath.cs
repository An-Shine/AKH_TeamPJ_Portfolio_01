using System;

namespace _02.Scripts.Monster.Presentation
{
    // 시각적 반동만 계산합니다. 루트 Transform / Collider / AI 위치는 변경하지 않습니다.
    internal static class NormalMonsterHitMath
    {
        internal static float Flash(float age, float hold, float fade)
        {
            return 1f - Ease((age - hold) / Math.Max(0.001f, fade));
        }

        internal static float Scale(float age, float duration, float amount)
        {
            float progress = ClampUnit(age / Math.Max(0.001f, duration));
            if (progress < 0.1f)
            {
                return 1f + amount * Blend(0.55f, 1f, Ease(progress / 0.1f));
            }

            if (progress < 0.62f)
            {
                return Blend(1f + amount, 1f - amount * 0.18f, Ease((progress - 0.1f) / 0.52f));
            }

            return Blend(1f - amount * 0.18f, 1f, Ease((progress - 0.62f) / 0.38f));
        }

        internal static float Recoil(float age, float duration)
        {
            float remaining = 1f - ClampUnit(age / Math.Max(0.001f, duration));
            return remaining * remaining * remaining;
        }

        private static float ClampUnit(float value)
        {
            return Math.Min(1f, Math.Max(0f, value));
        }

        private static float Ease(float value)
        {
            float progress = ClampUnit(value);
            return progress * progress * (3f - 2f * progress);
        }

        private static float Blend(float from, float to, float progress)
        {
            return from + (to - from) * progress;
        }
    }
}
