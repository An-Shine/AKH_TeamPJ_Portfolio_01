using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class EndingCutscenePlayer : MonoBehaviour
{
    [SerializeField] private GameObject _endingCanvas;
    [SerializeField, Min(0f)] private float _deathAnimationDelay = 2f;
    [SerializeField] private AudioSource[] _gameAudioSources;
    [SerializeField] private Behaviour[] _disableDuringEnding;
    [SerializeField] private GameStateManager _gameStateManager;

    private VideoPlayer _videoPlayer;
    private float _previousTimeScale;
    private bool _requested;
    private bool _paused;
    private bool _failed;
    private bool _completed;
    private bool _timeScaleCaptured;

    private void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = false;
        _videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        _videoPlayer.loopPointReached += OnVideoFinished;
        _videoPlayer.errorReceived += OnVideoError;

        if (_endingCanvas != null)
        {
            _endingCanvas.SetActive(false);
        }
    }

    public void PlayAfterBossDeath()
    {
        if (_requested || _completed)
            return;

        if (_endingCanvas == null || _videoPlayer == null || _videoPlayer.clip == null)
        {
            Debug.LogError("엔딩 Canvas와 Video Clip을 연결하세요.", this);
            return;
        }

        if (_endingCanvas == gameObject || transform.IsChildOf(_endingCanvas.transform))
        {
            Debug.LogError("엔딩 재생 스크립트는 EndingCanvas 밖의 활성 오브젝트에 두세요.", this);
            return;
        }

        if (_gameStateManager == null)
        {
            _gameStateManager = FindFirstObjectByType<GameStateManager>();
        }

        if (_gameStateManager == null)
        {
            Debug.LogError("스테이지 클리어 화면을 표시할 GameStateManager를 연결하세요.", this);
            return;
        }

        _previousTimeScale = Time.timeScale;

        if (_gameStateManager != null)
        {
            _gameStateManager.BeginEnding();

            if (_gameStateManager.CurrentState != GameStateManager.GameState.Ending)
            {
                return;
            }
        }

        _timeScaleCaptured = true;
        _requested = true;
        _failed = false;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return new WaitForSecondsRealtime(_deathAnimationDelay);

        if (_failed)
        {
            _requested = false;
            RestoreTimeScale();
            yield break;
        }

        _videoPlayer.Prepare();
        float timeout = Time.realtimeSinceStartup + 20f;

        while (!_videoPlayer.isPrepared && !_failed)
        {
            if (Time.realtimeSinceStartup >= timeout)
            {
                Debug.LogError("엔딩 영상 준비 시간이 초과됐습니다. 영상 파일과 Console을 확인하세요.", this);
                _failed = true;
                break;
            }

            yield return null;
        }

        if (_failed)
        {
            _requested = false;
            RestoreTimeScale();
            yield break;
        }

        _paused = true;
        Time.timeScale = 0f;

        if (_disableDuringEnding != null)
        {
            foreach (Behaviour component in _disableDuringEnding)
            {
                if (component != null && component != this && component != _videoPlayer)
                {
                    component.enabled = false;
                }
            }
        }

        if (_gameAudioSources != null)
        {
            foreach (AudioSource source in _gameAudioSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }

        _endingCanvas.SetActive(true);
        _videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        if (!_paused || _completed || _failed)
            return;

        _completed = true;
        _videoPlayer.Stop();

        if (_endingCanvas != null)
        {
            _endingCanvas.SetActive(false);
        }

        RestoreTimeScale();

        if (_gameStateManager != null)
        {
            _gameStateManager.CompleteGame();
        }

    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        _failed = true;
        Debug.LogError($"엔딩 영상 재생 오류: {message}", this);

        RestoreTimeScale();

        if (_endingCanvas != null)
        {
            _endingCanvas.SetActive(false);
        }
    }

    private void RestoreTimeScale()
    {
        if (!_timeScaleCaptured)
            return;

        Time.timeScale = _previousTimeScale;
        _timeScaleCaptured = false;
        _paused = false;
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.loopPointReached -= OnVideoFinished;
            _videoPlayer.errorReceived -= OnVideoError;
        }

        RestoreTimeScale();
    }
}
