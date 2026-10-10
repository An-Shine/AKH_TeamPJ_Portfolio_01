using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02.Scripts.Monster.Presentation
{
    // One bounded pool per scene. These systems never own collision or damage.
    public sealed class MonsterNeonVfx : MonoBehaviour
    {
        private enum Kind
        {
            Spark,
            Smoke,
            Energy,
            Ring,
            Flame,
            Explosion,
            Slash,
            Debris,
            Lightning,
            Vortex,
            Plasma,
            Ripple,
            Blast,
            BlastDebris,
            FireColumn
        }

        private static readonly Dictionary<int, MonsterNeonVfx> Pools = new Dictionary<int, MonsterNeonVfx>();
        private readonly ParticleSystem[] _systems = new ParticleSystem[15];
        private readonly ParticleSystemRenderer[] _renderers = new ParticleSystemRenderer[15];
        private readonly Material[] _materials = new Material[15];
        private int _budgetFrame = -1;
        private int _frameBudget;
        private int _impactBudget;
        private int _sceneHandle;
        private bool _paused;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools()
        {
            Pools.Clear();
        }

        public static MonsterNeonVfx ForScene(Scene scene)
        {
            if (Pools.TryGetValue(scene.handle, out MonsterNeonVfx pool) && pool != null)
                return pool;
            GameObject root = new GameObject("MonsterNeonVfxPool");
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(root, scene);
            return root.AddComponent<MonsterNeonVfx>();
        }

        private void Awake()
        {
            _sceneHandle = gameObject.scene.handle;
            Pools[_sceneHandle] = this;
            string[] textures =
            {
                "trace_01",
                "smoke_04",
                "light_01",
                "circle_03",
                "flame_01",
                "explosion_pixelfied",
                "slash_01",
                "dirt_01",
                "spark_01",
                "twirl_02",
                "Effect36",
                "Effect51",
                "Explosion25",
                "ToonExplosion31",
                "Fire01"
            };
            int[] limits =
            {
                768,
                192,
                256,
                128,
                256,
                64,
                96,
                128,
                96,
                64,
                96,
                96,
                64,
                64,
                96
            };
            Shader shader = Resources.Load<Shader>("Monster/NeonVfx/NeonParticle");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            for (int i = 0; i < _systems.Length; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>("Monster/NeonVfx/Particles/" + textures[i]);
                GameObject child = new GameObject(((Kind)i).ToString(), typeof(ParticleSystem));
                child.transform.SetParent(transform, false);
                ParticleSystem particles = child.GetComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = limits[i];
                main.startSpeed = 0f;
                main.startLifetime = 0.6f;
                main.startSize3D = true;
                main.gravityModifier = i == 7 ? 0.35f : i == 0 ? 0.06f : 0f;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.enabled = false;
                var color = particles.colorOverLifetime;
                color.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.12f),
                        new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;
                var size = particles.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, i == 3 ? new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 2.3f)) : i == 1 ? new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.8f)) : i == 9 ? new AnimationCurve(new Keyframe(0f, 1.2f), new Keyframe(1f, 0.15f)) : new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.18f, 1f), new Keyframe(1f, 0.2f)));
                // The downloaded sheets already animate their size and dissipation.
                if (i >= 10)
                    size.size = new ParticleSystem.MinMaxCurve(1f);
                var rotation = particles.rotationOverLifetime;
                rotation.enabled = i == 1 || i == 2 || i == 7 || i == 9;
                rotation.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
                var noise = particles.noise;
                noise.enabled = i == 1 || i == 4;
                noise.strength = 0.13f;
                noise.frequency = 0.9f;
                noise.scrollSpeed = 0.5f;
                noise.quality = ParticleSystemNoiseQuality.Low;
                if (i == 5 || i >= 10)
                {
                    var sheet = particles.textureSheetAnimation;
                    sheet.enabled = true;
                    sheet.numTilesX = i == 14 ? 8 : 4;
                    sheet.numTilesY = 4;
                    sheet.animation = ParticleSystemAnimationType.WholeSheet;
                    sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
                    sheet.cycleCount = 1;
                }

                Material material = new Material(shader)
                {
                    name = "Neon_" + textures[i],
                    hideFlags = HideFlags.DontSave
                };
                material.mainTexture = texture;
                material.SetFloat("_Additive", i == 1 || i == 7 || i == 12 || i == 13 ? 0f : i >= 10 ? 0.65f : 1f);
                material.SetFloat("_Desaturate", i == 5 || i == 10 || i == 11 ? 1f : i >= 12 ? 0.35f : 0f);
                ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = material;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.maxParticleSize = 2f;
                _materials[i] = material;
                _systems[i] = particles;
                _renderers[i] = renderer;
                particles.Play();
            }
        }

        public void Burst(Vector3 point, float radius, Color color, int sortingLayer, int order, int sparks = 20, bool explosion = false)
        {
            if (Time.timeScale <= 0f)
                return;
            radius = Mathf.Clamp(radius, 0.1f, 5f);
            Color core = Color.Lerp(color, Color.white, 0.5f);
            core.a *= 0.7f;
            Emit(Kind.Energy, point, Vector2.zero, radius * 0.85f, 0.065f, core, sortingLayer, order + 2);
            Emit(Kind.Ring, point, Vector2.zero, radius * 1.3f, 0.3f, color, sortingLayer, order + 1);
            if (explosion)
            {
                Color fire = Color.Lerp(color, Color.white, 0.3f);
                Emit(Kind.Blast, point, Vector2.zero, radius * 2.4f, 0.42f, fire, sortingLayer, order + 3);
                Emit(Kind.BlastDebris, point, Vector2.zero, radius * 2.6f, 0.36f, fire, sortingLayer, order + 4, Random.Range(-18f, 18f));
            }
            else
            {
                Emit(Kind.Ripple, point, Vector2.zero, radius * 2.3f, 0.36f, color, sortingLayer, order + 2, Random.Range(0f, 90f));
            }
            int count = Mathf.Clamp(sparks, 3, 48);
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Emit(Kind.Spark, point, direction * Random.Range(2.3f, 5.5f) * radius, Random.Range(0.07f, 0.12f) * Mathf.Sqrt(radius), Random.Range(0.23f, 0.5f), Color.Lerp(color, Color.white, i % 4 == 0 ? 0.75f : 0.1f), sortingLayer, order + 4, angle * Mathf.Rad2Deg - 90f, 3.3f);
            }

            for (int i = 0; i < (explosion ? 4 : 1); i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius * 0.3f;
                Color smoke = Color.Lerp(color, new Color(0.18f, 0.2f, 0.27f), 0.7f);
                smoke.a = 0.18f;
                Emit(Kind.Smoke, point + (Vector3)offset, offset * 0.8f + Vector2.up * radius * 0.4f, radius * Random.Range(0.45f, 0.85f), Random.Range(0.4f, 0.7f), smoke, sortingLayer, order - 1);
                if (explosion)
                    Emit(Kind.Debris, point, offset * 5f + Vector2.up * radius, radius * 0.12f, 0.6f, new Color(0.3f, 0.23f, 0.24f, 0.8f), sortingLayer, order);
            }
        }

        public void Pillar(Vector3 point, float radius, Color color, int sortingLayer, int order)
        {
            Burst(point, radius, color, sortingLayer, order, 10, true);
            Emit(Kind.Lightning, point + Vector3.up * radius, Vector2.zero, radius * 1.4f, 0.22f, Color.Lerp(color, Color.white, 0.4f), sortingLayer, order + 4, 0f, 2.4f);
            Emit(Kind.FireColumn, point + Vector3.up * radius * 1.25f, Vector2.zero, radius * 1.5f, 0.85f, Color.Lerp(color, Color.white, 0.65f), sortingLayer, order + 2, 0f, 2.2f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-radius * 0.35f, radius * 0.35f), radius * i * 0.18f, 0f);
                Emit(Kind.Flame, point + offset, Vector2.up * radius * Random.Range(1f, 2f), radius * Random.Range(0.5f, 0.85f), Random.Range(0.3f, 0.65f), color, sortingLayer, order + 2, 0f, 1.8f);
            }
        }

        public void Trail(Vector3 point, Vector2 direction, float size, Color color, int sortingLayer, int order)
        {
            color = MonsterVfxReadability.Trail(color);
            point -= (Vector3)(direction * size * 0.35f);
            Emit(Kind.Energy, point, -direction * 0.3f, Mathf.Clamp(size * 0.7f, 0.07f, 0.45f), 0.1f, color, sortingLayer, order);
            Emit(Kind.Spark, point, -direction * 0.6f, Mathf.Clamp(size * 0.22f, 0.025f, 0.08f), 0.1f, color, sortingLayer, order + 1, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f, 3f);
        }

        public void Charge(Vector3 point, float radius, float progress, Color color, int sortingLayer, int order)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * radius * Random.Range(1.1f, 1.9f);
                Emit(Kind.Spark, point + (Vector3)offset, -offset * (2f + progress * 3f), 0.045f, 0.2f,
                    MonsterVfxReadability.WithOpacity(color, 0.35f), sortingLayer, order + 2, Mathf.Atan2(-offset.y, -offset.x) * Mathf.Rad2Deg - 90f, 3f);
            }

            if (Random.value < 0.25f)
                Emit(Kind.Vortex, point, Vector2.zero, radius * 2.8f, 0.45f, color * 0.55f, sortingLayer, order);
        }

        public void PlasmaShot(Vector3 point, Vector2 direction, float size, Color color, int sortingLayer, int order)
        {
            size = Mathf.Clamp(size, 0.2f, 1.5f);
            Emit(Kind.Plasma, point, direction * 0.5f, size * 2f, 0.24f, color, sortingLayer, order + 3);
            Burst(point, size * 0.32f, color, sortingLayer, order, 12);
        }

        public void Slash(Vector3 point, float radius, float angle, Color color, int sortingLayer, int order, float intensity = 1f)
        {
            color.a *= intensity;
            Emit(Kind.Slash, point, Vector2.zero, radius * 2.05f, 0.23f, color, sortingLayer, order + 2, angle + 90f);
            if (intensity < 0.5f)
                return;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Burst(point + (Vector3)(direction * radius * 0.75f), radius * 0.2f, color, sortingLayer, order, 16);
            Emit(Kind.Slash, point, Vector2.zero, radius * 1.85f, 0.1f, Color.white * 0.8f, sortingLayer, order + 3, angle + 95f);
        }

        public void Vortex(Vector3 point, float radius, float angle, Color color, int sortingLayer, int order)
        {
            color.a *= 0.55f;
            Emit(Kind.Vortex, point, Vector2.zero, radius * 2f, 0.45f, color, sortingLayer, order, angle);
        }

        private void Emit(Kind kind, Vector3 point, Vector2 velocity, float size, float lifetime, Color color, int sortingLayer, int order, float rotation = 0f, float aspect = 1f)
        {
            if (Time.timeScale <= 0f)
                return;
            if (_budgetFrame != Time.frameCount)
            {
                _budgetFrame = Time.frameCount;
                _frameBudget = MonsterVfxReadability.DecorativeBudget;
                _impactBudget = MonsterVfxReadability.ImpactBudget;
            }

            bool impact = kind == Kind.Blast || kind == Kind.BlastDebris || kind == Kind.Plasma
                || kind == Kind.Slash || kind == Kind.Ripple || kind == Kind.Lightning;
            if ((impact ? _impactBudget-- : _frameBudget--) <= 0)
                return;
            ParticleSystem system = _systems[(int)kind];
            if (system == null || system.particleCount >= system.main.maxParticles)
                return;
            ParticleSystemRenderer renderer = _renderers[(int)kind];
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = kind == Kind.Smoke || kind == Kind.Debris
                ? order - 5 : order;
            system.Emit(new ParticleSystem.EmitParams { position = point, velocity = new Vector3(velocity.x, velocity.y, 0f), startSize3D = new Vector3(size, size * aspect, 1f), startLifetime = lifetime, startColor = color, rotation = rotation }, 1);
        }

        private void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (paused == _paused)
                return;
            _paused = paused;
            foreach (ParticleSystem system in _systems)
            {
                if (paused)
                    system.Pause();
                else
                    system.Play();
            }
        }

        private void OnDestroy()
        {
            if (Pools.TryGetValue(_sceneHandle, out MonsterNeonVfx pool) && pool == this)
                Pools.Remove(_sceneHandle);
            foreach (Material material in _materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }
}
