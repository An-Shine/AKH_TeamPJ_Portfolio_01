using UnityEngine;

public sealed class BossAttackIndicator : MonoBehaviour
{
    private const int TextureSize = 64;
    private static Sprite _ringSprite;
    private static Sprite _bulletSprite;
    private SpriteRenderer _renderer;
    private Color _color;
    private float _radius;
    private float _age;
    private float _lifetime;
    private bool _isImpact;

    public static Sprite DefaultBulletSprite
    {
        get
        {
            if (_bulletSprite == null)
            {
                _bulletSprite = CreateSprite(16, false);
            }

            return _bulletSprite;
        }
    }

    public static BossAttackIndicator Spawn(Vector3 position, float radius, Color color,
        Transform source, bool isImpact, Transform parent = null)
    {
        if (_ringSprite == null)
        {
            _ringSprite = CreateSprite(TextureSize, true);
        }

        MonsterCombat.GetEffectSorting(source, out int layer, out int sortingLayer, out int order);
        GameObject marker = new GameObject(isImpact ? "BossImpactRing" : "BossWarningRing");
        marker.layer = layer;
        marker.transform.position = position;
        marker.transform.SetParent(parent, true);
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = _ringSprite;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = order - 2;
        BossAttackIndicator indicator = marker.AddComponent<BossAttackIndicator>();
        indicator._renderer = renderer;
        indicator._color = color;
        indicator._radius = Mathf.Max(0.1f, radius);
        indicator._isImpact = isImpact;
        indicator._lifetime = isImpact ? 0.3f : 0f;
        indicator.RefreshVisual();
        return indicator;
    }

    public void SetRadius(float radius)
    {
        _radius = Mathf.Max(0.1f, radius);
        RefreshVisual();
    }

    public void ExpireAfter(float seconds)
    {
        _lifetime = Mathf.Max(0.01f, seconds);
    }

    public void Flash()
    {
        _isImpact = true;
        _age = 0f;
        _lifetime = 0.3f;
        RefreshVisual();
    }

    private void Update()
    {
        _age += Time.deltaTime;
        RefreshVisual();
        if (_lifetime > 0f && _age >= _lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void RefreshVisual()
    {
        if (_renderer == null)
        {
            return;
        }

        float progress = _isImpact ? Mathf.Clamp01(_age / _lifetime) : 0f;
        Color color = _isImpact ? Color.Lerp(Color.white, _color, progress) : _color;
        color.a = _isImpact ? 1f - progress : 0.5f + Mathf.PingPong(_age * 0.7f, 0.3f);
        _renderer.color = color;
        transform.localScale = Vector3.one * (_radius * TextureSize / 29f);
    }

    private static Sprite CreateSprite(int size, bool ring)
    {
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - (size - 1) * 0.5f;
                float dy = y - (size - 1) * 0.5f;
                float squaredRadius = dx * dx + dy * dy;
                bool visible = ring ? squaredRadius >= 28f * 28f && squaredRadius <= 30f * 30f
                    : Mathf.Abs(dx) + Mathf.Abs(dy) <= 6f;
                if (visible)
                {
                    pixels[y * size + x] = new Color32(255, 255, 255, 255);
                }
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontSave;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
