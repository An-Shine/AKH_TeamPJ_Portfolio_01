using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class DialogueManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject _dialogueRoot;
    [SerializeField] private Image _portraitImage;
    [SerializeField] private TMP_Text _speakerNameText;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private GameObject _continueIndicator;

    [Header("타이핑 설정")]
    [SerializeField][Min(1f)] private float _charactersPerSecond = 30f;

    [Header("타이핑 사운드")]
    [SerializeField] private AudioSource _typingAudioSource;

    private DialogueData _currentDialogue;
    private int _currentLineIndex;
    private bool _isDialogueActive;
    private bool _isTyping;
    private bool _skipTypingRequested; // 상호작용키로 나머지 대사 전체 한번에 출력하기위함
    private bool _advanceRequested; // 완성된 문장에서 다음 줄로 이동

    public bool IsDialogueActive => _isDialogueActive;
    public bool IsTyping => _isTyping;

    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _dialogueRoot.SetActive(false);
    }

    public async UniTask<bool> PlayDialogueAsync(DialogueData dialogueData)
    {
        // 전달받은 Dialogue 데이터 검사
        if (dialogueData == null || dialogueData.LineCount == 0) return false;

        // 이미 다른 대화가 출력중인지 검사
        if (_isDialogueActive) return false;

        // 현재 대화 데이터와 대사 인덱스 초기화
        _currentDialogue = dialogueData;
        _currentLineIndex = 0;
        _isDialogueActive = true;

        _dialogueRoot.SetActive(true);

        // 등록된 모든 대사를 순서대로 출력
        while (_currentLineIndex < _currentDialogue.LineCount)
        {
            // 현재 인덱스의 대사 가져오기
            var currentLine = _currentDialogue.Lines[_currentLineIndex];

            await ShowLineAsync(currentLine); // 현대 대사의 화자 이름, 초상화, 본문표시, 타이핑완료까지 대기
            await WaitForAdvanceAsync(); // 현재 문장이 완성된 후 다음진행 입력까지 대기

            _currentLineIndex++; // 다음 대사로 이동
        }

        EndDialogue(); // 대사끝났으면 대화 종료처리
        return true;
    }

    private async UniTask ShowLineAsync(DialogueLine line)
    {
        _speakerNameText.text = line.SpeakerName;
        _portraitImage.sprite = line.Portrait;
        _portraitImage.enabled = line.Portrait != null; // 초상화가 없는 대사라면 초상화 칸 비움처리
        _dialogueText.text = line.Text;
        _dialogueText.maxVisibleCharacters = 0; // 처음에는 보이는 글자수를 0으로 설정
        _continueIndicator.SetActive(false);
        _isTyping = true;
        _skipTypingRequested = false;
        _dialogueText.ForceMeshUpdate();
        var characterCount = _dialogueText.textInfo.characterCount; // 태그를 제외한 실제 표시 글자수 가져오기
        var visibleCharacterCount = 0f;
        if (characterCount > 0) PlayTypingAudio();
        while (_dialogueText.maxVisibleCharacters < characterCount)
        {
            // 타이핑 도중 진행 입력이 들어오면 반복 종료, 한번에 전체 출력
            if (_skipTypingRequested) break;

            visibleCharacterCount += _charactersPerSecond * Time.deltaTime; // 실제 표시 글자수 누적

            _dialogueText.maxVisibleCharacters = Mathf.FloorToInt(visibleCharacterCount); // 누적된 수만큼 실제 표시글자수 갱신

            if (_dialogueText.maxVisibleCharacters >= characterCount) StopTypingAudio();

            await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken); // 다음 Upadate 프레임까지 비동기 대기
        }

        // 반복문이 끝나면 남은 글자를 포함해 전체 문장 표시
        _dialogueText.maxVisibleCharacters = characterCount;

        StopTypingAudio();

        _isTyping = false;
        _skipTypingRequested = false;
        _continueIndicator.SetActive(true);
    }

    private async UniTask WaitForAdvanceAsync()
    {
        _advanceRequested = false;
        // 외부에서 진행 요청이 들어올 때 까지 매 프레임 대기
        await UniTask.WaitUntil(() => _advanceRequested, PlayerLoopTiming.Update, destroyCancellationToken);
        _advanceRequested = false;
        _continueIndicator.SetActive(false);
    }

    public void RequestAdvance()
    {
        // 현재 진행중인 대화 없으면 입력 무시
        if (!_isDialogueActive) return;

        // 문장을 타이핑 중이라면 남은문장 즉시 표시하도록 요청
        if (_isTyping)
        {
            _skipTypingRequested = true;
            StopTypingAudio();
            return;
        }

        //문장출력 완료상태면 다음 문장으로 진행요청
        _advanceRequested = true;
    }

    private void EndDialogue()
    {
        StopTypingAudio();

        // 대화창 UI 전체 숨기기
        _dialogueRoot.SetActive(false);

        // 다음 진행 표시 숨기기
        _continueIndicator.SetActive(false);

        // 이전 대사의 텍스트 제거
        _speakerNameText.text = string.Empty;
        _dialogueText.text = string.Empty;

        // 이전 대사의 초상화 제거
        _portraitImage.sprite = null;
        _portraitImage.enabled = false;

        // 현재 대화 데이터와 인덱스 초기화
        _currentDialogue = null;
        _currentLineIndex = 0;

        // 모든 대화 진행 상태 초기화
        _isDialogueActive = false;
        _isTyping = false;
        _skipTypingRequested = false;
        _advanceRequested = false;
    }

    private void PlayTypingAudio()
    {
        if (!isActiveAndEnabled || _typingAudioSource == null || !_typingAudioSource.isActiveAndEnabled || _typingAudioSource.clip == null)
        {
            return;
        }
        _typingAudioSource.Stop();
        _typingAudioSource.loop = true;
        _typingAudioSource.Play();
    }

    private void StopTypingAudio()
    {
        if (_typingAudioSource != null) _typingAudioSource.Stop();
    }

    private void OnDisable()
    {
        StopTypingAudio();
    }
}