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

    // charge
    [SerializeField] private Color _chargeColor = new Color(0.65f, 0.2f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float _chargeFlashDuration = 0.18f;
    [SerializeField] private CrosshairChargeRing _chargeRing;
    [SerializeField, Range(0.1f, 1f)] private float _chargedScale = 0.7f;
    private ChargeFire _trackedCharge;
    private bool _wasFullyCharged;
    private float _chargeFlashAge = float.PositiveInfinity;
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

            UpdateColor(weapon, Time.deltaTime);
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
        else if (weapon.FireBehaviour is ChargeFire charge && weapon.Data != null)
        {
            UpdateChargeScale(weapon, charge, Time.deltaTime);
            _reloadAngle = Mathf.MoveTowardsAngle(_reloadAngle, 0f, _rotationReturnSpeed * Time.deltaTime);
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

    private void UpdateChargeScale(Weapon weapon, ChargeFire charge, float deltaTime)
    {
        // 차지 무기는 일반 발사 축소를 겹치지 않고 현재 크기에서 바로 복귀합니다.
        _pulseAge = float.PositiveInfinity;
        if (charge.IsFiring && weapon.RemainingAmmo > 0)
        {
            float progress = weapon.Data.ChargeTime <= 0f
                ? 1f
                : Mathf.Clamp01(charge.ChargeTime / weapon.Data.ChargeTime);
            _currentScale = Mathf.Lerp(1f, _chargedScale, progress);
        }
        else
        {
            float speed = (1f - _chargedScale) / Mathf.Max(0.001f, _recoverDuration);
            _currentScale = Mathf.MoveTowards(_currentScale, 1f, speed * deltaTime);
        }
    }

    private void UpdateColor(Weapon weapon, float deltaTime)
    {
        ChargeFire charge = weapon != null ? weapon.FireBehaviour as ChargeFire : null;
        bool charging = !PlayerManager.IsInputBlocked && weapon != null && weapon.Data != null &&
                        !weapon.IsReloading && weapon.RemainingAmmo > 0 && charge != null && charge.IsFiring;
        if (!charging)
        {
            _trackedCharge = null;
            _wasFullyCharged = false;
            _chargeFlashAge = float.PositiveInfinity;
            _image.color = weapon != null && !weapon.IsReloading && weapon.RemainingAmmo <= 0
                ? new Color(1f, 0f, 0f, _baseColor.a)
                : _baseColor;
            return;
        }

        if (_trackedCharge != charge)
        {
            _trackedCharge = charge;
            _wasFullyCharged = false;
            _chargeFlashAge = float.PositiveInfinity;
        }

        float progress = weapon.Data.ChargeTime <= 0f
            ? 1f
            : Mathf.Clamp01(charge.ChargeTime / weapon.Data.ChargeTime);
        bool fullyCharged = progress >= 1f;
        if (fullyCharged && !_wasFullyCharged)
        {
            _chargeFlashAge = 0f;
            if (_chargeRing != null)
            {
                _chargeRing.Play();
            }
        }
        else
        {
            _chargeFlashAge += deltaTime;
        }

        if (!fullyCharged)
        {
            _chargeFlashAge = float.PositiveInfinity;
        }

        _wasFullyCharged = fullyCharged;

        Color purple = new Color(_chargeColor.r, _chargeColor.g, _chargeColor.b, _baseColor.a);
        Color color = Color.Lerp(_baseColor, purple, progress);
        float flash = 1f - Mathf.Clamp01(_chargeFlashAge / Mathf.Max(0.01f, _chargeFlashDuration));
        _image.color = fullyCharged
            ? new Color(0f, 1f, 0f, _baseColor.a)
            : Color.Lerp(color, new Color(1f, 1f, 1f, _baseColor.a), flash);
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
        if (_chargeRing != null)
        {
            _chargeRing.Hide();
        }

        _trackedCharge = null;
        _wasFullyCharged = false;
        _chargeFlashAge = float.PositiveInfinity;
        ResetFeedback();
        if (_image != null)
        {
            _image.color = _baseColor;
        }

        Cursor.visible = _previousCursorVisible;
    }
}
