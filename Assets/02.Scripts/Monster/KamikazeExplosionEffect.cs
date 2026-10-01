using UnityEngine;

// 폭발 시 자동 생성되는 시각 효과입니다. 충돌이나 데미지는 처리하지 않습니다.
public sealed class KamikazeExplosionEffect : MonoBehaviour
{
    private const int TextureSize = 48;
    private const float Duration = 0.35f;

    private static Sprite _ringSprite;
    private static Sprite _flashSprite;

    private SpriteRenderer _ring;
    private SpriteRenderer _flash;
    private Color _color;
    private float _maximumScale;
    private float _age;

    public static void Spawn(
        Vector3 position, float radius, Color color,
        int objectLayer, int sortingLayerId, int sortingOrder)
    {
        if (_ringSprite == null)
            _ringSprite = CreateSprite(false);
        if (_flashSprite == null)
            _flashSprite = CreateSprite(true);

        GameObject effectObject = new GameObject("KamikazeExplosionEffect");
        effectObject.layer = objectLayer;
        effectObject.transform.position = position;
        KamikazeExplosionEffect effect = effectObject.AddComponent<KamikazeExplosionEffect>();
        effect._color = color;
        effect._maximumScale = Mathf.Max(0.1f, radius) * 2.4f;
        effect._ring = CreateRenderer(effectObject.transform, "Shockwave", _ringSprite,
            sortingLayerId, sortingOrder);
        effect._flash = CreateRenderer(effectObject.transform, "Flash", _flashSprite,
            sortingLayerId, sortingOrder + 1);
        effect.RefreshVisual();
        Destroy(effectObject, Duration + 0.1f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        RefreshVisual();

        if (_age >= Duration)
            Destroy(gameObject);
    }

    private void RefreshVisual()
    {
        float progress = Mathf.Clamp01(_age / Duration);
        float expansion = 1f - (1f - progress) * (1f - progress);
        _ring.transform.localScale = Vector3.one *
            Mathf.Lerp(_maximumScale * 0.15f, _maximumScale, expansion);

        Color ringColor = _color;
        ringColor.a *= 1f - progress;
        _ring.color = ringColor;

        float flashProgress = Mathf.Clamp01(_age / 0.12f);
        _flash.transform.localScale = Vector3.one *
            Mathf.Lerp(_maximumScale * 0.25f, _maximumScale * 0.5f, flashProgress);
        Color flashColor = Color.Lerp(Color.white, _color, flashProgress);
        flashColor.a = _color.a * (1f - flashProgress);
        _flash.color = flashColor;
    }

    private static SpriteRenderer CreateRenderer(
        Transform parent, string objectName, Sprite sprite, int layerId, int order)
    {
        GameObject child = new GameObject(objectName);
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = layerId;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite CreateSprite(bool flash)
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.name = flash ? "KamikazeExplosionFlash" : "KamikazeExplosionRing";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontSave;
        Color32[] pixels = new Color32[TextureSize * TextureSize];
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = x - (TextureSize - 1) * 0.5f;
                float dy = y - (TextureSize - 1) * 0.5f;
                float squaredRadius = dx * dx + dy * dy;
                bool visible = flash
                    ? Mathf.Abs(dx) + Mathf.Abs(dy) <= 18f ||
                      (Mathf.Abs(dx) <= 2.5f && Mathf.Abs(dy) <= 22f) ||
                      (Mathf.Abs(dy) <= 2.5f && Mathf.Abs(dx) <= 22f)
                    : squaredRadius <= 400f && squaredRadius >= 289f;

                if (visible)
                    pixels[y * TextureSize + x] = white;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f), TextureSize, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
