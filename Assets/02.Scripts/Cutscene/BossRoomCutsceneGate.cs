using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BossRoomCutsceneGate : MonoBehaviour
{
    [Header("연결 대상")]
    [SerializeField] private VideoClip _cutsceneClip;
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private Transform _bossSpawnPoint;
    [SerializeField] private GameObject _bossFightRoot;
    [SerializeField] private Camera _gameplayCamera;

    [Header("컷신 동안 멈출 항목")]
    [Tooltip("플레이어 이동, 조준, 발사 스크립트를 넣으세요. 이 스크립트 자신은 넣지 마세요.")]
    [SerializeField] private Behaviour[] _disableDuringCutscene = new Behaviour[0];
    [Tooltip("컷신 동안 잠시 멈출 게임 BGM 또는 환경음 AudioSource를 넣으세요.")]
    [SerializeField] private AudioSource[] _pauseDuringCutscene = new AudioSource[0];

    [Header("재생 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float _cutsceneVolume = 1f;
    [Min(1f)]
    [SerializeField] private float _prepareTimeout = 15f;
    [Tooltip("일반 Transform 추적 카메라에 사용하세요. Cinemachine 카메라는 안내문을 확인하세요.")]
    [SerializeField] private bool _snapGameplayCamera = true;

    [Header("기존 시스템 연결 (필요할 때만)")]
    [SerializeField] private UnityEvent _onPlayerTeleported = new UnityEvent();
    [SerializeField] private UnityEvent _onCombatStarted = new UnityEvent();

    private readonly List<Behaviour> _reenableAfterCutscene = new List<Behaviour>();
    private readonly List<AudioSource> _resumeAfterCutscene = new List<AudioSource>();
    private VideoPlayer _videoPlayer;
    private AudioSource _videoAudio;
    private GameObject _overlayRoot;
    private RawImage _videoImage;
    private RenderTexture _renderTexture;
    private Coroutine _prepareRoutine;
    private float _previousTimeScale;
    private bool _playing;
    private bool _hasPlayed;

    public bool IsPlaying => _playing;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (_bossFightRoot != null)
        {
            if (transform.IsChildOf(_bossFightRoot.transform))
            {
                Debug.LogError("BossCutsceneGate는 Boss Fight Root 밖에 배치하세요.", this);
                enabled = false;
                return;
            }

            _bossFightRoot.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_playerRoot == null || _playing || _hasPlayed)
        {
            return;
        }

        if (other.transform == _playerRoot || other.transform.IsChildOf(_playerRoot))
        {
            PlayCutscene();
        }
    }

    public void PlayCutscene()
    {
        if (_playing || _hasPlayed || !isActiveAndEnabled)
        {
            return;
        }

        if (_cutsceneClip == null || _playerRoot == null || _bossSpawnPoint == null || _bossFightRoot == null)
        {
            Debug.LogError("Cutscene Clip, Player Root, Boss Spawn Point, Boss Fight Root를 연결하세요.", this);
            return;
        }

        EnsurePresentation();
        _playing = true;
        _previousTimeScale = Time.timeScale;
        PauseGameplay();
        Time.timeScale = 0f;
        StopPlayerMotion();

        ClearRenderTexture();
        _videoImage.enabled = false;
        _overlayRoot.SetActive(true);
        _videoPlayer.clip = _cutsceneClip;
        _prepareRoutine = StartCoroutine(PrepareAndPlay());
    }

    // UI 버튼을 만들 경우 이 함수를 On Click에 연결할 수 있습니다.
    public void SkipCutscene()
    {
        if (_playing)
        {
            EnterBossRoom();
        }
    }

    private IEnumerator PrepareAndPlay()
    {
        float deadline = Time.realtimeSinceStartup + _prepareTimeout;
        _videoPlayer.Prepare();

        while (_playing && !_videoPlayer.isPrepared)
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                _prepareRoutine = null;
                Debug.LogError("컷신 준비 시간이 초과됐습니다. VideoClip의 Import 설정을 확인하세요.", this);
                AbortPlayback();
                yield break;
            }

            yield return null;
        }

        _prepareRoutine = null;

        if (_playing)
        {
            _videoPlayer.Play();
        }
    }

    private void OnFirstFrame(VideoPlayer source, long frameIndex)
    {
        if (_playing)
        {
            _videoImage.enabled = true;
        }
    }

    private void OnVideoEnded(VideoPlayer source)
    {
        if (_playing)
        {
            EnterBossRoom();
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("보스 컷신 재생 오류: " + message, this);
        AbortPlayback();
    }

    private void EnterBossRoom()
    {
        if (_playerRoot == null || _bossSpawnPoint == null)
        {
            AbortPlayback();
            return;
        }

        _playing = false;
        _hasPlayed = true;
        StopPreparation();
        _videoImage.enabled = false;
        _videoPlayer.Stop();
        _videoAudio.Stop();

        Vector3 previousPosition = _playerRoot.position;
        _playerRoot.position = _bossSpawnPoint.position;
        Rigidbody2D playerBody = _playerRoot.GetComponent<Rigidbody2D>();

        if (playerBody != null)
        {
            playerBody.position = _bossSpawnPoint.position;
        }

        StopPlayerMotion();
        Physics2D.SyncTransforms();

        if (_snapGameplayCamera && _gameplayCamera != null)
        {
            Vector3 displacement = _playerRoot.position - previousPosition;
            displacement.z = 0f;
            _gameplayCamera.transform.position += displacement;
        }

        _onPlayerTeleported.Invoke();

        if (_bossFightRoot != null)
        {
            _bossFightRoot.SetActive(true);
        }

        RestoreGameplay();
        _onCombatStarted.Invoke();
        _overlayRoot.SetActive(false);
    }

    private void PauseGameplay()
    {
        _reenableAfterCutscene.Clear();
        _resumeAfterCutscene.Clear();

        foreach (Behaviour component in _disableDuringCutscene)
        {
            if (component != null && component != this && component.enabled)
            {
                if (!_reenableAfterCutscene.Contains(component))
                {
                    _reenableAfterCutscene.Add(component);
                    component.enabled = false;
                }
            }
        }

        foreach (AudioSource source in _pauseDuringCutscene)
        {
            if (source != null && source != _videoAudio && source.isPlaying)
            {
                if (!_resumeAfterCutscene.Contains(source))
                {
                    _resumeAfterCutscene.Add(source);
                    source.Pause();
                }
            }
        }
    }

    private void RestoreGameplay()
    {
        Time.timeScale = _previousTimeScale;

        foreach (Behaviour component in _reenableAfterCutscene)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }

        foreach (AudioSource source in _resumeAfterCutscene)
        {
            if (source != null)
            {
                source.UnPause();
            }
        }

        _reenableAfterCutscene.Clear();
        _resumeAfterCutscene.Clear();
    }

    private void StopPlayerMotion()
    {
        if (_playerRoot == null)
        {
            return;
        }

        Rigidbody2D playerBody = _playerRoot.GetComponent<Rigidbody2D>();

        if (playerBody == null)
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        playerBody.linearVelocity = Vector2.zero;
#else
        playerBody.velocity = Vector2.zero;
#endif
        playerBody.angularVelocity = 0f;
    }

    private void EnsurePresentation()
    {
        if (_videoPlayer != null)
        {
            return;
        }

        _overlayRoot = new GameObject("BossCutsceneOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        _overlayRoot.transform.SetParent(transform, false);
        Canvas canvas = _overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        GameObject backdrop = new GameObject("BlackBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(_overlayRoot.transform, false);
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = Color.black;
        backdrop.GetComponent<Image>().raycastTarget = true;

        GameObject videoSurface = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        videoSurface.transform.SetParent(_overlayRoot.transform, false);
        Stretch(videoSurface.GetComponent<RectTransform>());
        _videoImage = videoSurface.GetComponent<RawImage>();
        _videoImage.raycastTarget = false;
        AspectRatioFitter fitter = videoSurface.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)_cutsceneClip.width / _cutsceneClip.height;

        _renderTexture = new RenderTexture((int)_cutsceneClip.width, (int)_cutsceneClip.height, 0, RenderTextureFormat.ARGB32);
        _renderTexture.name = "BossCutsceneVideoTexture";
        _renderTexture.filterMode = FilterMode.Point;
        _renderTexture.Create();
        _videoImage.texture = _renderTexture;

        GameObject playback = new GameObject("CutscenePlayback", typeof(VideoPlayer), typeof(AudioSource));
        playback.transform.SetParent(transform, false);
        _videoAudio = playback.GetComponent<AudioSource>();
        _videoAudio.playOnAwake = false;
        _videoAudio.loop = false;
        _videoAudio.spatialBlend = 0f;
        _videoAudio.volume = _cutsceneVolume;
        _videoAudio.ignoreListenerPause = true;

        _videoPlayer = playback.GetComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = false;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.clip = _cutsceneClip;
        _videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _renderTexture;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.controlledAudioTrackCount = 1;
        _videoPlayer.EnableAudioTrack(0, true);
        _videoPlayer.SetTargetAudioSource(0, _videoAudio);
        _videoPlayer.sendFrameReadyEvents = true;
        _videoPlayer.frameReady += OnFirstFrame;
        _videoPlayer.loopPointReached += OnVideoEnded;
        _videoPlayer.errorReceived += OnVideoError;
        _overlayRoot.SetActive(false);
    }

    private static void Stretch(RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private void ClearRenderTexture()
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
    }

    private void StopPreparation()
    {
        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }
    }

    private void AbortPlayback()
    {
        if (!_playing)
        {
            return;
        }

        _playing = false;
        StopPreparation();
        _videoPlayer.Stop();
        _videoAudio.Stop();
        _overlayRoot.SetActive(false);
        RestoreGameplay();
    }

    private void OnDisable()
    {
        AbortPlayback();
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.frameReady -= OnFirstFrame;
            _videoPlayer.loopPointReached -= OnVideoEnded;
            _videoPlayer.errorReceived -= OnVideoError;
            _videoPlayer.targetTexture = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }
}
