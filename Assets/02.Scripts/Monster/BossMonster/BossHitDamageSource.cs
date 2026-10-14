using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace _02.Scripts.Monster.BossMonster
{
    public static class BossHitDamageSource
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsContinuousDamageCall()
        {
            // TakeDamage has no source parameter. Read the caller without changing attack scripts.
            StackTrace trace = new StackTrace(1, false);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                Type source = trace.GetFrame(i)?.GetMethod()?.DeclaringType;
                if (source != null &&
                    (typeof(LaserProjectile).IsAssignableFrom(source) ||
                     typeof(GammaField).IsAssignableFrom(source) ||
                     typeof(BlackHoleDamageControl).IsAssignableFrom(source)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
