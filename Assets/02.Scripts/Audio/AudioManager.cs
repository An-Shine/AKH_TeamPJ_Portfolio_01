using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _loopSfxSource;
    [SerializeField] private AudioSource _footstepSource;
    [SerializeField] private List<AudioSource> _sfxSources;
    [SerializeField] private List<BGMClip> _bgmEntries;
    [SerializeField] private List<SFXClip> _sfxEntries;

    private Dictionary<BGMType, AudioClip> _bgmDictionary;
    private Dictionary<SFXType, AudioClip> _sfxDictionary;
    private readonly HashSet<BossMonster> _activeBossBattles = new HashSet<BossMonster>();

    public float MasterVolume => AudioListener.volume;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _bgmDictionary = _bgmEntries.ToDictionary(x => x.Type, x => x.Clip);
        _sfxDictionary = _sfxEntries.ToDictionary(x => x.Type, x => x.Clip);
        if (_footstepSource == null)
        {
            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake = false;
            _footstepSource.volume = 0.35f;
            if (_loopSfxSource != null)
            {
                _footstepSource.outputAudioMixerGroup = _loopSfxSource.outputAudioMixerGroup;
            }
        }

        _footstepSource.loop = true;
    }

    private void Start()
    {
        if (_bgmDictionary.ContainsKey(BGMType.Normal)) PlayBGM(BGMType.Normal);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || PlayerManager.IsInputBlocked) SetPlayerWalking(false);
    }

    public void SetBossBattle(BossMonster boss, bool active)
    {
        if (boss == null) return;
        bool changed = active ? _activeBossBattles.Add(boss) : _activeBossBattles.Remove(boss);
        if (changed) PlayBGM(_activeBossBattles.Count > 0 ? BGMType.Boss : BGMType.Normal);
    }

    public void SetPlayerWalking(bool walking)
    {
        if (_footstepSource == null) return;
        if (!walking || Time.timeScale <= 0f || PlayerManager.IsInputBlocked)
        {
            _footstepSource.Stop();
            return;
        }

        if (!_sfxDictionary.TryGetValue(SFXType.PlayerWalk, out AudioClip clip) || clip == null) return;
        if (_footstepSource.clip == clip && _footstepSource.isPlaying) return;
        _footstepSource.clip = clip;
        _footstepSource.Play();
    }

    public void PlayBGM(BGMType type)
    {
        if (_bgmSource == null || !_bgmDictionary.TryGetValue(type, out var clip) || clip == null)
        {
            Debug.LogWarning("BGM Clip: " + type + " is not found!");
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.loop = true;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void PlaySFX(SFXType type)
    {
        if (!_sfxDictionary.TryGetValue(type, out var clip))
        {
            Debug.LogWarning("SFX Clip: " + type + " is not found!");
            return;
        }

        var source = _sfxSources.FirstOrDefault(x => x != null && !x.isPlaying);

        if (source == null)
            return;

        source.clip = clip;
        source.Play();
    }

    public void PlayLoopSFX(SFXType type)
    {
        if (!_sfxDictionary.TryGetValue(type, out var clip))
        {
            Debug.LogWarning("SFX Clip: " + type + " is not found!");
            return;
        }

        if (_loopSfxSource == null)
            return;

        _loopSfxSource.loop = true;
        _loopSfxSource.clip = clip;
        _loopSfxSource.Play();
    }

    public void StopLoopSFX()
    {
        if (_loopSfxSource != null) _loopSfxSource.Stop();
    }

    public void StopBGM()
    {
        if (_bgmSource != null) _bgmSource.Stop();
    }

    public void StopSFX()
    {
        _sfxSources.ForEach(x =>
        {
            if (x != null) x.Stop();
        });
        StopLoopSFX();
        SetPlayerWalking(false);
    }

    public void StopAll()
    {
        StopBGM();
        StopSFX();
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    private void OnDisable()
    {
        if (_footstepSource != null) _footstepSource.Stop();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public void PlayButtonPressed()
    {
        PlaySFX(SFXType.ButtonPressed);
    }

    public void PlayButtonReleased()
    {
        PlaySFX(SFXType.ButtonReleased);
    }
}