using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LoadingController : MonoBehaviour
{
    [Header("로딩 시간 설정")]
    [SerializeField] private float _loadingDisplayDuration = 2f;

    [Header("애니메이션")]
    [SerializeField] private Animator _loadingAnimator;
    [SerializeField] private string _loadingStateName = "Loading";

    private void Start()
    {
        _loadingAnimator.Play(_loadingStateName);
        LoadTargetSceneAsync().Forget();
    }

    private async UniTask LoadTargetSceneAsync()
    {
        var targetSceneName = SceneFlowManager.Instance.TargetSceneName;

        var loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);

        loadOperation.allowSceneActivation = false; // 로딩이 끝나도 바로 씬 활성화하지는 않도록 설정
        var elapsedTime = 0f; // 로딩화면 표시 시간

        // 설정한 표시 시간이 지나고 실제 씬 로딩도 준비될 때까지 대기
        while (elapsedTime < _loadingDisplayDuration || loadOperation.progress < 0.9f)
        {
            elapsedTime += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update); // 다음 업데이트 프레임까지 대기
        }

        SceneFlowManager.Instance.UpdateBackgroundForTargetScene(targetSceneName);
        // 다음씬 전환요청 받을수있도록 전환상태 초기화
        SceneFlowManager.Instance.CompleteTransition();

        // 준비된 목적지 씬 활성화
        loadOperation.allowSceneActivation = true;
    }
}