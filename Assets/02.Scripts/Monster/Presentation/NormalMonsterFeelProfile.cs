using UnityEngine;
using UnityEngine.Audio;

namespace _02.Scripts.Monster.Presentation
{
    [CreateAssetMenu(menuName = "Monster/Normal Monster Game Feel Profile")]
    public sealed class NormalMonsterFeelProfile : ScriptableObject
    {

        [Header("네온 색상")]
        [SerializeField] private Color _meleeColor = new Color(1f, 0.12f, 0.58f, 1f);
        [SerializeField] private Color _rangeColor = new Color(0.08f, 0.82f, 1f, 1f);
        [SerializeField] private Color _accentColor = new Color(1f, 0.65f, 0.12f, 1f);
        [SerializeField, Range(0f, 1f)] private float _outlineOpacity = 0.28f;

        [Header("전투 이펙트")]
        [SerializeField, Range(0.5f, 2f)] private float _decorationScale = 1.3f;
        [SerializeField, Range(4, 40)] private int _meleeSparks = 22;
        [SerializeField, Range(4, 40)] private int _impactSparks = 26;
        [SerializeField, Min(0.01f)] private float _trailInterval = 0.035f;
        [SerializeField, Range(0f, 0.3f)] private float _cameraShake = 0.09f;

        [Header("사운드")]
        [SerializeField] private AudioClip[] _meleeChargeSounds;
        [SerializeField] private AudioClip[] _meleeSlashSounds;
        [SerializeField] private AudioClip[] _meleeImpactSounds;
        [SerializeField] private AudioClip[] _rangeChargeSounds;
        [SerializeField] private AudioClip[] _rangeShotSounds;
        [SerializeField] private AudioClip[] _rangeImpactSounds;
        [SerializeField, Range(0f, 1f)] private float _attackVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _impactVolume = 0.82f;
        [SerializeField] private AudioMixerGroup _soundMixerGroup;
        public Color MeleeColor => _meleeColor;
        public Color RangeColor => _rangeColor;
        public Color AccentColor => _accentColor;
        public float OutlineOpacity => Mathf.Clamp01(_outlineOpacity);
        public float DecorationScale => Mathf.Clamp(_decorationScale, 0.5f, 2f);
        public int MeleeSparks => Mathf.Clamp(_meleeSparks, 4, 40);
        public int ImpactSparks => Mathf.Clamp(_impactSparks, 4, 40);
        public float TrailInterval => Mathf.Max(0.01f, _trailInterval);
        public float CameraShake => Mathf.Clamp(_cameraShake, 0f, 0.3f);
        public float AttackVolume => Mathf.Clamp01(_attackVolume);
        public float ImpactVolume => Mathf.Clamp01(_impactVolume);
        public AudioMixerGroup SoundMixerGroup => _soundMixerGroup;
        public AudioClip[] MeleeChargeSounds => _meleeChargeSounds;
        public AudioClip[] MeleeSlashSounds => _meleeSlashSounds;
        public AudioClip[] MeleeImpactSounds => _meleeImpactSounds;
        public AudioClip[] RangeChargeSounds => _rangeChargeSounds;
        public AudioClip[] RangeShotSounds => _rangeShotSounds;
        public AudioClip[] RangeImpactSounds => _rangeImpactSounds;
    }
}
