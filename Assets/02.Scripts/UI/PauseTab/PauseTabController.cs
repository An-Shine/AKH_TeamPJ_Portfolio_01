using Unity.VisualScripting;
using UnityEngine;

public class PauseTabController : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenuRoot;
    [SerializeField] private Animator _pauseTabAnimator;
    [SerializeField] private GameStateManager _gameStateManager;

    private bool _isPaused;
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.Window In");

    private bool IsPauseBlocked => (_gameStateManager != null && !_gameStateManager.IsPlaying) ||
                                   (PlayerManager.Instance != null && PlayerManager.Instance.IsGameOver);

    private void Awake()
    {
        _pauseMenuRoot.SetActive(false);

        _isPaused = false;
        Time.timeScale = 1f;
    }

    private void OpenPauseTab()
    {
        if (IsPauseBlocked) return;

        _pauseMenuRoot.SetActive(true);
        _pauseTabAnimator.Play(WindowInHash, 0, 0f);
        Time.timeScale = 0f;
        _isPaused = true;
    }

    public void ResumeGame()
    {
        if (IsPauseBlocked) return;
        Time.timeScale = 1.0f;
        _isPaused = false;
        _pauseMenuRoot.SetActive(false);
    }

    public void GoToMainMenu()
    {
        if (SceneFlowManager.Instance == null)
        {
            Debug.LogError("SceneFlowManager 확인하세요");
            return;
        }

        SceneFlowManager.Instance.LoadMainMenu();
    }

    private void Update()
    {
        if (IsPauseBlocked)
        {
            _isPaused = false;
            _pauseMenuRoot.SetActive(false);
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_isPaused)
        {
            ResumeGame();
            return;
        }

        OpenPauseTab();
    }
}