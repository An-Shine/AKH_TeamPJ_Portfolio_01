using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[AddComponentMenu("Monster/Range Projectile Pass Through")]
public sealed class RangeProjectilePassThrough : MonoBehaviour
{
    private string _previousTag;
    private bool _retagged;

    private void OnEnable()
    {
        // Existing player projectiles destroy themselves on Wall/Obstacle tags.
        // Keep the ground collider/layer, but remove those projectile-only blocking tags.
        if (CompareTag("Wall") || CompareTag("Obstacle"))
        {
            _previousTag = gameObject.tag;
            gameObject.tag = "Untagged";
            _retagged = true;
        }
    }

    private void OnDisable()
    {
        if (_retagged && CompareTag("Untagged"))
            gameObject.tag = _previousTag;
        _retagged = false;
    }

    internal static bool Allows(Collider2D collider)
    {
        if (collider == null)
            return false;
        RangeProjectilePassThrough marker = collider.GetComponentInParent<RangeProjectilePassThrough>();
        return marker != null && marker.isActiveAndEnabled;
    }
}

// The same square artwork is used for raised structures and floor holes.
// Keep structures with a front panel solid; separate only the floor-hole cells at runtime.
internal static class RangeProjectileTerrain
{
    private static readonly string[] HoleImages =
    {
        "BaseSquareWall_3x3_64px",
        "BaseSquareWall_Extendable_3x4_64px",
        "BaseCornerWall_3x4_64px",
        "BaseCornerWall_Upper_3x4_64px",
        "BaseCornerWall_Lower_3x4_64px",
        "CollapsedChasms_4Piece_Sheet",
        "DesertErodedCrater_Large_7x7_64px"
    };
    private static readonly HashSet<int> PreparedScenes = new HashSet<int>();
#if UNITY_EDITOR
    private static readonly HashSet<int> ReportedColliders = new HashSet<int>();
#endif

    private struct HoleCell
    {
        public Vector3Int Position;
        public TileBase Tile;
        public Color Color;
        public Matrix4x4 Matrix;
        public TileFlags Flags;
        public Tile.ColliderType ColliderType;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        PreparedScenes.Clear();
#if UNITY_EDITOR
        ReportedColliders.Clear();
#endif
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            PrepareScene(SceneManager.GetSceneAt(i));
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PrepareScene(scene);
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        PreparedScenes.Remove(scene.handle);
    }

    internal static void PrepareScene(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded || !PreparedScenes.Add(scene.handle))
            return;
        List<Tilemap> maps = new List<Tilemap>();
        foreach (GameObject root in scene.GetRootGameObjects())
            maps.AddRange(root.GetComponentsInChildren<Tilemap>(true));
        int moved = 0;
        foreach (Tilemap map in maps)
        {
            TilemapCollider2D collider = map.GetComponent<TilemapCollider2D>();
            if (collider == null || !collider.enabled)
                continue;
            if (map.name == "MapBoundary")
            {
                if (map.GetComponent<RangeProjectilePassThrough>() == null)
                    map.gameObject.AddComponent<RangeProjectilePassThrough>();
                continue;
            }
            if (RangeProjectilePassThrough.Allows(collider))
                continue;
            try
            {
                moved += SeparateHoles(map, collider);
            }
            catch (Exception exception)
            {
                Debug.LogError("바닥 구멍 충돌 분리에 실패했습니다: " + map.name + " / " + exception.Message, map);
            }
        }
        Physics2D.SyncTransforms();
#if UNITY_EDITOR
        if (moved > 0)
            Debug.Log("[RangeProjectile] 바닥 구멍 " + moved + "칸을 GroundHoles로 분리했습니다. 씬: " + scene.name);
#endif
    }

    private static int SeparateHoles(Tilemap source, TilemapCollider2D sourceCollider)
    {
        List<HoleCell> cells = new List<HoleCell>();
        foreach (Vector3Int position in source.cellBounds.allPositionsWithin)
        {
            TileBase tile = source.GetTile(position);
            if (tile == null)
                continue;
            if (IsRaisedSquareStructure(tile, source, position))
            {
                // Fill the whole structure cell, including the dark/starry part of its top.
                source.SetColliderType(position, Tile.ColliderType.Grid);
                continue;
            }
            if (!IsHoleTile(tile, source, position))
                continue;
            cells.Add(new HoleCell
            {
                Position = position,
                Tile = tile,
                Color = source.GetColor(position),
                Matrix = source.GetTransformMatrix(position),
                Flags = source.GetTileFlags(position),
                ColliderType = source.GetColliderType(position)
            });
        }
        if (cells.Count == 0)
        {
            RefreshSourceCollider(source, sourceCollider);
            return 0;
        }
        GameObject holder = new GameObject("GroundHoles", typeof(Tilemap), typeof(TilemapRenderer));
        holder.layer = source.gameObject.layer;
        holder.tag = source.gameObject.tag;
        holder.transform.SetParent(source.transform, false);
        Tilemap destination = holder.GetComponent<Tilemap>();
        destination.tileAnchor = source.tileAnchor;
        destination.orientation = source.orientation;
        destination.orientationMatrix = source.orientationMatrix;
        destination.color = source.color;
        destination.animationFrameRate = source.animationFrameRate;
        TilemapRenderer renderer = holder.GetComponent<TilemapRenderer>();
        TilemapRenderer sourceRenderer = source.GetComponent<TilemapRenderer>();
        if (sourceRenderer != null)
        {
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            renderer.sortingOrder = sourceRenderer.sortingOrder;
            renderer.mode = sourceRenderer.mode;
            renderer.sortOrder = sourceRenderer.sortOrder;
            renderer.maskInteraction = sourceRenderer.maskInteraction;
            renderer.enabled = sourceRenderer.enabled;
        }
        Vector3Int[] positions = new Vector3Int[cells.Count];
        TileBase[] tiles = new TileBase[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            positions[i] = cells[i].Position;
            tiles[i] = cells[i].Tile;
        }
        destination.SetTiles(positions, tiles);
        foreach (HoleCell cell in cells)
        {
            destination.SetTileFlags(cell.Position, TileFlags.None);
            destination.SetColor(cell.Position, cell.Color);
            destination.SetTransformMatrix(cell.Position, cell.Matrix);
            destination.SetColliderType(cell.Position, cell.ColliderType);
            destination.SetTileFlags(cell.Position, cell.Flags);
        }
        // A separate rigidbody prevents this collider from merging into a parent's wall composite.
        Rigidbody2D body = holder.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = sourceCollider.attachedRigidbody == null || sourceCollider.attachedRigidbody.simulated;
        TilemapCollider2D holeCollider = holder.AddComponent<TilemapCollider2D>();
        holeCollider.offset = sourceCollider.offset;
        holeCollider.sharedMaterial = sourceCollider.sharedMaterial;
        holeCollider.isTrigger = sourceCollider.isTrigger;
        holder.AddComponent<RangeProjectilePassThrough>();
        // Clear the originals only after the destination is complete.
        source.SetTiles(positions, new TileBase[cells.Count]);
        RefreshSourceCollider(source, sourceCollider);
        holeCollider.ProcessTilemapChanges();
        return cells.Count;
    }

    private static void RefreshSourceCollider(Tilemap source, TilemapCollider2D sourceCollider)
    {
        sourceCollider.ProcessTilemapChanges();
        CompositeCollider2D composite = source.GetComponent<CompositeCollider2D>();
        if (composite != null && composite.generationType == CompositeCollider2D.GenerationType.Manual)
            composite.GenerateGeometry();
    }

    private static bool IsRaisedSquareStructure(TileBase tile, Tilemap map, Vector3Int position)
    {
        if (HasSquareFrontPanel(tile.name, map, position))
            return true;
        Sprite sprite = map.GetSprite(position);
        return sprite != null && HasSquareFrontPanel(sprite.name, map, position);
    }

    private static bool HasSquareFrontPanel(string original, Tilemap map, Vector3Int position)
    {
        const string image = "BaseSquareWall_3x3_64px";
        string name = original.TrimStart('$');
        if (!name.StartsWith(image + "_", StringComparison.Ordinal))
            return false;
        int row = ReadIndex(name, "_r");
        int column = ReadIndex(name, "_c");
        if (row < 0 || row > 2 || column < 0 || column > 2)
            return false;
        // In this sheet row 2 is the raised front face, two cells below row 0.
        // The lower room's floor holes use only rows 0/1 and have no front face.
        Vector3Int frontPosition = position + new Vector3Int(0, row - 2, 0);
        TileBase frontTile = map.GetTile(frontPosition);
        if (frontTile == null)
            return false;
        string frontName = image + "_r2_c" + column;
        if (frontTile.name.TrimStart('$') == frontName)
            return true;
        Sprite frontSprite = map.GetSprite(frontPosition);
        return frontSprite != null && frontSprite.name.TrimStart('$') == frontName;
    }

    private static bool IsHoleTile(TileBase tile, Tilemap map, Vector3Int position)
    {
        if (IsHoleName(tile.name))
            return true;
        Sprite sprite = map.GetSprite(position);
        return sprite != null && IsHoleName(sprite.name);
    }

    private static bool IsHoleName(string original)
    {
        string name = original.TrimStart('$');
        foreach (string image in HoleImages)
        {
            if (name != image && !name.StartsWith(image + "_", StringComparison.Ordinal))
                continue;
            int row = ReadIndex(name, "_r");
            int column = ReadIndex(name, "_c");
            // The same sheets also contain raised front panels. Those stay solid.
            if (image == "BaseSquareWall_3x3_64px")
                return row >= 0 && row < 2;
            if (image == "BaseSquareWall_Extendable_3x4_64px")
                return row >= 0 && row < 3;
            if (image == "BaseCornerWall_Upper_3x4_64px")
                return row == 2 && column == 2;
            if (image == "BaseCornerWall_Lower_3x4_64px")
                return row >= 0 && row < 2 && column == 0;
            return true;
        }
        return false;
    }

    private static int ReadIndex(string name, string prefix)
    {
        int start = name.LastIndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
            return -1;
        start += prefix.Length;
        int end = start;
        while (end < name.Length && char.IsDigit(name[end]))
            end++;
        return int.TryParse(name.Substring(start, end - start), out int value) ? value : -1;
    }

    internal static void ReportBlockedCollider(Collider2D collider)
    {
#if UNITY_EDITOR
        if (collider == null || !ReportedColliders.Add(collider.GetInstanceID()))
            return;
        string path = collider.name;
        for (Transform parent = collider.transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        Debug.Log("[RangeProjectile] 탄환 적중 장애물: " + path + " / Layer: "
            + LayerMask.LayerToName(collider.gameObject.layer) + " / Tag: " + collider.tag, collider);
#endif
    }
}
