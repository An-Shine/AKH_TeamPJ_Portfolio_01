using TMPro;
using UnityEngine;

public class DataCollectionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _questText;
    [SerializeField, Min(1)] private int _maxCollectCount = 1;

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
            "\n" +
            $"1. 좌우 연구실 중 한 곳에서 데이터회수 ( {_collectedCount} / {_maxCollectCount})\n" +
            "2. 거대괴수 네메시스 제거";
    }
}
