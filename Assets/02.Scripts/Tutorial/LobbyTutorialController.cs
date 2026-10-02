using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyTutorialController : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private DialogueData _lobbyIntroductionDialogue;
    [SerializeField] private DialogueData _campaignGuideDialogue;
    [SerializeField] private DialogueData _departureGuideDialogue;

    private bool _isRunning;

    private async UniTask<bool> RunTutorialAsync(DialogueData dialogueData)
    {
        if (_isRunning) return false;

        if (TutorialProgressStore.IsCompleted(dialogueData.DialogueId)) return true;

        _isRunning = true;

        var completed = await DialogueManager.Instance.PlayDialogueAsync(dialogueData);

        if (completed) TutorialProgressStore.MarkCompleted(dialogueData.DialogueId);

        _isRunning = false;
        return completed;
    }

    private void Start()
    {
        RunTutorialAsync(_lobbyIntroductionDialogue).Forget();
    }

    public void OnQuestTabOpened()
    {
        if (!TutorialProgressStore.IsCompleted(_lobbyIntroductionDialogue.DialogueId)) return;
        RunTutorialAsync(_campaignGuideDialogue).Forget();
    }

    public async UniTask<bool> PlayDepartureGuideAsync()
    {
        if (!TutorialProgressStore.IsCompleted(_campaignGuideDialogue.DialogueId)) return false;

        return await RunTutorialAsync(_departureGuideDialogue);
    }

    [ContextMenu("Reset Lobby Tutorial Progress")]
    private void ResetLobbyTutorialProgress()
    {
        TutorialProgressStore.ResetCompleted(_lobbyIntroductionDialogue.DialogueId);

        TutorialProgressStore.ResetCompleted(_campaignGuideDialogue.DialogueId);

        TutorialProgressStore.ResetCompleted(_departureGuideDialogue.DialogueId);

        Debug.Log("로비 튜토리얼 진행 기록 초기화", this);
    }
}