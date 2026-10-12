using TMPro;
using UnityEngine;

public class DataCollectionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _questText;
    [SerializeField, Min(1)] private int _maxCollectCount = 2;

    private int _collectedCount;

    public int CollectedCount => _collectedCount;
    public int MaxCollectCount => _maxCollectCount;

    private void Awake()
    {
        RefreshText();
    }

    public void CollectData()
    {
        if (_collectedCount >= _maxCollectCount) return;

        _collectedCount++;
        RefreshText();
    }

    private void RefreshText()
    {
        _questText.text =
            "의뢰목록\n" +
            $"1. 소실된 연구소 데이터 수집 ({_collectedCount} / {_maxCollectCount})\n" +
            "2. 보스몬스터 ??? 처치";
    }
}