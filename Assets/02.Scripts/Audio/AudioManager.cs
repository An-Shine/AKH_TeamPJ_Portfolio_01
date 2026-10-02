using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _loopSfxSource;
    [SerializeField] private List<AudioSource> _sfxSources;
    [SerializeField] private List<BGMClip> _bgmEntries;
    [SerializeField] private List<SFXClip> _sfxEntries;

    private Dictionary<BGMType, AudioClip> _bgmDictionary;
    private Dictionary<SFXType, AudioClip> _sfxDictionary;

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
    }

    public void PlayBGM(BGMType type)
    {
        if (!_bgmDictionary.TryGetValue(type, out var clip))
        {
            Debug.LogWarning("BGM Clip: " + type + " is not found!");
            return;
        }

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

        var source = _sfxSources.FirstOrDefault(x => !x.isPlaying);

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

        var source = _sfxSources.FirstOrDefault(x => !x.isPlaying);

        if (_loopSfxSource == null)
            return;

        _loopSfxSource.clip = clip;
        _loopSfxSource.Play();
    }

    public void StopLoopSFX()
    {
        _loopSfxSource.Stop();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void StopSFX()
    {
        _sfxSources.ForEach(x => x.Stop());
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

    public void PlayButtonPressed()
    {
        PlaySFX(SFXType.ButtonPressed);
    }

    public void PlayButtonReleased()
    {
        PlaySFX(SFXType.ButtonReleased);
    }
}