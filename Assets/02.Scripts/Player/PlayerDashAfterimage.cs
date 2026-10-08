using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDashAfterimage : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField, Range(2, 16)] private int _imageCount = 6;
    [SerializeField, Min(0.01f)] private float _duration = 0.45f;
    [SerializeField] private Color _tint = new Color(0.25f, 0.85f, 1f, 0.55f);

    private sealed class Afterimage
    {
        public GameObject Root;
        public SpriteRenderer[] Parts;
        public float[] Alpha;
        public float Age;
        public float Lifetime;
    }

    private SpriteRenderer[] _sources;
    private Afterimage[] _images;
    private GameObject _container;

    private void Awake()
    {
        _sources = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void InitializePool()
    {
        if (_images != null || _material == null) return;
        _container = new GameObject("Player Dash Afterimages");
        SceneManager.MoveGameObjectToScene(_container, gameObject.scene);
        _images = new Afterimage[Mathf.Clamp(_imageCount, 2, 16)];
        for (int i = 0; i < _images.Length; i++)
        {
            var image = new Afterimage
            {
                Root = new GameObject("Afterimage " + i),
                Parts = new SpriteRenderer[_sources.Length],
                Alpha = new float[_sources.Length]
            };
            image.Root.transform.SetParent(_container.transform, false);
            for (int j = 0; j < _sources.Length; j++)
            {
                var part = new GameObject("Sprite " + j).AddComponent<SpriteRenderer>();
                part.transform.SetParent(image.Root.transform, false);
                part.sharedMaterial = _material;
                image.Parts[j] = part;
            }

            image.Root.SetActive(false);
            _images[i] = image;
        }
    }

    public void Play(Vector2 start, Vector2 destination)
    {
        if (!isActiveAndEnabled || (destination - start).sqrMagnitude < 0.0001f) return;
        InitializePool();
        if (_images == null) return;

        // TODO 게임씬 수정권 있을 때 프리팹으로 수정하기
        for (int i = 0; i < _images.Length; i++)
        {
            Afterimage image = _images[i];
            // 도착 지점은 실제 플레이어가 차지하므로 잔상은 그 직전까지만 남깁니다.
            float progress = i / (float)_images.Length;
            Vector2 position = Vector2.Lerp(start, destination, progress);
            image.Root.transform.position = new Vector3(position.x, position.y, transform.position.z);
            image.Age = 0f;
            image.Lifetime = Mathf.Max(0.01f, _duration) * Mathf.Lerp(0.55f, 1f, progress);
            for (int j = 0; j < _sources.Length; j++)
            {
                SpriteRenderer source = _sources[j];
                SpriteRenderer part = image.Parts[j];
                bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy &&
                               source.sprite != null && source.name != "Shadow";
                part.enabled = visible;
                if (!visible) continue;
                part.sprite = source.sprite;
                part.flipX = source.flipX;
                part.flipY = source.flipY;
                part.sortingLayerID = source.sortingLayerID;
                part.sortingOrder = source.sortingOrder - 10;
                part.gameObject.layer = source.gameObject.layer;
                part.transform.localPosition = source.transform.position - transform.position;
                part.transform.rotation = source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;
                image.Alpha[j] = _tint.a * source.color.a * Mathf.Lerp(0.5f, 1f, progress);
                part.color = new Color(_tint.r, _tint.g, _tint.b, image.Alpha[j]);
            }

            image.Root.SetActive(true);
        }
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (_images == null) return;
        foreach (Afterimage image in _images)
        {
            if (!image.Root.activeSelf) continue;
            image.Age += deltaTime;
            float remaining = 1f - Mathf.Clamp01(image.Age / image.Lifetime);
            if (remaining <= 0f)
            {
                image.Root.SetActive(false);
                continue;
            }

            for (int j = 0; j < image.Parts.Length; j++)
            {
                Color color = image.Parts[j].color;
                color.a = image.Alpha[j] * remaining;
                image.Parts[j].color = color;
            }
        }
    }

    private void OnDisable()
    {
        if (_images == null) return;
        foreach (Afterimage image in _images)
        {
            if (image.Root != null) image.Root.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_container != null) Destroy(_container);
    }
}