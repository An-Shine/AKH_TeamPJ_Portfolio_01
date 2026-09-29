using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;
    
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private List<AudioSource> _sfxSources;
    
    [SerializeField] private List<BGMClip> _bgmEntries;
    [SerializeField] private List<SFXClip> _sfxEntries;

    private Dictionary<BGMType, AudioClip> _bgmDictionary;
    private Dictionary<SFXType, AudioClip> _sfxDictionary;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }

        _instance = this;
        _bgmDictionary = _bgmEntries.ToDictionary(x => x.Type, x => x.Clip);
        _sfxDictionary = _sfxEntries.ToDictionary(x => x.Type, x => x.Clip);
    }
    
    public void PlayBGM(BGMType type)
    {
        if (!_bgmDictionary.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning("BGM Clip: " + type + " is not found!");
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.Play();
    }
    
    public void PlaySFX(SFXType type)
    {
        if (!_sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning("SFX Clip: " + type + " is not found!");
            return;
        }

        AudioSource source = _sfxSources.FirstOrDefault(x => !x.isPlaying);

        if (source == null)
            return;

        source.clip = clip;
        source.Play();
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

    public void PlayButtonPressed()
    {
        PlaySFX(SFXType.ButtonPressed);
    }

    public void PlayButtonReleased()
    {
        PlaySFX(SFXType.ButtonReleased);
    }
}