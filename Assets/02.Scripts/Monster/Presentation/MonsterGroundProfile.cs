using UnityEngine;
using UnityEngine.Audio;

namespace _02.Scripts.Monster.Presentation
{
    [CreateAssetMenu(menuName = "Monster/Ground Presentation Profile")]
    public sealed class MonsterGroundProfile : ScriptableObject
    {

        [Header("발 위치와 보행 간격")]
        [Tooltip("스프라이트 높이 중 중심에서 발밑까지 내려갈 비율입니다.")]
        [SerializeField, Range(0f, 0.5f)] private float _groundOffsetFactor = 0.32f;
        [Tooltip("스프라이트 너비에 대한 좌우 발 사이의 절반 간격입니다.")]
        [SerializeField, Range(0f, 0.4f)] private float _footSeparationFactor = 0.1f;
        [Tooltip("스프라이트 너비에 대한 한 걸음의 이동 거리입니다.")]
        [SerializeField, Range(0.05f, 1f)] private float _stepDistanceFactor = 0.22f;
        [Tooltip("기본값은 실제 이동 거리로 자동 처리합니다. Animation Event를 사용할 때만 켜세요.")]
        [SerializeField] private bool _useAnimationEvents;

        [Header("바닥 그림자")]
        [SerializeField] private bool _showShadow = true;
        [SerializeField] private Color _shadowColor = new Color(0f, 0.02f, 0.04f, 0.32f);
        [SerializeField, Range(0.1f, 1.5f)] private float _shadowWidthFactor = 0.58f;
        [SerializeField, Range(0.02f, 0.6f)] private float _shadowHeightFactor = 0.16f;
        [SerializeField, Range(0f, 0.2f)] private float _shadowPulse = 0.04f;
        [SerializeField, Range(0f, 1f)] private float _deadShadowOpacity = 0.6f;

        [Header("남는 발자국")]
        [SerializeField] private bool _showFootprints = true;
        [SerializeField] private Color _footprintColor = new Color(0.04f, 0.12f, 0.17f, 0.35f);
        [SerializeField, Range(0.02f, 0.3f)] private float _footprintWidthFactor = 0.085f;
        [SerializeField, Min(0.1f)] private float _footprintLifetime = 2f;

        [Header("발밑 먼지")]
        [SerializeField] private bool _showDust = true;
        [SerializeField] private Color _dustColor = new Color(0.65f, 0.85f, 0.95f, 0.4f);
        [SerializeField, Range(0.02f, 0.5f)] private float _dustWidthFactor = 0.12f;
        [SerializeField, Min(0.05f)] private float _dustLifetime = 0.25f;
        [SerializeField, Range(1, 4)] private int _dustPuffsPerStep = 2;

        [Header("발소리")]
        [Tooltip("여러 클립을 넣으면 직전 소리를 피해서 무작위로 재생합니다. 비워 두면 소리만 생략합니다.")]
        [SerializeField] private AudioClip[] _stepSounds = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float _stepVolume = 0.25f;
        [SerializeField, Range(0.5f, 1.5f)] private float _minimumPitch = 0.95f;
        [SerializeField, Range(0.5f, 1.5f)] private float _maximumPitch = 1.05f;
        [SerializeField] private AudioMixerGroup _soundMixerGroup;

        [Header("표시 순서")]
        [Tooltip("본체와 같은 Sorting Layer에서 본체보다 뒤에 표시합니다.")]
        [SerializeField, Range(-100, -1)] private int _shadowOrderOffset = -2;
        [SerializeField, Range(-100, -1)] private int _footprintOrderOffset = -3;
        [SerializeField, Range(-100, 100)] private int _dustOrderOffset = 1;
        public float GroundOffsetFactor => Mathf.Clamp(_groundOffsetFactor, 0f, 0.5f);
        public float FootSeparationFactor => Mathf.Clamp(_footSeparationFactor, 0f, 0.4f);
        public float StepDistanceFactor => Mathf.Max(0.05f, _stepDistanceFactor);
        public bool UseAnimationEvents => _useAnimationEvents;
        public bool ShowShadow => _showShadow;
        public Color ShadowColor => _shadowColor;
        public float ShadowWidthFactor => Mathf.Max(0.1f, _shadowWidthFactor);
        public float ShadowHeightFactor => Mathf.Max(0.02f, _shadowHeightFactor);
        public float ShadowPulse => Mathf.Clamp(_shadowPulse, 0f, 0.2f);
        public float DeadShadowOpacity => Mathf.Clamp01(_deadShadowOpacity);
        public bool ShowFootprints => _showFootprints;
        public Color FootprintColor => _footprintColor;
        public float FootprintWidthFactor => Mathf.Max(0.02f, _footprintWidthFactor);
        public float FootprintLifetime => Mathf.Max(0.1f, _footprintLifetime);
        public bool ShowDust => _showDust;
        public Color DustColor => _dustColor;
        public float DustWidthFactor => Mathf.Max(0.02f, _dustWidthFactor);
        public float DustLifetime => Mathf.Max(0.05f, _dustLifetime);
        public int DustPuffsPerStep => Mathf.Clamp(_dustPuffsPerStep, 1, 4);
        public AudioClip[] StepSounds => _stepSounds;
        public float StepVolume => Mathf.Clamp01(_stepVolume);
        public float MinimumPitch => Mathf.Clamp(_minimumPitch, 0.5f, 1.5f);
        public float MaximumPitch => Mathf.Clamp(_maximumPitch, MinimumPitch, 1.5f);
        public AudioMixerGroup SoundMixerGroup => _soundMixerGroup;
        public int ShadowOrderOffset => Mathf.Min(-1, _shadowOrderOffset);
        public int FootprintOrderOffset => Mathf.Min(-1, _footprintOrderOffset);
        public int DustOrderOffset => _dustOrderOffset;
    }
}
