using System;
using UnityEngine;
using _02.Scripts.Monster.BossMonster;

namespace _02.Scripts.Monster.Presentation
{
    // A fuse uses the real explosion radius/progress. Direction cues only show aim.
    internal sealed class MonsterAttackCue : IDisposable
    {
        private readonly Material _material;
        private readonly BossShapeVisual _fill;
        private readonly BossShapeVisual _contrast;
        private readonly BossShapeVisual _edge;
        private readonly BossShapeVisual _clock;
        private readonly SpriteRenderer _body;

        public MonsterAttackCue(Transform owner, SpriteRenderer body)
        {
            _body = body;
            Shader shader = Resources.Load<Shader>(BossVfxUpgrade.ResourceRoot + "BossCrispVfx");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            _material = new Material(shader)
            {
                name = "MonsterAttackCue",
                hideFlags = HideFlags.DontSave,
                mainTexture = Texture2D.whiteTexture
            };
            _fill = Create("MonsterWarningFill", owner, MonsterVfxReadability.GroundOrder);
            _contrast = Create("MonsterWarningContrast", owner, MonsterVfxReadability.WarningContrastOrder);
            _edge = Create("MonsterWarningEdge", owner, MonsterVfxReadability.WarningEdgeOrder);
            _clock = Create("MonsterWarningClock", owner, MonsterVfxReadability.WarningDetailOrder);
            Hide();
        }

        private BossShapeVisual Create(string name, Transform owner, int order)
        {
            return new BossShapeVisual(name, owner, _material, owner.gameObject.layer,
                _body.sortingLayerID, _body.sortingOrder + order, owner.position.z);
        }

        private void Show()
        {
            _fill.SetSorting(_body.sortingLayerID, _body.sortingOrder + MonsterVfxReadability.GroundOrder);
            _contrast.SetSorting(_body.sortingLayerID, _body.sortingOrder + MonsterVfxReadability.WarningContrastOrder);
            _edge.SetSorting(_body.sortingLayerID, _body.sortingOrder + MonsterVfxReadability.WarningEdgeOrder);
            _clock.SetSorting(_body.sortingLayerID, _body.sortingOrder + MonsterVfxReadability.WarningDetailOrder);
            _fill.SetVisible(true);
            _contrast.SetVisible(true);
            _edge.SetVisible(true);
            _clock.SetVisible(true);
        }

        public void Circle(Vector2 center, float radius, float progress)
        {
            Show();
            float thickness = Mathf.Clamp(radius * 0.035f, 0.035f, 0.075f);
            Color edge = MonsterVfxReadability.WarningEdge(progress, progress >= 0.85f);
            _fill.SetArc(center, 0f, radius, 0f, 360f, MonsterVfxReadability.WarningFill(progress));
            _contrast.SetArc(center, Mathf.Max(0f, radius - thickness * 1.8f), radius + thickness * 1.8f,
                0f, 360f, new Color(0.025f, 0.018f, 0.045f, 0.95f));
            _edge.SetArc(center, Mathf.Max(0f, radius - thickness * 0.5f), radius + thickness * 0.5f,
                0f, 360f, edge);
            _clock.SetReticle(center, radius, -90f, 360f, progress,
                new Color(1f, 0.87f, 0.7f, 0.9f));
        }

        public void Direction(Vector2 origin, Vector2 direction, float length, Color color)
        {
            Show();
            length = Mathf.Clamp(length, 0.3f, 2.2f);
            Vector2 center = origin + direction.normalized * length * 0.5f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _fill.SetVisible(false);
            _contrast.SetBeam(center, angle, length, 0.085f, new Color(0.025f, 0.018f, 0.045f, 0.9f));
            _edge.SetBeam(center, angle, length, 0.025f, MonsterVfxReadability.WithOpacity(color, 0.72f));
            _clock.SetReticle(origin, length, angle - 8f, 16f, 0f,
                MonsterVfxReadability.WithOpacity(color, 0.8f));
        }

        public void Hide()
        {
            _fill.SetVisible(false);
            _contrast.SetVisible(false);
            _edge.SetVisible(false);
            _clock.SetVisible(false);
        }

        public void Dispose()
        {
            _fill.Dispose();
            _contrast.Dispose();
            _edge.Dispose();
            _clock.Dispose();
            UnityEngine.Object.Destroy(_material);
        }
    }
}
