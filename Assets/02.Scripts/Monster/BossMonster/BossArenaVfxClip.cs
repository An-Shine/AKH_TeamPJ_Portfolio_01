using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    // Clips visual geometry only; combat timing and hitboxes remain owned by the patterns.
    public sealed class BossArenaVfxClip : MonoBehaviour
    {
        public const string ShaderPath = "Monster/BossHitUpgrade/BossPatternClip";

        private int _arenaId;
        private int _clipId;
        private int _maskId;
        private int _maskClipId;
        private readonly List<Renderer> _renderers = new List<Renderer>(256);
        private readonly List<Collider2D> _walls = new List<Collider2D>(64);
        private MaterialPropertyBlock _properties;
        private global::BossMonster _owner;
        private Transform _bodyHits;
        private Texture2D _mask;
        private Rect _arena;

        private void Awake()
        {
            EnsureProperties();
        }

        private void EnsureProperties()
        {
            if (_properties != null)
            {
                return;
            }

            _arenaId = Shader.PropertyToID("_BossArena");
            _clipId = Shader.PropertyToID("_BossArenaClip");
            _maskId = Shader.PropertyToID("_BossArenaMask");
            _maskClipId = Shader.PropertyToID("_BossMaskClip");
            _properties = new MaterialPropertyBlock();
        }

        public void Initialize(global::BossMonster owner, Transform bodyHits)
        {
            EnsureProperties();
            _owner = owner;
            _bodyHits = bodyHits;
            if (_owner != null)
            {
                RebuildMask();
            }
        }

        private void RebuildMask()
        {
            _arena = _owner.Arena;
            _walls.Clear();
            foreach (Collider2D collider in Physics2D.OverlapBoxAll(_arena.center, _arena.size, 0f))
            {
                if (collider == null || !collider.enabled || collider.isTrigger ||
                    collider.GetComponentInParent<NormalMonster>() != null ||
                    MonsterCombat.ResolvePlayerCollider(collider, _owner.Player) != null)
                {
                    continue;
                }

                Rigidbody2D body = collider.attachedRigidbody;
                if (body == null || body.bodyType == RigidbodyType2D.Static)
                {
                    _walls.Add(collider);
                }
            }

            const int resolution = 192;
            if (_mask != null)
            {
                Destroy(_mask);
            }

            _mask = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "BossArenaVisualMask",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color32[] pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 point = new Vector2(_arena.xMin + (x + 0.5f) * _arena.width / resolution,
                        _arena.yMin + (y + 0.5f) * _arena.height / resolution);
                    bool clear = true;
                    foreach (Collider2D wall in _walls)
                    {
                        if (wall.OverlapPoint(point))
                        {
                            clear = false;
                            break;
                        }
                    }

                    byte value = clear ? (byte)255 : (byte)0;
                    pixels[y * resolution + x] = new Color32(value, value, value, 255);
                }
            }

            _mask.SetPixels32(pixels);
            _mask.Apply(false, true);
        }

        private void LateUpdate()
        {
            if (_owner == null)
            {
                return;
            }

            EnsureProperties();
            if (_mask == null || _arena != _owner.Arena)
            {
                RebuildMask();
            }

            GetComponentsInChildren<Renderer>(true, _renderers);
            Vector4 bounds = new Vector4(_arena.xMin, _arena.yMin, _arena.xMax, _arena.yMax);
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null ||
                    !renderer.sharedMaterial.HasProperty(_clipId))
                {
                    continue;
                }

                renderer.GetPropertyBlock(_properties);
                _properties.SetVector(_arenaId, bounds);
                _properties.SetFloat(_clipId, 1f);
                _properties.SetTexture(_maskId, _mask);

                // Airborne body hits retain their full silhouette beside a wall.
                bool bodyHit = _bodyHits != null && renderer.transform.IsChildOf(_bodyHits);
                _properties.SetFloat(_maskClipId, bodyHit ? 0f : 1f);
                renderer.SetPropertyBlock(_properties);
            }
        }

        private void OnDestroy()
        {
            if (_mask != null)
            {
                Destroy(_mask);
            }
        }
    }
}