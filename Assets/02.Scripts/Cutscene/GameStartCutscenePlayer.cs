using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-300)]
[DisallowMultipleComponent]
public sealed class GameStartCutscenePlayer : MonoBehaviour
{
    private static readonly string[] ControlTypeNames =
    {
        "PlayerController", "PlayerAim", "PlayerInput", "PauseTabController", "DialogueHandler"
    };

    [Header("시작 영상")]
    [SerializeField] private VideoClip _cutsceneClip;
    [SerializeField] private string _resourcePath = "Cutscene/DesertIntro";
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _playOncePerSession = true;

    [Header("컷씬 동안 멈출 항목")]
    [SerializeField] private bool _autoFindControls = true;
    [SerializeField] private Behaviour[] _disableDuringCutscene = new Behaviour[0];
    [SerializeField] private bool _muteGameAudio = true;

    [Header("재생과 건너뛰기")]
    [SerializeField, Range(0f, 1f)] private float _cutsceneVolume = 1f;
    [SerializeField, Min(1f)] private float _prepareTimeout = 20f;
    [SerializeField] private bool _allowSkip = true;
    [SerializeField, Min(0.1f)] private float _skipHoldDuration = 0.5f;
    [SerializeField] private UnityEvent _onGameplayReady = new UnityEvent();

    private static bool _hasPlayedInSession;
    private static GameStartCutscenePlayer _activeCutscene;

    private readonly List<Behaviour> _suspendedControls = new List<Behaviour>();
    private readonly Dictionary<AudioSource, bool> _previousAudioMutes = new Dictionary<AudioSource, bool>();
    private readonly List<AudioSource> _pausedAudio = new List<AudioSource>();
    private VideoPlayer _videoPlayer;
    private AudioSource _videoAudio;
    private GameObject _overlayRoot;
    private RawImage _videoImage;
    private RenderTexture _renderTexture;
    private Coroutine _prepareRoutine;
    private float _previousTimeScale;
    private float _skipHeldTime;
    private bool _playing;
    private bool _hasFinished;

    public bool IsPlaying => _playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        _hasPlayedInSession = false;
        _activeCutscene = null;
    }

    private void Start()
    {
        if (_playOnStart)
        {
            PlayCutscene();
        }
    }

    public void PlayCutscene()
    {
        if (!isActiveAndEnabled || _playing || _hasFinished)
            return;

        if (_activeCutscene != null && _activeCutscene != this)
            return;

        if (_playOncePerSession && _hasPlayedInSession)
        {
            _hasFinished = true;
            _onGameplayReady?.Invoke();
            return;
        }

        if (_cutsceneClip == null && !string.IsNullOrWhiteSpace(_resourcePath))
        {
            _cutsceneClip = Resources.Load<VideoClip>(_resourcePath.Trim());
        }

        if (_cutsceneClip == null)
        {
            Debug.LogWarning("시작 Cutscene Clip을 연결하세요. 영상이 없어 게임 플레이를 바로 시작합니다.", this);
            _hasFinished = true;
            _onGameplayReady?.Invoke();
            return;
        }

        EnsurePresentation();
        _previousTimeScale = Time.timeScale;
        _playing = true;
        _activeCutscene = this;
        SuspendGameplay();
        Time.timeScale = 0f;
        _videoImage.enabled = false;
        _overlayRoot.SetActive(true);
        ClearRenderTexture();
        _prepareRoutine = StartCoroutine(PrepareAndPlay());
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
                Debug.LogWarning("시작 영상 준비 시간이 초과돼 게임 플레이를 시작합니다.", this);
                FinishPlayback(false);
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

    private void Update()
    {
        if (!_playing || !_allowSkip)
            return;

        if (!IsSkipKeyHeld())
        {
            _skipHeldTime = 0f;
            return;
        }

        _skipHeldTime += Time.unscaledDeltaTime;

        if (_skipHeldTime >= _skipHoldDuration)
        {
            SkipCutscene();
        }
    }

    private static bool IsSkipKeyHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Escape);
#else
        return false;
#endif
    }

    public void SkipCutscene()
    {
        if (_playing && _allowSkip)
        {
            FinishPlayback(true);
        }
    }

    private void OnFirstFrame(VideoPlayer source, long frameIndex)
    {
        if (_playing)
        {
            _videoImage.enabled = true;
            source.sendFrameReadyEvents = false;
        }
    }

    private void OnVideoEnded(VideoPlayer source)
    {
        FinishPlayback(true);
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("시작 영상 재생 오류로 게임 플레이를 시작합니다: " + message, this);
        FinishPlayback(false);
    }

    private void FinishPlayback(bool markViewed, bool notify = true)
    {
        if (!_playing)
            return;

        _playing = false;
        _hasFinished = true;

        if (_activeCutscene == this)
        {
            _activeCutscene = null;
        }

        if (markViewed)
        {
            _hasPlayedInSession = true;
        }

        StopPreparation();

        if (_videoPlayer != null)
        {
            _videoPlayer.Stop();
        }

        if (_videoAudio != null)
        {
            _videoAudio.Stop();
        }

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }

        RestoreGameplay();

        if (notify)
        {
            _onGameplayReady?.Invoke();
        }
    }

    private void SuspendGameplay()
    {
        _suspendedControls.Clear();

        if (_disableDuringCutscene != null)
        {
            foreach (Behaviour control in _disableDuringCutscene)
            {
                SuspendControl(control);
            }
        }

        if (_autoFindControls)
        {
            foreach (MonoBehaviour control in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string typeName = control.GetType().Name;

                foreach (string supportedTypeName in ControlTypeNames)
                {
                    if (typeName == supportedTypeName)
                    {
                        SuspendControl(control);
                        break;
                    }
                }
            }
        }

        _previousAudioMutes.Clear();
        _pausedAudio.Clear();

        if (!_muteGameAudio)
            return;

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (source == _videoAudio)
                continue;

            _previousAudioMutes.Add(source, source.mute);

            if (source.isPlaying)
            {
                source.Pause();
                _pausedAudio.Add(source);
            }

            source.mute = true;
        }
    }

    private void SuspendControl(Behaviour control)
    {
        if (control == null || control == this || control == _videoPlayer || !control.enabled)
            return;

        if (!_suspendedControls.Contains(control))
        {
            _suspendedControls.Add(control);
            control.enabled = false;
        }
    }

    private void RestoreGameplay()
    {
        Time.timeScale = _previousTimeScale;

        foreach (Behaviour control in _suspendedControls)
        {
            if (control != null)
            {
                control.enabled = true;
            }
        }

        foreach (KeyValuePair<AudioSource, bool> entry in _previousAudioMutes)
        {
            if (entry.Key != null)
            {
                entry.Key.mute = entry.Value;
            }
        }

        foreach (AudioSource source in _pausedAudio)
        {
            if (source != null)
            {
                source.UnPause();
            }
        }

        _suspendedControls.Clear();
        _previousAudioMutes.Clear();
        _pausedAudio.Clear();
    }

    private void EnsurePresentation()
    {
        if (_videoPlayer != null)
            return;

        _overlayRoot = new GameObject("GameStartCutsceneOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        _overlayRoot.transform.SetParent(transform, false);
        Canvas canvas = _overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        GameObject backdrop = new GameObject("BlackBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(_overlayRoot.transform, false);
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = Color.black;

        GameObject videoSurface = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        videoSurface.transform.SetParent(_overlayRoot.transform, false);
        Stretch(videoSurface.GetComponent<RectTransform>());
        _videoImage = videoSurface.GetComponent<RawImage>();
        _videoImage.raycastTarget = false;
        AspectRatioFitter fitter = videoSurface.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)_cutsceneClip.width / _cutsceneClip.height;

        _renderTexture = new RenderTexture((int)_cutsceneClip.width, (int)_cutsceneClip.height, 0, RenderTextureFormat.ARGB32);
        _renderTexture.name = "GameStartCutsceneTexture";
        _renderTexture.filterMode = FilterMode.Point;
        _renderTexture.Create();
        _videoImage.texture = _renderTexture;

        GameObject playback = new GameObject("GameStartCutscenePlayback", typeof(VideoPlayer), typeof(AudioSource));
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

        if (_cutsceneClip.audioTrackCount > 0)
        {
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, _videoAudio);
        }
        else
        {
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

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

    private void OnDisable()
    {
        FinishPlayback(false, false);
    }

    private void OnDestroy()
    {
        FinishPlayback(false, false);

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
