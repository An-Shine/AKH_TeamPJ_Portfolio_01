using TMPro;
using UnityEngine;

public class ToolTipManager : MonoBehaviour
{
    private static ToolTipManager _instance;
    public static ToolTipManager Instance => _instance;

    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private Animator _tooltipAnimator;
    [SerializeField] private TextMeshProUGUI _tooltipText;
    [SerializeField] private RectTransform _canvasRectTransform;

    private Camera _camera;
    private RectTransform _panelRectTransform;
    private Transform _target;
    private Vector3 _offset;

    private bool _isOpenTooltip;

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
        _isOpenTooltip = false;
        if (_tooltipPanel != null)
        {
            _panelRectTransform = _tooltipPanel.GetComponent<RectTransform>();
        }

        _camera = Camera.main;
        SetActiveTooltip(true);
    }

    private void Update()
    {
        RefreshTooltip();
    }

    public void OpenItemTooltip(Transform targetTransform, Vector3 offset, string description)
    {
        SetItemDescription(description);
        _target = targetTransform;
        _offset = offset;
        _isOpenTooltip = true;
        RefreshTooltip();
        _tooltipAnimator.Play(nameof(TooltipState.Open));
    }

    private void RefreshTooltip()
    {
        if (!_isOpenTooltip) return;
        Vector3 screenPos = _camera.WorldToScreenPoint(_target.position + _offset);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            screenPos,
            _camera, // Overlay mode -> null, Camera mode -> mainCamera
            out Vector2 localPos
        );
        _panelRectTransform.anchoredPosition = localPos;
    }

    public void CloseItemTooltip()
    {
        if (_tooltipAnimator == null) return;
        _tooltipAnimator.Play(nameof(TooltipState.Close));
        _isOpenTooltip = false;
    }

    public void SetActiveTooltip(bool active)
    {
        if (_tooltipPanel == null) return;
        _tooltipPanel.SetActive(active);
    }

    private void SetItemDescription(string description)
    {
        if (_tooltipText == null) return;
        if (string.IsNullOrEmpty(description)) return;

        _tooltipText.text = description;
    }

    enum TooltipState
    {
        Open,
        Close
    }
}