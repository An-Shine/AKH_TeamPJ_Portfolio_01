using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster
{
    // Change only the render mesh. Source pixels, pivots, animation and colliders stay intact.
    internal static class BossSpriteArtifactGuard
    {
        private static readonly Dictionary<Sprite, Sprite> Cache = new Dictionary<Sprite, Sprite>();
        private static readonly HashSet<Sprite> Filtered = new HashSet<Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach (Sprite sprite in Filtered)
            {
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
            }
            Cache.Clear();
            Filtered.Clear();
        }

        internal static bool IsFiltered(Sprite sprite)
        {
            return sprite != null && Filtered.Contains(sprite);
        }

        internal static Sprite Get(Sprite source)
        {
            if (source == null || Filtered.Contains(source))
                return source;
            if (Cache.TryGetValue(source, out Sprite result))
                return result;
            Cache[source] = source;
            try
            {
                int width = Mathf.RoundToInt(source.rect.width);
                int height = Mathf.RoundToInt(source.rect.height);
                Color32[] pixels = ReadPixels(source, width, height);
                bool[] removed = FindDetachedBladePixels(pixels, width, height);
                if (removed == null)
                    return source;
                bool[] blocked = ExpandMask(removed, pixels, width, height);
                BuildMesh(source, blocked, width, height, out Vector2[] vertices, out ushort[] triangles);
                if (vertices.Length == 0)
                    return source;
                result = UnityEngine.Object.Instantiate(source);
                result.name = source.name + "_NoDetachedBlade";
                result.hideFlags = HideFlags.DontSave;
                result.OverrideGeometry(vertices, triangles);
                Filtered.Add(result);
                Cache[source] = result;
                return result;
            }
            catch (Exception exception)
            {
                if (result != null && result != source)
                    UnityEngine.Object.Destroy(result);
                Debug.LogWarning("보스 칼 조각 표시 정리에 실패했습니다: " + source.name + " / " + exception.Message);
                return source;
            }
        }

        private static Color32[] ReadPixels(Sprite source, int width, int height)
        {
            Vector2[] positions = source.vertices;
            Vector2[] uvs = source.uv;
            ushort[] sourceTriangles = source.triangles;
            bool packed = source.packed;
            Vector2 xAxis = Vector2.zero;
            Vector2 yAxis = Vector2.zero;
            bool mapped = false;
            for (int i = 1; i < positions.Length && !mapped; i++)
            {
                for (int j = i + 1; j < positions.Length; j++)
                {
                    Vector2 a = positions[i] - positions[0];
                    Vector2 b = positions[j] - positions[0];
                    float determinant = Cross(a, b);
                    if (Mathf.Abs(determinant) < 0.000001f)
                        continue;
                    Vector2 ua = uvs[i] - uvs[0];
                    Vector2 ub = uvs[j] - uvs[0];
                    xAxis = (ua * b.y - ub * a.y) / determinant;
                    yAxis = (ub * a.x - ua * b.x) / determinant;
                    mapped = true;
                    break;
                }
            }
            if (!mapped)
                throw new InvalidOperationException("Sprite UV mapping is empty.");
            Vector2 uvOrigin = uvs[0] - xAxis * positions[0].x - yAxis * positions[0].y;
            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            foreach (Vector2 uv in uvs)
            {
                minimum = Vector2.Min(minimum, uv);
                maximum = Vector2.Max(maximum, uv);
            }
            Texture2D texture = source.texture;
            int x = Mathf.Clamp(Mathf.FloorToInt(minimum.x * texture.width), 0, texture.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(minimum.y * texture.height), 0, texture.height - 1);
            int readWidth = Mathf.Clamp(Mathf.CeilToInt(maximum.x * texture.width) - x, 1, texture.width - x);
            int readHeight = Mathf.Clamp(Mathf.CeilToInt(maximum.y * texture.height) - y, 1, texture.height - y);
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(readWidth, readHeight, 0, RenderTextureFormat.ARGB32);
            Texture2D readable = null;
            try
            {
                Graphics.Blit(texture, target, new Vector2((float)readWidth / texture.width, (float)readHeight / texture.height),
                    new Vector2((float)x / texture.width, (float)y / texture.height));
                RenderTexture.active = target;
                readable = new Texture2D(readWidth, readHeight, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, readWidth, readHeight), 0, 0);
                readable.Apply(false, false);
                Color32[] sampled = readable.GetPixels32();
                Color32[] pixels = new Color32[width * height];
                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        Vector2 local = (new Vector2(column + 0.5f, row + 0.5f) - source.pivot) / source.pixelsPerUnit;
                        if (packed && !InsideMesh(local, positions, sourceTriangles))
                            continue;
                        Vector2 uv = uvOrigin + xAxis * local.x + yAxis * local.y;
                        int sx = Mathf.FloorToInt(uv.x * texture.width) - x;
                        int sy = Mathf.FloorToInt(uv.y * texture.height) - y;
                        if (sx >= 0 && sx < readWidth && sy >= 0 && sy < readHeight)
                            pixels[row * width + column] = sampled[sy * readWidth + sx];
                    }
                }
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (readable != null)
                    UnityEngine.Object.Destroy(readable);
            }
        }

        internal static bool[] FindDetachedBladePixels(Color32[] pixels, int width, int height)
        {
            int[] labels = new int[pixels.Length];
            int[] queue = new int[pixels.Length];
            List<int> sizes = new List<int> { 0 };
            List<int> cyanCounts = new List<int> { 0 };
            int largest = 0;
            for (int start = 0; start < pixels.Length; start++)
            {
                if (labels[start] != 0 || pixels[start].a < 8)
                    continue;
                int label = sizes.Count;
                int head = 0;
                int tail = 1;
                int cyan = 0;
                queue[0] = start;
                labels[start] = label;
                while (head < tail)
                {
                    int index = queue[head++];
                    Color32 color = pixels[index];
                    if (color.r <= 170 && color.g >= 115 && color.b >= 145 && color.b - color.r >= 45)
                        cyan++;
                    int px = index % width;
                    int py = index / width;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = px + dx;
                            int ny = py + dy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                continue;
                            int next = ny * width + nx;
                            if (labels[next] != 0 || pixels[next].a < 8)
                                continue;
                            labels[next] = label;
                            queue[tail++] = next;
                        }
                    }
                }
                sizes.Add(tail);
                cyanCounts.Add(cyan);
                if (tail > sizes[largest])
                    largest = label;
            }
            bool[] discard = new bool[sizes.Count];
            bool any = false;
            for (int label = 1; label < sizes.Count; label++)
            {
                discard[label] = label != largest && sizes[label] * 5 <= sizes[largest]
                    && cyanCounts[label] >= 2 && cyanCounts[label] * 100 >= sizes[label] * 8;
                any |= discard[label];
            }
            if (!any)
                return null;
            bool[] removed = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                removed[i] = discard[labels[i]];
            return removed;
        }

        private static bool[] ExpandMask(bool[] removed, Color32[] pixels, int width, int height)
        {
            bool[] blocked = new bool[removed.Length];
            bool[] protectedPixels = new bool[removed.Length];
            for (int i = 0; i < removed.Length; i++)
            {
                if (pixels[i].a > 0 && !removed[i])
                    MarkSquare(protectedPixels, i % width, i / width, 1, width, height);
                if (removed[i])
                    MarkSquare(blocked, i % width, i / width, 3, width, height);
            }
            for (int i = 0; i < blocked.Length; i++)
                blocked[i] = removed[i] || (blocked[i] && !protectedPixels[i]);
            return blocked;
        }

        private static void MarkSquare(bool[] mask, int x, int y, int radius, int width, int height)
        {
            for (int row = Mathf.Max(0, y - radius); row <= Mathf.Min(height - 1, y + radius); row++)
            {
                for (int column = Mathf.Max(0, x - radius); column <= Mathf.Min(width - 1, x + radius); column++)
                    mask[row * width + column] = true;
            }
        }

        private static void BuildMesh(Sprite source, bool[] blocked, int width, int height,
            out Vector2[] vertices, out ushort[] triangles)
        {
            List<RectInt> rectangles = new List<RectInt>();
            Dictionary<Vector2Int, int> previous = new Dictionary<Vector2Int, int>();
            for (int y = 0; y < height; y++)
            {
                Dictionary<Vector2Int, int> current = new Dictionary<Vector2Int, int>();
                int x = 0;
                while (x < width)
                {
                    while (x < width && blocked[y * width + x])
                        x++;
                    int start = x;
                    while (x < width && !blocked[y * width + x])
                        x++;
                    if (x == start)
                        continue;
                    Vector2Int span = new Vector2Int(start, x);
                    if (previous.TryGetValue(span, out int index))
                    {
                        RectInt rectangle = rectangles[index];
                        rectangle.height++;
                        rectangles[index] = rectangle;
                    }
                    else
                    {
                        index = rectangles.Count;
                        rectangles.Add(new RectInt(start, y, x - start, 1));
                    }
                    current[span] = index;
                }
                previous = current;
            }
            List<Vector2> meshVertices = new List<Vector2>();
            List<ushort> meshTriangles = new List<ushort>();
            Vector2[] original = source.vertices;
            ushort[] originalTriangles = source.triangles;
            foreach (RectInt rectangle in rectangles)
            {
                Vector2 minimum = (new Vector2(rectangle.xMin, rectangle.yMin) - source.pivot) / source.pixelsPerUnit;
                Vector2 maximum = (new Vector2(rectangle.xMax, rectangle.yMax) - source.pivot) / source.pixelsPerUnit;
                for (int t = 0; t < originalTriangles.Length; t += 3)
                {
                    List<Vector2> polygon = new List<Vector2>
                    {
                        original[originalTriangles[t]], original[originalTriangles[t + 1]], original[originalTriangles[t + 2]]
                    };
                    polygon = Clip(polygon, 0, minimum.x, true);
                    polygon = Clip(polygon, 0, maximum.x, false);
                    polygon = Clip(polygon, 1, minimum.y, true);
                    polygon = Clip(polygon, 1, maximum.y, false);
                    if (polygon.Count < 3)
                        continue;
                    float area = 0f;
                    for (int i = 1; i + 1 < polygon.Count; i++)
                        area += Mathf.Abs(Cross(polygon[i] - polygon[0], polygon[i + 1] - polygon[0]));
                    if (area < 0.00000001f)
                        continue;
                    if (meshVertices.Count + polygon.Count > ushort.MaxValue)
                        throw new InvalidOperationException("Sprite mesh exceeds the vertex limit.");
                    ushort first = (ushort)meshVertices.Count;
                    meshVertices.AddRange(polygon);
                    for (int i = 1; i + 1 < polygon.Count; i++)
                    {
                        meshTriangles.Add(first);
                        meshTriangles.Add((ushort)(first + i));
                        meshTriangles.Add((ushort)(first + i + 1));
                    }
                }
            }
            // OverrideGeometry accepts pixel coordinates; Sprite.vertices uses pivot-relative units.
            for (int i = 0; i < meshVertices.Count; i++)
            {
                Vector2 pixel = meshVertices[i] * source.pixelsPerUnit + source.pivot;
                meshVertices[i] = new Vector2(Mathf.Clamp(pixel.x, 0f, width), Mathf.Clamp(pixel.y, 0f, height));
            }
            vertices = meshVertices.ToArray();
            triangles = meshTriangles.ToArray();
        }

        private static List<Vector2> Clip(List<Vector2> polygon, int axis, float boundary, bool keepGreater)
        {
            List<Vector2> output = new List<Vector2>();
            if (polygon.Count == 0)
                return output;
            Vector2 previous = polygon[polygon.Count - 1];
            bool wasInside = keepGreater ? previous[axis] >= boundary : previous[axis] <= boundary;
            foreach (Vector2 point in polygon)
            {
                bool inside = keepGreater ? point[axis] >= boundary : point[axis] <= boundary;
                if (inside != wasInside)
                {
                    float ratio = (boundary - previous[axis]) / (point[axis] - previous[axis]);
                    output.Add(Vector2.LerpUnclamped(previous, point, ratio));
                }
                if (inside)
                    output.Add(point);
                previous = point;
                wasInside = inside;
            }
            return output;
        }

        private static bool InsideMesh(Vector2 point, Vector2[] vertices, ushort[] triangles)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 a = vertices[triangles[i]];
                Vector2 b = vertices[triangles[i + 1]];
                Vector2 c = vertices[triangles[i + 2]];
                float ab = Cross(b - a, point - a);
                float bc = Cross(c - b, point - b);
                float ca = Cross(a - c, point - c);
                if ((ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f))
                    return true;
            }
            return false;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
