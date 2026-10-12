using System;
using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    internal sealed class RangePatternCue : IDisposable
    {
        private const int MaximumLines = 8;
        private readonly LineRenderer[] _edges = new LineRenderer[MaximumLines];
        private readonly LineRenderer[] _contrast = new LineRenderer[MaximumLines];
        private readonly SpriteRenderer _body;
        private readonly SpriteRenderer _icon;
        private readonly bool _hasSpriteMaterial;
        private readonly bool _hasLineMaterial;

        public RangePatternCue(Transform owner, SpriteRenderer body, Material spriteMaterial, Material lineMaterial)
        {
            _body = body;
            _hasSpriteMaterial = spriteMaterial != null;
            _hasLineMaterial = lineMaterial != null;
            for (int i = 0; i < MaximumLines; i++)
            {
                _contrast[i] = CreateLine("RangeAimContrast", owner, lineMaterial, 0.065f, 12);
                _edges[i] = CreateLine("RangeAimEdge", owner, lineMaterial, 0.025f, 13);
            }

            GameObject icon = new GameObject("RangePatternChargeIcon");
            icon.transform.SetParent(owner, false);
            icon.layer = body.gameObject.layer;
            _icon = icon.AddComponent<SpriteRenderer>();
            _icon.sharedMaterial = spriteMaterial;
            Hide();
        }

        private LineRenderer CreateLine(string name, Transform owner, Material material, float width, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(owner, false);
            child.layer = _body.gameObject.layer;
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width * 0.6f;
            line.sortingLayerID = _body.sortingLayerID;
            line.sortingOrder = _body.sortingOrder + order;
            return line;
        }

        public void Show(RangeAttackPattern pattern, Vector3 muzzle, Vector2 aim, int sweepSign, float progress)
        {
            if (_body == null || !_body.enabled || !_body.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }
            Color color = RangeAttackPatterns.Color(pattern);
            color.a = Mathf.Lerp(0.35f, 0.85f, progress);
            Color dark = new Color(0.025f, 0.018f, 0.045f, 0.8f);
            int count = RangeAttackPatterns.CueCount(pattern);
            for (int i = 0; i < MaximumLines; i++)
            {
                bool visible = i < count && _hasLineMaterial;
                _edges[i].enabled = visible;
                _contrast[i].enabled = visible;
                if (!visible)
                    continue;
                Vector2 direction = RangeAttackPatterns.Direction(aim, RangeAttackPatterns.CueAngle(pattern, i, sweepSign));
                Vector3 start = muzzle + (Vector3)(direction * 0.1f);
                Vector3 end = muzzle + (Vector3)(direction * Mathf.Lerp(0.6f, 1.25f, progress));
                SetLine(_edges[i], start, end, color);
                SetLine(_contrast[i], start, end, dark);
            }

            _icon.sprite = RangePatternVfx.GetSprite(pattern, RangePatternSprite.Projectile);
            _icon.enabled = _hasSpriteMaterial && _icon.sprite != null;
            if (_icon.sprite == null)
                return;
            _icon.sortingLayerID = _body.sortingLayerID;
            _icon.sortingOrder = _body.sortingOrder + 14;
            _icon.transform.position = muzzle;
            _icon.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            float size = Mathf.Lerp(0.08f, 0.22f, progress) / Mathf.Max(0.01f, _icon.sprite.bounds.size.x);
            _icon.transform.localScale = Vector3.one * size;
            _icon.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.4f, 1f, progress));
        }

        private static void SetLine(LineRenderer line, Vector3 start, Vector3 end, Color color)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = color;
            line.endColor = color;
        }

        public void Hide()
        {
            foreach (LineRenderer line in _edges)
                line.enabled = false;
            foreach (LineRenderer line in _contrast)
                line.enabled = false;
            _icon.enabled = false;
        }

        public void Dispose()
        {
            foreach (LineRenderer line in _edges)
            {
                if (line != null)
                    UnityEngine.Object.Destroy(line.gameObject);
            }

            foreach (LineRenderer line in _contrast)
            {
                if (line != null)
                    UnityEngine.Object.Destroy(line.gameObject);
            }

            if (_icon != null)
                UnityEngine.Object.Destroy(_icon.gameObject);
        }
    }
}
