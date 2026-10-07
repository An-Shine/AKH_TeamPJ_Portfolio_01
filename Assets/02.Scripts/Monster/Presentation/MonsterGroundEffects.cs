using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02.Scripts.Monster.Presentation
{
    // 몬스터와 분리된 장면별 풀입니다. 본체의 피격·크기 고정·파괴 처리가 발자국이나 잔향에 적용되지 않습니다.
    internal sealed class MonsterGroundEffects : MonoBehaviour
    {
        private const int MaximumStamps = 256;
        private const int MaximumVoices = 24;
        private const float MinimumSoundInterval = 0.025f;
        private static readonly Dictionary<int, MonsterGroundEffects> ScenePools = new Dictionary<int, MonsterGroundEffects>();
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
        private Sprite _shadowSprite;
        private Sprite _footprintSprite;
        private Sprite _dustSprite;
        private int _sceneHandle;
        private int _createdStamps;
        private int _nextStamp;
        private float _nextSoundTime;
        private bool _paused;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetScenePools()
        {
            ScenePools.Clear();
        }

        internal static MonsterGroundEffects ForScene(Scene scene)
        {
            if (ScenePools.TryGetValue(scene.handle, out MonsterGroundEffects pool) && pool != null)
            {
                return pool;
            }

            GameObject root = new GameObject("MonsterGroundEffects");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root.AddComponent<MonsterGroundEffects>();
        }

        private void Awake()
        {
            _sceneHandle = gameObject.scene.handle;
            ScenePools[_sceneHandle] = this;
            _shadowSprite = CreateSprite("MonsterGroundShadow", 32, 0);
            _footprintSprite = CreateSprite("MonsterFootprint", 16, 1);
            _dustSprite = CreateSprite("MonsterFootDust", 16, 2);
        }

        internal SpriteRenderer CreateShadow(string ownerName)
        {
            SpriteRenderer renderer = CreateRenderer(ownerName + "_GroundShadow");
            renderer.sprite = _shadowSprite;
            renderer.enabled = false;
            return renderer;
        }

        internal void Footprint(Vector3 position, Vector2 direction, float width, Color color, float lifetime, int layer, int sortingLayer, int order)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Vector2 size = new Vector2(width, width * 1.5f);
            Emit(_footprintSprite, position, angle, size, size, Vector2.zero, color, lifetime, layer, sortingLayer, order);
        }

        internal void Dust(Vector3 position, Vector2 direction, float width, Color color, float lifetime, int count, int layer, int sortingLayer, int order)
        {
            Vector2 side = new Vector2(-direction.y, direction.x);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : i / (float)(count - 1) * 2f - 1f;
                Vector2 velocity = side * offset * width * 2f - direction * width;
                Vector2 startSize = Vector2.one * width;
                Vector2 endSize = startSize * 2.5f;
                Emit(_dustSprite, position, i * 37f, startSize, endSize, velocity, color, lifetime, layer, sortingLayer, order);
            }
        }

        internal void PlayStep(AudioClip clip, Vector3 position, MonsterGroundProfile profile)
        {
            if (clip == null || profile.StepVolume <= 0f || Time.timeScale <= 0f || Time.time < _nextSoundTime)
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
                    GameObject soundObject = new GameObject("MonsterFootstepSound");
                    soundObject.transform.SetParent(transform, false);
                    voice = soundObject.AddComponent<AudioSource>();
                    voice.playOnAwake = false;
                    voice.spatialBlend = 0f;
                    voice.loop = false;
                    _voices[i] = voice;
                }

                voice.transform.position = position;
                voice.outputAudioMixerGroup = profile.SoundMixerGroup;
                voice.clip = clip;
                voice.volume = profile.StepVolume;
                voice.pitch = Random.Range(profile.MinimumPitch, profile.MaximumPitch);
                voice.Play();
                _nextSoundTime = Time.time + MinimumSoundInterval;
                return;
            }
        }

        private void Emit(Sprite sprite, Vector3 position, float angle, Vector2 startSize, Vector2 endSize, Vector2 velocity, Color color, float lifetime, int layer, int sortingLayer, int order)
        {
            int index = _nextStamp;
            _nextStamp = (_nextStamp + 1) % MaximumStamps;
            Stamp stamp = _stamps[index];
            if (stamp.Renderer == null)
            {
                stamp.Renderer = CreateRenderer("MonsterGroundStamp");
                _createdStamps = Mathf.Max(_createdStamps, index + 1);
            }

            SpriteRenderer renderer = stamp.Renderer;
            renderer.gameObject.layer = layer;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
            renderer.transform.position = position;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            renderer.transform.localScale = new Vector3(startSize.x, startSize.y, 1f);
            renderer.enabled = true;
            stamp.Origin = position;
            stamp.Velocity = velocity;
            stamp.StartSize = startSize;
            stamp.EndSize = endSize;
            stamp.Color = color;
            stamp.Age = 0f;
            stamp.Lifetime = Mathf.Max(0.05f, lifetime);
            stamp.Active = true;
            _stamps[index] = stamp;
        }

        private SpriteRenderer CreateRenderer(string objectName)
        {
            GameObject renderObject = new GameObject(objectName);
            renderObject.transform.SetParent(transform, false);
            return renderObject.AddComponent<SpriteRenderer>();
        }

        private void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (paused != _paused)
            {
                SetPaused(paused);
            }

            if (paused)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _createdStamps; i++)
            {
                Stamp stamp = _stamps[i];
                if (!stamp.Active || stamp.Renderer == null)
                {
                    continue;
                }

                stamp.Age += deltaTime;
                float progress = Mathf.Clamp01(stamp.Age / stamp.Lifetime);
                if (progress >= 1f)
                {
                    stamp.Renderer.enabled = false;
                    stamp.Active = false;
                }
                else
                {
                    Vector2 size = Vector2.Lerp(stamp.StartSize, stamp.EndSize, progress);
                    Color color = stamp.Color;
                    color.a *= 1f - progress;
                    stamp.Renderer.color = color;
                    stamp.Renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
                    stamp.Renderer.transform.position = stamp.Origin + new Vector3(stamp.Velocity.x, stamp.Velocity.y, 0f) * stamp.Age;
                }

                _stamps[i] = stamp;
            }
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource voice = _voices[i];
                if (voice == null)
                {
                    continue;
                }

                if (paused && voice.isPlaying)
                {
                    voice.Pause();
                    _pausedVoices[i] = true;
                }
                else if (!paused && _pausedVoices[i])
                {
                    voice.UnPause();
                    _pausedVoices[i] = false;
                }
            }
        }

        private static Sprite CreateSprite(string spriteName, int size, int kind)
        {
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float radius = nx * nx + ny * ny;
                    float alpha;
                    if (kind == 1)
                    {
                        bool toe = y >= size / 2 && y < size - 2 && x >= size / 4 && x < size * 3 / 4;
                        bool heel = y >= 2 && y < size / 2 - 1 && x >= size / 4 + 1 && x < size * 3 / 4 - 1;
                        alpha = toe || heel ? 1f : 0f;
                        if (y % 3 == 0)
                        {
                            alpha *= 0.55f;
                        }
                    }
                    else
                    {
                        float innerRadius = kind == 0 ? 0.35f : 0.15f;
                        alpha = 1f - Mathf.InverseLerp(innerRadius, 1f, radius);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = spriteName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.DontSave;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private void OnDestroy()
        {
            if (ScenePools.TryGetValue(_sceneHandle, out MonsterGroundEffects pool) && pool == this)
            {
                ScenePools.Remove(_sceneHandle);
            }

            DestroySprite(_shadowSprite);
            DestroySprite(_footprintSprite);
            DestroySprite(_dustSprite);
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
