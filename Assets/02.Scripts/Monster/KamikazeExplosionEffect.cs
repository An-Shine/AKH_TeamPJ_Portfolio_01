using UnityEngine;

// 자동 생성 또는 별도의 이펙트 프리팹에서 사용하는 시각 효과입니다.
// 충돌이나 데미지는 처리하지 않습니다.
public sealed class KamikazeExplosionEffect : MonoBehaviour
{
    private const int TextureSize = 48;
    private const float Duration = 0.35f;

    private static Sprite _ringSprite;
    private static Sprite _flashSprite;

    [Header("별도 이펙트 프리팹의 기본값")]
    [Tooltip("Spawn으로 자동 생성할 때는 몬스터가 전달한 반경을 사용합니다.")]
    [SerializeField, Min(0.1f)] private float _effectRadius = 2f;
    [SerializeField] private Color _effectColor = new Color(0.2f, 0.9f, 1f, 1f);

    private SpriteRenderer _ring;
    private SpriteRenderer _flash;
    private Color _color;
    private float _maximumScale;
    private float _age;
    private bool _initialized;

    public static void Spawn(
        Vector3 position, float radius, Color color,
        int objectLayer, int sortingLayerId, int sortingOrder)
    {
        GameObject effectObject = new GameObject("KamikazeExplosionEffect");
        effectObject.layer = objectLayer;
        effectObject.transform.position = position;
        KamikazeExplosionEffect effect = effectObject.AddComponent<KamikazeExplosionEffect>();
        effect.Initialize(radius, color, sortingLayerId, sortingOrder);
    }

    private void Start()
    {
        // Spawn은 생성 직후 초기화합니다. 프리팹으로 생성한 경우에만 보충합니다.
        if (_initialized)
        {
            return;
        }

        // 몬스터에 잘못 붙었을 때 이펙트의 수명 처리로 본체를 삭제하지 않습니다.
        if (GetComponentInParent<NormalMonster>() != null)
        {
            enabled = false;
            Debug.LogWarning(
                $"{name}: 몬스터 본체에서 KamikazeExplosionEffect 컴포넌트를 제거하세요. " +
                "폭발 시 별도 이펙트 오브젝트가 자동으로 생성됩니다.", this);
            return;
        }

        SpriteRenderer reference = GetComponentInChildren<SpriteRenderer>(true);
        int sortingLayerId = reference != null ? reference.sortingLayerID : 0;
        int sortingOrder = reference != null ? reference.sortingOrder + 1 : 1;
        Initialize(_effectRadius, _effectColor, sortingLayerId, sortingOrder);
    }

    private void Initialize(float radius, Color color, int sortingLayerId, int sortingOrder)
    {
        if (_initialized)
        {
            return;
        }

        if (_ringSprite == null)
        {
            _ringSprite = CreateSprite(false);
        }

        if (_flashSprite == null)
        {
            _flashSprite = CreateSprite(true);
        }

        _color = color;
        _maximumScale = Mathf.Max(0.1f, radius) * 2.4f;
        _age = 0f;
        _ring = CreateRenderer(transform, "Shockwave", _ringSprite,
            sortingLayerId, sortingOrder);
        _flash = CreateRenderer(transform, "Flash", _flashSprite,
            sortingLayerId, sortingOrder + 1);
        _initialized = true;
        RefreshVisual();
        Destroy(gameObject, Duration + 0.1f);
    }

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        if (_ring == null || _flash == null)
        {
            enabled = false;
            return;
        }

        _age += Time.deltaTime;
        RefreshVisual();

        if (_age >= Duration)
        {
            Destroy(gameObject);
        }
    }

    private void RefreshVisual()
    {
        if (!_initialized || _ring == null || _flash == null)
        {
            return;
        }

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
                {
                    pixels[y * TextureSize + x] = white;
                }
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
