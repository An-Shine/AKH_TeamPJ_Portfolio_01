using UnityEngine;
using UnityEngine.UI;

public class GaugeController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _canvasRectTransform;
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset;

    [SerializeField] private GameObject _gaugeObject;
    [SerializeField] private Image _gauge;

    private RectTransform _panelRectTransform;

    private FireBehaviour _chargeBehaviour;
    private bool _isDoNotHaveCharge;
    private bool _isCharging;
    private float _maxChargeTime;

    private void Awake()
    {
        if (_gauge == null)
        {
            _isDoNotHaveCharge = true;
            return;
        }

        _panelRectTransform = _gaugeObject.GetComponent<RectTransform>();
    }

    private void Update()
    {
        SearchCurrentWeapon();
    }

    private void LateUpdate()
    {
        DisplayGauge();
        GaugeUpdate();
        RefreshGaugePosition();
    }

    private void GaugeUpdate()
    {
        if (_isDoNotHaveCharge) return;

        float currentValue = ((ChargeFire)_chargeBehaviour).ChargeTime;
        _gauge.fillAmount = currentValue / _maxChargeTime;
    }

    private void DisplayGauge()
    {
        if (_isDoNotHaveCharge)
        {
            _gaugeObject.SetActive(false);
            return;
        }

        _isCharging = ((ChargeFire)_chargeBehaviour).IsFiring;
        if (!_isCharging)
        {
            _gaugeObject.SetActive(false);
            return;
        }

        _gaugeObject.SetActive(true);
    }

    private void SearchCurrentWeapon()
    {
        WeaponManager weaponManager = WeaponManager.Instance;
        if (weaponManager == null)
        {
            _isDoNotHaveCharge = true;
            return;
        }

        Weapon weapon = weaponManager.CurrentWeapon;
        if (weapon == null)
        {
            _isDoNotHaveCharge = true;
            return;
        }

        _chargeBehaviour = weapon.FireBehaviour;
        if (_chargeBehaviour == null)
        {
            _isDoNotHaveCharge = true;
            return;
        }

        if (_chargeBehaviour is ChargeFire)
        {
            _isDoNotHaveCharge = false;
            _maxChargeTime = weapon.Data.ChargeTime;
        }
        else
        {
            _isDoNotHaveCharge = true;
        }
    }

    private void RefreshGaugePosition()
    {
        if (_isDoNotHaveCharge || !_isCharging) return;

        Vector3 screenPos = _camera.WorldToScreenPoint(_target.position + _offset);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            screenPos,
            _camera, // Overlay mode -> null, Camera mode -> mainCamera
            out Vector2 localPos
        );
        _panelRectTransform.anchoredPosition = localPos;
    }
}