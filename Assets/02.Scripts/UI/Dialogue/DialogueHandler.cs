using UnityEngine;

[RequireComponent(typeof(DialogueManager))]
public class DialogueHandler : MonoBehaviour
{
    [Header("테스트용 입력키")]
    [SerializeField] private KeyCode _advanceKey = KeyCode.F;

    private DialogueManager _dialogueManager;

    private void Awake()
    {
        _dialogueManager = GetComponent<DialogueManager>(); // 같은 게임오브젝트의 DialogueManager 를 가져와서 보관
    }

    private void Update()
    {
        if (Input.GetKeyDown(_advanceKey)) _dialogueManager.RequestAdvance();
    }
}