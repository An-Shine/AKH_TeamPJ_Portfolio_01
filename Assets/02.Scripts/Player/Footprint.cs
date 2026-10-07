using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Footprint : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _holdDuration = 2f;
    [SerializeField, Min(0f)] private float _fadeDuration = 1f;

    private SpriteRenderer _spriteRenderer;
    private Color _baseColor;
    private FootprintPool _owner;
    private float _age;
    private bool _isSpawned;

    public void Initialize(FootprintPool owner)
    {
        _owner = owner;
        if (_spriteRenderer != null) return;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _baseColor = _spriteRenderer.color;
    }

    public void Show(Vector3 position, Quaternion rotation, bool flipY)
    {
        _age = 0f;
        _isSpawned = true;
        transform.SetPositionAndRotation(position, rotation);
        _spriteRenderer.color = _baseColor;
        _spriteRenderer.flipY = flipY;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _isSpawned = false;
        _age = 0f;
        _spriteRenderer.color = _baseColor;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (!_isSpawned) return;
        _age += deltaTime;
        if (_age >= _holdDuration + _fadeDuration)
        {
            _owner.Release(this);
            return;
        }

        float alpha = _fadeDuration > 0f
            ? 1f - Mathf.Clamp01((_age - _holdDuration) / _fadeDuration)
            : 1f;
        Color color = _baseColor;
        color.a *= alpha;
        _spriteRenderer.color = color;
    }
}
