using TMPro;
using UnityEngine;

public class ToolTipManager : MonoBehaviour
{
    private static ToolTipManager _instance;
    public static ToolTipManager Instance => _instance;

    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private TextMeshProUGUI _tooltipText;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        SetActiveTooltip(false);
    }

    public void OpenItemTooltip(Vector3 position, string description)
    {
        SetItemDescription(description);
        _tooltipPanel.transform.localPosition = position;
        SetActiveTooltip(true);
    }

    public void SetActiveTooltip(bool active)
    {
        _tooltipPanel.SetActive(active);
    }

    private void SetItemDescription(string description)
    {
        if (_tooltipText == null) return;
        if (string.IsNullOrEmpty(description)) return;

        _tooltipText.text = description;
    }
}