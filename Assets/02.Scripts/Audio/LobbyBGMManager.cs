using UnityEngine;

public class LobbyBGMManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    private static LobbyBGMManager _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        if (_audioSource == null) return;
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
    }

    public void Play()
    {
        if (_audioSource == null || _audioSource.clip == null || _audioSource.isPlaying) return;
        _audioSource.Play();
    }

    public void Stop()
    {
        if (_audioSource != null) _audioSource.Stop();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
