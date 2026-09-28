using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    private RectTransform _crosshair;
    private RectTransform _parentRect;
    private Canvas _canvas;
    private bool _previousCursorVisible;

    private void Awake()
    {
        _crosshair = GetComponent<RectTransform>();
        _parentRect = transform.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>().rootCanvas;

        // 조준점 중심이 정확히 마우스 위치에 오도록 설정
        _crosshair.pivot = new Vector2(0.5f, 0.5f);

        if (TryGetComponent(out Image image))
        {
            image.raycastTarget = false;
        }
    }
}