using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02.Scripts.Monster.Presentation
{
    internal enum NormalFeelShape
    {
        Slash,
        Ring,
        Star,
        Pixel,
        Glow
    }

    // 장면별 풀에 연출을 보관하므로 몬스터/탄환이 사라져도 효과와 사운드가 끝까지 재생됩니다.
    internal sealed class NormalMonsterFeelEffects : MonoBehaviour
    {
        private const int MaximumStamps = 512;
        private const int MaximumVoices = 64;
        private static readonly Dictionary<int, NormalMonsterFeelEffects> ScenePools = new Dictionary<int, NormalMonsterFeelEffects>();
        private struct Stamp
        {
            public SpriteRenderer Renderer;
            public Vector3 Origin;
            public Vector2 Velocity;
            public Vector2 StartSize;
            public Vector2 EndSize;
            public Color Color;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        private readonly Stamp[] _stamps = new Stamp[MaximumStamps];
        private readonly AudioSource[] _voices = new AudioSource[MaximumVoices];
        private readonly bool[] _pausedVoices = new bool[MaximumVoices];
        private readonly Sprite[] _shapes = new Sprite[5];
        private readonly Dictionary<int, Sprite> _slashSprites = new Dictionary<int, Sprite>();
        private Material _additiveMaterial;
        private Material _outlineMaterial;
        private int _sceneHandle;
        private int _nextStamp;
        private int _createdStamps;
        private bool _paused;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools()
        {
            ScenePools.Clear();
        }

        internal static NormalMonsterFeelEffects ForScene(Scene scene)
        {
            if (ScenePools.TryGetValue(scene.handle, out NormalMonsterFeelEffects pool) && pool != null)
            {
                return pool;
            }

            GameObject root = new GameObject("NormalMonsterGameFeelEffects");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root.AddComponent<NormalMonsterFeelEffects>();
        }

        private void Awake()
        {
            _sceneHandle = gameObject.scene.handle;
            ScenePools[_sceneHandle] = this;
            Shader additive = Resources.Load<Shader>("Monster/NormalGameFeel/NormalFeelAdditive");
            if (additive == null)
            {
                additive = Shader.Find("Sprites/Default");
            }

            if (additive != null)
            {
                _additiveMaterial = new Material(additive)
                { hideFlags = HideFlags.DontSave };
            }

            Shader outline = Resources.Load<Shader>("Monster/NormalGameFeel/NormalFeelPixelRim");
            if (outline != null)
            {
                _outlineMaterial = new Material(outline)
                { hideFlags = HideFlags.DontSave };
            }

            for (int i = 0; i < _shapes.Length; i++)
            {
                _shapes[i] = CreateShape((NormalFeelShape)i);
            }
        }

        internal SpriteRenderer CreatePersistentRenderer(string name, bool outline = false)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(transform, false);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            Material material = outline ? _outlineMaterial : _additiveMaterial;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.enabled = false;
            return renderer;
        }

        internal bool HasOutlineMaterial => _outlineMaterial != null;
        internal Sprite Shape(NormalFeelShape shape) => _shapes[(int)shape];
        internal Sprite Slash(float angle)
        {
            int key = Mathf.Clamp(Mathf.RoundToInt(angle), 1, 360);
            if (key == 90)
            {
                return Shape(NormalFeelShape.Slash);
            }

            if (!_slashSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateShape(NormalFeelShape.Slash, key);
                _slashSprites[key] = sprite;
            }

            return sprite;
        }

        internal void Emit(Sprite sprite, Vector3 position, float rotation, Vector2 startSize, Vector2 endSize, Vector2 velocity, Color color, float lifetime, int layer, int sortingLayer, int order, bool flipX = false, bool flipY = false)
        {
            if (sprite == null)
            {
                return;
            }

            int index = _nextStamp;
            _nextStamp = (_nextStamp + 1) % MaximumStamps;
            Stamp stamp = _stamps[index];
            if (stamp.Renderer == null)
            {
                stamp.Renderer = CreatePersistentRenderer("NormalMonsterFeelStamp");
                _createdStamps = Mathf.Max(_createdStamps, index + 1);
            }

            SpriteRenderer renderer = stamp.Renderer;
            renderer.gameObject.layer = layer;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.flipX = flipX;
            renderer.flipY = flipY;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
            renderer.transform.position = position;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            ApplySize(renderer, startSize);
            renderer.enabled = true;
            stamp.Origin = position;
            stamp.Velocity = velocity;
            stamp.StartSize = startSize;
            stamp.EndSize = endSize;
            stamp.Color = color;
            stamp.Age = 0f;
            stamp.Lifetime = Mathf.Max(0.02f, lifetime);
            stamp.Active = true;
            _stamps[index] = stamp;
        }

        internal void PlaySound(AudioClip clip, Vector3 position, float volume, NormalMonsterFeelProfile profile)
        {
            if (clip == null || volume <= 0f || Time.timeScale <= 0f)
            {
                return;
            }

            Camera camera = Camera.main;
            float distance = camera != null ? ((Vector2)camera.transform.position - (Vector2)position).magnitude : 0f;
            if (distance > 22f)
            {
                return;
            }

            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource voice = _voices[i];
                if (voice != null && (voice.isPlaying || _pausedVoices[i]))
                {
                    continue;
                }

                if (voice == null)
                {
                    GameObject sound = new GameObject("NormalMonsterFeelSound");
                    sound.transform.SetParent(transform, false);
                    voice = sound.AddComponent<AudioSource>();
                    voice.playOnAwake = false;
                    voice.loop = false;
                    voice.spatialBlend = 0f;
                    _voices[i] = voice;
                }

                voice.transform.position = position;
                voice.clip = clip;
                voice.volume = Mathf.Clamp01(volume) / (1f + distance * 0.06f);
                voice.pitch = Random.Range(0.96f, 1.04f);
                voice.outputAudioMixerGroup = profile.SoundMixerGroup;
                voice.Play();
                return;
            }
            // 사용 중인 소리를 덮어쓰지 않습니다. 풀이 가득 차면 새 소리만 생략합니다.
        }

        internal static void ApplySize(SpriteRenderer renderer, Vector2 size)
        {
            Vector3 source = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(size.x / Mathf.Max(0.001f, source.x), size.y / Mathf.Max(0.001f, source.y), 1f);
        }

        private void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (paused != _paused)
            {
                for (int i = 0; i < _voices.Length; i++)
                {
                    AudioSource voice = _voices[i];
                    if (voice == null)
                    {
                        continue;
                    }

                    if (paused && voice.isPlaying)
                    {
                        _pausedVoices[i] = true;
                        voice.Pause();
                    }
                    else if (!paused && _pausedVoices[i])
                    {
                        _pausedVoices[i] = false;
                        voice.UnPause();
                    }
                }

                _paused = paused;
            }

            if (paused)
            {
                return;
            }

            for (int i = 0; i < _createdStamps; i++)
            {
                Stamp stamp = _stamps[i];
                if (!stamp.Active || stamp.Renderer == null)
                {
                    continue;
                }

                stamp.Age += Time.deltaTime;
                float progress = Mathf.Clamp01(stamp.Age / stamp.Lifetime);
                if (progress >= 1f)
                {
                    stamp.Active = false;
                    stamp.Renderer.enabled = false;
                }
                else
                {
                    stamp.Renderer.transform.position = stamp.Origin + new Vector3(stamp.Velocity.x, stamp.Velocity.y, 0f) * stamp.Age;
                    ApplySize(stamp.Renderer, Vector2.Lerp(stamp.StartSize, stamp.EndSize, progress));
                    Color color = stamp.Color;
                    color.a *= (1f - progress) * (1f - progress);
                    stamp.Renderer.color = color;
                }

                _stamps[i] = stamp;
            }
        }

        private static Sprite CreateShape(NormalFeelShape shape, float slashAngle = 90f)
        {
            const int Size = 96;
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x - (Size - 1) * 0.5f) / (Size * 0.5f);
                    float dy = (y - (Size - 1) * 0.5f) / (Size * 0.5f);
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;
                    switch (shape)
                    {
                        case NormalFeelShape.Slash:
                        {
                            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                            float halfAngle = Mathf.Max(0.5f, slashAngle * 0.5f);
                            float inner = 0.64f + 0.22f * Mathf.Abs(angle) / halfAngle;
                            alpha = radius <= 0.95f && radius >= inner && Mathf.Abs(angle) <= halfAngle ? 1f : 0f;
                            break;
                        }

                        case NormalFeelShape.Ring:
                        {
                            alpha = radius >= 0.8f && radius <= 0.93f ? 1f : 0f;
                            break;
                        }

                        case NormalFeelShape.Star:
                        {
                            alpha = Mathf.Abs(dx) + Mathf.Abs(dy) <= 0.48f || (Mathf.Abs(dx) <= 0.06f && Mathf.Abs(dy) <= 0.93f) || (Mathf.Abs(dy) <= 0.06f && Mathf.Abs(dx) <= 0.93f) ? 1f : 0f;
                            break;
                        }

                        case NormalFeelShape.Pixel:
                        {
                            alpha = Mathf.Abs(dx) <= 0.75f && Mathf.Abs(dy) <= 0.75f ? 1f : 0f;
                            break;
                        }

                        case NormalFeelShape.Glow:
                        {
                            alpha = radius < 0.95f ? Mathf.Pow(1f - radius / 0.95f, 2f) : 0f;
                            alpha = Mathf.Round(alpha * 6f) / 6f;
                            break;
                        }
                    }

                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "NormalMonsterFeel_" + shape;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.DontSave;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private void OnDestroy()
        {
            if (ScenePools.TryGetValue(_sceneHandle, out NormalMonsterFeelEffects pool) && pool == this)
            {
                ScenePools.Remove(_sceneHandle);
            }

            foreach (Sprite sprite in _shapes)
            {
                if (sprite == null)
                {
                    continue;
                }

                Destroy(sprite.texture);
                Destroy(sprite);
            }

            foreach (Sprite sprite in _slashSprites.Values)
            {
                if (sprite != null)
                {
                    Destroy(sprite.texture);
                    Destroy(sprite);
                }
            }

            if (_additiveMaterial != null)
            {
                Destroy(_additiveMaterial);
            }

            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
            }
        }
    }
}
