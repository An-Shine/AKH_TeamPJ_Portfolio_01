using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using _02.Scripts.Monster.Presentation;

namespace _02.Scripts.Monster.BossMonster
{
    // Two reused meshes draw all transient rings, electrical branches and cracks.
    // These objects never contain colliders or alter the combat clock.
    internal sealed class BossVfxMesh : IDisposable
    {
        private enum Kind { Ring, Arc, Lightning, Crack, Shard }

        private struct Stroke
        {
            public bool Active;
            public Kind Kind;
            public Vector2 Center;
            public Vector2 End;
            public Color Color;
            public float Age;
            public float Lifetime;
            public float Radius;
            public float EndRadius;
            public float Width;
            public float Angle;
            public float Span;
            public int Seed;
        }

        private sealed class Buffer
        {
            public readonly GameObject Root;
            public readonly Mesh Mesh;
            public readonly Material Material;
            public readonly List<Vector3> Vertices = new List<Vector3>(16384);
            public readonly List<int> Indices = new List<int>(24576);
            public readonly List<Color> Colors = new List<Color>(16384);
            public readonly List<Vector2> Uvs = new List<Vector2>(16384);

            public Buffer(Transform parent, Shader shader, int layer, int sortingLayer,
                int order, float z, bool additive)
            {
                Root = new GameObject(additive ? "BossVfxEnergyMesh" : "BossVfxContrastMesh",
                    typeof(MeshFilter), typeof(MeshRenderer));
                Root.layer = layer;
                Root.transform.SetParent(parent, false);
                Root.transform.position = new Vector3(0f, 0f, z);
                Mesh = new Mesh { name = Root.name, indexFormat = IndexFormat.UInt32 };
                Mesh.MarkDynamic();
                Material = new Material(shader) { hideFlags = HideFlags.DontSave };
                Material.mainTexture = Texture2D.whiteTexture;
                Material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                Material.SetFloat("_Gain", 1f);
                Root.GetComponent<MeshFilter>().sharedMesh = Mesh;
                MeshRenderer renderer = Root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Material;
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = order;
            }

            public void Begin()
            {
                Vertices.Clear();
                Indices.Clear();
                Colors.Clear();
                Uvs.Clear();
            }

            public void Line(Vector2 from, Vector2 to, float width, Color color)
            {
                Vector2 delta = to - from;
                if (delta.sqrMagnitude < 0.000001f || color.a <= 0.001f)
                    return;
                Vector2 side = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
                int start = Vertices.Count;
                Vertices.Add(from - side);
                Vertices.Add(from + side);
                Vertices.Add(to + side);
                Vertices.Add(to - side);
                for (int i = 0; i < 4; i++)
                {
                    Colors.Add(color);
                    Uvs.Add(Vector2.one * 0.5f);
                }
                Indices.Add(start);
                Indices.Add(start + 1);
                Indices.Add(start + 2);
                Indices.Add(start);
                Indices.Add(start + 2);
                Indices.Add(start + 3);
            }

            public void Finish()
            {
                Mesh.Clear();
                if (Vertices.Count == 0)
                    return;
                Mesh.SetVertices(Vertices);
                Mesh.SetColors(Colors);
                Mesh.SetUVs(0, Uvs);
                Mesh.SetTriangles(Indices, 0);
                Mesh.RecalculateBounds();
            }

            public void TaperedLine(Vector2 from, Vector2 to, float fromWidth, float toWidth,
                Color fromColor, Color toColor)
            {
                Vector2 delta = to - from;
                if (delta.sqrMagnitude < 0.000001f || fromColor.a <= 0.001f)
                    return;
                Vector2 normal = new Vector2(-delta.y, delta.x).normalized * 0.5f;
                int start = Vertices.Count;
                Vertices.Add(from - normal * fromWidth);
                Vertices.Add(from + normal * fromWidth);
                Vertices.Add(to + normal * toWidth);
                Vertices.Add(to - normal * toWidth);
                Colors.Add(fromColor);
                Colors.Add(fromColor);
                Colors.Add(toColor);
                Colors.Add(toColor);
                for (int i = 0; i < 4; i++)
                {
                    Uvs.Add(Vector2.one * 0.5f);
                }

                Indices.Add(start);
                Indices.Add(start + 1);
                Indices.Add(start + 2);
                Indices.Add(start);
                Indices.Add(start + 2);
                Indices.Add(start + 3);
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(Mesh);
                UnityEngine.Object.Destroy(Material);
                UnityEngine.Object.Destroy(Root);
            }
        }

        private const int Capacity = 192;
        private readonly Stroke[] _strokes = new Stroke[Capacity];
        private readonly Buffer _contrast;
        private readonly Buffer _energy;
        private int _cursor;
        private int _sequence;

        public BossVfxMesh(Transform parent, Shader shader, int layer, int sortingLayer, int order, float z)
        {
            _contrast = new Buffer(parent, shader, layer, sortingLayer, order + 8, z, false);
            _energy = new Buffer(parent, shader, layer, sortingLayer, order + 9, z, true);
        }

        public void Ring(Vector2 center, float from, float to, float width, Color color,
            float lifetime = 0.4f, float delay = 0f)
        {
            Add(new Stroke
            {
                Kind = Kind.Ring,
                Center = center,
                Radius = from,
                EndRadius = to,
                Width = width,
                Span = 360f,
                Color = color,
                Lifetime = lifetime,
                Age = -delay
            });
        }

        public void Arc(Vector2 center, float radius, float angle, float span,
            float width, Color color, float lifetime = 0.2f, float delay = 0f)
        {
            Add(new Stroke
            {
                Kind = Kind.Arc,
                Center = center,
                Radius = radius,
                EndRadius = radius,
                Width = width,
                Angle = angle,
                Span = span,
                Color = color,
                Lifetime = lifetime,
                Age = -delay
            });
        }

        public void Lightning(Vector2 from, Vector2 to, float width, Color color,
            float lifetime = 0.22f, float delay = 0f)
        {
            Add(new Stroke
            {
                Kind = Kind.Lightning,
                Center = from,
                End = to,
                Width = width,
                Color = color,
                Lifetime = lifetime,
                Age = -delay
            });
        }

        public void Cracks(Vector2 center, float radius, Color color, int count = 7)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * 360f / count + Noise(_sequence + i, 7) * 15f;
                Add(new Stroke
                {
                    Kind = Kind.Crack,
                    Center = center,
                    End = center + Direction(angle) * radius,
                    Width = 0.035f,
                    Color = color,
                    Lifetime = 0.68f
                });
            }
        }

        public void Shards(Vector2 center, float radius, Color color, int count = 12,
            float heading = 0f, float span = 360f)
        {
            count = Mathf.Clamp(count, 3, 24);
            radius = Mathf.Clamp(radius, 0.1f, 4.2f);
            for (int i = 0; i < count; i++)
            {
                float angle = heading - span * 0.5f + span * (i + 0.5f) / count;
                angle += Noise(_sequence + i, 11) * Mathf.Min(12f, span / count * 0.3f);
                Vector2 direction = Direction(angle);
                Add(new Stroke
                {
                    Kind = Kind.Shard,
                    Center = center + direction * radius * 0.1f,
                    End = direction * radius * (1.25f + Mathf.Abs(Noise(_sequence + i, 13)) * 0.9f),
                    Radius = radius * (0.16f + Mathf.Abs(Noise(_sequence + i, 17)) * 0.18f),
                    Width = Mathf.Clamp(radius * 0.026f, 0.015f, 0.07f),
                    Color = i % 4 == 0 ? Color.Lerp(color, Color.white, 0.55f) : color,
                    Lifetime = 0.28f + i % 4 * 0.035f,
                    Age = -(i % 3) * 0.012f
                });
            }
        }

        private void Add(Stroke stroke)
        {
            // Reuse an inactive slot; under overload replace the oldest decoration.
            int slot = -1;
            float oldest = -1f;
            int oldSlot = _cursor;
            for (int i = 0; i < Capacity; i++)
            {
                int index = (_cursor + i) % Capacity;
                if (!_strokes[index].Active)
                {
                    slot = index;
                    break;
                }
                float age = _strokes[index].Age / _strokes[index].Lifetime;
                if (age > oldest)
                {
                    oldest = age;
                    oldSlot = index;
                }
            }
            if (slot < 0)
                slot = oldSlot;
            stroke.Active = true;
            stroke.Lifetime = Mathf.Max(0.035f, stroke.Lifetime);
            stroke.Seed = ++_sequence;
            _strokes[slot] = stroke;
            _cursor = (slot + 1) % Capacity;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            _contrast.Begin();
            _energy.Begin();
            for (int i = 0; i < Capacity; i++)
            {
                if (!_strokes[i].Active)
                    continue;
                Stroke stroke = _strokes[i];
                stroke.Age += deltaTime;
                if (stroke.Age >= stroke.Lifetime)
                {
                    stroke.Active = false;
                    _strokes[i] = stroke;
                    continue;
                }
                _strokes[i] = stroke;
                if (stroke.Age < 0f)
                    continue;
                float progress = Mathf.Clamp01(stroke.Age / stroke.Lifetime);
                float alpha = stroke.Kind == Kind.Crack ? Mathf.Pow(1f - progress, 1.2f)
                    : MonsterVfxReadability.TransientFade(stroke.Age, stroke.Lifetime);
                if (stroke.Kind == Kind.Shard)
                    DrawShard(stroke, progress, alpha);
                else if (stroke.Kind == Kind.Ring || stroke.Kind == Kind.Arc)
                    DrawArc(stroke, progress, alpha);
                else
                    DrawLightning(stroke, alpha);
            }
            _contrast.Finish();
            _energy.Finish();
        }

        private void DrawShard(Stroke stroke, float progress, float alpha)
        {
            float travel = 1f - (1f - progress) * (1f - progress);
            Vector2 head = stroke.Center + stroke.End * travel;
            Vector2 tail = head - stroke.End.normalized * stroke.Radius * (1f - progress * 0.65f);
            float width = stroke.Width * (1f - progress * 0.6f);
            Color dark = new Color(0.012f, 0.018f, 0.04f, alpha * 0.75f);
            _contrast.TaperedLine(head, tail, width * 2.8f, width * 0.6f, dark, dark);
            Color color = stroke.Color;
            color.a *= alpha * 0.85f;
            Color tip = Color.Lerp(color, Color.white, 0.65f);
            tip.a = color.a;
            _energy.TaperedLine(head, tail, width * 1.25f, width * 0.15f, tip, color);
        }

        private void DrawArc(Stroke stroke, float progress, float alpha)
        {
            float radius = Mathf.Lerp(stroke.Radius, stroke.EndRadius,
                1f - (1f - progress) * (1f - progress));
            int segments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(stroke.Span) / 4f), 2, 96);
            Vector2 previous = stroke.Center + Direction(stroke.Angle) * radius;
            for (int i = 1; i <= segments; i++)
            {
                Vector2 next = stroke.Center + Direction(stroke.Angle + stroke.Span * i / segments) * radius;
                StrokeLine(previous, next, stroke.Width, stroke.Color, alpha, 0.32f);
                previous = next;
            }
        }

        private void DrawLightning(Stroke stroke, float alpha)
        {
            Vector2 delta = stroke.End - stroke.Center;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;
            Vector2 side = new Vector2(-delta.y, delta.x).normalized;
            int segments = Mathf.Clamp(Mathf.CeilToInt(length * 4f), 6, 24);
            int flicker = stroke.Kind == Kind.Crack ? 0 : Mathf.FloorToInt(stroke.Age * 30f);
            float deviation = stroke.Kind == Kind.Crack ? length * 0.05f
                : Mathf.Min(0.55f, length * 0.075f);
            Vector2 previous = stroke.Center;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 next = stroke.Center + delta * t;
                if (i < segments)
                    next += side * Noise(stroke.Seed + flicker * 71, i) * deviation;
                StrokeLine(previous, next, stroke.Width, stroke.Color, alpha,
                    stroke.Kind == Kind.Crack ? 0.08f : 0.96f);
                if (stroke.Kind == Kind.Lightning && (i == segments / 3 || i == segments * 2 / 3))
                {
                    Vector2 end = next + delta.normalized * length * 0.12f
                        + side * deviation * (i % 2 == 0 ? 2f : -2f);
                    Vector2 middle = Vector2.Lerp(next, end, 0.5f) + side * deviation * 0.35f;
                    StrokeLine(next, middle, stroke.Width * 0.55f, stroke.Color, alpha * 0.75f, 0.8f);
                    StrokeLine(middle, end, stroke.Width * 0.35f, stroke.Color, alpha * 0.6f, 0.8f);
                }
                previous = next;
            }
        }

        private void StrokeLine(Vector2 from, Vector2 to, float width, Color color, float alpha, float white)
        {
            Color dark = new Color(0.015f, 0.025f, 0.06f, alpha * 0.88f);
            _contrast.Line(from, to, width * 3.4f, dark);
            Color halo = color;
            halo.a *= alpha * 0.32f;
            _energy.Line(from, to, width * 2.8f, halo);
            color.a *= alpha * 0.95f;
            _energy.Line(from, to, width * 1.5f, color);
            Color core = Color.Lerp(color, Color.white, white);
            core.a = alpha * (white > 0.5f ? 0.95f : 0.5f);
            _energy.Line(from, to, width * 0.48f, core);
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static float Noise(int seed, int index)
        {
            unchecked
            {
                uint value = (uint)(seed * 92837111 + index * 689287499);
                value ^= value >> 16;
                value *= 0x7feb352d;
                value ^= value >> 15;
                return (value & 65535) / 32767.5f - 1f;
            }
        }

        public void Clear()
        {
            Array.Clear(_strokes, 0, _strokes.Length);
            _contrast.Begin();
            _energy.Begin();
            _contrast.Finish();
            _energy.Finish();
        }

        public void Dispose()
        {
            _contrast.Dispose();
            _energy.Dispose();
        }
    }
}
