using UnityEngine;

public class Afterimage : MonoBehaviour
{
    [SerializeField] private Transform _sourceRoot;
    [SerializeField] private SpriteRenderer[] _sources;
    [SerializeField] private SpriteRenderer[] _parts;

    private float[] _alpha;
    private float _age;
    private float _lifetime;

    private void Awake()
    {
        _alpha = new float[_parts.Length];
    }

    public void Spawn(Vector2 position, Color color, float lifetime)
    {
        if (_sourceRoot == null || !_sourceRoot.gameObject.activeInHierarchy) return;
        gameObject.SetActive(true);
        transform.position = new Vector3(position.x, position.y, _sourceRoot.position.z);
        _age = 0f;
        _lifetime = Mathf.Max(0.01f, lifetime);

        for (int i = 0; i < _parts.Length; i++)
        {
            SpriteRenderer source = _sources[i];
            SpriteRenderer part = _parts[i];
            bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy &&
                           source.sprite != null;
            part.enabled = visible;
            if (!visible) continue;
            part.sprite = source.sprite;
            part.flipX = source.flipX;
            part.flipY = source.flipY;
            part.transform.position = transform.position + source.transform.position - _sourceRoot.position;
            part.transform.rotation = source.transform.rotation;
            part.transform.localScale = source.transform.lossyScale;
            _alpha[i] = color.a * source.color.a;
            part.color = new Color(color.r, color.g, color.b, _alpha[i]);
        }
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        _age += deltaTime;
        float remaining = 1f - Mathf.Clamp01(_age / _lifetime);
        if (remaining <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        for (int i = 0; i < _parts.Length; i++)
        {
            Color color = _parts[i].color;
            color.a = _alpha[i] * remaining;
            _parts[i].color = color;
        }
    }
}
