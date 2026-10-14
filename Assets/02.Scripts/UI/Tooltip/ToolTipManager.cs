using System.Collections.Generic;
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

    private struct Request
    {
        public Vector3 Offset;
        public string Description;
    }

    private readonly Dictionary<Transform, Request> _requests = new Dictionary<Transform, Request>();
    private readonly List<Transform> _invalidTargets = new List<Transform>();
    private Camera _camera;
    private RectTransform _panelRectTransform;
    private Canvas _canvas;
    private Transform _target;
    private Vector3 _offset;
    private bool _isOpenTooltip;
    public bool IsOpenTooltip => _isOpenTooltip;
    public bool IsTooltipTarget(Transform target) => _isOpenTooltip && _target == target;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _camera = Camera.main;
        if (_tooltipPanel != null)
        {
            _panelRectTransform = _tooltipPanel.GetComponent<RectTransform>();
            _canvas = _tooltipPanel.GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvas = _canvas.rootCanvas;
            }

            _tooltipPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void LateUpdate()
    {
        _invalidTargets.Clear();
        foreach (var pair in _requests)
        {
            if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy)
            {
                _invalidTargets.Add(pair.Key);
            }
        }

        foreach (Transform invalid in _invalidTargets) _requests.Remove(invalid);

        Transform next = null;
        if (_target != null && _requests.ContainsKey(_target))
        {
            next = _target;
        }
        else
        {
            foreach (var pair in _requests)
            {
                if (next == null || pair.Key.GetInstanceID() < next.GetInstanceID())
                {
                    next = pair.Key;
                }
            }
        }

        if (next == null)
        {
            HideTooltip();
            return;
        }

        if (_tooltipPanel == null || !_tooltipPanel.activeInHierarchy) return;

        Request request = _requests[next];
        bool changed = !_isOpenTooltip || _target != next;
        _target = next;
        _offset = request.Offset;
        if (_tooltipText != null)
        {
            _tooltipText.text = request.Description ?? string.Empty;
        }

        _isOpenTooltip = true;
        RefreshTooltip();

        if (changed)
        {
            AudioManager.Instance?.PlaySFX(SFXType.TooltipOpen);
            if (_tooltipAnimator != null)
            {
                _tooltipAnimator.Play(nameof(TooltipState.Open), 0, 0f);
            }
        }
    }

    public void OpenItemTooltip(Transform targetTransform, Vector3 offset, string description)
    {
        if (targetTransform == null) return;
        _requests[targetTransform] = new Request { Offset = offset, Description = description };
        if (_tooltipPanel != null && !_tooltipPanel.activeSelf)
        {
            _tooltipPanel.SetActive(true);
        }
    }

    public void CloseItemTooltip(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            _requests.Remove(targetTransform);
        }
    }

    public void CloseItemTooltip()
    {
        _requests.Clear();
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (_isOpenTooltip && _tooltipAnimator != null && _tooltipAnimator.isActiveAndEnabled)
        {
            _tooltipAnimator.Play(nameof(TooltipState.Close), 0, 0f);
        }

        _isOpenTooltip = false;
        _target = null;
    }

    private void RefreshTooltip()
    {
        if (!_isOpenTooltip || _target == null || _panelRectTransform == null || _canvasRectTransform == null) return;
        if (_camera == null)
        {
            _camera = Camera.main;
        }
        if (_camera == null) return;

        Camera uiCamera = _canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (_canvas != null ? _canvas.worldCamera : _camera);
        Vector3 screenPos = _camera.WorldToScreenPoint(_target.position + _offset);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform, screenPos, uiCamera, out Vector2 localPos))
        {
            _panelRectTransform.position = _canvasRectTransform.TransformPoint(localPos);
        }
    }

    public void SetActiveTooltip(bool active)
    {
        if (!active) CloseItemTooltip();
        if (_tooltipPanel != null)
        {
            _tooltipPanel.SetActive(active);
        }
    }

    private enum TooltipState
    {
        Open,
        Close
    }
}