using UnityEngine;

public class TutorialProgressReset : MonoBehaviour
{
    [Header("시작 시 튜토리얼 초기화")]
    [Tooltip("켜면 InitSetting 씬 시작 시 등록된 튜토리얼의 완료 기록을 초기화합니다.")]
    [SerializeField] private bool _resetOnStart = true;

    [SerializeField] private DialogueData[] _tutorialDialogues;

    private void Awake()
    {
        if (!_resetOnStart || _tutorialDialogues == null) return;

        foreach (var dialogue in _tutorialDialogues)
        {
            if (dialogue == null) continue;

            TutorialProgressStore.ResetCompleted(dialogue.DialogueId);
        }
    }
}