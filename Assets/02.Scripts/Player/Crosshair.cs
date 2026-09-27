using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Crosshair : MonoBehaviour
{
    private RectTransform crosshair;
    private RectTransform parentRect;
    private Canvas canvas;
    private bool previousCursorVisible;

    private void Awake()
    {
        crosshair = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>().rootCanvas;

        // 조준점 중심이 정확히 마우스 위치에 오도록 설정
        crosshair.pivot = new Vector2(0.5f, 0.5f);

        if (TryGetComponent(out Image image))
            image.raycastTarget = false;
    }

    private void OnEnable()
    {
        previousCursorVisible = Cursor.visible;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, Input.mousePosition, uiCamera, out Vector2 position))
        {
            crosshair.localPosition = new Vector3(position.x, position.y, 0f);
        }
    }

    private void OnDisable()
    {
        Cursor.visible = previousCursorVisible;
    }
}