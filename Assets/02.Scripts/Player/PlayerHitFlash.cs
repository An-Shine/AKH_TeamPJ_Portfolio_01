using DG.Tweening;
using UnityEngine;

public class PlayerHitFlash : MonoBehaviour
{
    private float _duration = 0.3f;
    private float _dashGlowDuration = 0.5f;
    [SerializeField, ColorUsage(false, true)]
    private Color _dashGlowColor = new Color(1.5f, 2.5f, 3f, 1f);
    private readonly int _flashAmountId = Shader.PropertyToID("_FlashAmount");
    private readonly int _flashColorId = Shader.PropertyToID("_FlashColor");
    private Color _flashColor = Color.red;
    private readonly int _dashGlowAmountId = Shader.PropertyToID("_DashGlowAmount");
    private readonly int _dashGlowColorId = Shader.PropertyToID("_DashGlowColor");
    private SpriteRenderer[] _renderers;
    private MaterialPropertyBlock _properties;
    private Tween _tween;
    private float _amount;
    private Tween _dashTween;
    private float _dashAmount;

    private void Awake()
    {
        _renderers = transform.GetComponentsInChildren<SpriteRenderer>(true);
        _properties = new MaterialPropertyBlock();
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        _tween?.Kill();
        _flashColor = Color.red;
        Apply(1f);
        _tween = DOTween.To(() => _amount, Apply, 0f, _duration)
            .SetEase(Ease.Linear)
            .SetTarget(this)
            .OnComplete(() => _tween = null);
    }

    public void PlayRevive(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f) return;

        _tween?.Kill();
        _flashColor = Color.white;
        Apply(1f);
        // 0.1초마다 흰색과 원래 색을 번갈아 표시하고 종료 시 원래 색으로 복원합니다.
        float elapsed = 0f;
        _tween = DOTween.To(() => elapsed, value =>
            {
                elapsed = value;
                Apply(Mathf.FloorToInt(value / 0.1f) % 2 == 0 ? 1f : 0f);
            }, duration, duration)
            .SetEase(Ease.Linear)
            .SetTarget(this)
            .OnComplete(() =>
            {
                Apply(0f);
                _flashColor = Color.red;
                _tween = null;
            });
    }

    public void PlayDashGlow()
    {
        if (!isActiveAndEnabled) return;
        _dashTween?.Kill();
        ApplyDashGlow(1f);
        _dashTween = DOTween.To(() => _dashAmount, ApplyDashGlow, 0f, _dashGlowDuration)
            .SetEase(Ease.OutQuad)
            .SetTarget(this)
            .OnComplete(() => _dashTween = null);
    }

    private void ApplyDashGlow(float amount)
    {
        _dashAmount = amount;
        foreach (SpriteRenderer sprite in _renderers)
        {
            // 발광 셰이더를 쓰는 몸체/팔만 처리하고 그림자와 일반 스프라이트는 유지합니다.
            if (sprite == null || sprite.sharedMaterial == null ||
                !sprite.sharedMaterial.HasProperty(_dashGlowAmountId))
            {
                continue;
            }

            sprite.GetPropertyBlock(_properties);
            _properties.SetFloat(_dashGlowAmountId, amount);
            _properties.SetColor(_dashGlowColorId, _dashGlowColor);
            sprite.SetPropertyBlock(_properties);
        }
    }

    private void Apply(float amount)
    {
        _amount = amount;
        foreach (SpriteRenderer sprite in _renderers)
        {
            if (sprite == null)
            {
                continue;
            }

            sprite.GetPropertyBlock(_properties);
            _properties.SetFloat(_flashAmountId, amount);
            _properties.SetColor(_flashColorId, _flashColor);
            sprite.SetPropertyBlock(_properties);
        }
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;
        _dashTween?.Kill();
        _dashTween = null;
        if (_renderers != null)
        {
            Apply(0f);
            ApplyDashGlow(0f);
        }
    }
}
