using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CrosshairFollowMouse : MonoBehaviour
{
    private RectTransform _crosshair;
    private RectTransform _parentRect;
    private Canvas _canvas;
    private bool _previousCursorVisible;
    private Image _image;
    private Color _baseColor;

    // fire
    [SerializeField] private float _fireScale = 0.82f;
    [SerializeField] private float _shrinkDuration = 0.035f;
    [SerializeField] private float _recoverDuration = 0.12f;

    // reload
    [SerializeField] private float _reloadRotationSpeed = 720f;
    [SerializeField] private float _rotationReturnSpeed = 1080f;
    private Vector3 _baseScale;
    private Quaternion _baseRotation;
    private Weapon _trackedWeapon;
    private int _shotSequence;
    private float _pulseAge = float.PositiveInfinity;
    private float _pulseStartScale = 1f;
    private float _currentScale = 1f;
    private float _reloadAngle;

    private void Awake()
    {
        _crosshair = GetComponent<RectTransform>();
        _parentRect = transform.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>().rootCanvas;
        _baseScale = _crosshair.localScale;
        _baseRotation = _crosshair.localRotation;

        // 조준점 중심이 정확히 마우스 위치에 오도록 설정
        _crosshair.pivot = new Vector2(0.5f, 0.5f);

        if (TryGetComponent(out Image image))
        {
            _image = image;
            _baseColor = image.color;
            image.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        _previousCursorVisible = Cursor.visible;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (_image != null)
        {
            Weapon weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;

            _image.color = weapon != null && weapon.RemainingAmmo <= 0
                ? new Color(1f, 0f, 0f, _baseColor.a)
                : _baseColor;
        }

        if (PlayerManager.IsInputBlocked)
        {
            ResetFeedback();
            return;
        }

        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, Input.mousePosition, uiCamera,
                out Vector2 position))
        {
            _crosshair.localPosition = new Vector3(position.x, position.y, 0f);
        }

        if (Time.timeScale > 0f) UpdateFeedback();
    }

    private void UpdateFeedback()
    {
        Weapon weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;
        if (weapon != _trackedWeapon)
        {
            ResetFeedback();
            _trackedWeapon = weapon;
            _shotSequence = weapon != null ? weapon.ShotSequence : 0;
            if (weapon != null && weapon.LastShotFrame == Time.frameCount) _pulseAge = 0f;
        }

        if (weapon == null) return;
        if (weapon.ShotSequence != _shotSequence)
        {
            _shotSequence = weapon.ShotSequence;
            _pulseStartScale = _currentScale;
            _pulseAge = 0f;
        }

        if (weapon.IsReloading)
        {
            _pulseAge = float.PositiveInfinity;
            _currentScale = 1f;
            _reloadAngle = Mathf.Repeat(_reloadAngle - _reloadRotationSpeed * Time.deltaTime, 360f);
        }
        else
        {
            float shrink = Mathf.Max(0.001f, _shrinkDuration);
            float recover = Mathf.Max(0.001f, _recoverDuration);
            _pulseAge += Time.deltaTime;
            if (_pulseAge < shrink)
            {
                _currentScale = Mathf.Lerp(_pulseStartScale, _fireScale,
                    Mathf.SmoothStep(0f, 1f, _pulseAge / shrink));
            }
            else
            {
                float progress = Mathf.Clamp01((_pulseAge - shrink) / recover);
                _currentScale = Mathf.Lerp(_fireScale, 1f, 1f - (1f - progress) * (1f - progress));
            }

            _reloadAngle = Mathf.MoveTowardsAngle(_reloadAngle, 0f, _rotationReturnSpeed * Time.deltaTime);
        }

        _crosshair.localScale = _baseScale * _currentScale;
        _crosshair.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, _reloadAngle);
    }

    private void ResetFeedback()
    {
        _trackedWeapon = null;
        _pulseAge = float.PositiveInfinity;
        _pulseStartScale = 1f;
        _currentScale = 1f;
        _reloadAngle = 0f;
        if (_crosshair == null) return;
        _crosshair.localScale = _baseScale;
        _crosshair.localRotation = _baseRotation;
    }

    private void OnDisable()
    {
        ResetFeedback();
        if (_image != null) _image.color = _baseColor;
        Cursor.visible = _previousCursorVisible;
    }
}