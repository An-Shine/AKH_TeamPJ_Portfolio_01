using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using _02.Scripts.Monster.Presentation;

namespace _02.Scripts.Monster.BossMonster
{
    public sealed class BossBattleEffects : MonoBehaviour
    {
        private sealed class Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public Vector3 StartScale;
            public Color Color;
            public float Age;
            public float Lifetime;
            public float Spin;
            public bool Active;
        }

        private Particle[] _particles;
        private Sprite _glowSprite;
        private Sprite _sparkSprite;
        private Sprite _flameSprite;
        private Sprite _spikeSprite;
        private Material _solidMaterial;
        private Material _glowMaterial;
        private int _layer;
        private int _sortingLayer;
        private int _order;
        private int _nextParticle;
        private float _z;
        private float _retireAge;
        private bool _retiring;
        private static readonly Dictionary<int, BossBattleEffects> Owners = new Dictionary<int, BossBattleEffects>();
        private BossVfxUpgrade _upgrade;
        private BossHitVfx _bodyHits;
        private int _ownerId;
        public BossVfxUpgrade Upgrade => _upgrade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOwners()
        {
            Owners.Clear();
        }

        public static BossBattleEffects ForOwner(GameObject owner)
        {
            if (owner != null && Owners.TryGetValue(owner.GetInstanceID(), out BossBattleEffects effects) && effects != null)
            {
                return effects;
            }
            return null;
        }
        private float _nextCharge;
        private bool _ownsGlow, _ownsSpark, _ownsFlame;
        internal Material SolidMaterial => _solidMaterial;
        internal Material GlowMaterial => _glowMaterial;

        public void PlayBodyHit()
        {
            if (_bodyHits != null)
            {
                _bodyHits.PlayHit();
            }
        }

        public static BossBattleEffects Create(GameObject owner, BossEncounterProfile profile)
        {
            GameObject root = new GameObject(owner.name + "_BossEffects");
            SceneManager.MoveGameObjectToScene(root, owner.scene);
            BossBattleEffects effects = root.AddComponent<BossBattleEffects>();
            MonsterCombat.GetEffectSorting(owner.transform, out effects._layer, out effects._sortingLayer, out effects._order);
            effects._z = owner.transform.position.z;
            Shader shader = Resources.Load<Shader>(BossArenaVfxClip.ShaderPath);
            if (shader == null)
            {
                shader = Resources.Load<Shader>(BossVfxUpgrade.ResourceRoot + "BossCrispVfx");
            }
            if (shader == null)
            {
                shader = profile.EffectShader;
            }
            if (shader == null)
            {
                shader = Shader.Find("Monsters/BossGlow");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning("보스 효과 셰이더가 없습니다. BossOverhaul 패키지의 BossGlow.shader를 확인하세요.", owner);
            }

            effects._solidMaterial = new Material(shader)
            {
                name = "BossTransparent",
                hideFlags = HideFlags.DontSave
            };
            effects._glowMaterial = new Material(shader)
            {
                name = "BossAdditive",
                hideFlags = HideFlags.DontSave
            };
            effects._glowMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
            effects._ownerId = owner.GetInstanceID();
            Owners[effects._ownerId] = effects;
            GameObject hitsRoot = new GameObject("BossBodyHitEffects");
            hitsRoot.transform.SetParent(root.transform, false);
            effects._bodyHits = hitsRoot.AddComponent<BossHitVfx>();
            global::BossMonster boss = owner.GetComponent<global::BossMonster>();
            effects._bodyHits.Initialize(boss, effects._solidMaterial, effects._layer,
                effects._sortingLayer, effects._order);
            root.AddComponent<BossArenaVfxClip>().Initialize(boss, hitsRoot.transform);
            effects._upgrade = root.AddComponent<BossVfxUpgrade>();
            effects._upgrade.Initialize(effects._layer, effects._sortingLayer, effects._order, effects._z, effects._ownerId);
            effects._glowSprite = Resources.Load<Sprite>(BossVfxUpgrade.ResourceRoot + "Kenney/light_01");
            effects._sparkSprite = Resources.Load<Sprite>(BossVfxUpgrade.ResourceRoot + "Kenney/trace_01");
            effects._flameSprite = Resources.Load<Sprite>(BossVfxUpgrade.ResourceRoot + "Kenney/flame_01");
            if (effects._glowSprite == null)
            {
                effects._glowSprite = effects.CreateSprite(32, 32, 0);
                effects._ownsGlow = true;
            }

            if (effects._sparkSprite == null)
            {
                effects._sparkSprite = effects.CreateSprite(8, 8, 1);
                effects._ownsSpark = true;
            }

            if (effects._flameSprite == null)
            {
                effects._flameSprite = effects.CreateSprite(32, 64, 2);
                effects._ownsFlame = true;
            }

            effects._spikeSprite = effects.CreateSprite(16, 32, 3);
            effects._particles = new Particle[profile.MaximumParticles];
            return effects;
        }

        internal BossShapeVisual CreateShape(string name, bool additive, int orderOffset = 0)
        {
            return new BossShapeVisual(name, transform, additive ? _glowMaterial : _solidMaterial, _layer, _sortingLayer, _order + orderOffset, _z);
        }

        public void Burst(Vector2 point, float radius, Color color, int sparks = 18)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Explosion(point, radius, color, sparks);
                return;
            }

            Emit(point, Vector2.zero, radius * 2.6f, radius * 2.6f, Color.white, 0.12f, 0, 0f);
            Emit(point, Vector2.zero, radius * 3f, radius * 3f, color, 0.4f, 0, 0f);
            for (int i = 0; i < sparks; i++)
            {
                Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(radius * 2f, radius * 5f);
                Emit(point, velocity, radius * 0.08f, radius * 0.36f, Color.Lerp(color, Color.white, Random.value * 0.7f), Random.Range(0.25f, 0.55f), 1, Random.Range(-240f, 240f));
            }
        }

        public void Pillar(Vector2 point, float radius, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.FirePillar(point, radius, color);
                return;
            }

            Burst(point, radius, color, 12);
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Vector2.right * Random.Range(-radius * 0.5f, radius * 0.5f);
                Emit(point + offset + Vector2.up * radius, Vector2.up * radius * 0.5f, radius * Random.Range(0.6f, 1.1f), radius * Random.Range(2.8f, 4.5f), Color.Lerp(color, Color.white, i * 0.1f), Random.Range(0.35f, 0.7f), 2, 0f);
            }
        }

        public void Trail(Vector2 point, Vector2 direction, float size, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Trail(point, direction, size, color);
                return;
            }

            Emit(point, -direction * 0.35f, size * 1.8f, size * 1.8f, color, 0.18f, 0, 0f);
        }

        public void Spikes(Vector2 point, float radius, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.GroundBreak(point, radius, color);
                return;
            }
            Burst(point, radius * 0.8f, color, 10);
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius * 0.5f;
                float height = radius * Random.Range(2.2f, 3.8f);
                Emit(point + offset + Vector2.up * height * 0.3f, Vector2.up * 0.15f, radius * 0.4f, height, Color.Lerp(color, Color.white, i * 0.12f), Random.Range(0.28f, 0.5f), 3, 0f);
            }
        }

        public void Afterimage(SpriteRenderer source, Color color)
        {
            if (source == null || source.sprite == null || !source.enabled)
            {
                return;
            }

            color.a = 0.2f;
            Vector3 scale = source.transform.lossyScale;
            Emit(source.transform.position, Vector2.zero, Mathf.Abs(scale.x), Mathf.Abs(scale.y), color, 0.24f, 4, 0f, source.sprite);
        }

        public void Charge(Vector2 point, float radius, float progress, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady && Time.time >= _nextCharge)
            {
                _nextCharge = Time.time + 0.04f;
                _upgrade.Charge(point, radius, progress, color);
            }
        }

        public void Slash(Vector2 point, float radius, float angle, Color color, float intensity = 1f, float span = 120f)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Slash(point, radius, angle, color, intensity, span);
            }
        }

        public void Vortex(Vector2 point, float radius, float angle, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Vortex(point, radius, angle, color);
            }
        }

        public void ArenaDetonation(Rect arena, Vector2 safeCenter, float safeRadius, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.ArenaBlast(arena, safeCenter, safeRadius, color);
            }
        }

        public void Lightning(Vector2 point, float radius, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Lightning(point, radius, color);
            }
            else
            {
                Pillar(point, radius, color);
            }
        }

        public void Beam(Vector2 point, float angle, float length, float width, Color color, bool rift, Rect arena)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Beam(point, angle, length, width, color, rift, arena);
            }
        }

        public void NovaArc(Vector2 point, float radius, float start, float span, float width, Color color, bool impact)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.NovaArc(point, radius, start, span, width, color, impact);
            }
        }

        public void Collapse(Vector2 point, float radius, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Vortex(point, radius, 0f, color, true);
            }
        }

        public void BloodPool(Vector2 point, float radius, Color color, bool impact)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.BloodPool(point, radius, color, impact);
            }
        }

        public void PreImpact(BossEncounterPattern kind, Vector2 point, float radius, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.PreImpact(kind, point, radius, color);
            }
        }

        public void ProjectileTrail(Vector2 point, Vector2 direction, float size, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Trail(point, direction, size, color, true);
            }
            else
            {
                Trail(point, direction, size, color);
            }
        }

        public void Muzzle(Vector2 point, Vector2 direction, float size, Color color)
        {
            if (_upgrade != null && _upgrade.IsReady)
            {
                _upgrade.Muzzle(point, direction, size, color);
            }
        }

        internal SpriteRenderer CreateBoltRenderer()
        {
            GameObject root = new GameObject("BossNeonBolt", typeof(SpriteRenderer));
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(0f, 0f, _z);
            root.layer = _layer;
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>(BossVfxUpgrade.ResourceRoot + "Kenney/magic_03");
            if (renderer.sprite == null)
            {
                renderer.sprite = _glowSprite;
            }
            renderer.sharedMaterial = _solidMaterial;
            renderer.sortingLayerID = _sortingLayer;
            renderer.sortingOrder = _order + MonsterVfxReadability.ProjectileBodyOrder;
            return renderer;
        }

        public void Retire()
        {
            _retiring = true;
            _retireAge = 0f;
        }

        private void Emit(Vector2 point, Vector2 velocity, float width, float height, Color color, float lifetime, int spriteKind, float spin, Sprite spriteOverride = null)
        {
            Particle particle = _particles[_nextParticle];
            if (particle == null)
            {
                GameObject root = new GameObject("BossParticle");
                root.layer = _layer;
                root.transform.SetParent(transform, false);
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = _glowMaterial;
                renderer.sortingLayerID = _sortingLayer;
                renderer.sortingOrder = _order + 4;
                particle = new Particle
                {
                    Renderer = renderer
                };
                _particles[_nextParticle] = particle;
            }

            _nextParticle = (_nextParticle + 1) % _particles.Length;
            particle.Active = true;
            particle.Age = 0f;
            particle.Lifetime = lifetime;
            particle.Velocity = velocity;
            particle.Color = color;
            particle.Spin = spin;
            SpriteRenderer body = particle.Renderer;
            body.sprite = spriteOverride != null ? spriteOverride : spriteKind == 3 ? _spikeSprite : spriteKind == 2 ? _flameSprite : spriteKind == 1 ? _sparkSprite : _glowSprite;
            Vector3 bounds = body.sprite.bounds.size;
            particle.StartScale = spriteKind == 4 ? new Vector3(width, height, 1f) : new Vector3(width / Mathf.Max(0.001f, bounds.x), height / Mathf.Max(0.001f, bounds.y), 1f);
            body.enabled = true;
            body.color = color;
            body.transform.position = new Vector3(point.x, point.y, _z);
            body.transform.localScale = particle.StartScale;
            body.transform.rotation = Quaternion.Euler(0f, 0f, spriteKind == 1 ? BossBattleGeometry.Angle(velocity) - 90f : 0f);
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || _particles == null)
            {
                return;
            }

            bool busy = false;
            foreach (Particle particle in _particles)
            {
                if (particle == null || !particle.Active)
                {
                    continue;
                }

                particle.Age += Time.deltaTime;
                if (particle.Age >= particle.Lifetime)
                {
                    particle.Active = false;
                    particle.Renderer.enabled = false;
                    continue;
                }

                busy = true;
                float progress = particle.Age / particle.Lifetime;
                particle.Renderer.transform.position += (Vector3)(particle.Velocity * Time.deltaTime);
                particle.Velocity *= Mathf.Exp(-Time.deltaTime * 4f);
                particle.Renderer.transform.Rotate(0f, 0f, particle.Spin * Time.deltaTime);
                particle.Renderer.transform.localScale = particle.StartScale * Mathf.Lerp(1f, 1.5f, progress);
                Color color = particle.Color;
                color.a *= (1f - progress) * (1f - progress);
                particle.Renderer.color = color;
            }

            if (_retiring)
            {
                _retireAge += Time.deltaTime;
                if ((!busy && (_bodyHits == null || !_bodyHits.HasActiveBursts)) || _retireAge > 3f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private Sprite CreateSprite(int width, int height, int kind)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "BossProcedural" + kind;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.DontSave;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f) / width * 2f - 1f;
                    float ny = (y + 0.5f) / height * 2f - 1f;
                    float alpha;
                    if (kind == 0)
                    {
                        alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny)), 1.3f);
                    }
                    else if (kind == 1)
                    {
                        alpha = Mathf.Abs(nx) + Mathf.Abs(ny) * 0.45f <= 0.8f ? 1f : 0f;
                    }
                    else if (kind == 2)
                    {
                        float taper = 0.65f * (1f - (ny + 1f) * 0.4f);
                        taper += Mathf.Sin(y * 0.83f) * 0.1f;
                        alpha = Mathf.Clamp01((taper - Mathf.Abs(nx)) * 6f) * Mathf.Clamp01(1f - ny);
                    }
                    else
                    {
                        float taper = (1f - (ny + 1f) * 0.5f) * 0.8f;
                        alpha = Mathf.Abs(nx) <= taper ? Mathf.Clamp01(1.2f - Mathf.Abs(nx) * 0.8f) : 0f;
                    }

                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f, width, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private void OnDestroy()
        {
            if (Owners.TryGetValue(_ownerId, out BossBattleEffects ownerEffects) && ownerEffects == this)
            {
                Owners.Remove(_ownerId);
            }
            if (_ownsGlow)
            {
                DestroySprite(_glowSprite);
            }
            if (_ownsSpark)
            {
                DestroySprite(_sparkSprite);
            }
            if (_ownsFlame)
            {
                DestroySprite(_flameSprite);
            }
            DestroySprite(_spikeSprite);
            Destroy(_solidMaterial);
            Destroy(_glowMaterial);
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
