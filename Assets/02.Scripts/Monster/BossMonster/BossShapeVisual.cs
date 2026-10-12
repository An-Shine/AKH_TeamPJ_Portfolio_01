using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal sealed class BossShapeVisual : IDisposable
    {
        private const int Segments = 96;
        private readonly GameObject _root;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private readonly List<Vector3> _vertices = new List<Vector3>(300);
        private readonly List<int> _triangles = new List<int>(450);
        private readonly List<Color> _colors = new List<Color>(300);
        private readonly List<Vector2> _uvs = new List<Vector2>(300);
        private readonly List<float> _arenaAngles = new List<float>(80);
        private readonly float _z;
        public BossShapeVisual(string name, Transform parent, Material material, int layer, int sortingLayer, int order, float z)
        {
            _z = z;
            _root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            _root.layer = layer;
            _root.transform.SetParent(parent, false);
            _mesh = new Mesh
            {
                name = name + "Mesh"
            };
            _mesh.MarkDynamic();
            _root.GetComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer renderer = _root.GetComponent<MeshRenderer>();
            _renderer = renderer;
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
        }

        public void SetArc(Vector2 center, float innerRadius, float outerRadius, float startDegrees, float spanDegrees, Color color)
        {
            Begin(center);
            innerRadius = Mathf.Max(0f, innerRadius);
            outerRadius = Mathf.Max(innerRadius, outerRadius);
            int count = Mathf.Max(2, Mathf.CeilToInt(Segments * Mathf.Abs(spanDegrees) / 360f));
            for (int i = 0; i <= count; i++)
            {
                float degrees = startDegrees + spanDegrees * i / count;
                Vector2 direction = BossBattleGeometry.Direction(degrees);
                AddVertex(direction * innerRadius, color);
                AddVertex(direction * outerRadius, color);
                if (i > 0)
                {
                    int index = i * 2;
                    AddTriangle(index - 2, index, index - 1);
                    AddTriangle(index - 1, index, index + 1);
                }
            }

            Finish();
        }

        public void SetBeam(Vector2 center, float angle, float length, float width, Color color)
        {
            Begin(center);
            Vector2 forward = BossBattleGeometry.Direction(angle) * (length * 0.5f);
            Vector2 side = BossBattleGeometry.Direction(angle + 90f) * (width * 0.5f);
            AddVertex(-forward - side, color);
            AddVertex(-forward + side, color);
            AddVertex(forward + side, color);
            AddVertex(forward - side, color);
            AddTriangle(0, 1, 2);
            AddTriangle(0, 2, 3);
            Finish();
        }

        public void SetBeamEdges(Vector2 center, float angle, float length, float width, float thickness, Color color)
        {
            Begin(center);
            Vector2 forward = BossBattleGeometry.Direction(angle) * length * 0.5f;
            Vector2 side = BossBattleGeometry.Direction(angle + 90f) * width * 0.5f;
            AddLine(-forward - side, forward - side, thickness, color);
            AddLine(-forward + side, forward + side, thickness, color);
            AddLine(-forward - side, -forward + side, thickness, color);
            AddLine(forward - side, forward + side, thickness, color);
            Finish();
        }

        public void SetSectorEdges(Vector2 center, float radius, float start, float span, float thickness, Color color)
        {
            Begin(center);
            radius = Mathf.Max(0f, radius);
            int count = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(span) / 4f));
            Vector2 previous = BossBattleGeometry.Direction(start) * radius;
            for (int i = 1; i <= count; i++)
            {
                Vector2 next = BossBattleGeometry.Direction(start + span * i / count) * radius;
                AddLine(previous, next, thickness, color);
                previous = next;
            }
            if (Mathf.Abs(span) < 359.9f)
            {
                AddLine(Vector2.zero, BossBattleGeometry.Direction(start) * radius, thickness, color);
                AddLine(Vector2.zero, BossBattleGeometry.Direction(start + span) * radius, thickness, color);
            }

            Finish();
        }

        public void SetReticle(Vector2 center, float radius, float start, float span, float progress, Color color)
        {
            Begin(center);
            int ticks = Mathf.Clamp(Mathf.CeilToInt(span / 30f), 2, 12);
            for (int i = 0; i <= ticks; i++)
            {
                Vector2 direction = BossBattleGeometry.Direction(start + span * i / ticks);
                AddLine(direction * Mathf.Max(0f, radius - 0.15f), direction * (radius + 0.045f), 0.03f, color);
            }
            if (progress > 0f)
            {
                float meterRadius = Mathf.Max(0.03f, radius - 0.12f);
                float meterSpan = span * Mathf.Clamp01(progress);
                int count = Mathf.Max(2, Mathf.CeilToInt(meterSpan / 4f));
                Vector2 previous = BossBattleGeometry.Direction(start) * meterRadius;
                for (int i = 1; i <= count; i++)
                {
                    Vector2 next = BossBattleGeometry.Direction(start + meterSpan * i / count) * meterRadius;
                    AddLine(previous, next, 0.026f, color);
                    previous = next;
                }
            }
            Finish();
        }

        public void SetBeamTicks(Vector2 center, float angle, float length, float width, Color color)
        {
            Begin(center);
            Vector2 forward = BossBattleGeometry.Direction(angle);
            Vector2 side = BossBattleGeometry.Direction(angle + 90f);
            int count = Mathf.Clamp(Mathf.CeilToInt(length / 1.3f), 2, 24);
            for (int i = 0; i <= count; i++)
            {
                Vector2 point = forward * ((float)i / count - 0.5f) * length;
                AddLine(point + side * width * 0.33f, point + side * width * 0.52f, 0.025f, color);
                AddLine(point - side * width * 0.33f, point - side * width * 0.52f, 0.025f, color);
            }
            Finish();
        }

        private void AddLine(Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.000001f)
                return;
            Vector2 side = new Vector2(-direction.y, direction.x).normalized * width * 0.5f;
            int start = _vertices.Count;
            AddVertex(from - side, color);
            AddVertex(from + side, color);
            AddVertex(to + side, color);
            AddVertex(to - side, color);
            AddTriangle(start, start + 1, start + 2);
            AddTriangle(start, start + 2, start + 3);
        }

        public void SetArenaOutsideCircle(Rect arena, Vector2 center, float safeRadius, Color color)
        {
            Begin(center);
            _arenaAngles.Clear();
            for (int i = 0; i < Segments; i++)
                _arenaAngles.Add(i * 360f / Segments);
            _arenaAngles.Add(Mathf.Repeat(BossBattleGeometry.Angle(new Vector2(arena.xMin, arena.yMin) - center), 360f));
            _arenaAngles.Add(Mathf.Repeat(BossBattleGeometry.Angle(new Vector2(arena.xMax, arena.yMin) - center), 360f));
            _arenaAngles.Add(Mathf.Repeat(BossBattleGeometry.Angle(new Vector2(arena.xMax, arena.yMax) - center), 360f));
            _arenaAngles.Add(Mathf.Repeat(BossBattleGeometry.Angle(new Vector2(arena.xMin, arena.yMax) - center), 360f));
            _arenaAngles.Sort();
            for (int i = 0; i <= _arenaAngles.Count; i++)
            {
                Vector2 direction = BossBattleGeometry.Direction(_arenaAngles[i % _arenaAngles.Count]);
                float x = Mathf.Abs(direction.x) < 0.00001f ? float.PositiveInfinity : ((direction.x > 0f ? arena.xMax : arena.xMin) - center.x) / direction.x;
                float y = Mathf.Abs(direction.y) < 0.00001f ? float.PositiveInfinity : ((direction.y > 0f ? arena.yMax : arena.yMin) - center.y) / direction.y;
                float wall = Mathf.Max(0f, Mathf.Min(x, y));
                AddVertex(direction * Mathf.Min(safeRadius, wall), color);
                AddVertex(direction * wall, color);
                if (i > 0)
                {
                    int index = i * 2;
                    AddTriangle(index - 2, index, index - 1);
                    AddTriangle(index - 1, index, index + 1);
                }
            }

            Finish();
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.SetActive(visible);
            }
        }

        public void SetSorting(int sortingLayer, int order)
        {
            _renderer.sortingLayerID = sortingLayer;
            _renderer.sortingOrder = order;
        }

        private void Begin(Vector2 center)
        {
            _root.transform.position = new Vector3(center.x, center.y, _z);
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();
            _uvs.Clear();
        }

        private void AddVertex(Vector2 point, Color color)
        {
            _vertices.Add(new Vector3(point.x, point.y, 0f));
            _colors.Add(color);
            _uvs.Add(Vector2.one * 0.5f);
        }

        private void AddTriangle(int a, int b, int c)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
        }

        private void Finish()
        {
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_mesh);
            UnityEngine.Object.Destroy(_root);
        }
    }
}
