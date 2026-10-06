using UnityEngine;
using UnityEngine.Audio;

namespace _02.Scripts.Monster.NormalMonsters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonsterEffectPlayback : MonoBehaviour
    {
        [Header("이펙트 프레임")]
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(1f)] private float _frameRate = 24f;

        [Header("사운드")]
        [SerializeField] private AudioClip _sound;
        [SerializeField, Range(0f, 1f)] private float _soundVolume = 0.75f;
        [SerializeField] private AudioMixerGroup _soundMixerGroup;

        private SpriteRenderer _spriteRenderer;
        private float _age;
        private int _frameIndex;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_frames == null || _frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            _age = 0f;
            _frameIndex = 0;
            _spriteRenderer.sprite = _frames[0];
            PlaySound();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || _frames == null || _frames.Length == 0)
            {
                return;
            }

            _age += Time.deltaTime;
            int nextFrame = Mathf.FloorToInt(_age * Mathf.Max(1f, _frameRate));

            if (nextFrame >= _frames.Length)
            {
                Destroy(gameObject);
                return;
            }

            if (nextFrame != _frameIndex)
            {
                _frameIndex = nextFrame;
                _spriteRenderer.sprite = _frames[_frameIndex];
            }
        }

        private void PlaySound()
        {
            if (_sound == null || _soundVolume <= 0f)
            {
                return;
            }

            // 이펙트가 끝나거나 몬스터가 사라져도 소리는 끝까지 재생합니다.
            GameObject soundObject = new GameObject("MonsterEffectSound");
            soundObject.transform.position = transform.position;

            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = _sound;
            source.volume = Mathf.Clamp01(_soundVolume);
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _soundMixerGroup;
            source.Play();

            Destroy(soundObject, _sound.length + 0.1f);
        }
    }
}
