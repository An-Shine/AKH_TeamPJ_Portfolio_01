using System.Collections.Generic;
using UnityEngine;

// 근접 공격과 적중을 보여 주는 픽셀 효과입니다. 데미지는 몬스터/투사체가 한 번만 처리합니다.
public sealed class MonsterCombatEffect : MonoBehaviour
{
    private const int SlashTextureSize = 64;
    private static readonly Dictionary<int, Sprite> SlashSprites = new Dictionary<int, Sprite>();
    private static Sprite _impactSprite;

    private SpriteRenderer _renderer;
    private Color _color;
    private float _duration;
    private float _age;
    private float _startScale;
    private float _endScale;

    public static void SpawnSlash(
        Vector3 position, Vector2 direction, float range, float angle, Color color, Transform source)
    {
        int angleKey = Mathf.Clamp(Mathf.RoundToInt(angle), 1, 360);
        if (!SlashSprites.TryGetValue(angleKey, out Sprite sprite) || sprite == null)
        {
            sprite = CreateSlash(angleKey);
            SlashSprites[angleKey] = sprite;
        }

        // 텍스처의 30px 반지름을 실제 공격 범위에 맞춥니다.
        float scale = Mathf.Max(0.1f, range) * SlashTextureSize / 30f;
        Create(position, direction, sprite, color, source, 0.16f, scale * 0.92f, scale);
    }

    public static void SpawnImpact(Vector3 position, Vector2 direction, Color color, Transform source)
    {
        if (_impactSprite == null)
            _impactSprite = CreateImpact();
        Create(position, direction, _impactSprite, color, source, 0.18f, 0.28f, 0.5f);
    }

    private static void Create(
        Vector3 position, Vector2 direction, Sprite sprite, Color color, Transform source,
        float duration, float startScale, float endScale)
    {
        MonsterCombat.GetEffectSorting(source, out int layer, out int sortingLayer, out int order);
        GameObject effectObject = new GameObject("MonsterCombatEffect");
        effectObject.layer = layer;
        effectObject.transform.position = position;

        if (direction.sqrMagnitude > 0.000001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effectObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = order;
        MonsterCombatEffect effect = effectObject.AddComponent<MonsterCombatEffect>();
        effect._renderer = renderer;
        effect._color = color;
        effect._duration = duration;
        effect._startScale = startScale;
        effect._endScale = endScale;
        effect.RefreshVisual();
        Destroy(effectObject, duration + 0.1f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        RefreshVisual();
        if (_age >= _duration)
            Destroy(gameObject);
    }

    private void RefreshVisual()
    {
        float progress = Mathf.Clamp01(_age / _duration);
        transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, progress);
        Color color = Color.Lerp(Color.white, _color, Mathf.Clamp01(progress * 4f));
        color.a = _color.a * (1f - progress);
        _renderer.color = color;
    }

    private static Sprite CreateSlash(int angle)
    {
        Color32[] pixels = new Color32[SlashTextureSize * SlashTextureSize];
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < SlashTextureSize; y++)
        {
            for (int x = 0; x < SlashTextureSize; x++)
            {
                float dx = x - (SlashTextureSize - 1) * 0.5f;
                float dy = y - (SlashTextureSize - 1) * 0.5f;
                float squaredRadius = dx * dx + dy * dy;
                float pixelAngle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (squaredRadius <= 900f && squaredRadius >= 625f &&
                    Mathf.Abs(pixelAngle) <= angle * 0.5f)
                    pixels[y * SlashTextureSize + x] = white;
            }
        }
        return CreateSprite("MonsterMeleeSlash", SlashTextureSize, pixels);
    }

    private static Sprite CreateImpact()
    {
        const int size = 24;
        Color32[] pixels = new Color32[size * size];
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - (size - 1) * 0.5f;
                float dy = y - (size - 1) * 0.5f;
                bool visible = Mathf.Abs(dx) + Mathf.Abs(dy) <= 7f ||
                    (Mathf.Abs(dx) <= 1.5f && Mathf.Abs(dy) <= 10f) ||
                    (Mathf.Abs(dy) <= 1.5f && Mathf.Abs(dx) <= 10f);
                if (visible)
                    pixels[y * size + x] = white;
            }
        }
        return CreateSprite("MonsterHitImpact", size, pixels);
    }

    private static Sprite CreateSprite(string spriteName, int size, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontSave;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        sprite.name = spriteName;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
