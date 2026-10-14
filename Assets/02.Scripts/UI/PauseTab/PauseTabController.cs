using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PauseTabController : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenuRoot;
    [SerializeField] private Animator _pauseTabAnimator;
    [SerializeField] private GameStateManager _gameStateManager;

    [Header("메인메뉴 복귀 확인")]
    [SerializeField] private GameObject _goToMenuRoot;
    [SerializeField] private Button _goToMenuYesButton;
    [SerializeField] private Button _goToMenuCancelButton;

    private bool _isPaused;
    private bool _isLeaving;
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.Window In");

    private bool IsPauseBlocked => (_gameStateManager != null && !_gameStateManager.IsPlaying) ||
                                   (PlayerManager.Instance != null && PlayerManager.Instance.IsGameOver);

    private void Awake()
    {
        CloseGoToMenu();
        if (_goToMenuYesButton != null) _goToMenuYesButton.onClick.AddListener(GoToMainMenu);
        if (_goToMenuCancelButton != null) _goToMenuCancelButton.onClick.AddListener(CloseGoToMenu);
        _pauseMenuRoot.SetActive(false);

        _isPaused = false;
        Time.timeScale = 1f;
    }

    private void OpenPauseTab()
    {
        if (IsPauseBlocked) return;

        CloseGoToMenu();
        _pauseMenuRoot.SetActive(true);
        _pauseTabAnimator.Play(WindowInHash, 0, 0f);
        Time.timeScale = 0f;
        _isPaused = true;
    }

    public void ResumeGame()
    {
        if (IsPauseBlocked || _isLeaving || IsGoToMenuOpen) return;
        CloseGoToMenu();
        Time.timeScale = 1.0f;
        _isPaused = false;
        _pauseMenuRoot.SetActive(false);
    }

    private bool IsGoToMenuOpen => _goToMenuRoot != null && _goToMenuRoot.activeSelf;

    public void OpenGoToMenu()
    {
        if (!_isPaused || IsPauseBlocked || _isLeaving || IsGoToMenuOpen) return;
        if (_goToMenuRoot == null) return;

        _goToMenuRoot.transform.SetAsLastSibling();
        _goToMenuRoot.SetActive(true);

        var animator = _goToMenuRoot.GetComponent<Animator>();
        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.Play(WindowInHash, 0, 0f);
        }
    }

    public void CloseGoToMenu()
    {
        if (_isLeaving) return;
        if (_goToMenuRoot != null) _goToMenuRoot.SetActive(false);
    }

    public void GoToMainMenu()
    {
        if (!_isPaused || !IsGoToMenuOpen || _isLeaving || IsPauseBlocked) return;
        if (SceneFlowManager.Instance == null)
        {
            Debug.LogError("SceneFlowManager 확인하세요");
            return;
        }

        if (SceneFlowManager.Instance.IsTransitioning) return;

        _isLeaving = true;
        SceneFlowManager.Instance.LoadMainMenu();
    }

    private void Update()
    {
        if (IsPauseBlocked)
        {
            CloseGoToMenu();
            _isPaused = false;
            _pauseMenuRoot.SetActive(false);
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (_isLeaving) return;

        if (IsGoToMenuOpen)
        {
            CloseGoToMenu();
            return;
        }

        // 컷씬을 넘긴 ESC 입력으로 일시정지 메뉴까지 열리지 않도록 합니다.
        foreach (var cutscene in FindObjectsByType<BossRoomCutsceneGate>(FindObjectsSortMode.None))
        {
            if (!cutscene.IsPlaying) continue;

            CloseGoToMenu();
            _isPaused = false;
            _pauseMenuRoot.SetActive(false);
            cutscene.SkipCutscene();
            return;
        }

        if (_isPaused)
        {
            ResumeGame();
            return;
        }

        OpenPauseTab();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (_goToMenuYesButton != null) _goToMenuYesButton.onClick.RemoveListener(GoToMainMenu);
        if (_goToMenuCancelButton != null) _goToMenuCancelButton.onClick.RemoveListener(CloseGoToMenu);
    }
}
