using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    internal static class MonsterCombatAudio
    {
        private const string ResourceRoot = "Monster/CombatAudioUpgrade/";
        private const int VariantsPerCue = 4;
        private static readonly Dictionary<string, AudioClip[]> CueCache = new Dictionary<string, AudioClip[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            CueCache.Clear();
        }

        public static AudioClip[] Normal(string cue, AudioClip[] fallback)
        {
            AudioClip[] clips = Load("Normal/" + cue);
            return clips.Length > 0 ? clips : fallback;
        }

        public static AudioClip First(string cue, AudioClip fallback)
        {
            AudioClip[] clips = Load(cue);
            return clips.Length > 0 ? clips[0] : fallback;
        }

        public static AudioClip Pick(string cue, AudioClip fallback, ref int previous)
        {
            AudioClip[] clips = Load(cue);
            if (clips.Length == 0)
                return fallback;
            int index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == previous)
            {
                index = (index + 1) % clips.Length;
            }

            previous = index;
            return clips[index];
        }

        private static AudioClip[] Load(string cue)
        {
            if (CueCache.TryGetValue(cue, out AudioClip[] cached))
                return cached;
            List<AudioClip> clips = new List<AudioClip>(VariantsPerCue);
            for (int i = 1; i <= VariantsPerCue; i++)
            {
                AudioClip clip = Resources.Load<AudioClip>(ResourceRoot + cue + "_" + i.ToString("00"));
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            AudioClip[] result = clips.ToArray();
            CueCache.Add(cue, result);
            return result;
        }
    }
}
