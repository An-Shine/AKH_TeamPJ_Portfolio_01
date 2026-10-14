using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    public sealed class BossHitVfx : MonoBehaviour
    {
        private const float HitSizeMultiplier = 0.5f;

        private sealed class Burst
        {
            public SpriteRenderer Renderer;
            public bool Blood;
            public float Age;
            public float Lifetime;
            public Vector2 Velocity;
            public bool Active;
        }

        private readonly Burst[] _bursts = new Burst[96];
        private readonly Sprite[] _sparkFrames = new Sprite[8];
        private readonly Sprite[] _bloodFrames = new Sprite[8];
        private SpriteRenderer[] _bodyRenderers;
        private global::BossMonster _owner;
        private Material _material;
        private int _layer;
        private int _sortingLayer;
        private int _order;
        private int _nextBurst;
        private bool _ready;

        public bool HasActiveBursts { get; private set; }

        public void Initialize(global::BossMonster owner, Material material, int layer, int sortingLayer, int order)
        {
            _owner = owner;
            _material = material;
            _layer = layer;
            _sortingLayer = sortingLayer;
            _order = order;
            _bodyRenderers = owner.GetComponentsInChildren<SpriteRenderer>(true);
            Texture2D atlas = Resources.Load<Texture2D>("Monster/BossHitUpgrade/BossHitAtlas");
            if (atlas == null)
            {
                Debug.LogError("보스 피격 이미지가 누락됐습니다. BossHitAtlas.png도 함께 Import하세요.", owner);
                return;
            }

            float cellWidth = atlas.width / 4f;
            float cellHeight = atlas.height / 4f;
            for (int i = 0; i < 8; i++)
            {
                int column = i % 4;
                int sparkRow = i / 4;
                _sparkFrames[i] = CreateFrame(atlas, column, sparkRow, cellWidth, cellHeight, "Spark_" + i);
                _bloodFrames[i] = CreateFrame(atlas, column, sparkRow + 2, cellWidth, cellHeight, "Blood_" + i);
            }

            _ready = true;
        }

        private static Sprite CreateFrame(Texture2D atlas, int column, int row, float width, float height, string name)
        {
            int left = Mathf.RoundToInt(column * width);
            int right = Mathf.RoundToInt((column + 1) * width);
            int bottom = Mathf.RoundToInt(atlas.height - (row + 1) * height);
            int top = Mathf.RoundToInt(atlas.height - row * height);
            Sprite sprite = Sprite.Create(atlas,
                new Rect(left + 1, bottom + 1, right - left - 2, top - bottom - 2),
                Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "BossHit_" + name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        public void PlayHit()
        {
            if (!_ready || _owner == null || Time.timeScale <= 0f)
            {
                return;
            }

            Bounds bounds = new Bounds(_owner.transform.position, Vector3.one * _owner.BodyWidth);
            float largestArea = 0f;
            foreach (SpriteRenderer body in _bodyRenderers)
            {
                if (body == null || body.sprite == null || !body.enabled)
                {
                    continue;
                }

                Bounds candidate = body.bounds;
                float area = candidate.size.x * candidate.size.y;
                if (area > largestArea)
                {
                    largestArea = area;
                    bounds = candidate;
                }
            }

            float size = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * 0.9f, 1.8f, 4.8f)
                         * HitSizeMultiplier;
            Vector2 point = (Vector2)bounds.center + new Vector2(
                Random.Range(-bounds.extents.x * 0.6f, bounds.extents.x * 0.6f),
                Random.Range(-bounds.extents.y * 0.25f, bounds.extents.y * 0.6f));
            // One accepted hit plays either sparks or blood, never both.
            bool blood = Random.value < 0.5f;
            float variation = blood ? Random.Range(0.8f, 1.15f) : Random.Range(0.85f, 1.2f);
            Emit(point, size * variation, blood);
        }

        private void Emit(Vector2 point, float size, bool blood)
        {
            Burst burst = _bursts[_nextBurst];
            if (burst == null)
            {
                GameObject root = new GameObject("BossBodyHit", typeof(SpriteRenderer));
                root.layer = _layer;
                root.transform.SetParent(transform, false);
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                renderer.sharedMaterial = _material;
                renderer.sortingLayerID = _sortingLayer;
                burst = new Burst { Renderer = renderer };
                _bursts[_nextBurst] = burst;
            }

            _nextBurst = (_nextBurst + 1) % _bursts.Length;
            burst.Blood = blood;
            burst.Age = 0f;
            burst.Lifetime = blood ? 0.42f : 0.3f;
            burst.Velocity = Random.insideUnitCircle * (blood ? 0.6f : 0.25f);
            burst.Active = true;
            SpriteRenderer body = burst.Renderer;
            body.sprite = blood ? _bloodFrames[0] : _sparkFrames[0];
            body.sortingOrder = _order + (blood ? 20 : 21);
            body.color = Color.white;
            body.transform.position = new Vector3(point.x, point.y, _owner.transform.position.z);
            body.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            body.transform.localScale = new Vector3(size / body.sprite.bounds.size.x,
                size / body.sprite.bounds.size.y, 1f);
            body.enabled = true;
            HasActiveBursts = true;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
            {
                return;
            }

            HasActiveBursts = false;
            foreach (Burst burst in _bursts)
            {
                if (burst == null || !burst.Active)
                {
                    continue;
                }

                burst.Age += Time.deltaTime;
                if (burst.Age >= burst.Lifetime)
                {
                    burst.Active = false;
                    burst.Renderer.enabled = false;
                    continue;
                }

                HasActiveBursts = true;
                float progress = burst.Age / burst.Lifetime;
                int frame = Mathf.Min(7, Mathf.FloorToInt(progress * 8f));
                burst.Renderer.sprite = burst.Blood ? _bloodFrames[frame] : _sparkFrames[frame];
                burst.Renderer.transform.position += (Vector3)(burst.Velocity * Time.deltaTime);
                burst.Renderer.color = new Color(1f, 1f, 1f, 1f - progress * progress);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < 8; i++)
            {
                if (_sparkFrames[i] != null)
                {
                    Destroy(_sparkFrames[i]);
                }

                if (_bloodFrames[i] != null)
                {
                    Destroy(_bloodFrames[i]);
                }
            }
        }
    }
}
