using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 정보")]
    [SerializeField] private string _questId;
    [SerializeField] private string _questTitle;

    [SerializeField][TextArea] private string _questDescription;

    [SerializeField] private Sprite _questImage;
    [SerializeField] private string _difficultyText;
    [SerializeField] private string _rewardText;

    [Header("퀘스트 상태")]
    [SerializeField] private bool _isLocked;

    // 프로퍼티
    public string QuestId => _questId;
    public string QuestTitle => _questTitle;
    public string QuestDescription => _questDescription;
    public Sprite QuestImage => _questImage;
    public string DifficultyText => _difficultyText;
    public string RewardText => _rewardText;
    public bool IsLocked => _isLocked;
}