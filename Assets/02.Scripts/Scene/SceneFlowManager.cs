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
    [SerializeField] private LobbyBGMManager _lobbyBGMManager;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
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

        // 게임으로 들어가거나 게임에서 나오는 로딩 구간에는 로비 음악을 재생하지 않습니다.
        if (targetSceneName == _gameplaySceneName || SceneManager.GetActiveScene().name == _gameplaySceneName)
        {
            _lobbyBGMManager?.Stop();
        }
        SceneManager.LoadScene(_loadingSceneName, LoadSceneMode.Single);
    }

    public void CompleteTransition()
    {
        _isTransitioning = false;
        _targetSceneName = string.Empty;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 초기 설정에서는 재생하지 않고 PressAnyKey부터 시작합니다.
        if (!_hasShownPressAnyKey || scene.name == _loadingSceneName) return;

        if (scene.name == _gameplaySceneName)
            _lobbyBGMManager?.Stop();
        else
            _lobbyBGMManager?.Play();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    public void UpdateBackgroundForTargetScene(string targetSceneName)
    {
        var showBackgound = targetSceneName != _gameplaySceneName;
        _backgroundCanvas.SetActive(showBackgound);
    }
}
