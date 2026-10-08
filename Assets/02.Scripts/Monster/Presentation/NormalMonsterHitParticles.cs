using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    // 네이티브 파티클 풀: 원본 몬스터가 사망해도 파편과 오일은 끝까지 재생합니다.
    internal sealed class NormalMonsterHitParticles : MonoBehaviour
    {
        private const int MaximumEmissionsPerFrame = 256;
        private readonly ParticleSystem[] _systems = new ParticleSystem[3];
        private readonly ParticleSystemRenderer[] _renderers = new ParticleSystemRenderer[3];
        private readonly Material[] _materials = new Material[3];
        private int _budgetFrame = -1;
        private int _remainingBudget;
        private bool _paused;

        internal void Configure(Material additive, Material opaque, Sprite spark, Sprite shard, Sprite oil)
        {
            Sprite[] sprites = { spark, shard, oil };
            int[] limits = { 384, 192, 256 };
            for (int i = 0; i < _systems.Length; i++)
            {
                GameObject child = new GameObject(i == 0 ? "MetalSparks" : i == 1 ? "MetalShards" : "MachineOil", typeof(ParticleSystem));
                child.transform.SetParent(transform, false);
                ParticleSystem particles = child.GetComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = limits[i];
                main.startSpeed = 0f;
                main.startSize3D = true;
                main.gravityModifier = i == 0 ? 0.05f : i == 1 ? 0.5f : 0.7f;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.enabled = false;
                var color = particles.colorOverLifetime;
                color.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;
                var size = particles.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(0.12f, 1f), new Keyframe(1f, 0.15f)));
                var rotation = particles.rotationOverLifetime;
                rotation.enabled = i == 1;
                rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
                ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.View;
                Material source = i == 0 ? additive : opaque;
                if (source != null)
                {
                    Material material = new Material(source)
                    { hideFlags = HideFlags.DontSave };
                    material.mainTexture = sprites[i].texture;
                    renderer.sharedMaterial = material;
                    _materials[i] = material;
                }

                _systems[i] = particles;
                _renderers[i] = renderer;
                particles.Play();
            }
        }

        internal void Burst(Vector3 point, Vector2 direction, float width, Color accent, NormalMonsterFeelProfile profile, int layer, int sortingLayer, int order, bool lethal)
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].gameObject.layer = layer;
                _renderers[i].sortingLayerID = sortingLayer;
                _renderers[i].sortingOrder = order + i;
            }

            int sparks = Mathf.Min(24, profile.HitSparks + (lethal ? 4 : 0));
            for (int i = 0; i < sparks; i++)
            {
                float angle = heading + Random.Range(-75f, 75f);
                Vector2 velocity = Direction(angle) * Random.Range(2.4f, 5.5f) * width;
                float size = Random.Range(0.025f, 0.045f) * width;
                Color color = i % 4 == 0 ? accent : new Color(1f, 0.77f, 0.3f, 1f);
                Emit(0, point, velocity, new Vector3(size * 3.8f, size, 1f), Random.Range(0.13f, 0.27f), color, angle);
            }

            for (int i = 0; i < profile.HitMetalShards; i++)
            {
                float angle = heading + Random.Range(-100f, 100f);
                float size = Random.Range(0.055f, 0.09f) * width;
                Color color = i % 2 == 0 ? new Color(0.73f, 0.82f, 0.88f, 1f) : new Color(0.28f, 0.36f, 0.45f, 1f);
                Emit(1, point, Direction(angle) * Random.Range(1.5f, 3.2f) * width, new Vector3(size, size * 0.7f, 1f), Random.Range(0.23f, 0.4f), color, Random.Range(0f, 360f));
            }

            for (int i = 0; i < profile.HitOilDrops; i++)
            {
                float angle = heading + Random.Range(-80f, 80f);
                float size = Random.Range(0.045f, 0.085f) * width;
                Color color = i % 3 == 0 ? new Color(0.35f, 0.018f, 0.045f, 0.95f) : new Color(0.055f, 0.015f, 0.035f, 0.95f);
                Emit(2, point, Direction(angle) * Random.Range(0.7f, 2.5f) * width, new Vector3(size * 1.35f, size, 1f), Random.Range(0.26f, 0.44f), color, angle);
            }
        }

        private void Emit(int kind, Vector3 point, Vector2 velocity, Vector3 size, float lifetime, Color color, float angle)
        {
            if (_budgetFrame != Time.frameCount)
            {
                _budgetFrame = Time.frameCount;
                _remainingBudget = MaximumEmissionsPerFrame;
            }

            ParticleSystem system = _systems[kind];
            if (_remainingBudget <= 0 || system == null || system.particleCount >= system.main.maxParticles)
            {
                return;
            }

            _remainingBudget--;
            system.Emit(new ParticleSystem.EmitParams
            {
                position = point,
                velocity = new Vector3(velocity.x, velocity.y, 0f),
                startSize3D = size,
                startLifetime = lifetime,
                startColor = color,
                rotation = angle
            }, 1);
        }

        private static Vector2 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (paused == _paused)
            {
                return;
            }

            _paused = paused;
            foreach (ParticleSystem system in _systems)
            {
                if (system == null)
                {
                    continue;
                }

                if (paused)
                {
                    system.Pause();
                }
                else
                {
                    system.Play();
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }
    }
}
