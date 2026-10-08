using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace _02.Scripts.Monster.BossMonster
{
    public enum BossEncounterPattern
    {
        CrescentCleave = 0,
        RiftLanes = 1,
        HellfireRain = 2,
        SpiralBarrage = 3,
        CrossSweep = 4,
        Cataclysm = 5,
        DoubleClaw = 6,
        GroundSlam = 7,
        SerpentWave = 8,
        ThornFan = 9,
        RadialSpikes = 10,
        ExpandingNova = 11,
        TwinNova = 12,
        TrackingEruption = 13,
        MeteorLattice = 14,
        BloodPools = 15,
        TargetedVolley = 16,
        HomingOrbs = 17,
        EchoTurrets = 18,
        OrbitRelease = 19,
        PerimeterBarrage = 20,
        DashRend = 21,
        BurrowAmbush = 22,
        ApocalypseSequence = 23,
        SanguineLances = 24,
        StormPillars = 25,
        RotatingScythes = 26,
        GravityCollapse = 27,
        AlternatingMaze = 28,
        ExecutionNova = 29
    }

    [Serializable]
    public sealed class BossPatternSound
    {
        [SerializeField]
        private AudioClip _start;
        [SerializeField]
        private AudioClip _loop;
        [SerializeField]
        private AudioClip _impact;
        [SerializeField]
        private AudioClip _end;
        public AudioClip Start => _start;
        public AudioClip Loop => _loop;
        public AudioClip Impact => _impact;
        public AudioClip End => _end;

        public void LoadIncludedSounds(string prefix)
        {
            string path = "Monster/BossOverhaul/Audio/" + prefix;
            _start = Resources.Load<AudioClip>(path + "_Start");
            _loop = Resources.Load<AudioClip>(path + "_Loop");
            _impact = Resources.Load<AudioClip>(path + "_Impact");
            _end = Resources.Load<AudioClip>(path + "_End");
        }
    }

    [Serializable]
    public sealed class BossEncounterMove
    {
        [SerializeField]
        private BossEncounterPattern _kind;
        [SerializeField]
        private bool _enabled = true;
        [SerializeField, Range(1, 3)]
        private int _minimumPhase = 1;
        [SerializeField, Min(0.1f)]
        private float _weight = 1f;
        [SerializeField, Min(0f)]
        private float _cooldown = 7f;
        [SerializeField, Min(0.1f)]
        private float _warning = 1f;
        [SerializeField, Min(0.1f)]
        private float _duration = 2f;
        [SerializeField, Min(0.1f)]
        private float _interval = 0.6f;
        [SerializeField, Min(1)]
        private int _waves = 3;
        [SerializeField, Min(1)]
        private int _count = 4;
        [SerializeField, Min(0f)]
        private float _damage = 12f;
        [SerializeField, Min(0.1f)]
        private float _damageInterval = 0.5f;
        [Tooltip("장식 효과의 크기입니다. 실제 피해 범위와 빨간 예고 범위는 바뀌지 않습니다.")]
        [SerializeField, Range(1f, 3f)]
        private float _effectScale = 1.4f;
        [SerializeField, Min(0.1f)]
        private float _radius = 0.8f;
        [SerializeField, Min(0.1f)]
        private float _width = 0.65f;
        [SerializeField, Min(0.1f)]
        private float _speed = 4.8f;
        [SerializeField, Min(0f)]
        private float _recovery = 1.1f;
        [SerializeField, Range(0f, 0.6f)]
        private float _impactShake = 0.18f;
        [SerializeField, Range(0f, 0.25f)]
        private float _sustainShake = 0.025f;
        [SerializeField]
        private Color _color = new Color(1f, 0.12f, 0.2f, 1f);
        [Tooltip("Base Layer.BossMonster_{이름}_{S/SE/E/NE/N/NW/W/SW}. 없으면 기존 공격 애니메이션을 사용합니다.")]
        [SerializeField]
        private string _animationMotion;
        [SerializeField]
        private string _loopMotion = string.Empty;
        [SerializeField]
        private string _endMotion = string.Empty;
        [SerializeField, Range(0.1f, 0.9f)]
        private float _releaseNormalizedTime = 0.53f;
        [Tooltip("시각 효과만 있는 프리팹입니다. 피해 판정은 보스 코드가 처리합니다.")]
        [SerializeField]
        private GameObject _impactPrefab = null;
        [SerializeField, Min(0.1f)]
        private float _impactPrefabLifetime = 3f;
        [SerializeField]
        private BossPatternSound _sound = new BossPatternSound();
        public BossEncounterPattern Kind => _kind;
        public bool Enabled => _enabled;
        public int MinimumPhase => Mathf.Clamp(_minimumPhase, 1, 3);
        public float Weight => Mathf.Max(0.1f, _weight);
        public float Cooldown => Mathf.Max(0f, _cooldown);
        public float Warning => Mathf.Max(0.5f, _warning);
        public float Duration => Mathf.Max(0.1f, _duration);
        public float Interval => Mathf.Max(0.12f, _interval);
        public int Waves => Mathf.Clamp(_waves, 1, 12);
        public int Count => Mathf.Clamp(_count, 1, 16);
        public float Damage => Mathf.Max(0f, _damage);
        public float DamageInterval => Mathf.Max(0.1f, _damageInterval);
        public float EffectScale => Mathf.Clamp(_effectScale, 1f, 3f);
        public float Radius => Mathf.Max(0.1f, _radius);
        public float Width => Mathf.Max(0.15f, _width);
        public float Speed => Mathf.Max(0.5f, _speed);
        public float Recovery => Mathf.Max(0f, _recovery);
        public float ImpactShake => _impactShake;
        public float SustainShake => _sustainShake;
        public Color Color => _color;
        public string AnimationMotion => _animationMotion;
        public string LoopMotion => _loopMotion;
        public string EndMotion => _endMotion;
        public float ReleaseNormalizedTime => _releaseNormalizedTime;
        public GameObject ImpactPrefab => _impactPrefab;
        public float ImpactPrefabLifetime => Mathf.Max(0.1f, _impactPrefabLifetime);
        public BossPatternSound Sound => _sound;

        public static BossEncounterMove Create(BossEncounterPattern kind)
        {
            BossEncounterMove move = new BossEncounterMove();
            move._kind = kind;
            move._minimumPhase = BossPatternCatalog.MinimumPhase(kind);
            move._animationMotion = "attack_" + kind.ToString().ToLowerInvariant();
            switch (kind)
            {
                case BossEncounterPattern.CrescentCleave:
                    move._radius = 3.6f;
                    move._waves = 3;
                    move._interval = 0.95f;
                    move._warning = 0.8f;
                    move._duration = 0.16f;
                    break;
                case BossEncounterPattern.RiftLanes:
                    move._count = 4;
                    move._warning = 1.15f;
                    move._duration = 0.55f;
                    move._width = 0.7f;
                    move._color = new Color(1f, 0.28f, 0.1f, 1f);
                    break;
                case BossEncounterPattern.HellfireRain:
                    move._count = 3;
                    move._waves = 3;
                    move._interval = 0.75f;
                    move._warning = 1f;
                    move._duration = 0.8f;
                    move._radius = 0.7f;
                    move._impactShake = 0.13f;
                    move._damage = 10f;
                    move._color = new Color(1f, 0.16f, 0.1f, 1f);
                    break;
                case BossEncounterPattern.SpiralBarrage:
                    move._count = 10;
                    move._waves = 5;
                    move._interval = 0.43f;
                    move._warning = 1.15f;
                    move._duration = 2.8f;
                    move._radius = 0.12f;
                    move._width = 0.3f;
                    move._damage = 8f;
                    move._impactShake = 0.075f;
                    move._speed = 4.5f;
                    break;
                case BossEncounterPattern.CrossSweep:
                    move._warning = 1.3f;
                    move._duration = 3.2f;
                    move._width = 0.55f;
                    move._damage = 10f;
                    move._sustainShake = 0.055f;
                    move._color = new Color(1f, 0.15f, 0.55f, 1f);
                    break;
                case BossEncounterPattern.Cataclysm:
                    move._warning = 1.5f;
                    move._duration = 3.5f;
                    move._width = 0.48f;
                    move._damage = 18f;
                    move._cooldown = 14f;
                    move._recovery = 1.6f;
                    move._impactShake = 0.3f;
                    move._sustainShake = 0.06f;
                    move._color = new Color(0.65f, 0.25f, 1f, 1f);
                    break;
                case BossEncounterPattern.DoubleClaw:
                    move._waves = 2;
                    move._interval = 0.8f;
                    move._warning = 0.75f;
                    move._radius = 3.2f;
                    move._duration = 0.18f;
                    move._impactShake = 0.19f;
                    break;
                case BossEncounterPattern.GroundSlam:
                    move._waves = 2;
                    move._interval = 1.4f;
                    move._warning = 0.95f;
                    move._radius = 2f;
                    move._duration = 0.22f;
                    move._width = 0.4f;
                    move._impactShake = 0.25f;
                    move._damage = 14f;
                    move._color = new Color(1f, 0.38f, 0.08f, 1f);
                    break;
                case BossEncounterPattern.SerpentWave:
                    move._waves = 3;
                    move._interval = 0.85f;
                    move._warning = 0.9f;
                    move._duration = 2f;
                    move._width = 0.52f;
                    move._radius = 0.7f;
                    move._count = 2;
                    move._damage = 12f;
                    move._speed = 4.8f;
                    move._color = new Color(1f, 0.1f, 0.28f, 1f);
                    break;
                case BossEncounterPattern.ThornFan:
                    move._count = 5;
                    move._waves = 2;
                    move._interval = 1.2f;
                    move._warning = 0.85f;
                    move._duration = 0.2f;
                    move._radius = 0.48f;
                    move._width = 0.55f;
                    move._damage = 11f;
                    move._color = new Color(1f, 0.22f, 0.1f, 1f);
                    break;
                case BossEncounterPattern.RadialSpikes:
                    move._count = 8;
                    move._waves = 2;
                    move._interval = 1.7f;
                    move._warning = 0.85f;
                    move._duration = 0.28f;
                    move._radius = 0.55f;
                    move._width = 0.5f;
                    move._damage = 12f;
                    break;
                case BossEncounterPattern.ExpandingNova:
                    move._waves = 3;
                    move._interval = 1.3f;
                    move._warning = 0.95f;
                    move._duration = 1.9f;
                    move._radius = 0.65f;
                    move._width = 0.5f;
                    move._damage = 13f;
                    move._impactShake = 0.23f;
                    move._color = new Color(0.8f, 0.12f, 1f, 1f);
                    break;
                case BossEncounterPattern.TwinNova:
                    move._waves = 2;
                    move._interval = 2.2f;
                    move._warning = 1.25f;
                    move._duration = 1.6f;
                    move._radius = 0.7f;
                    move._width = 0.4f;
                    move._damage = 14f;
                    move._cooldown = 12f;
                    move._color = new Color(0.6f, 0.3f, 1f, 1f);
                    break;
                case BossEncounterPattern.TrackingEruption:
                    move._waves = 6;
                    move._interval = 0.45f;
                    move._warning = 0.75f;
                    move._duration = 0.25f;
                    move._radius = 0.65f;
                    move._damage = 10f;
                    move._impactShake = 0.12f;
                    break;
                case BossEncounterPattern.MeteorLattice:
                    move._waves = 3;
                    move._interval = 1.2f;
                    move._warning = 1.1f;
                    move._duration = 0.3f;
                    move._radius = 0.6f;
                    move._count = 4;
                    move._damage = 12f;
                    move._cooldown = 12f;
                    move._color = new Color(1f, 0.45f, 0.12f, 1f);
                    break;
                case BossEncounterPattern.BloodPools:
                    move._waves = 2;
                    move._count = 4;
                    move._interval = 1.1f;
                    move._warning = 1.1f;
                    move._duration = 3.6f;
                    move._radius = 0.85f;
                    move._damage = 8f;
                    move._impactShake = 0.1f;
                    move._sustainShake = 0.02f;
                    move._color = new Color(0.75f, 0.04f, 0.18f, 1f);
                    break;
                case BossEncounterPattern.TargetedVolley:
                    move._count = 5;
                    move._waves = 5;
                    move._interval = 0.6f;
                    move._warning = 0.85f;
                    move._duration = 3f;
                    move._speed = 5f;
                    move._radius = 0.11f;
                    move._width = 0.27f;
                    move._damage = 8f;
                    move._impactShake = 0.08f;
                    break;
                case BossEncounterPattern.HomingOrbs:
                    move._count = 3;
                    move._waves = 3;
                    move._interval = 0.95f;
                    move._warning = 1.1f;
                    move._duration = 3.4f;
                    move._radius = 0.14f;
                    move._width = 0.32f;
                    move._speed = 3.3f;
                    move._damage = 9f;
                    move._impactShake = 0.1f;
                    move._color = new Color(0.6f, 0.25f, 1f, 1f);
                    break;
                case BossEncounterPattern.EchoTurrets:
                    move._count = 3;
                    move._waves = 4;
                    move._interval = 0.65f;
                    move._warning = 1.1f;
                    move._duration = 2.8f;
                    move._radius = 0.12f;
                    move._width = 0.3f;
                    move._speed = 4f;
                    move._damage = 8f;
                    move._cooldown = 13f;
                    move._sustainShake = 0.03f;
                    move._color = new Color(0.65f, 0.35f, 1f, 1f);
                    break;
                case BossEncounterPattern.OrbitRelease:
                    move._count = 8;
                    move._waves = 2;
                    move._interval = 1.4f;
                    move._warning = 1.2f;
                    move._duration = 2.6f;
                    move._radius = 0.11f;
                    move._width = 0.28f;
                    move._speed = 4f;
                    move._damage = 8f;
                    move._cooldown = 10f;
                    move._color = new Color(0.35f, 0.75f, 1f, 1f);
                    break;
                case BossEncounterPattern.PerimeterBarrage:
                    move._count = 6;
                    move._waves = 4;
                    move._interval = 0.85f;
                    move._warning = 1.05f;
                    move._duration = 2.8f;
                    move._radius = 0.12f;
                    move._width = 0.28f;
                    move._speed = 4.8f;
                    move._damage = 8f;
                    move._cooldown = 13f;
                    move._color = new Color(1f, 0.2f, 0.45f, 1f);
                    break;
                case BossEncounterPattern.DashRend:
                    move._waves = 1;
                    move._warning = 1.05f;
                    move._duration = 0.65f;
                    move._speed = 10f;
                    move._radius = 1.5f;
                    move._width = 1.2f;
                    move._damage = 15f;
                    move._impactShake = 0.25f;
                    move._sustainShake = 0.045f;
                    move._cooldown = 11f;
                    break;
                case BossEncounterPattern.BurrowAmbush:
                    move._waves = 1;
                    move._warning = 1.6f;
                    move._duration = 0.7f;
                    move._speed = 7f;
                    move._radius = 2f;
                    move._damage = 18f;
                    move._impactShake = 0.28f;
                    move._cooldown = 14f;
                    move._recovery = 1.4f;
                    move._color = new Color(0.6f, 0.2f, 1f, 1f);
                    break;
                case BossEncounterPattern.ApocalypseSequence:
                    move._waves = 3;
                    move._interval = 1.4f;
                    move._warning = 1.4f;
                    move._duration = 1.8f;
                    move._count = 6;
                    move._radius = 0.75f;
                    move._width = 0.5f;
                    move._damage = 12f;
                    move._cooldown = 22f;
                    move._recovery = 1.8f;
                    move._impactShake = 0.3f;
                    move._sustainShake = 0.055f;
                    move._effectScale = 1.8f;
                    move._color = new Color(1f, 0.13f, 0.4f, 1f);
                    break;
            }

            switch (kind)
            {
                case BossEncounterPattern.SanguineLances:
                    move._waves = 3;
                    move._count = 3;
                    move._warning = 1.1f;
                    move._interval = 1.2f;
                    move._duration = 0.32f;
                    move._width = 0.65f;
                    move._damage = 13f;
                    move._color = new Color(1f, 0.08f, 0.42f, 1f);
                    break;
                case BossEncounterPattern.StormPillars:
                    move._waves = 3;
                    move._count = 4;
                    move._warning = 1.2f;
                    move._interval = 1.5f;
                    move._duration = 0.45f;
                    move._radius = 0.75f;
                    move._damage = 14f;
                    move._color = new Color(0.14f, 0.85f, 1f, 1f);
                    break;
                case BossEncounterPattern.RotatingScythes:
                    move._waves = 1;
                    move._count = 3;
                    move._warning = 1.45f;
                    move._duration = 3.8f;
                    move._radius = 4.2f;
                    move._width = 0.45f;
                    move._damage = 14f;
                    move._cooldown = 13f;
                    move._color = new Color(0.75f, 0.2f, 1f, 1f);
                    break;
                case BossEncounterPattern.GravityCollapse:
                    move._waves = 3;
                    move._warning = 1.5f;
                    move._interval = 1.35f;
                    move._duration = 0.38f;
                    move._radius = 1.55f;
                    move._damage = 16f;
                    move._color = new Color(0.5f, 0.35f, 1f, 1f);
                    break;
                case BossEncounterPattern.AlternatingMaze:
                    move._waves = 4;
                    move._count = 4;
                    move._warning = 1.2f;
                    move._interval = 1.65f;
                    move._duration = 0.32f;
                    move._width = 0.55f;
                    move._damage = 14f;
                    move._color = new Color(1f, 0.3f, 0.1f, 1f);
                    break;
                case BossEncounterPattern.ExecutionNova:
                    move._waves = 1;
                    move._count = 1;
                    move._warning = 5f;
                    move._duration = 0.4f;
                    move._radius = 2.3f;
                    move._damage = 99999f;
                    move._cooldown = 28f;
                    move._recovery = 1.65f;
                    move._impactShake = 0.32f;
                    move._sustainShake = 0.045f;
                    move._effectScale = 1.9f;
                    move._color = new Color(1f, 0.06f, 0.32f, 1f);
                    break;
            }

            int animation = BossPatternCatalog.Animation(kind);
            string channel = animation == 1 ? "melee" : animation == 2 ? "projectile" : "explosion";
            move._animationMotion = "charge_" + channel;
            move._loopMotion = "channel_" + channel;
            move._endMotion = "recover_" + channel;
            move._sound.LoadIncludedSounds(BossPatternCatalog.Sound(kind));
            return move;
        }
    }

    [CreateAssetMenu(fileName = "BossEncounter", menuName = "Monsters/Boss Encounter")]
    public sealed class BossEncounterProfile : ScriptableObject
    {
        [Header("추적·고난도 전투")]
        [Tooltip("장식이 아닌 실제 공격 반경과 폭에 적용합니다. 섬멸 안전지대는 제외합니다.")]
        [SerializeField, Range(1f, 1.8f)]
        private float _attackRangeMultiplier = 1.35f;
        [SerializeField, Range(1f, 1.6f)]
        private float _projectileSpeedMultiplier = 1.2f;
        [SerializeField, Range(1f, 2f)]
        private float _damageMultiplier = 1.15f;
        [SerializeField, Range(0.4f, 1f)]
        private float _recoveryMultiplier = 0.65f;
        [SerializeField, Range(0.5f, 1f)]
        private float _cooldownMultiplier = 0.8f;
        [SerializeField, Range(0, 2)]
        private int _extraWaves = 1;
        [SerializeField, Range(0f, 0.4f)]
        private float _targetLeadTime = 0.2f;
        [Tooltip("발동 직전 이 시간 동안 예고 위치·방향을 고정하여 회피 기회를 줍니다.")]
        [SerializeField, Range(0.3f, 0.6f)]
        private float _targetLockTime = 0.42f;
        [SerializeField, Range(3f, 12f)]
        private float _groundTrackingSpeed = 8f;
        [SerializeField, Range(60f, 240f)]
        private float _aimTrackingTurnRate = 150f;
        [SerializeField, Range(15f, 90f)]
        private float _projectileTurnRate = 56f;
        [SerializeField, Range(0.5f, 2.5f)]
        private float _projectileHomingDuration = 1.7f;
        [SerializeField]
        private BossEncounterMove[] _moves;
        [SerializeField, Range(0.1f, 0.9f)]
        private float _secondPhaseHealth = 0.7f;
        [SerializeField, Range(0.05f, 0.5f)]
        private float _thirdPhaseHealth = 0.35f;
        [SerializeField, Range(0.7f, 1f)]
        private float _phaseWarningMultiplier = 0.88f;
        [SerializeField, Min(0f)]
        private float _phaseDamageBonus = 0.12f;
        [SerializeField]
        private bool _introducePatterns = true;
        [SerializeField, Range(0f, 1f)]
        private float _soundVolume = 0.7f;
        [SerializeField]
        private AudioMixerGroup _mixerGroup = null;
        [SerializeField, Range(0f, 2f)]
        private float _shakeMultiplier = 1f;
        [SerializeField, Min(1)]
        private int _maximumProjectiles = 320;
        [SerializeField, Min(1)]
        private int _maximumZones = 96;
        [SerializeField, Range(64, 768)]
        private int _maximumParticles = 512;
        [SerializeField]
        private Shader _effectShader;
        [SerializeField]
        private AudioClip _phaseSound;
        [SerializeField]
        private AudioClip _deathSound;
        public BossEncounterMove[] Moves => _moves;
        public float AttackRangeMultiplier => Mathf.Clamp(_attackRangeMultiplier, 1f, 1.8f);
        public float ProjectileSpeedMultiplier => Mathf.Clamp(_projectileSpeedMultiplier, 1f, 1.6f);
        public float DamageMultiplier => Mathf.Clamp(_damageMultiplier, 1f, 2f);
        public float RecoveryMultiplier => Mathf.Clamp(_recoveryMultiplier, 0.4f, 1f);
        public float CooldownMultiplier => Mathf.Clamp(_cooldownMultiplier, 0.5f, 1f);
        public int ExtraWaves => Mathf.Clamp(_extraWaves, 0, 2);
        public float TargetLeadTime => Mathf.Clamp(_targetLeadTime, 0f, 0.4f);
        public float TargetLockTime => Mathf.Clamp(_targetLockTime, 0.3f, 0.6f);
        public float GroundTrackingSpeed => Mathf.Clamp(_groundTrackingSpeed, 3f, 12f);
        public float AimTrackingTurnRate => Mathf.Clamp(_aimTrackingTurnRate, 60f, 240f);
        public float ProjectileTurnRate => Mathf.Clamp(_projectileTurnRate, 15f, 90f);
        public float ProjectileHomingDuration => Mathf.Clamp(_projectileHomingDuration, 0.5f, 2.5f);
        public float SecondPhaseHealth => _secondPhaseHealth;
        public float ThirdPhaseHealth => Mathf.Min(_thirdPhaseHealth, _secondPhaseHealth - 0.05f);
        public float PhaseWarningMultiplier => _phaseWarningMultiplier;
        public float PhaseDamageBonus => _phaseDamageBonus;
        public bool IntroducePatterns => _introducePatterns;
        public float SoundVolume => _soundVolume;
        public AudioMixerGroup MixerGroup => _mixerGroup;
        public float ShakeMultiplier => _shakeMultiplier;
        public int MaximumProjectiles => Mathf.Clamp(_maximumProjectiles, 1, 512);
        public int MaximumZones => Mathf.Clamp(_maximumZones, 1, 96);
        public int MaximumParticles => Mathf.Clamp(_maximumParticles, 64, 768);
        public Shader EffectShader => _effectShader;
        public AudioClip PhaseSound => _phaseSound;
        public AudioClip DeathSound => _deathSound;

        public void InitializeDefaults()
        {
            _moves = new BossEncounterMove[BossPatternCatalog.Count];
            for (int i = 0; i < _moves.Length; i++)
            {
                _moves[i] = BossEncounterMove.Create((BossEncounterPattern)i);
            }

            _effectShader = Resources.Load<Shader>("Monster/BossOverhaul/BossGlow");
            _phaseSound = Resources.Load<AudioClip>("Monster/BossOverhaul/Audio/Doom_Start");
            _deathSound = Resources.Load<AudioClip>("Monster/BossOverhaul/Audio/Doom_End");
        }

        public int AddMissingPatterns()
        {
            List<BossEncounterMove> moves = new List<BossEncounterMove>();
            bool[] found = new bool[BossPatternCatalog.Count];
            if (_moves != null)
            {
                foreach (BossEncounterMove move in _moves)
                {
                    moves.Add(move);
                    if (move != null && BossPatternCatalog.IsValid(move.Kind))
                    {
                        found[(int)move.Kind] = true;
                    }
                }
            }

            int added = 0;
            for (int i = 0; i < found.Length; i++)
            {
                if (!found[i])
                {
                    moves.Add(BossEncounterMove.Create((BossEncounterPattern)i));
                    added++;
                }
            }

            _moves = moves.ToArray();
            return added;
        }
    }
}
