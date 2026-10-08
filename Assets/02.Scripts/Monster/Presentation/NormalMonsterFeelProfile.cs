using UnityEngine;
using UnityEngine.Audio;

namespace _02.Scripts.Monster.Presentation
{
    [CreateAssetMenu(menuName = "Monster/Normal Monster Game Feel Profile")]
    public sealed class NormalMonsterFeelProfile : ScriptableObject
    {
        [Header("네온 색상")]
        [SerializeField]
        private Color _meleeColor = new Color(1f, 0.12f, 0.58f, 1f);
        [SerializeField]
        private Color _rangeColor = new Color(0.08f, 0.82f, 1f, 1f);
        [SerializeField]
        private Color _kamikazeColor = new Color(1f, 0.32f, 0.06f, 1f);
        [SerializeField]
        private Color _accentColor = new Color(1f, 0.65f, 0.12f, 1f);
        [SerializeField, Range(0f, 1f)]
        private float _outlineOpacity = 0.28f;
        [Header("전투 이펙트")]
        [SerializeField, Range(0.5f, 2f)]
        private float _decorationScale = 1.3f;
        [SerializeField, Range(4, 40)]
        private int _meleeSparks = 22;
        [SerializeField, Range(4, 40)]
        private int _impactSparks = 26;
        [SerializeField, Min(0.01f)]
        private float _trailInterval = 0.035f;
        [SerializeField, Range(0f, 0.3f)]
        private float _cameraShake = 0.09f;
        [Header("피격: 빨간 실루엣과 크기 반동")]
        [SerializeField, Range(0.03f, 0.2f)]
        private float _hitFlashHold = 0.1f;
        [SerializeField, Range(0.02f, 0.15f)]
        private float _hitFlashFade = 0.065f;
        [SerializeField, Range(0.12f, 0.4f)]
        private float _hitPopDuration = 0.22f;
        [SerializeField, Range(0f, 0.25f)]
        private float _hitPopAmount = 0.14f;
        [SerializeField, Range(0f, 0.1f)]
        private float _hitRecoil = 0.035f;
        [SerializeField, Range(0f, 0.08f)]
        private float _hitCameraShake = 0.026f;
        [Header("피격: 기계 파편과 오일")]
        [SerializeField, Range(0, 24)]
        private int _hitSparks = 12;
        [SerializeField, Range(0, 12)]
        private int _hitMetalShards = 5;
        [SerializeField, Range(0, 16)]
        private int _hitOilDrops = 7;
        [SerializeField, Range(0.3f, 5f)]
        private float _hitOilLifetime = 2.2f;
        [SerializeField]
        private AudioClip[] _hitSounds;
        [SerializeField, Range(0f, 1f)]
        private float _hitVolume = 0.62f;
        [Header("사운드")]
        [SerializeField]
        private AudioClip[] _meleeChargeSounds;
        [SerializeField]
        private AudioClip[] _meleeSlashSounds;
        [SerializeField]
        private AudioClip[] _meleeImpactSounds;
        [SerializeField]
        private AudioClip[] _rangeChargeSounds;
        [SerializeField]
        private AudioClip[] _rangeShotSounds;
        [SerializeField]
        private AudioClip[] _rangeImpactSounds;
        [SerializeField]
        private AudioClip[] _kamikazeChargeSounds;
        [SerializeField]
        private AudioClip[] _kamikazeTickSounds;
        [SerializeField]
        private AudioClip[] _kamikazeExplosionSounds;
        [SerializeField, Range(0f, 1f)]
        private float _attackVolume = 0.75f;
        [SerializeField, Range(0f, 1f)]
        private float _impactVolume = 0.82f;
        [SerializeField]
        private AudioMixerGroup _soundMixerGroup;
        public Color MeleeColor => _meleeColor;
        public Color RangeColor => _rangeColor;
        public Color KamikazeColor => _kamikazeColor;
        public Color AccentColor => _accentColor;
        public float OutlineOpacity => Mathf.Clamp01(_outlineOpacity);
        public float DecorationScale => Mathf.Clamp(_decorationScale, 0.5f, 2f);
        public int MeleeSparks => Mathf.Clamp(_meleeSparks, 4, 40);
        public int ImpactSparks => Mathf.Clamp(_impactSparks, 4, 40);
        public float TrailInterval => Mathf.Max(0.01f, _trailInterval);
        public float CameraShake => Mathf.Clamp(_cameraShake, 0f, 0.3f);
        public float HitFlashHold => Mathf.Clamp(_hitFlashHold, 0.03f, 0.2f);
        public float HitFlashFade => Mathf.Clamp(_hitFlashFade, 0.02f, 0.15f);
        public float HitPopDuration => Mathf.Clamp(_hitPopDuration, 0.12f, 0.4f);
        public float HitPopAmount => Mathf.Clamp(_hitPopAmount, 0f, 0.25f);
        public float HitRecoil => Mathf.Clamp(_hitRecoil, 0f, 0.1f);
        public float HitCameraShake => Mathf.Clamp(_hitCameraShake, 0f, 0.08f);
        public int HitSparks => Mathf.Clamp(_hitSparks, 0, 24);
        public int HitMetalShards => Mathf.Clamp(_hitMetalShards, 0, 12);
        public int HitOilDrops => Mathf.Clamp(_hitOilDrops, 0, 16);
        public float HitOilLifetime => Mathf.Clamp(_hitOilLifetime, 0.3f, 5f);
        public AudioClip[] HitSounds => _hitSounds;
        public float HitVolume => Mathf.Clamp01(_hitVolume);
        public float AttackVolume => Mathf.Clamp01(_attackVolume);
        public float ImpactVolume => Mathf.Clamp01(_impactVolume);
        public AudioMixerGroup SoundMixerGroup => _soundMixerGroup;
        public AudioClip[] MeleeChargeSounds => _meleeChargeSounds;
        public AudioClip[] MeleeSlashSounds => _meleeSlashSounds;
        public AudioClip[] MeleeImpactSounds => _meleeImpactSounds;
        public AudioClip[] RangeChargeSounds => _rangeChargeSounds;
        public AudioClip[] RangeShotSounds => _rangeShotSounds;
        public AudioClip[] RangeImpactSounds => _rangeImpactSounds;
        public AudioClip[] KamikazeChargeSounds => _kamikazeChargeSounds;
        public AudioClip[] KamikazeTickSounds => _kamikazeTickSounds;
        public AudioClip[] KamikazeExplosionSounds => _kamikazeExplosionSounds;
    }
}
