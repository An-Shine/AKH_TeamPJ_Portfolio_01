using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace _02.Scripts.Monster.BossMonster
{
    [Serializable]
    public sealed class BossVfxTextureDefinition
    {
        public string key = string.Empty;
        public string path = string.Empty;
        public int columns = 1;
        public int rows = 1;
        public int frames = 1;
        public int frameWidth = 1;
        public int frameHeight = 1;
        public int fps = 30;
        public int limit = 64;
        public int additive = 1;
        public float desaturate = 0f;
        public float grow = 0f;
        public float gravity = 0f;
        public float spin = 0f;
        public int order = 5;
    }

    [Serializable]
    public sealed class BossVfxCatalog
    {
        public BossVfxTextureDefinition[] textures = new BossVfxTextureDefinition[0];
    }

    // An isolated boss pool. Normal monsters, Player, Items and UI do not use it.
    public sealed class BossVfxUpgrade : MonoBehaviour
    {
        public const string ResourceRoot = "Monster/BossVfxUpgrade/";

        private sealed class Pool
        {
            public ParticleSystem Particles;
            public Material Material;
            public BossVfxTextureDefinition Definition;
        }

        private readonly Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();
        private System.Random _random;
        private BossVfxMesh _mesh;
        private Shader _shader;
        private int _layer;
        private int _sortingLayer;
        private int _order;
        private float _z;
        private int _budgetFrame = -1;
        private int _decorativeBudget;
        private int _primaryBudget;
        private float _nextCharge;
        private float _nextMuzzle;
        private bool _ready;

        public bool IsReady => _ready;
        public int TextureCount => _pools.Count;

        public void Initialize(int layer, int sortingLayer, int order, float z, int seed)
        {
            _layer = layer;
            _sortingLayer = sortingLayer;
            _order = order;
            _z = z;
            _random = new System.Random(seed & int.MaxValue);
            _shader = Resources.Load<Shader>(ResourceRoot + "BossCrispVfx");
            TextAsset data = Resources.Load<TextAsset>(ResourceRoot + "BossVfxCatalog");
            if (_shader == null || data == null)
            {
                Debug.LogError("보스 전용 VFX 파일이 빠졌습니다. BossMonster_VFX_Upgrade 패키지를 전체 Import하세요.", this);
                return;
            }
            BossVfxCatalog catalog = JsonUtility.FromJson<BossVfxCatalog>(data.text);
            if (catalog == null || catalog.textures == null)
                return;
            foreach (BossVfxTextureDefinition definition in catalog.textures)
            {
                Texture2D texture = Resources.Load<Texture2D>(definition.path);
                if (texture == null)
                {
                    Debug.LogError("보스 VFX 텍스처 누락: " + definition.path, this);
                    continue;
                }
                _pools.Add(definition.key, CreatePool(definition, texture));
            }
            _mesh = new BossVfxMesh(transform, _shader, layer, sortingLayer, order, z);
            _ready = _pools.Count == catalog.textures.Length && _pools.Count > 0;
        }

        private Pool CreatePool(BossVfxTextureDefinition definition, Texture2D texture)
        {
            GameObject root = new GameObject("BossVfx_" + definition.key, typeof(ParticleSystem));
            root.layer = _layer;
            root.transform.SetParent(transform, false);
            ParticleSystem particles = root.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Clamp(definition.limit, 8, 768);
            main.startSize3D = true;
            main.startSpeed = 0f;
            main.startLifetime = 0.5f;
            main.gravityModifier = definition.gravity;
            main.useUnscaledTime = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(definition.frames > 1 ? 0.7f : 0f, 1f) });
            color.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 1f, 1f, Mathf.Max(0.1f, 1f + definition.grow)));
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = Mathf.Abs(definition.spin) > 0.001f;
            rotation.z = new ParticleSystem.MinMaxCurve(-definition.spin, definition.spin);
            if (definition.frames > 1)
            {
                var sheet = particles.textureSheetAnimation;
                sheet.enabled = true;
                sheet.numTilesX = definition.columns;
                sheet.numTilesY = definition.rows;
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                // Exclude padding and blank tail cells without skipping the last valid frame.
                float last = (definition.frames - 0.001f) / (definition.columns * definition.rows);
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f,
                    AnimationCurve.Linear(0f, 0f, 1f, Mathf.Clamp01(last)));
                sheet.cycleCount = 1;
            }
            Material material = new Material(_shader)
            {
                name = "BossCrisp_" + definition.key,
                hideFlags = HideFlags.DontSave,
                mainTexture = texture
            };
            material.SetFloat("_DstBlend", (float)(definition.additive == 1 ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_Desaturate", definition.desaturate);
            material.SetFloat("_Gain", definition.additive == 1 ? 1.22f : 1.08f);
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 4f;
            renderer.sortingLayerID = _sortingLayer;
            renderer.sortingOrder = _order + definition.order;
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)_random.Next(1, int.MaxValue);
            particles.Play();
            return new Pool { Particles = particles, Material = material, Definition = definition };
        }

        private void Emit(string key, Vector2 point, Vector2 velocity, float width, float height,
            float lifetime, Color color, float angle = 0f, bool primary = false)
        {
            if (!_ready || Time.timeScale <= 0f || !_pools.TryGetValue(key, out Pool pool))
                return;
            if (_budgetFrame != Time.frameCount)
            {
                _budgetFrame = Time.frameCount;
                _decorativeBudget = 320;
                _primaryBudget = 128;
            }
            if (pool.Particles.particleCount >= pool.Definition.limit
                || (primary ? _primaryBudget-- : _decorativeBudget--) <= 0)
            {
                return;
            }

            pool.Particles.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(point.x, point.y, _z),
                velocity = new Vector3(velocity.x, velocity.y, 0f),
                startSize3D = new Vector3(Mathf.Max(0.01f, width), Mathf.Max(0.01f, height), 1f),
                startLifetime = Mathf.Max(0.035f, lifetime),
                startColor = color,
                rotation = angle
            }, 1);
        }

        private void Square(string key, Vector2 point, float size, float lifetime, Color color,
            float angle = 0f, bool primary = false)
        {
            Emit(key, point, Vector2.zero, size, size, lifetime, color, angle, primary);
        }

        private float Range(float from, float to)
        {
            return Mathf.Lerp(from, to, (float)_random.NextDouble());
        }

        private Vector2 Direction(float angle)
        {
            return BossBattleGeometry.Direction(angle);
        }

        private void Sparks(Vector2 point, float size, Color color, int count, float angle = 0f, float span = 360f)
        {
            for (int i = 0; i < count; i++)
            {
                float directionAngle = angle + Range(0f, span);
                Vector2 direction = Direction(directionAngle);
                float width = Range(0.024f, 0.055f) * Mathf.Sqrt(size);
                Color tint = Color.Lerp(color, Color.white, i % 4 == 0 ? 0.85f : 0.18f);
                Emit("spark", point, direction * size * Range(2.8f, 5.7f), width,
                    width * Range(4f, 7f), Range(0.2f, 0.48f), tint, directionAngle - 90f);
                if (i % 5 == 0)
                {
                    Emit("glint", point, direction * size * Range(1.8f, 3.3f),
                        width * 2.3f, width * 2.3f, 0.28f, Color.white);
                }
            }
        }

        public void Explosion(Vector2 point, float radius, Color color, int sparks = 28)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.12f, 4.2f);
            // The white core is local and short; the larger outline retains the pattern's hue.
            Square("impact", point, radius * 2.3f, 0.15f, Color.white, Range(0f, 360f), true);
            string variant = sparks > 44 ? "explosion_heavy" : _random.Next(2) == 0 ? "explosion" : "explosion_round";
            Square(variant, point, radius * 2.65f, 0.68f, Color.Lerp(color, Color.white, 0.75f), 0f, true);
            Square("sunburst", point, radius * 1.65f, 0.4f, Color.Lerp(color, Color.white, 0.55f), 0f, true);
            Square("glow", point, radius * 1.4f, 0.12f, Alpha(color, 0.35f));
            Square("shock_ring", point, radius * 1.25f, 0.28f, Alpha(color, 0.28f));
            _mesh.Ring(point, radius * 0.22f, radius * 1.45f, 0.07f, color, 0.32f);
            _mesh.Ring(point, radius * 0.35f, radius * 1.65f, 0.028f, Color.Lerp(color, Color.white, 0.4f), 0.42f, 0.035f);
            Sparks(point, radius, color, Mathf.Clamp(sparks, 8, 64));
            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = Direction(Range(0f, 360f));
                Vector2 offset = direction * radius * Range(0.1f, 0.35f);
                Emit("smoke", point + offset, direction * radius * 0.35f + Vector2.up * radius * 0.4f,
                    radius * 0.65f, radius * 0.65f, Range(0.6f, 1f), new Color(0.18f, 0.21f, 0.3f, 0.32f));
                Emit("debris", point, direction * radius * Range(1.5f, 3.2f) + Vector2.up * radius,
                    radius * 0.07f, radius * 0.1f, 0.65f, new Color(0.32f, 0.26f, 0.32f, 0.95f), Range(0f, 360f));
            }
            if (radius >= 2.5f)
            {
                Square("nova", point, radius * 2.2f, 0.65f, Alpha(color, 0.65f), 0f, true);
                Square("disintegrate", point, radius * 1.3f, 0.42f, Color.Lerp(color, Color.white, 0.2f), 0f, true);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 end = point + Direction(i * 60f + Range(-8f, 8f)) * radius;
                    _mesh.Lightning(point, end, 0.035f, color, 0.26f, 0.025f * i);
                }
            }
        }

        public void FirePillar(Vector2 point, float radius, Color color)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.2f, 2.8f);
            Explosion(point, radius * 0.7f, color, 24);
            Square("fire_spin", point, radius * 1.9f, 0.35f, Alpha(color, 0.55f));
            Emit("fire_column", point + Vector2.up * radius * 0.85f, Vector2.zero,
                radius * 1.9f, radius * 3.2f, 0.58f, Color.Lerp(color, Color.white, 0.65f), 0f, true);
            Emit("fire_plume", point + Vector2.up * radius * 0.65f, Vector2.up * radius * 0.3f,
                radius * 1.55f, radius * 2.5f, 0.65f, Color.Lerp(color, Color.white, 0.6f));
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = new Vector2(Range(-0.45f, 0.45f), Range(0f, 0.35f)) * radius;
                Emit("pixel_flame", point + offset, Vector2.up * Range(1.2f, 2.4f) * radius,
                    radius * 0.38f, radius * 0.65f, 0.48f, Color.Lerp(color, Color.white, 0.4f));
                Emit("embers", point + offset, new Vector2(Range(-1f, 1f), Range(1.2f, 2.2f)) * radius,
                    radius * 0.27f, radius * 0.27f, 0.55f, color);
            }
        }

        public void Lightning(Vector2 point, float radius, Color color)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.25f, 2.3f);
            float height = radius * 3.5f + 1.8f;
            _mesh.Lightning(point + Vector2.up * height, point, 0.04f + radius * 0.012f, color, 0.2f);
            _mesh.Lightning(point + new Vector2(radius * 1.2f, height * 0.75f), point,
                0.027f, color, 0.16f, 0.025f);
            Emit("lightning_column", point + Vector2.up * height * 0.45f, Vector2.zero,
                radius * 1.4f, height, 0.22f, Color.Lerp(color, Color.white, 0.7f), 0f, true);
            Square("prism_hit", point, radius * 3.2f, 0.25f, Color.white, 0f, true);
            Square("flare", point, radius * 1.8f, 0.075f, Color.white, 0f, true);
            Square("electric_ball", point, radius * 2.1f, 0.35f, color, 0f, true);
            Square("electric_aura", point, radius * 2f, 0.46f, Alpha(color, 0.7f));
            _mesh.Ring(point, radius * 0.25f, radius * 1.55f, 0.06f, color, 0.32f);
            Sparks(point, radius, color, 36);
            for (int i = 0; i < 4; i++)
            {
                Vector2 end = point + Direction(i * 90f + Range(-15f, 15f)) * radius * 1.1f;
                _mesh.Lightning(point, end, 0.028f, color, 0.22f, 0.035f);
                Square("pixel_lightning", end, radius * 0.6f, 0.23f, color);
            }
            Square("dust", point, radius * 1.6f, 0.48f, new Color(0.16f, 0.22f, 0.3f, 0.45f));
        }

        public void GroundBreak(Vector2 point, float radius, Color color)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.2f, 3.2f);
            Square("earth_burst", point, radius * 2.4f, 0.36f, Color.Lerp(color, Color.white, 0.65f), 0f, true);
            Square("earth_ring", point, radius * 2.8f, 0.46f, new Color(0.58f, 0.42f, 0.36f, 0.8f), 0f, true);
            Square("impact", point, radius * 1.7f, 0.14f, Color.white, 0f, true);
            Square("earth_shards", point + Vector2.up * radius * 0.25f,
                radius * 2.2f, 0.45f, Color.Lerp(color, Color.white, 0.3f), 0f, true);
            _mesh.Cracks(point, radius * 1.35f, color, 9);
            _mesh.Ring(point, radius * 0.25f, radius * 1.4f, 0.075f, color, 0.3f);
            Sparks(point, radius * 0.7f, color, 24);
            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = Direction(Range(0f, 360f)) * radius * Range(1.1f, 2.3f);
                Emit("debris", point, velocity + Vector2.up * radius * 1.7f,
                    radius * 0.1f, radius * 0.15f, Range(0.5f, 0.8f), new Color(0.43f, 0.33f, 0.33f, 1f), Range(0f, 360f));
                Emit("dust", point, velocity * 0.3f, radius * 0.6f, radius * 0.4f,
                    0.65f, new Color(0.28f, 0.23f, 0.3f, 0.4f));
            }
        }

        public void Slash(Vector2 point, float radius, float angle, Color color, float intensity, float span)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.3f, 8f);
            color.a *= intensity;
            _mesh.Arc(point, radius * 0.88f, angle - span * 0.5f, span,
                intensity >= 0.5f ? 0.095f : 0.035f, color, intensity >= 0.5f ? 0.2f : 0.12f);
            if (intensity < 0.5f)
                return;
            Square("slash", point, radius * 2f, 0.18f, color, angle + 90f, true);
            Square("fire_cleave", point + Direction(angle) * radius * 0.35f,
                radius * 1.6f, 0.24f, color, angle - 90f, true);
            _mesh.Arc(point, radius * 0.82f, angle - span * 0.48f, span * 0.96f,
                0.033f, Color.white, 0.11f);
            Vector2 end = point + Direction(angle) * radius * 0.85f;
            Square("magic_hit", end, radius * 0.7f, 0.2f, Color.white, 0f, true);
            Sparks(end, radius * 0.25f, color, 18, angle - 60f, 120f);
        }

        public void Beam(Vector2 center, float angle, float length, float width, Color color, bool rift, Rect arena)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            Vector2 direction = Direction(angle);
            float first = -length * 0.5f;
            float last = length * 0.5f;
            if (!ClipBeamAxis(center.x, direction.x, arena.xMin, arena.xMax, ref first, ref last)
                || !ClipBeamAxis(center.y, direction.y, arena.yMin, arena.yMax, ref first, ref last))
            {
                return;
            }

            Vector2 from = center + direction * first;
            Vector2 to = center + direction * last;
            _mesh.Lightning(from, to, Mathf.Clamp(width * 0.08f, 0.026f, 0.075f), color, 0.22f);
            int samples = Mathf.Clamp(Mathf.CeilToInt((last - first) / 2.4f), 3, 12);
            for (int i = 0; i < samples; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, (i + 0.5f) / samples);
                if (!arena.Contains(point))
                    continue;
                if (rift)
                {
                    Square("earth_shards", point, width * 2.6f, 0.38f, color, 0f, true);
                    _mesh.Cracks(point, width * 1.4f, color, 3);
                }
                else
                {
                    Square("disintegrate", point, width * 1.4f, 0.22f, color, angle, true);
                    Emit("electric_surface", point, Vector2.zero, width * 3f, width * 0.6f,
                        0.16f, Alpha(color, 0.7f), angle);
                }
                Sparks(point, width * 0.45f, color, 6);
            }
        }

        private static bool ClipBeamAxis(float center, float direction, float minimum, float maximum,
            ref float first, ref float last)
        {
            if (Mathf.Abs(direction) < 0.00001f)
                return center >= minimum && center <= maximum;
            float a = (minimum - center) / direction;
            float b = (maximum - center) / direction;
            first = Mathf.Max(first, Mathf.Min(a, b));
            last = Mathf.Min(last, Mathf.Max(a, b));
            return last > first;
        }

        public void NovaArc(Vector2 point, float radius, float start, float span, float width, Color color, bool impact)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            _mesh.Arc(point, radius, start, span, Mathf.Clamp(width * 0.12f, 0.035f, 0.09f), color, impact ? 0.24f : 0.13f);
            int count = impact ? 16 : 3;
            for (int i = 0; i < count; i++)
            {
                // Keep both ends inside the dangerous arc; the escape gap stays empty.
                float angle = start + span * (i + 0.5f) / count;
                Vector2 direction = Direction(angle);
                Vector2 sample = point + direction * radius;
                Emit("spark", sample, direction * (impact ? 1.6f : 0.25f), 0.035f,
                    0.19f, 0.2f, Color.Lerp(color, Color.white, 0.55f), angle - 90f);
                if (impact && i % 3 == 0)
                    Square("magic_hit", sample, Mathf.Min(0.7f, width * 1.6f), 0.22f, color, 0f, true);
            }
        }

        public void Vortex(Vector2 point, float radius, float angle, Color color, bool impact = false)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            Square("vortex", point, radius * 1.95f, 0.42f, Alpha(color, 0.55f), angle);
            Square("twirl", point, radius * 1.4f, 0.28f, Alpha(color, 0.65f), -angle);
            _mesh.Arc(point, radius * 0.65f, angle, 210f, 0.035f, color, 0.25f);
            if (impact)
            {
                Square("arcane_collapse", point, radius * 2.1f, 0.55f, color, 0f, true);
                Square("nova", point, radius * 1.5f, 0.38f, color, 0f, true);
                _mesh.Ring(point, radius, radius * 0.12f, 0.075f, color, 0.4f);
                Square("photon_ring", point, radius * 1.8f, 0.4f, color);
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Direction(angle + i * 72f) * radius * 0.8f;
                Emit("spark", point + offset, -offset * 3f,
                    0.035f, 0.18f, 0.25f, color, BossBattleGeometry.Angle(-offset) - 90f);
            }
        }

        public void BloodPool(Vector2 point, float radius, Color color, bool impact)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            Square("blood_pool", point, radius * 2f, 0.62f, Alpha(color, 0.55f), 0f, impact);
            if (impact)
            {
                Square("magic_hit", point, radius * 1.4f, 0.2f, Color.Lerp(color, Color.white, 0.5f), 0f, true);
                _mesh.Ring(point, radius * 0.2f, radius, 0.04f, color, 0.38f);
                Sparks(point, radius * 0.45f, color, 12);
            }
        }

        public void Trail(Vector2 point, Vector2 direction, float size, Color color, bool projectile = false)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            size = Mathf.Clamp(size, 0.07f, 0.65f);
            float angle = BossBattleGeometry.Angle(direction);
            Emit("spark", point, -direction * 0.65f, size * 0.13f, size * 0.65f,
                0.16f, Color.Lerp(color, Color.white, 0.35f), angle - 90f);
            if (projectile)
            {
                Emit("plasma_tail", point - direction * size * 0.3f, -direction * 0.4f,
                    size * 1.8f, size * 0.9f, 0.17f, Alpha(color, 0.7f), angle);
                Square("arcane_orb", point, size * 0.95f, 0.15f, Alpha(color, 0.35f));
            }
            else
            {
                Square("sparkles", point, size * 1.7f, 0.23f, Alpha(color, 0.65f));
            }
        }

        public void Muzzle(Vector2 point, Vector2 direction, float size, Color color)
        {
            if (!_ready || Time.timeScale <= 0f || Time.time < _nextMuzzle)
                return;
            _nextMuzzle = Time.time + 0.055f;
            size = Mathf.Clamp(size, 0.18f, 0.7f);
            float angle = BossBattleGeometry.Angle(direction);
            Square("muzzle", point, size * 2f, 0.085f, Color.white, angle - 90f, true);
            Emit("plasma", point, direction * 0.7f, size * 2.2f, size * 1.2f,
                0.18f, color, angle, true);
            Square("plasma_burst", point, size * 1.7f, 0.2f, color, 0f, true);
            Square("bolt_body", point, size * 1.3f, 0.16f, Alpha(color, 0.65f));
            Sparks(point, size * 0.6f, color, 7, angle - 40f, 80f);
        }

        public void Charge(Vector2 point, float radius, float progress, Color color)
        {
            if (!_ready || Time.timeScale <= 0f || Time.time < _nextCharge)
                return;
            _nextCharge = Time.time + 0.055f;
            progress = Mathf.Clamp01(progress);
            for (int i = 0; i < 5; i++)
            {
                float angle = Time.time * 150f + i * 72f;
                Vector2 offset = Direction(angle) * radius * Mathf.Lerp(2.2f, 1.35f, progress);
                Emit("spark", point + offset, -offset * (2f + progress * 4f),
                    0.035f, 0.21f, 0.2f, Color.Lerp(color, Color.white, progress * 0.65f), angle + 90f);
            }
            _mesh.Arc(point, radius * 1.15f, Time.time * 160f, 130f, 0.028f, Alpha(color, 0.75f), 0.095f);
            _mesh.Arc(point, radius * 1.3f, -Time.time * 140f, 100f, 0.022f, Alpha(color, 0.6f), 0.095f);
            if (_random.Next(4) == 0)
            {
                Square("charge_rune", point, radius * 2f, 0.32f, Alpha(color, 0.5f));
                Square("orbit_rune", point, radius * 1.7f, 0.32f, Alpha(color, 0.4f));
                Square("sigil", point, radius * 2.1f, 0.32f, Alpha(color, 0.28f));
            }
            if (progress > 0.65f)
            {
                Vector2 offset = Direction(Range(0f, 360f)) * radius * 1.5f;
                _mesh.Lightning(point + offset, point, 0.02f, Alpha(color, 0.65f), 0.085f);
            }
        }

        public void PreImpact(BossEncounterPattern kind, Vector2 point, float radius, Color color)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.2f, 3f);
            _mesh.Ring(point, radius * 0.85f, radius * 0.25f, 0.025f,
                Alpha(color, 0.55f), 0.24f);
            if (kind == BossEncounterPattern.StormPillars)
            {
                _mesh.Lightning(point + Vector2.up * (radius * 2f + 2f), point,
                    0.012f, Alpha(color, 0.23f), 0.13f);
            }
            else if (kind == BossEncounterPattern.HellfireRain || kind == BossEncounterPattern.MeteorLattice)
            {
                Vector2 offset = new Vector2(-1.2f, 2.8f) * Mathf.Sqrt(radius);
                float duration = 0.24f;
                Emit("fireball", point + offset, -offset / duration,
                    radius * 0.65f, radius * 0.65f, duration, Color.Lerp(color, Color.white, 0.35f),
                    BossBattleGeometry.Angle(-offset), true);
                Emit("flame", point + offset, -offset / duration,
                    radius * 0.35f, radius * 1.2f, duration, Alpha(color, 0.7f),
                    BossBattleGeometry.Angle(-offset) - 90f);
            }
        }

        public void ArenaBlast(Rect arena, Vector2 safeCenter, float safeRadius, Color color)
        {
            if (!_ready || Time.timeScale <= 0f)
                return;
            // Use the full visual footprint when excluding the refuge, not just its center.
            float spacing = 2.5f;
            int columns = Mathf.Clamp(Mathf.CeilToInt(arena.width / spacing), 3, 10);
            int rows = Mathf.Clamp(Mathf.CeilToInt(arena.height / spacing), 3, 8);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Vector2 point = new Vector2(Mathf.Lerp(arena.xMin, arena.xMax, (x + 0.5f) / columns),
                        Mathf.Lerp(arena.yMin, arena.yMax, (y + 0.5f) / rows));
                    float clearance = (point - safeCenter).magnitude - safeRadius;
                    if (clearance < 1.25f)
                        continue;
                    float size = Mathf.Min(0.7f, clearance / 2.5f);
                    Square("explosion_heavy", point, size * 2.2f, 0.55f,
                        Color.Lerp(color, Color.white, 0.6f), 0f, true);
                    Square("impact", point, size * 1.5f, 0.12f, Color.white, 0f, true);
                    _mesh.Ring(point, size * 0.15f, size * 1.4f, 0.035f, color, 0.28f,
                        ((x + y) % 4) * 0.025f);
                    Emit("black_smoke", point, Vector2.up * 0.3f, size, size, 0.75f,
                        new Color(0.28f, 0.22f, 0.3f, 0.4f));
                }
            }
        }

        private static Color Alpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        private void LateUpdate()
        {
            if (_ready && _mesh != null)
                _mesh.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                _mesh.Dispose();
            foreach (Pool pool in _pools.Values)
            {
                if (pool.Material != null)
                    Destroy(pool.Material);
            }
        }
    }
}
