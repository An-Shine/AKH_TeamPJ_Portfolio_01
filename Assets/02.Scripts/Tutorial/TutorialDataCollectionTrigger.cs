using UnityEngine;
using Cysharp.Threading.Tasks;

public class TutorialDataCollectionTrigger : MonoBehaviour
{
    [SerializeField] private DataCollectionUI _dataCollectionUI;
    [SerializeField] private DialogueData _dialogueData;

    private bool _isRunning;

    private void Update()
    {
        if (_isRunning) return;

        if (_dataCollectionUI == null || _dialogueData == null) return;

        if (TutorialProgressStore.IsCompleted(_dialogueData.DialogueId)) return;

        if (_dataCollectionUI.CollectedCount < _dataCollectionUI.MaxCollectCount) return;

        _isRunning = true;
        Debug.Log("데이터 수집 완료 조건 통과", this);
        RunTutorialAsync().Forget();
    }

    private async UniTask RunTutorialAsync()
    {
        var dialogueData = _dialogueData;

        var isCanceled = await UniTask
            .WaitUntil(() => DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive,
                cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

        if (isCanceled) return;

        var completed = await DialogueManager.Instance.PlayDialogueAsync(dialogueData);

        if (completed) TutorialProgressStore.MarkCompleted(dialogueData.DialogueId);

        _isRunning = false;
    }

    [ContextMenu("Reset Tutorial Progress")]
    private void ResetTutorialProgress()
    {
        TutorialProgressStore.ResetCompleted(_dialogueData.DialogueId);
        Debug.Log($"튜토리얼 진행기록 초기화 : {_dialogueData.DialogueId}", this);
    }
}