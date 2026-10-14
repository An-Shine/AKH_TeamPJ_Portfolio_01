using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform _descriptionBox;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Animator _descriptionAnimator;
    [SerializeField] private AnimationClip _closeAnimation;
    [SerializeField] private Vector2 _descriptionOffset = new(12f, -12f);

    private static readonly int OpenHash = Animator.StringToHash("Base Layer.Open");
    private static readonly int CloseHash = Animator.StringToHash("Base Layer.Close");

    private RectTransform _iconRect;
    private RectTransform _canvasRect;
    private Canvas _canvas;
    private ItemData _itemData;
    private Coroutine _closeRoutine;
    private bool _isHovered;
    private readonly Vector3[] _corners = new Vector3[4];

    private void Awake()
    {
        _iconRect = transform as RectTransform;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            _canvas = canvas.rootCanvas;
            _canvasRect = _canvas.transform as RectTransform;
        }

        if (_descriptionAnimator != null)
            _descriptionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (_descriptionBox != null) _descriptionBox.gameObject.SetActive(false);
    }

    public void Initialize(ItemData itemData)
    {
        _itemData = itemData;
        if (_descriptionText != null)
            _descriptionText.text = itemData != null ? itemData.Description : string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_itemData == null || string.IsNullOrWhiteSpace(_itemData.Description) || _descriptionBox == null ||
            _descriptionText == null || _canvasRect == null)
        {
            return;
        }
        CancelClose();
        _isHovered = true;
        _descriptionText.text = _itemData.Description;

        // 레이아웃과 인접 아이콘의 가림을 피하되, 박스의 소유자는 이 아이콘입니다.
        _descriptionBox.SetParent(_canvasRect, false);
        _descriptionBox.SetAsLastSibling();
        _descriptionBox.gameObject.SetActive(true);
        RefreshPosition();

        if (_descriptionAnimator != null)
        {
            _descriptionAnimator.Play(OpenHash, 0, 0f);
            _descriptionAnimator.Update(0f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (_descriptionBox == null || !_descriptionBox.gameObject.activeSelf) return;

        CancelClose();
        if (_descriptionAnimator == null || _closeAnimation == null)
        {
            HideDescription();
            return;
        }

        _descriptionAnimator.Play(CloseHash, 0, 0f);
        _descriptionAnimator.Update(0f);
        _closeRoutine = StartCoroutine(CloseAfterAnimation());
    }

    private IEnumerator CloseAfterAnimation()
    {
        yield return new WaitForSecondsRealtime(_closeAnimation.length);
        _closeRoutine = null;
        HideDescription();
    }

    private void LateUpdate()
    {
        if (_isHovered) RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (_iconRect == null || _canvasRect == null || _descriptionBox == null)
        {
            return;
        }

        _iconRect.GetWorldCorners(_corners);
        var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, camera, out var point))
        {
            return;
        }

        point += _descriptionOffset;
        var bounds = _canvasRect.rect;
        var size = _descriptionBox.rect.size;
        var pivot = _descriptionBox.pivot;
        point.x = Mathf.Clamp(point.x, bounds.xMin + size.x * pivot.x,
            Mathf.Max(bounds.xMin + size.x * pivot.x, bounds.xMax - size.x * (1f - pivot.x)));
        point.y = Mathf.Clamp(point.y, bounds.yMin + size.y * pivot.y,
            Mathf.Max(bounds.yMin + size.y * pivot.y, bounds.yMax - size.y * (1f - pivot.y)));
        _descriptionBox.localPosition = new Vector3(point.x, point.y, 0f);
    }

    private void CancelClose()
    {
        if (_closeRoutine == null) return;
        StopCoroutine(_closeRoutine);
        _closeRoutine = null;
    }

    private void HideDescription()
    {
        if (_descriptionBox == null) return;
        _descriptionBox.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        _isHovered = false;
        CancelClose();
        HideDescription();
    }

    private void OnDestroy()
    {
        if (_descriptionBox != null && _descriptionBox.parent != transform)
            Destroy(_descriptionBox.gameObject);
    }
}