using UnityEngine;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Collider2D))]
public class TutorialDialogueTrigger2D : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private DialogueData _dialogueData;

    [Header("플레이어 연결")]
    [SerializeField] private LayerMask _playerLayer;

    private bool _isRunning;

    private bool IsPlayer(Collider2D other)
    {
        var body = other.attachedRigidbody;

        if (body == null) return false;

        var bodyLayer = body.gameObject.layer;
        return (_playerLayer.value & (1 << bodyLayer)) != 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        if (_dialogueData == null) return;

        if (_isRunning) return;

        if (TutorialProgressStore.IsCompleted(_dialogueData.DialogueId)) return;
        _isRunning = true;
        RunTutorialAsync().Forget();
    }

    private async UniTask RunTutorialAsync()
    {
        // 실행을 시작한 시점의 대화 데이터 보관
        var dialogueData = _dialogueData;

        // 대화를 실행하고 마지막 줄까지 정상 완료됐는지 확인
        var completed =
            await DialogueManager.Instance.PlayDialogueAsync(dialogueData);

        // 대화가 정상적으로 끝났다면 튜토리얼 완료 기록 저장
        if (completed)
        {
            TutorialProgressStore.MarkCompleted(
                dialogueData.DialogueId
            );
        }

        // 현재 트리거의 실행 상태 초기화
        _isRunning = false;
    }

    [ContextMenu("Reset Tutorial Progress")]
    private void ResetTutorialProgress()
    {
        TutorialProgressStore.ResetCompleted(_dialogueData.DialogueId);
        Debug.Log($"튜토리얼 진행기록 초기화 : {_dialogueData.DialogueId}", this);
    }
}