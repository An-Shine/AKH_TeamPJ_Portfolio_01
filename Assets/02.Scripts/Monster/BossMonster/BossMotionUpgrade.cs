using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    [Serializable]
    public sealed class BossMotionClipEntry
    {
        public int family;
        public int stage;
        public int direction;
        public string path = string.Empty;
    }

    [Serializable]
    public sealed class BossMotionCatalog
    {
        public int version;
        public int spriteCount;
        public string[] sprites = new string[0];
        public BossMotionClipEntry[] clips = new BossMotionClipEntry[0];
    }

    // This component only replaces this boss instance's sprite animation clips.
    // The shared controller, colliders, damage, navigation and cast clock stay intact.
    [DisallowMultipleComponent]
    public sealed class BossMotionUpgrade : MonoBehaviour
    {
        public const string ResourceRoot = "Monster/BossMotionUpgrade/";
        public const int ExpectedSpriteCount = 245;
        public const int ExpectedClipCount = 104;

        private const int Blade = 0;
        private const int Slam = 1;
        private const int Core = 2;
        private const int Walk = 3;
        private const int Windup = 0;
        private const int Hold = 1;
        private const int Impact = 2;
        private const int Recover = 3;

        private readonly AnimationClip[,,] _clips = new AnimationClip[4, 4, 8];
        private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(160);
        private readonly Dictionary<string, AnimationClip> _originalOverrides =
            new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        private Animator _animator;
        private RuntimeAnimatorController _originalController;
        private AnimatorOverrideController _controller;
        private int _family = -1;
        private bool _holding;
        private bool _ready;

        public bool IsReady => _ready;
        public int CurrentFamily => _family;
        public bool IsHolding => _holding;

        internal static BossMotionUpgrade Install(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return null;
            global::BossMonster owner = animator.GetComponentInParent<global::BossMonster>();
            if (owner == null)
                return null;
            BossMotionUpgrade existing = owner.GetComponent<BossMotionUpgrade>();
            if (existing != null)
                return existing.IsReady ? existing : null;
            TextAsset text = Resources.Load<TextAsset>(ResourceRoot + "BossMotionCatalog");
            if (text == null)
                return null;
            BossMotionUpgrade upgrade = owner.gameObject.AddComponent<BossMotionUpgrade>();
            if (!upgrade.Initialize(animator, text))
            {
                Destroy(upgrade);
                return null;
            }
            return upgrade;
        }

        private bool Initialize(Animator animator, TextAsset text)
        {
            BossMotionCatalog catalog = JsonUtility.FromJson<BossMotionCatalog>(text.text);
            if (catalog == null || catalog.version != 1 || catalog.spriteCount != ExpectedSpriteCount
                || catalog.sprites == null || catalog.sprites.Length != ExpectedSpriteCount
                || catalog.clips == null || catalog.clips.Length != ExpectedClipCount)
            {
                Debug.LogWarning("보스 동작 카탈로그가 불완전합니다. BossMonster_Motion_Upgrade를 전체 적용하세요.", this);
                return false;
            }
            foreach (BossMotionClipEntry entry in catalog.clips)
            {
                if (entry == null || entry.family < 0 || entry.family > Walk || entry.stage < 0
                    || entry.stage > Recover || entry.direction < 0 || entry.direction >= 8
                    || _clips[entry.family, entry.stage, entry.direction] != null)
                {
                    Debug.LogWarning("보스 동작 카탈로그의 방향 또는 중복 항목을 확인하세요.", this);
                    return false;
                }
                AnimationClip clip = Resources.Load<AnimationClip>(entry.path);
                if (clip == null || clip.empty)
                {
                    Debug.LogWarning("보스 동작 클립 누락: " + entry.path, this);
                    return false;
                }
                _clips[entry.family, entry.stage, entry.direction] = clip;
            }
            for (int direction = 0; direction < 8; direction++)
            {
                for (int family = Blade; family <= Core; family++)
                {
                    for (int stage = Windup; stage <= Recover; stage++)
                    {
                        if (_clips[family, stage, direction] == null)
                            return false;
                    }
                }
                if (_clips[Walk, Impact, direction] == null)
                    return false;
            }
            _animator = animator;
            _originalController = animator.runtimeAnimatorController;
            AnimatorOverrideController prior = _originalController as AnimatorOverrideController;
            RuntimeAnimatorController baseController = prior != null
                ? prior.runtimeAnimatorController : _originalController;
            _controller = new AnimatorOverrideController(baseController)
            {
                name = "BossMotionUpgrade_Instance",
                hideFlags = HideFlags.DontSave
            };
            if (prior != null)
            {
                prior.GetOverrides(_overrides);
                _controller.ApplyOverrides(_overrides);
            }
            _controller.GetOverrides(_overrides);
            foreach (KeyValuePair<AnimationClip, AnimationClip> pair in _overrides)
            {
                if (pair.Key != null)
                    _originalOverrides[pair.Key.name] = pair.Value;
            }
            _ready = true;
            SetFamily(Blade, false);
            animator.runtimeAnimatorController = _controller;
            return true;
        }

        internal void SelectPattern(BossEncounterPattern pattern)
        {
            int family;
            switch (pattern)
            {
                case BossEncounterPattern.CrescentCleave:
                case BossEncounterPattern.DoubleClaw:
                case BossEncounterPattern.DashRend:
                case BossEncounterPattern.RotatingScythes:
                    family = Blade;
                    break;
                case BossEncounterPattern.GroundSlam:
                case BossEncounterPattern.RiftLanes:
                case BossEncounterPattern.SerpentWave:
                case BossEncounterPattern.ThornFan:
                case BossEncounterPattern.RadialSpikes:
                case BossEncounterPattern.TrackingEruption:
                case BossEncounterPattern.BurrowAmbush:
                case BossEncounterPattern.AlternatingMaze:
                    family = Slam;
                    break;
                default:
                    family = Core;
                    break;
            }
            SetFamily(family, false);
        }

        internal void SelectPhase()
        {
            SetFamily(Core, false);
        }

        internal void SetHolding(bool holding)
        {
            SetFamily(_family, holding);
        }

        private void SetFamily(int family, bool holding)
        {
            if (!_ready || family < Blade || family > Core || (_family == family && _holding == holding))
                return;
            _family = family;
            _holding = holding;
            for (int i = 0; i < _overrides.Count; i++)
            {
                AnimationClip original = _overrides[i].Key;
                if (original == null)
                    continue;
                AnimationClip replacement = null;
                if (TryMotion(original.name, out int category, out int direction))
                {
                    replacement = category == 4 ? _clips[Walk, Impact, direction]
                        : _clips[family, category == Impact && holding && original.name.Contains("_channel_")
                            ? Hold : category, direction];
                }
                else
                {
                    // Respect an existing override on every unrelated state, including hit/death.
                    _originalOverrides.TryGetValue(original.name, out replacement);
                }
                _overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
            }
            _controller.ApplyOverrides(_overrides);
        }

        private static bool TryMotion(string clipName, out int category, out int direction)
        {
            category = -1;
            direction = -1;
            if (!clipName.StartsWith("BossMonster_", StringComparison.Ordinal))
                return false;
            for (int d = 0; d < 8; d++)
            {
                string suffix = "_" + MonsterCombat.DirectionNames[d];
                if (!clipName.EndsWith(suffix, StringComparison.Ordinal))
                    continue;
                string motion = clipName.Substring("BossMonster_".Length,
                    clipName.Length - "BossMonster_".Length - suffix.Length);
                if (motion == "walk")
                {
                    category = 4;
                }
                else if (motion == "charge_melee" || motion == "charge_projectile" || motion == "charge_explosion")
                {
                    category = Windup;
                }
                else if (motion == "channel_melee" || motion == "channel_projectile" || motion == "channel_explosion"
                    || motion == "attack_melee" || motion == "attack_projectile" || motion == "attack_explosion")
                {
                    category = Impact;
                }
                else if (motion == "recover_melee" || motion == "recover_projectile" || motion == "recover_explosion")
                {
                    category = Recover;
                }
                else
                {
                    return false;
                }

                direction = d;
                return true;
            }
            return false;
        }

        internal void CopyClipLengths(Dictionary<string, float> lengths)
        {
            if (!_ready)
                return;
            foreach (KeyValuePair<AnimationClip, AnimationClip> pair in _overrides)
            {
                AnimationClip clip = pair.Value != null ? pair.Value : pair.Key;
                if (pair.Key != null && clip != null && !clip.empty)
                    lengths[pair.Key.name] = Mathf.Max(0.05f, clip.length);
            }
        }

        private void OnDestroy()
        {
            if (_animator != null && _animator.runtimeAnimatorController == _controller)
                _animator.runtimeAnimatorController = _originalController;
            if (_controller != null)
                Destroy(_controller);
        }
    }
}
