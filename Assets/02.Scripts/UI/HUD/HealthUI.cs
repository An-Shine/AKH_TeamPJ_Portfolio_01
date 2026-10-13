using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("체력변화 연출용")]
    [SerializeField] private Image _changeTrailImage;
    [SerializeField] private Color _damageTrailColor = new(1f, 0.68f, 0.17f, 1f);
    [SerializeField] private Color _healTrailColor = new(0.2f, 1f, 0.4f, 1f);
    [SerializeField][Min(0f)] private float _flashDuration = 0.2f;

    [SerializeField][Min(0.01f)] private float _damageTrailSpeed = 0.8f;
    [SerializeField][Min(0.01f)] private float _healFillSpeed = 0.8f;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private TMP_Text _healthText;

    private float _lastCurrentHP = -1f;
    private float _lastMaxHP = -1f;

    private enum HealthChangeState
    {
        None,
        Damage,
        Heal
    }

    private HealthChangeState _changeState;

    private Image _healthFillImage;
    private Color _originalFillColor;

    private float _targetRatio;
    private float _trailRatio;
    private float _fillRatio;
    private float _flashEndTime;

    private bool _isInitialized;

    private void Awake()
    {
        if (_healthSlider == null || _healthText == null || _changeTrailImage == null)
        {
            Debug.LogError("UI 연결안된거있음 확인바람", this);
            enabled = false;
            return;
        }

        if (_healthSlider.fillRect == null || !_healthSlider.fillRect.TryGetComponent(out _healthFillImage))
        {
            enabled = false;
            return;
        }

        _originalFillColor = _healthFillImage.color;
    }

    private void Update()
    {
        if (!_isInitialized) return;
        if (_changeState == HealthChangeState.None)
        {
            RefreshBar();
            return;
        }

        if (Time.time < _flashEndTime)
        {
            _healthFillImage.color = Color.white;
            RefreshBar();
            return;
        }

        _healthFillImage.color = _originalFillColor;

        if (_changeState == HealthChangeState.Damage)
        {
            _trailRatio = Mathf.MoveTowards(_trailRatio, _targetRatio, _damageTrailSpeed * Time.deltaTime);

            if (Mathf.Approximately(_trailRatio, _targetRatio))
            {
                _trailRatio = _targetRatio;
                _changeState = HealthChangeState.None;
            }
        }
        else if (_changeState == HealthChangeState.Heal)
        {
            _fillRatio = Mathf.MoveTowards(_fillRatio, _targetRatio, _healFillSpeed * Time.deltaTime);

            if (Mathf.Approximately(_fillRatio, _targetRatio))
            {
                _fillRatio = _targetRatio;
                _changeState = HealthChangeState.None;
            }
        }

        RefreshBar();
    }

    private void RefreshBar()
    {
        _healthSlider.value = _fillRatio * _lastMaxHP;

        var fillRect = _healthSlider.fillRect;
        var trailRect = _changeTrailImage.rectTransform;

        var anchorMin = fillRect.anchorMin;
        var anchorMax = fillRect.anchorMax;

        anchorMin.x = 0f;
        anchorMax.x = _trailRatio;

        trailRect.anchorMin = anchorMin;
        trailRect.anchorMax = anchorMax;

        trailRect.offsetMin = fillRect.offsetMin;
        trailRect.offsetMax = fillRect.offsetMax;
    }

    public void SetHealthData(float currentHP, float maxHP)
    {
        if (!enabled) return;

        if (Mathf.Approximately(_lastCurrentHP, currentHP) && Mathf.Approximately(_lastMaxHP, maxHP))
        {
            return;
        }
        var ratio = maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
        if (!_isInitialized || !Mathf.Approximately(_lastMaxHP, maxHP)) // 최대HP 증가로 인한 비율감소를 피해로 판단하지 않게
        {
            _targetRatio = ratio;
            _fillRatio = ratio;
            _trailRatio = ratio;

            _changeState = HealthChangeState.None;
            _healthFillImage.color = _originalFillColor;
            _isInitialized = true;
        }
        else if (ratio < _targetRatio) // 피해감지
        {
            if (_changeState == HealthChangeState.Heal) _trailRatio = _targetRatio;

            _trailRatio = Mathf.Max(_trailRatio, _targetRatio);
            _fillRatio = ratio;
            _targetRatio = ratio;

            _changeTrailImage.color = _damageTrailColor;
            _changeState = HealthChangeState.Damage;
            _flashEndTime = Time.time + _flashDuration;
        }
        else if (ratio > _targetRatio) // 회복 감지
        {
            if (_changeState == HealthChangeState.Damage) _fillRatio = _targetRatio;

            _targetRatio = ratio;
            _trailRatio = ratio;

            _changeTrailImage.color = _healTrailColor;
            _changeState = HealthChangeState.Heal;
            _flashEndTime = Time.time + _flashDuration;
        }

        _lastCurrentHP = currentHP;
        _lastMaxHP = maxHP;

        _healthSlider.maxValue = maxHP;
        _healthSlider.value = _fillRatio * maxHP;

        _healthText.text = $"{Mathf.RoundToInt(currentHP)} / {Mathf.RoundToInt(maxHP)}";
    }
}