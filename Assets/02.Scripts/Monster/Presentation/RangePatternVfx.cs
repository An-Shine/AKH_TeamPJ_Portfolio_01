using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02.Scripts.Monster.Presentation
{
    public enum RangePatternSprite
    {
        Projectile,
        Muzzle,
        Impact,
        Trail
    }

    public sealed class RangePatternVfx : MonoBehaviour
    {
        public const string ResourceRoot = "Monster/Range/PatternVfx/";
        private const int MaximumStamps = 96;
        private const int MaximumEmissionsPerFrame = 16;
        private static readonly string[] PatternNames =
        {
            "Fan", "Burst", "Sweep", "Twin", null, "EliteFan", "EliteSweep", "EliteTwin"
        };
        private static readonly string[] SpriteNames = { "Projectile", "Muzzle", "Impact", "Trail" };
        private static readonly Dictionary<int, RangePatternVfx> Pools = new Dictionary<int, RangePatternVfx>();
        private static readonly Sprite[,] Sprites = new Sprite[RangeAttackPatterns.Count, 4];
        private static bool _resourcesLoaded;
        private readonly List<Stamp> _stamps = new List<Stamp>(MaximumStamps);
        private Material _material;
        private Material _lineMaterial;
        private int _sceneHandle;
        private int _emissionFrame = -1;
        private int _emissionCount;

        private sealed class Stamp
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Lifetime;
            public float StartScale;
            public float EndScale;
            public float Opacity;
            public Vector3 Velocity;
        }

        public Material SpriteMaterial => _material;
        public Material LineMaterial => _lineMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            Pools.Clear();
            System.Array.Clear(Sprites, 0, Sprites.Length);
            _resourcesLoaded = false;
        }

        public static Sprite GetSprite(RangeAttackPattern pattern, RangePatternSprite part)
        {
            if (!_resourcesLoaded)
            {
                _resourcesLoaded = true;
                for (int i = 0; i < PatternNames.Length; i++)
                {
                    if (PatternNames[i] == null)
                        continue;
                    Sprite[] atlas = Resources.LoadAll<Sprite>(ResourceRoot + PatternNames[i]);
                    foreach (Sprite sprite in atlas)
                    {
                        for (int j = 0; j < SpriteNames.Length; j++)
                        {
                            if (sprite.name == SpriteNames[j])
                                Sprites[i, j] = sprite;
                        }
                    }
                }
            }

            int patternIndex = (int)pattern;
            int partIndex = (int)part;
            return patternIndex >= 0 && patternIndex < RangeAttackPatterns.Count && partIndex >= 0 && partIndex < 4
                ? Sprites[patternIndex, partIndex] : null;
        }

        public static RangePatternVfx ForScene(Scene scene)
        {
            if (Pools.TryGetValue(scene.handle, out RangePatternVfx pool) && pool != null)
                return pool;
            GameObject root = new GameObject("NormalRangePatternVfxPool");
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(root, scene);
            return root.AddComponent<RangePatternVfx>();
        }

        private void Awake()
        {
            _sceneHandle = gameObject.scene.handle;
            Pools[_sceneHandle] = this;
            _material = Resources.Load<Material>(ResourceRoot + "RangePatternUnlit");
            _lineMaterial = Resources.Load<Material>(ResourceRoot + "RangePatternAim");
            if (_material != null && (_material.shader == null || !_material.shader.isSupported))
                _material = null;
            if (_lineMaterial != null && (_lineMaterial.shader == null || !_lineMaterial.shader.isSupported))
                _lineMaterial = null;
        }

        public void Muzzle(RangeAttackPattern pattern, Vector3 position, Vector2 direction, SpriteRenderer body)
        {
            Emit(pattern, RangePatternSprite.Muzzle, position, direction, body, 0.44f, 0.64f, 0.09f, 1f, 7);
        }

        public void Trail(RangeAttackPattern pattern, Vector3 position, Vector2 direction, SpriteRenderer body)
        {
            Emit(pattern, RangePatternSprite.Trail, position - (Vector3)(direction * 0.13f), direction,
                body, 0.28f, 0.12f, 0.09f, 0.3f, -1);
        }

        public void Impact(RangeAttackPattern pattern, Vector3 position, Vector2 direction, SpriteRenderer body, bool fullImpact)
        {
            float size = fullImpact ? 0.58f : 0.32f;
            Emit(pattern, RangePatternSprite.Impact, position, direction, body, size, size * 1.4f,
                0.16f, fullImpact ? 0.95f : 0.55f, -10);
        }

        private void Emit(RangeAttackPattern pattern, RangePatternSprite part, Vector3 position, Vector2 direction,
            SpriteRenderer body, float startWidth, float endWidth, float lifetime, float opacity, int order)
        {
            Sprite sprite = GetSprite(pattern, part);
            if (sprite == null || body == null || _material == null || Time.timeScale <= 0f)
                return;
            if (_emissionFrame != Time.frameCount)
            {
                _emissionFrame = Time.frameCount;
                _emissionCount = 0;
            }

            if (_emissionCount >= MaximumEmissionsPerFrame)
                return;
            Stamp stamp = null;
            foreach (Stamp candidate in _stamps)
            {
                if (!candidate.Renderer.enabled)
                {
                    stamp = candidate;
                    break;
                }
            }

            if (stamp == null)
            {
                if (_stamps.Count >= MaximumStamps)
                    return;
                GameObject child = new GameObject("RangePatternStamp");
                child.transform.SetParent(transform, false);
                stamp = new Stamp { Renderer = child.AddComponent<SpriteRenderer>() };
                stamp.Renderer.sharedMaterial = _material;
                _stamps.Add(stamp);
            }

            _emissionCount++;
            stamp.Renderer.sprite = sprite;
            stamp.Renderer.gameObject.layer = body.gameObject.layer;
            stamp.Renderer.flipX = false;
            stamp.Renderer.flipY = false;
            stamp.Renderer.color = new Color(1f, 1f, 1f, opacity);
            stamp.Renderer.sortingLayerID = body.sortingLayerID;
            stamp.Renderer.sortingOrder = body.sortingOrder + order;
            stamp.Renderer.transform.position = position;
            stamp.Renderer.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            float width = Mathf.Max(0.01f, sprite.bounds.size.x);
            stamp.StartScale = startWidth / width;
            stamp.EndScale = endWidth / width;
            stamp.Renderer.transform.localScale = Vector3.one * stamp.StartScale;
            stamp.Age = 0f;
            stamp.Lifetime = lifetime;
            stamp.Opacity = opacity;
            stamp.Velocity = part == RangePatternSprite.Trail ? -(Vector3)direction * 0.2f : Vector3.zero;
            stamp.Renderer.enabled = true;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
                return;
            foreach (Stamp stamp in _stamps)
            {
                if (!stamp.Renderer.enabled)
                    continue;
                stamp.Age += Time.deltaTime;
                if (stamp.Age >= stamp.Lifetime)
                {
                    stamp.Renderer.enabled = false;
                    continue;
                }

                float progress = stamp.Age / stamp.Lifetime;
                float remaining = 1f - progress;
                stamp.Renderer.transform.localScale = Vector3.one * Mathf.Lerp(stamp.StartScale, stamp.EndScale, progress);
                stamp.Renderer.transform.position += stamp.Velocity * Time.deltaTime;
                stamp.Renderer.color = new Color(1f, 1f, 1f, stamp.Opacity * remaining * remaining);
            }
        }

        private void OnDestroy()
        {
            if (Pools.TryGetValue(_sceneHandle, out RangePatternVfx pool) && pool == this)
                Pools.Remove(_sceneHandle);
        }
    }
}
