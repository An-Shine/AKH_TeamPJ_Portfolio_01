using Unity.VisualScripting;
using UnityEngine;

public class PauseTabController : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenuRoot;
    [SerializeField] private Animator _pauseTabAnimator;

    private bool _isPaused;
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.Window In");

    private void Awake()
    {
        _pauseMenuRoot.SetActive(false);

        _isPaused = false;
        Time.timeScale = 1f;
    }

    private void OpenPauseTab()
    {
        _pauseMenuRoot.SetActive(true);
        _pauseTabAnimator.Play(WindowInHash, 0, 0f);
        Time.timeScale = 0f;
        _isPaused = true;
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

    public void ResumeGame()
    {
        Time.timeScale = 1.0f;
        _isPaused = false;
        _pauseMenuRoot.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_isPaused)
        {
            ResumeGame();
            return;
        }

        OpenPauseTab();
    }
}