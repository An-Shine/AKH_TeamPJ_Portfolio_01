using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [SerializeField] private string _pressAnyKeySceneName = "01.PressAnyKey";
    [SerializeField] private string _loadingSceneName = "02.Loading";
    [SerializeField] private string _mainMenuSceneName = "03.MainMenu";
    [SerializeField] private string _gameplaySceneName = "04.GameScene";

    [SerializeField] private GameObject _backgroundCanvas;

    private bool _hasShownPressAnyKey; // PressAnyKey 는 게임을 처음 켰을때만 등장하게끔

    private string _targetSceneName;
    private bool _isTransitioning;
    public string TargetSceneName => _targetSceneName;
    public bool IsTransitioning => _isTransitioning;

    public static SceneFlowManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_hasShownPressAnyKey) return;
        _hasShownPressAnyKey = true;

        SceneManager.LoadScene(_pressAnyKeySceneName, LoadSceneMode.Single);
    }

    public void LoadMainMenu()
    {
        LoadScene(_mainMenuSceneName);
    }

    public void LoadScene(string targetSceneName)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        _targetSceneName = targetSceneName;
        Time.timeScale = 1f;

        SceneManager.LoadScene(_loadingSceneName, LoadSceneMode.Single);
    }

    public void CompleteTransition()
    {
        _isTransitioning = false;
        _targetSceneName = string.Empty;
    }

    public void UpdateBackgroundForTargetScene(string targetSceneName)
    {
        var showBackgound = targetSceneName != _gameplaySceneName;
        _backgroundCanvas.SetActive(showBackgound);
    }
}