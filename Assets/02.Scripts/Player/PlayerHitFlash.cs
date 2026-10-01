using DG.Tweening;
using UnityEngine;

public class PlayerHitFlash : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _duration = 0.12f;
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private SpriteRenderer[] _renderers;
    private MaterialPropertyBlock _properties;
    private Tween _tween;
    private float _amount;

    private void Awake()
    {
        Transform visual = transform.Find("PlayerVisual");
        _renderers = (visual != null ? visual : transform).GetComponentsInChildren<SpriteRenderer>(true);
        _properties = new MaterialPropertyBlock();
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        _tween?.Kill();
        Apply(1f);
        _tween = DOTween.To(() => _amount, Apply, 0f, _duration)
            .SetEase(Ease.Linear)
            .SetTarget(this)
            .OnComplete(() => _tween = null);
    }

    private void Apply(float amount)
    {
        _amount = amount;
        foreach (SpriteRenderer sprite in _renderers)
        {
            sprite.GetPropertyBlock(_properties);
            _properties.SetFloat(FlashAmountId, amount);
            sprite.SetPropertyBlock(_properties);
        }
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;
        if (_renderers != null)
        {
            Apply(0f);
        }
    }
}