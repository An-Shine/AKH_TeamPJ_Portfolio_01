using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Dying,
        GameOver,
        GameClear,
        Ending
    }

    [Header("게임오버")]
    [SerializeField] private GameObject _gameOverUI;
    [SerializeField] private TMP_Text _gameOverTimeText;
    [SerializeField] private TMP_Text _gameOverReviveText;

    [Header("게임클리어")]
    [SerializeField] private GameObject _stageClearUI;
    [SerializeField] private TMP_Text _stageClearTimeText;
    [SerializeField] private TMP_Text _stageClearReviveText;

    [Header("플레이어 사망 애니메이션 연결")]
    [SerializeField] private Animator _playerAnimator;

    private static readonly int DieHash = Animator.StringToHash("Base Layer.Die");
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.Window In");

    private float _playTimeSeconds;
    private bool _isLeaving;
    private readonly List<GameObject> _hiddenCrosshairs = new List<GameObject>();
    private bool _resultCursorActive;
    private bool _previousCursorVisible;
    private CursorLockMode _previousCursorLockState;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public float PlayTimeSeconds => _playTimeSeconds;
    public int ReviveCount { get; private set; }
    public bool IsPlaying => CurrentState == GameState.Playing;

    private void Awake()
    {
        CurrentState = GameState.Playing;
        _playTimeSeconds = 0f;
        ReviveCount = 0;

        _gameOverUI.SetActive(false);
        _stageClearUI.SetActive(false);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case GameState.Playing:
                UpdatePlaying();
                break;
            case GameState.Dying:
                UpdateDying();
                break;
        }
    }

    private void UpdatePlaying()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsGameOver)
        {
            CurrentState = GameState.Dying;
            return;
        }

        _playTimeSeconds += Time.deltaTime;
    }

    private void UpdateDying()
    {
        if (_playerAnimator == null) return;
        var state = _playerAnimator.GetCurrentAnimatorStateInfo(0);

        if (state.fullPathHash != DieHash || state.normalizedTime < 1f || _playerAnimator.IsInTransition(0)) // 현재 재생중인 애니메이터의 0번 레이어 정보 받아옴
        {
            return;
        }
        CurrentState = GameState.GameOver;
        ShowResult(_gameOverUI, _gameOverTimeText, _gameOverReviveText);
    }

    public void CompleteGame()
    {
        if (CurrentState != GameState.Ending) return;

        CurrentState = GameState.GameClear;
        ShowResult(_stageClearUI, _stageClearTimeText, _stageClearReviveText);
    }

    public void ShowResult(GameObject panel, TMP_Text timeText, TMP_Text reviveText)
    {
        var totalSeconds = Mathf.FloorToInt(_playTimeSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        timeText.text = $"플레이타임 {minutes:00} : {seconds:00}";
        if (reviveText != null) reviveText.text = ReviveCount.ToString();
        Time.timeScale = 0f;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        ShowResultCursor();

        var panelAnimator = panel.GetComponent<Animator>();

        if (panelAnimator != null)
        {
            panelAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            panelAnimator.Play(WindowInHash, 0, 0f);
        }
    }

    private void ShowResultCursor()
    {
        if (_resultCursorActive) return;
        _resultCursorActive = true;
        _previousCursorVisible = Cursor.visible;
        _previousCursorLockState = Cursor.lockState;

        // Image만 끄면 PlayerAimVisual이 다시 켜므로 오브젝트 자체를 숨깁니다.
        foreach (CrosshairFollowMouse crosshair in FindObjectsByType<CrosshairFollowMouse>(FindObjectsSortMode.None))
        {
            if (!crosshair.gameObject.activeInHierarchy) continue;
            _hiddenCrosshairs.Add(crosshair.gameObject);
            crosshair.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreGameplayCursor()
    {
        if (!_resultCursorActive) return;
        // Crosshair의 OnEnable이 결과 화면의 커서 상태를 저장하지 않도록 먼저 복원합니다.
        Cursor.lockState = _previousCursorLockState;
        Cursor.visible = _previousCursorVisible;
        foreach (GameObject crosshair in _hiddenCrosshairs)
        {
            if (crosshair != null) crosshair.SetActive(true);
        }
        _hiddenCrosshairs.Clear();
        _resultCursorActive = false;
    }

    public void ReturnToShip()
    {
        if (!CanLeaveResult()) return;
        _isLeaving = true;
        SceneFlowManager.Instance.LoadMainMenu();
    }

    public void RestartGame()
    {
        if (!CanLeaveResult()) return;
        _isLeaving = true;
        SceneFlowManager.Instance.LoadScene("04.GameScene");
    }

    private bool CanLeaveResult()
    {
        if (_isLeaving) return false;

        if (CurrentState != GameState.GameOver && CurrentState != GameState.GameClear) return false;

        if (SceneFlowManager.Instance == null)
        {
            Debug.LogError("SceneFlowManager 확인하세요");
            return false;
        }

        return !SceneFlowManager.Instance.IsTransitioning;
    }

    public void BeginEnding()
    {
        if (!IsPlaying) return;
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsGameOver)
        {
            return;
        }

        CurrentState = GameState.Ending;
    }

    public void RevivePlayer()
    {
        if (_isLeaving) return;

        if (CurrentState != GameState.GameOver) return;
        var playerManager = PlayerManager.Instance;
        if (playerManager == null) return;
        if (!playerManager.TryRevive()) return;
        ReviveCount++;
        _gameOverUI.SetActive(false);
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        RestoreGameplayCursor();
    }
}
