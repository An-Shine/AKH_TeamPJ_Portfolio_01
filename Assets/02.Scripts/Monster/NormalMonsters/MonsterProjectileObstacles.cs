using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 이동용 바닥 콜라이더를 유지하면서 원거리 몬스터의 탄과 시야만 같은 규칙으로 검사합니다.
internal static class MonsterProjectileObstacles
{
    private sealed class FloorGeometry
    {
        public readonly List<Rect> SolidCells = new List<Rect>(16);
    }

    private static readonly Dictionary<Tilemap, FloorGeometry> FloorMaps =
        new Dictionary<Tilemap, FloorGeometry>();
    private static readonly List<RaycastHit2D> SightHits = new List<RaycastHit2D>(32);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        FloorMaps.Clear();
        SightHits.Clear();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        FloorMaps.Clear();
    }

    public static bool HasClearShot(Vector2 origin, Vector2 target, Transform owner, Transform player)
    {
        Vector2 delta = target - origin;
        float distance = delta.magnitude;
        if (distance <= 0.000001f)
        {
            return true;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask((LayerMask)~0);
        filter.useTriggers = false;
        SightHits.Clear();
        int count = Physics2D.Raycast(origin, delta / distance, filter, SightHits, distance);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = SightHits[i];
            Collider2D collider = hit.collider;
            if (collider == null || !collider.enabled || collider.isTrigger ||
                !collider.gameObject.activeInHierarchy ||
                (owner != null && (collider.transform == owner || collider.transform.IsChildOf(owner))) ||
                collider.GetComponentInParent<NormalMonster>() != null ||
                MonsterCombat.ResolvePlayerCollider(collider, player) != null)
            {
                continue;
            }

            if (TryResolveHit(hit, origin, target, 0f, out _, out _, out _))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryResolveHit(RaycastHit2D hit, Vector2 origin, Vector2 target, float radius,
        out Vector2 point, out Vector2 centroid, out float distance)
    {
        point = hit.point;
        centroid = hit.centroid;
        distance = hit.distance;
        Tilemap map = hit.collider != null ? hit.collider.GetComponent<Tilemap>() : null;
        FloorGeometry geometry = GetFloorGeometry(map);
        if (geometry == null)
        {
            return true;
        }

        Vector2 localOrigin = map.transform.InverseTransformPoint(origin);
        Vector2 localTarget = map.transform.InverseTransformPoint(target);
        Vector3 scale = map.transform.lossyScale;
        Vector2 localRadius = new Vector2(radius / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            radius / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
        float nearest = float.PositiveInfinity;
        Rect selected = default(Rect);
        foreach (Rect cell in geometry.SolidCells)
        {
            if (TryIntersectCell(localOrigin, localTarget, cell, localRadius, out float fraction) &&
                fraction < nearest)
            {
                nearest = fraction;
                selected = cell;
            }
        }

        if (float.IsPositiveInfinity(nearest))
        {
            return false;
        }

        Vector2 localCenter = Vector2.Lerp(localOrigin, localTarget, nearest);
        Vector2 localContact = new Vector2(Mathf.Clamp(localCenter.x, selected.xMin, selected.xMax),
            Mathf.Clamp(localCenter.y, selected.yMin, selected.yMax));
        centroid = map.transform.TransformPoint(localCenter);
        point = map.transform.TransformPoint(localContact);
        distance = (centroid - origin).magnitude;
        return true;
    }

    public static bool BlocksAtPoint(Collider2D collider, Vector2 point)
    {
        Tilemap map = collider != null ? collider.GetComponent<Tilemap>() : null;
        FloorGeometry geometry = GetFloorGeometry(map);
        if (geometry == null)
        {
            return true;
        }

        Vector2 local = map.transform.InverseTransformPoint(point);
        foreach (Rect cell in geometry.SolidCells)
        {
            if (local.x >= cell.xMin - 0.001f && local.x <= cell.xMax + 0.001f &&
                local.y >= cell.yMin - 0.001f && local.y <= cell.yMax + 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    private static FloorGeometry GetFloorGeometry(Tilemap map)
    {
        if (map == null || !map.name.StartsWith("MapDecorations", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (FloorMaps.TryGetValue(map, out FloorGeometry cached))
        {
            return cached;
        }

        FloorGeometry geometry = new FloorGeometry();
        bool hasFloorFrames = false;
        foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
        {
            TileBase tile = map.GetTile(cell);
            if (tile == null)
            {
                continue;
            }

            if (IsFloorFrame(tile))
            {
                hasFloorFrames = true;
                continue;
            }

            if (map.GetColliderType(cell) == Tile.ColliderType.None)
            {
                continue;
            }

            Vector3 minimum = map.CellToLocal(cell);
            Vector3 maximum = map.CellToLocal(cell + new Vector3Int(1, 1, 0));
            geometry.SolidCells.Add(new Rect(Mathf.Min(minimum.x, maximum.x), Mathf.Min(minimum.y, maximum.y),
                Mathf.Abs(maximum.x - minimum.x), Mathf.Abs(maximum.y - minimum.y)));
        }

        // 같은 타일맵의 빈 프레임 첫 충돌 뒤에 있는 상자도 전체 경로에서 다시 검사합니다.
        cached = hasFloorFrames ? geometry : null;
        FloorMaps[map] = cached;
        return cached;
    }

    private static bool IsFloorFrame(TileBase tile)
    {
        string name = tile.name;
        return name.StartsWith("$BaseCornerWall_", StringComparison.Ordinal) ||
               name.StartsWith("BaseCornerWall_", StringComparison.Ordinal) ||
               name.StartsWith("$BaseSquareWall_", StringComparison.Ordinal) ||
               name.StartsWith("BaseSquareWall_", StringComparison.Ordinal);
    }

    private static bool TryIntersectCell(Vector2 origin, Vector2 target, Rect cell, Vector2 radius,
        out float fraction)
    {
        Vector2 delta = target - origin;
        float entry = 0f;
        float exit = 1f;
        for (int axis = 0; axis < 2; axis++)
        {
            float start = axis == 0 ? origin.x : origin.y;
            float movement = axis == 0 ? delta.x : delta.y;
            float minimum = axis == 0 ? cell.xMin - radius.x : cell.yMin - radius.y;
            float maximum = axis == 0 ? cell.xMax + radius.x : cell.yMax + radius.y;
            if (Mathf.Abs(movement) <= 0.000001f)
            {
                if (start < minimum || start > maximum)
                {
                    fraction = 0f;
                    return false;
                }

                continue;
            }

            float first = (minimum - start) / movement;
            float last = (maximum - start) / movement;
            if (first > last)
            {
                float swap = first;
                first = last;
                last = swap;
            }

            entry = Mathf.Max(entry, first);
            exit = Mathf.Min(exit, last);
            if (entry > exit)
            {
                fraction = 0f;
                return false;
            }
        }

        fraction = entry;
        return true;
    }
}
