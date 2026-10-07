using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    internal sealed class BossShapeVisual : IDisposable
    {
        private const int Segments = 72;
        private readonly GameObject _root;
        private readonly Mesh _mesh;
        private readonly List<Vector3> _vertices = new List<Vector3>(300);
        private readonly List<int> _triangles = new List<int>(450);
        private readonly List<Color> _colors = new List<Color>(300);
        private readonly List<Vector2> _uvs = new List<Vector2>(300);
        private readonly float _z;

        public BossShapeVisual(string name, Transform parent, Material material,
            int layer, int sortingLayer, int order, float z)
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
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
        }

        public void SetArc(Vector2 center, float innerRadius, float outerRadius,
            float startDegrees, float spanDegrees, Color color)
        {
            Begin(center);
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

        public void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.SetActive(visible);
            }
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
