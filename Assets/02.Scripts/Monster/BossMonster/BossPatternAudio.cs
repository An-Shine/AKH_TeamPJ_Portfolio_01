using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02.Scripts.Monster.BossMonster
{
    public sealed class BossPatternAudio : MonoBehaviour
    {
        private const int VoiceCount = 16;
        private readonly AudioSource[] _voices = new AudioSource[VoiceCount];
        private readonly bool[] _pausedVoices = new bool[VoiceCount];
        private AudioSource _loop;
        private BossEncounterProfile _profile;
        private BossPatternSound _sound;
        private float _targetLoopVolume;
        private float _nextImpact;
        private float _retireAge;
        private int _nextVoice;
        private bool _paused;
        private bool _pausedLoop;
        private bool _retiring;
        public static BossPatternAudio Create(GameObject owner, BossEncounterProfile profile)
        {
            GameObject root = new GameObject(owner.name + "_BossAudio");
            SceneManager.MoveGameObjectToScene(root, owner.scene);
            BossPatternAudio audio = root.AddComponent<BossPatternAudio>();
            audio._profile = profile;
            audio._loop = audio.CreateSource();
            audio._loop.loop = true;
            audio._loop.volume = 0f;
            for (int i = 0; i < VoiceCount; i++)
            {
                audio._voices[i] = audio.CreateSource();
            }

            return audio;
        }

        public void Begin(BossEncounterMove move)
        {
            _sound = move.Sound;
            _retiring = false;
            _retireAge = 0f;
            _nextImpact = 0f;
            Play(_sound.Start, 1f);
            _loop.Stop();
            _loop.clip = _sound.Loop;
            _loop.volume = 0f;
            _targetLoopVolume = _profile.SoundVolume * 0.58f;
            if (_loop.clip != null)
            {
                // 시작음을 재생하는 동안 루프를 겹쳐 올려 패턴 중간의 무음을 없앱니다.
                _loop.Play();
            }
        }

        public void Impact(float volume = 1f)
        {
            if (_sound == null || Time.time < _nextImpact)
            {
                return;
            }

            _nextImpact = Time.time + 0.1f;
            Play(_sound.Impact, volume);
        }

        public void Charge(float progress, bool ultimate)
        {
            if (_loop == null || _sound == null)
                return;
            _loop.pitch = Mathf.Lerp(ultimate ? 0.75f : 0.92f, ultimate ? 1.22f : 1.05f, Mathf.Clamp01(progress));
            _targetLoopVolume = _profile.SoundVolume * Mathf.Lerp(0.32f, ultimate ? 0.68f : 0.52f, progress);
        }

        public void End(bool playTail)
        {
            _targetLoopVolume = 0f;
            if (playTail && _sound != null)
            {
                Play(_sound.End, 0.85f);
            }

            _sound = null;
        }

        public void Play(AudioClip clip, float volume)
        {
            if (clip == null || _profile == null)
            {
                return;
            }

            AudioSource source = _voices[_nextVoice];
            _pausedVoices[_nextVoice] = false;
            _nextVoice = (_nextVoice + 1) % VoiceCount;
            source.Stop();
            source.clip = clip;
            source.pitch = 1f;
            source.volume = Mathf.Clamp01(_profile.SoundVolume * volume);
            source.Play();
        }

        public void Retire()
        {
            End(false);
            _retiring = true;
            _retireAge = 0f;
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _profile.MixerGroup;
            return source;
        }

        private void Update()
        {
            bool shouldPause = Time.timeScale <= 0f;
            if (_paused != shouldPause)
            {
                SetPaused(shouldPause);
            }

            if (_paused)
            {
                return;
            }

            _loop.volume = Mathf.MoveTowards(_loop.volume, _targetLoopVolume, Time.deltaTime * 4f);
            if (_loop.volume <= 0f && _targetLoopVolume <= 0f && _loop.isPlaying)
            {
                _loop.Stop();
            }

            if (!_retiring)
            {
                return;
            }

            _retireAge += Time.deltaTime;
            bool busy = _loop.isPlaying;
            for (int i = 0; i < VoiceCount; i++)
            {
                busy |= _voices[i].isPlaying;
            }

            if (!busy || _retireAge > 12f)
            {
                Destroy(gameObject);
            }
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            if (paused)
            {
                _pausedLoop = _loop.isPlaying;
                _loop.Pause();
                for (int i = 0; i < VoiceCount; i++)
                {
                    _pausedVoices[i] = _voices[i].isPlaying;
                    _voices[i].Pause();
                }

                return;
            }

            if (_pausedLoop)
            {
                _loop.UnPause();
            }

            for (int i = 0; i < VoiceCount; i++)
            {
                if (_pausedVoices[i])
                {
                    _voices[i].UnPause();
                }
            }
        }
    }
}
