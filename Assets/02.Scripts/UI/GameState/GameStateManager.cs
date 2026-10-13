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

    [Header("게임클리어")]
    [SerializeField] private GameObject _stageClearUI;
    [SerializeField] private TMP_Text _stageClearTimeText;

    [Header("플레이어 사망 애니메이션 연결")]
    [SerializeField] private Animator _playerAnimator;

    private static readonly int DieHash = Animator.StringToHash("Base Layer.Die");
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.Window In");

    private float _playTimeSeconds;
    private bool _isLeaving;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public float PlayTimeSeconds => _playTimeSeconds;
    public bool IsPlaying => CurrentState == GameState.Playing;

    private void Awake()
    {
        CurrentState = GameState.Playing;
        _playTimeSeconds = 0f;

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
        ShowResult(_gameOverUI, _gameOverTimeText);
    }

    public void CompleteGame()
    {
        if (CurrentState != GameState.Ending) return;

        CurrentState = GameState.GameClear;
        ShowResult(_stageClearUI, _stageClearTimeText);
    }

    public void ShowResult(GameObject panel, TMP_Text timeText)
    {
        var totalSeconds = Mathf.FloorToInt(_playTimeSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        timeText.text = $"플레이타임 {minutes:00} : {seconds:00}";
        Time.timeScale = 0f;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);

        var panelAnimator = panel.GetComponent<Animator>();

        if (panelAnimator != null)
        {
            panelAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            panelAnimator.Play(WindowInHash, 0, 0f);
        }
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
}