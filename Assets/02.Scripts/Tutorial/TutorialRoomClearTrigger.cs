using UnityEngine;
using Cysharp.Threading.Tasks;

public class TutorialRoomClearTrigger : MonoBehaviour
{
    [SerializeField] private MonsterSpawner _monsterSpawner;
    [SerializeField] private DialogueData _dialogueData;

    private bool _isRunning;

    private void Update()
    {
        if (_isRunning) return;

        if (_monsterSpawner == null || _dialogueData == null) return;

        if (TutorialProgressStore.IsCompleted(_dialogueData.DialogueId)) return;

        if (!_monsterSpawner.IsComplete) return;

        if (_monsterSpawner.RemainingMonsterCount != 0) return;

        _isRunning = true;
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

        if (completed) TutorialProgressStore.MarkCompleted(_dialogueData.DialogueId);

        _isRunning = false;
    }

    [ContextMenu("Reset Tutorial Progress")]
    private void ResetTutorialProgress()
    {
        TutorialProgressStore.ResetCompleted(_dialogueData.DialogueId);
        Debug.Log($"튜토리얼 진행기록 초기화 : {_dialogueData.DialogueId}", this);
    }
}