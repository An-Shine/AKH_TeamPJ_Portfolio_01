using TMPro;
using UnityEngine;

public class DataCollectionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _questText;

    private int _collectedCount;

    public int CollectedCount => _collectedCount;

    private void Awake()
    {
        RefreshText();
    }

    public void CollectData()
    {
        if (_collectedCount >= 2) return;

        _collectedCount++;
        RefreshText();
    }

    private void RefreshText()
    {
        _questText.text =
            "의뢰목록\n" +
            $"1. 소실된 연구소 데이터 수집 ({_collectedCount} / 2)\n" +
            "2. 보스몬스터 ??? 처치";
    }
}