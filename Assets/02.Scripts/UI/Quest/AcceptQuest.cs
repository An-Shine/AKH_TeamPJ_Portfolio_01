using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Michsky.UI.Shift;
using Unity.VisualScripting;

public class AcceptQuest : MonoBehaviour
{
    [SerializeField] private string _gameplaySceneName = "04.GameScene";

    private QuestData _selectedQuest;
    private QuestData _acceptedQuest;

    [Header("퀘스트 에셋 연결")]
    [SerializeField] private ModalWindowManager _questModal;

    [Header("의뢰 수락 패널")]
    [SerializeField] private GameObject _emptyAcceptedQuestView;
    [SerializeField] private GameObject _acceptedQuestView;

    [SerializeField] private Image _acceptedQuestImage;
    [SerializeField] private TMP_Text _acceptedQuestTitleText;
    [SerializeField] private TMP_Text _acceptedQuestDescriptionText;

    [SerializeField] private BlurManager _blurManager;

    public void SelectQuest(QuestData questData)
    {
        if (questData.IsLocked) return;

        _selectedQuest = questData;
        _blurManager.BlurInAnim();
        _questModal.ModalWindowIn();
    }

    public void ConfirmAcceptQuest()
    {
        _acceptedQuest = _selectedQuest;

        _acceptedQuestImage.sprite = _acceptedQuest.QuestImage;
        _acceptedQuestTitleText.text = _acceptedQuest.QuestTitle;
        _acceptedQuestDescriptionText.text = $"{_acceptedQuest.QuestDescription}\n" +
                                             $"난이도: {_acceptedQuest.DifficultyText}\n" +
                                             $"보상: {_acceptedQuest.RewardText}";

        _emptyAcceptedQuestView.SetActive(false);
        _acceptedQuestView.SetActive(true);

        _questModal.ModalWindowOut();
        _blurManager.BlurOutAnim();
    }

    public void StartAcceptedQuest()
    {
        if (_acceptedQuest == null) return;

        SceneFlowManager.Instance.LoadScene(_gameplaySceneName);
    }

    public void CancelAcceptQuest()
    {
        _selectedQuest = null;

        _questModal.ModalWindowOut();
        _blurManager.BlurOutAnim();
    }
}