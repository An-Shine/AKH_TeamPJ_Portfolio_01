using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class EndingCutscenePlayer : MonoBehaviour
{
    private const string MainMenuSceneName = "03.MainMenu";
    [SerializeField] private GameObject _endingCanvas;
    [SerializeField, Min(0f)] private float _deathAnimationDelay = 2f;
    [SerializeField] private AudioSource[] _gameAudioSources;
    [SerializeField] private Behaviour[] _disableDuringEnding;
    [SerializeField] private string _nextSceneName = MainMenuSceneName;
    [SerializeField] private UnityEvent _onFinished;

    private VideoPlayer _videoPlayer;
    private float _previousTimeScale;
    private bool _requested;
    private bool _paused;
    private bool _failed;
    private bool _completed;

    private string NextSceneName => string.IsNullOrWhiteSpace(_nextSceneName)
        ? MainMenuSceneName : _nextSceneName.Trim();

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
        if (_requested)
        {
            return;
        }

        if (_endingCanvas == null || _videoPlayer.clip == null)
        {
            Debug.LogError("엔딩 Canvas와 Video Clip을 연결하세요.", this);
            return;
        }

        if (_endingCanvas == gameObject || transform.IsChildOf(_endingCanvas.transform))
        {
            Debug.LogError("엔딩 재생 스크립트는 EndingCanvas 밖의 활성 오브젝트에 두세요.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(NextSceneName))
        {
            Debug.LogError($"다음 씬을 빌드 씬 목록에 추가하세요: {NextSceneName}", this);
            return;
        }

        _requested = true;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return new WaitForSeconds(_deathAnimationDelay);

        _failed = false;
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
            yield break;
        }

        _previousTimeScale = Time.timeScale;
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
        if (!_paused || _completed)
        {
            return;
        }

        _completed = true;
        string nextSceneName = NextSceneName;
        RestoreTimeScale();
        Time.timeScale = 1f;
        _videoPlayer.Stop();
        if (_endingCanvas != null)
        {
            _endingCanvas.SetActive(false);
        }

        // 이전에 다음 씬을 비우고 등록한 완료 이벤트도 유지합니다.
        if (string.IsNullOrWhiteSpace(_nextSceneName))
        {
            _onFinished?.Invoke();
            if (this == null)
                return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"다음 씬을 빌드 씬 목록에 추가하세요: {nextSceneName}", this);
            return;
        }

        if (SceneFlowManager.Instance != null)
        {
            // 완료 이벤트가 이미 씬 전환을 요청했다면 중복 요청하지 않습니다.
            if (!SceneFlowManager.Instance.IsTransitioning)
                SceneFlowManager.Instance.LoadScene(nextSceneName);
            return;
        }

        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        _failed = true;
        Debug.LogError($"엔딩 영상 재생 오류: {message}", this);

        if (_paused)
        {
            RestoreTimeScale();
            _endingCanvas.SetActive(false);
        }
    }

    private void RestoreTimeScale()
    {
        if (!_paused)
        {
            return;
        }

        Time.timeScale = _previousTimeScale;
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
