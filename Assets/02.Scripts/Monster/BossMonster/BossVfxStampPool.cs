using System;
using System.Collections.Generic;
using UnityEngine;
using _02.Scripts.Monster.Presentation;

namespace _02.Scripts.Monster.BossMonster
{
    // Signature artwork stays in the same XY plane and angle convention as attack meshes.
    internal sealed class BossVfxStampPool : IDisposable
    {
        private sealed class Stamp
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public Vector3 Scale;
            public Color Color;
            public float Age;
            public float Lifetime;
        }

        private readonly List<Stamp> _stamps = new List<Stamp>(16);
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly Sprite _sprite;
        private readonly BossVfxTextureDefinition _definition;
        private readonly int _layer;
        private readonly int _sortingLayer;
        private readonly int _order;
        private readonly float _z;
        public int ActiveCount { get; private set; }

        public BossVfxStampPool(Transform parent, Texture2D texture, Material material,
            BossVfxTextureDefinition definition, int layer, int sortingLayer, int order, float z)
        {
            _parent = parent;
            _material = material;
            _definition = definition;
            _layer = layer;
            _sortingLayer = sortingLayer;
            _order = order;
            _z = z;
            _sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect);
            _sprite.name = "BossSignature_" + definition.key;
            _sprite.hideFlags = HideFlags.DontSave;
        }

        public void Emit(Vector2 point, Vector2 velocity, float width, float height,
            float lifetime, Color color, float angle)
        {
            if (ActiveCount >= _definition.limit)
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
                GameObject root = new GameObject("BossSignature_" + _definition.key, typeof(SpriteRenderer));
                root.layer = _layer;
                root.transform.SetParent(_parent, false);
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = _sprite;
                renderer.sharedMaterial = _material;
                renderer.sortingLayerID = _sortingLayer;
                renderer.sortingOrder = _order;
                stamp = new Stamp { Renderer = renderer };
                _stamps.Add(stamp);
            }

            stamp.Velocity = velocity;
            stamp.Scale = new Vector3(width / _sprite.bounds.size.x, height / _sprite.bounds.size.y, 1f);
            stamp.Color = color;
            stamp.Age = 0f;
            stamp.Lifetime = Mathf.Max(0.035f, lifetime);
            SpriteRenderer body = stamp.Renderer;
            body.transform.position = new Vector3(point.x, point.y, _z);
            body.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            body.transform.localScale = stamp.Scale * ScaleAt(0f);
            body.color = color;
            body.enabled = true;
            ActiveCount++;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            foreach (Stamp stamp in _stamps)
            {
                SpriteRenderer body = stamp.Renderer;
                if (!body.enabled)
                    continue;
                stamp.Age += deltaTime;
                if (stamp.Age >= stamp.Lifetime)
                {
                    body.enabled = false;
                    ActiveCount--;
                    continue;
                }

                body.transform.position += (Vector3)(stamp.Velocity * deltaTime);
                body.transform.localScale = stamp.Scale * ScaleAt(stamp.Age / stamp.Lifetime);
                Color color = stamp.Color;
                color.a *= MonsterVfxReadability.TransientFade(stamp.Age, stamp.Lifetime);
                body.color = color;
            }
        }

        private float ScaleAt(float progress)
        {
            float end = Mathf.Max(0.1f, 1f + _definition.grow);
            if (!_definition.punch)
                return Mathf.Lerp(1f, end, progress);
            if (progress < 0.16f)
                return Mathf.Lerp(0.42f, 1.08f, progress / 0.16f);
            if (progress < 0.34f)
                return Mathf.Lerp(1.08f, 1f, (progress - 0.16f) / 0.18f);
            return Mathf.Lerp(1f, end, (progress - 0.34f) / 0.66f);
        }

        public void Dispose()
        {
            foreach (Stamp stamp in _stamps)
                UnityEngine.Object.Destroy(stamp.Renderer.gameObject);
            _stamps.Clear();
            ActiveCount = 0;
            UnityEngine.Object.Destroy(_sprite);
        }
    }
}
