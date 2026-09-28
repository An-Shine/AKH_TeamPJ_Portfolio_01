using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the first desert-space-base prototype map from the generated 32 px atlas.
/// The menu item can be used to rebuild the layout after manual experimentation.
/// </summary>
[InitializeOnLoad]
public static class DesertSpaceBaseMapBuilder
{
    private const string ScenePath = "Assets/01.Scenes/03.GameScene.unity";
    private const string TexturePath = "Assets/04.Sprites/Tilemaps/DesertSpaceBase/DesertSpaceBase_Tilesheet_32px.png";
    private const string CornerTexturePath = "Assets/04.Sprites/Tilemaps/DesertSpaceBase/DesertSpaceBase_CornerTiles_32px.png";
    private const string RootFolder = "Assets/04.Sprites/Tilemaps/DesertSpaceBase";
    private const string TileFolder = RootFolder + "/Tiles";
    private const string CornerTileFolder = TileFolder + "/Corners";
    private const string PaletteFolder = RootFolder + "/Palette";
    private const string PalettePath = PaletteFolder + "/Desert Space Base Palette.prefab";
    private const string BuildMarkerName = "__DesertSpaceBaseGenerated";
    private static readonly string[] CornerSpriteNames =
    {
        "DSB_Corner_12_3",
        "DSB_Corner_3_6",
        "DSB_Corner_6_9",
        "DSB_Corner_9_12"
    };

    private static bool isBuilding;

    static DesertSpaceBaseMapBuilder()
    {
        EditorApplication.delayCall += TryInitialBuild;
    }

    private static void TryInitialBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryInitialBuild;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || (scene.path != ScenePath && scene.name != "03.GameScene"))
            return;

        GameObject existingGrid = GameObject.Find("Grid");
        if (existingGrid != null && existingGrid.transform.Find(BuildMarkerName) != null)
            return;

        BuildMap();
    }

    [MenuItem("Tools/Desert Space Base/Rebuild Prototype Map")]
    public static void BuildMap()
    {
        if (isBuilding)
            return;

        isBuilding = true;
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || (scene.path != ScenePath && scene.name != "03.GameScene"))
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EnsureFolder(TileFolder);
            EnsureFolder(CornerTileFolder);
            EnsureFolder(PaletteFolder);
            ConfigureAndSliceTexture();
            ConfigureAndSliceCornerTexture();

            Dictionary<Vector2Int, Tile> tiles = CreateTileAssets();
            Dictionary<int, Tile> cornerTiles = CreateCornerTileAssets();
            CreateOrUpdatePalette(tiles, cornerTiles);
            BuildSceneHierarchyAndLayout(tiles, cornerTiles);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            FrameMapInSceneView();
            Debug.Log("[DesertSpaceBase] Prototype Tilemap, Walls, Obstacles and palette were built successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            isBuilding = false;
        }
    }

    [MenuItem("Tools/Desert Space Base/Fix Vertical Wall Continuity")]
    public static void FixVerticalWallContinuity()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || (scene.path != ScenePath && scene.name != "03.GameScene"))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject floorObject = GameObject.Find("Grid/Tilemap");
        GameObject wallsObject = GameObject.Find("Grid/Walls");
        if (floorObject == null || !floorObject.TryGetComponent(out Tilemap floor))
            throw new InvalidOperationException("Grid/Tilemap was not found.");
        if (wallsObject == null || !wallsObject.TryGetComponent(out Tilemap walls))
            throw new InvalidOperationException("Grid/Walls Tilemap was not found.");

        EnsureFolder(CornerTileFolder);
        ConfigureAndSliceCornerTexture();
        Dictionary<Vector2Int, Tile> tiles = LoadTileAssets();
        Dictionary<int, Tile> cornerTiles = CreateCornerTileAssets();
        HashSet<Vector3Int> floorCells = new HashSet<Vector3Int>();
        foreach (Vector3Int position in floor.cellBounds.allPositionsWithin)
        {
            if (floor.HasTile(position))
                floorCells.Add(position);
        }

        walls.ClearAllTiles();
        BuildBoundaryWalls(walls, floorCells, tiles, cornerTiles);
        PlaceDoors(walls, tiles);

        EditorUtility.SetDirty(walls);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[DesertSpaceBase] Rebuilt only the Walls layer with seamless vertical wall tiles.");
    }

    [MenuItem("Tools/Desert Space Base/Render Map Preview")]
    public static void RenderMapPreview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || (scene.path != ScenePath && scene.name != "03.GameScene"))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject cameraObject = new GameObject("__DesertSpaceBasePreviewCamera");
        Camera previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 32f;
        previewCamera.transform.position = new Vector3(-0.5f, 17.5f, -10f);
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 1f);
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;

        RenderTexture renderTexture = new RenderTexture(1200, 900, 24, RenderTextureFormat.ARGB32);
        renderTexture.filterMode = FilterMode.Point;
        previewCamera.targetTexture = renderTexture;
        previewCamera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D output = new Texture2D(1200, 900, TextureFormat.RGBA32, false);
        output.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0);
        output.Apply(false, false);
        File.WriteAllBytes(RootFolder + "/DesertSpaceBase_MapPreview.png", output.EncodeToPNG());

        RenderTexture.active = previous;
        previewCamera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(output);
        UnityEngine.Object.DestroyImmediate(renderTexture);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        AssetDatabase.Refresh();
        Debug.Log("[DesertSpaceBase] Map preview rendered to " + RootFolder + "/DesertSpaceBase_MapPreview.png");
    }

    private static void ConfigureAndSliceTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException("Tilesheet was not imported.", TexturePath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        List<SpriteRect> rects = new List<SpriteRect>(256);
        for (int row = 0; row < 16; row++)
        {
            for (int column = 0; column < 16; column++)
            {
                rects.Add(new SpriteRect
                {
                    name = SpriteName(row, column),
                    rect = new Rect(column * 32, (15 - row) * 32, 32, 32),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                    spriteID = GUID.Generate()
                });
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void ConfigureAndSliceCornerTexture()
    {
        AssetDatabase.ImportAsset(CornerTexturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(CornerTexturePath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException("Corner tilesheet was not imported.", CornerTexturePath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        SpriteRect[] rects = new SpriteRect[CornerSpriteNames.Length];
        for (int index = 0; index < CornerSpriteNames.Length; index++)
        {
            rects[index] = new SpriteRect
            {
                name = CornerSpriteNames[index],
                rect = new Rect(index * 32, 0, 32, 32),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = GUID.Generate()
            };
        }

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Dictionary<Vector2Int, Tile> CreateTileAssets()
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(TexturePath)
            .OfType<Sprite>()
            .ToArray();

        Dictionary<string, Sprite> spritesByName = sprites.ToDictionary(sprite => sprite.name, sprite => sprite);
        Dictionary<Vector2Int, Tile> result = new Dictionary<Vector2Int, Tile>(256);

        for (int row = 0; row < 16; row++)
        {
            for (int column = 0; column < 16; column++)
            {
                string spriteName = SpriteName(row, column);
                if (!spritesByName.TryGetValue(spriteName, out Sprite sprite))
                    continue;

                string assetPath = $"{TileFolder}/{spriteName}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, assetPath);
                }

                tile.name = spriteName;
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                tile.flags = TileFlags.LockTransform;
                tile.colliderType = row <= 3 ? Tile.ColliderType.None : Tile.ColliderType.Sprite;
                EditorUtility.SetDirty(tile);
                result[new Vector2Int(row, column)] = tile;
            }
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    private static Dictionary<Vector2Int, Tile> LoadTileAssets()
    {
        Dictionary<Vector2Int, Tile> result = new Dictionary<Vector2Int, Tile>(256);
        for (int row = 0; row < 16; row++)
        {
            for (int column = 0; column < 16; column++)
            {
                string assetPath = $"{TileFolder}/{SpriteName(row, column)}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tile != null)
                    result[new Vector2Int(row, column)] = tile;
            }
        }

        return result;
    }

    private static Dictionary<int, Tile> CreateCornerTileAssets()
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(CornerTexturePath)
            .OfType<Sprite>()
            .ToArray();
        Dictionary<string, Sprite> spritesByName = sprites.ToDictionary(sprite => sprite.name, sprite => sprite);
        Dictionary<int, Tile> result = new Dictionary<int, Tile>(CornerSpriteNames.Length);

        for (int index = 0; index < CornerSpriteNames.Length; index++)
        {
            string spriteName = CornerSpriteNames[index];
            if (!spritesByName.TryGetValue(spriteName, out Sprite sprite))
                throw new InvalidOperationException($"Missing corner sprite: {spriteName}");

            string assetPath = $"{CornerTileFolder}/{spriteName}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            tile.name = spriteName;
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockTransform;
            tile.colliderType = Tile.ColliderType.Sprite;
            EditorUtility.SetDirty(tile);
            result[index] = tile;
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    private static void CreateOrUpdatePalette(Dictionary<Vector2Int, Tile> tiles, Dictionary<int, Tile> cornerTiles)
    {
        GameObject paletteAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        if (paletteAsset == null)
        {
            paletteAsset = GridPaletteUtility.CreateNewPalette(
                PaletteFolder,
                "Desert Space Base Palette",
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Manual,
                Vector3.one,
                GridLayout.CellSwizzle.XYZ);
        }

        if (paletteAsset == null)
            throw new InvalidOperationException("Could not create the Tile Palette prefab.");

        GameObject paletteRoot = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            Tilemap paletteTilemap = paletteRoot.GetComponentInChildren<Tilemap>(true);
            if (paletteTilemap == null)
                throw new InvalidOperationException("The generated Tile Palette has no Tilemap layer.");

            paletteTilemap.ClearAllTiles();
            for (int row = 0; row < 16; row++)
            {
                for (int column = 0; column < 16; column++)
                {
                    if (tiles.TryGetValue(new Vector2Int(row, column), out Tile tile))
                        paletteTilemap.SetTile(new Vector3Int(column, 15 - row, 0), tile);
                }
            }

            for (int index = 0; index < CornerSpriteNames.Length; index++)
                paletteTilemap.SetTile(new Vector3Int(index, -2, 0), cornerTiles[index]);

            paletteTilemap.CompressBounds();
            PrefabUtility.SaveAsPrefabAsset(paletteRoot, PalettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(paletteRoot);
        }
    }

    private static void BuildSceneHierarchyAndLayout(Dictionary<Vector2Int, Tile> tiles, Dictionary<int, Tile> cornerTiles)
    {
        GameObject gridObject = GameObject.Find("Grid");
        if (gridObject == null)
        {
            gridObject = new GameObject("Grid", typeof(Grid));
            Undo.RegisterCreatedObjectUndo(gridObject, "Create Desert Space Base Grid");
        }

        Grid grid = gridObject.GetComponent<Grid>();
        if (grid == null)
            grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        Tilemap floor = GetOrCreateTilemap(gridObject.transform, "Tilemap", 0);
        Tilemap walls = GetOrCreateTilemap(gridObject.transform, "Walls", 10);
        Tilemap obstacles = GetOrCreateTilemap(gridObject.transform, "Obstacles", 20);
        floor.ClearAllTiles();
        walls.ClearAllTiles();
        obstacles.ClearAllTiles();

        HashSet<Vector3Int> floorCells = new HashSet<Vector3Int>();
        AddRect(floorCells, -16, -12, 32, 24); // Central combat room
        AddRect(floorCells, -40, -12, 16, 24); // Left item room
        AddRect(floorCells, 24, -12, 16, 24);  // Right item room
        AddRect(floorCells, -17, 24, 34, 24);  // Boss room
        AddRect(floorCells, -24, -2, 8, 5);    // Left corridor
        AddRect(floorCells, 16, -2, 8, 5);     // Right corridor
        AddRect(floorCells, -3, 12, 7, 12);    // Boss corridor

        Tile[] sandVariants =
        {
            GetTile(tiles, 0, 0), GetTile(tiles, 0, 1), GetTile(tiles, 0, 2),
            GetTile(tiles, 0, 3), GetTile(tiles, 0, 4), GetTile(tiles, 0, 5),
            GetTile(tiles, 1, 0), GetTile(tiles, 1, 1), GetTile(tiles, 1, 2)
        };

        foreach (Vector3Int position in floorCells)
        {
            int hash = Mathf.Abs((position.x * 73856093) ^ (position.y * 19349663));
            int variant = hash % 17 == 0 ? 8 : hash % 11 == 0 ? 6 : hash % 7 == 0 ? 3 : 0;
            floor.SetTile(position, sandVariants[variant]);
        }

        BuildBoundaryWalls(walls, floorCells, tiles, cornerTiles);
        PlaceDoors(walls, tiles);
        PlaceObstacles(obstacles, tiles);

        floor.CompressBounds();
        walls.CompressBounds();
        obstacles.CompressBounds();

        Transform oldMarker = gridObject.transform.Find(BuildMarkerName);
        if (oldMarker == null)
        {
            GameObject marker = new GameObject(BuildMarkerName);
            marker.transform.SetParent(gridObject.transform, false);
            marker.hideFlags = HideFlags.HideInHierarchy;
        }

        EditorUtility.SetDirty(gridObject);
        EditorUtility.SetDirty(floor);
        EditorUtility.SetDirty(walls);
        EditorUtility.SetDirty(obstacles);
    }

    private static void BuildBoundaryWalls(
        Tilemap walls,
        HashSet<Vector3Int> floorCells,
        Dictionary<Vector2Int, Tile> tiles,
        Dictionary<int, Tile> cornerTiles)
    {
        Dictionary<Vector3Int, int> boundary = new Dictionary<Vector3Int, int>();
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        int[] floorDirectionBits = { 2, 1, 8, 4 };

        foreach (Vector3Int floorCell in floorCells)
        {
            for (int index = 0; index < directions.Length; index++)
            {
                Vector3Int candidate = floorCell + directions[index];
                if (floorCells.Contains(candidate))
                    continue;

                boundary.TryGetValue(candidate, out int mask);
                boundary[candidate] = mask | floorDirectionBits[index];
            }
        }

        foreach (KeyValuePair<Vector3Int, int> pair in boundary)
        {
            Tile wallTile;
            int mask = pair.Value;
            bool multipleSides = (mask & (mask - 1)) != 0;
            if (multipleSides)
                wallTile = GetInnerCornerTile(mask, tiles, cornerTiles);
            else if ((mask & 2) != 0) // Floor below: upper wall
                wallTile = GetTile(tiles, 4, 1);
            else if ((mask & 1) != 0) // Floor above: lower wall
                wallTile = GetTile(tiles, 7, 1);
            else if ((mask & 8) != 0) // Floor to the right: left wall
                wallTile = GetTile(tiles, 4, 3);
            else                       // Floor to the left: right wall
                wallTile = GetTile(tiles, 4, 4);

            walls.SetTile(pair.Key, wallTile);
        }

        PlaceOuterCorners(walls, floorCells, cornerTiles);
    }

    private static Tile GetInnerCornerTile(
        int floorDirectionMask,
        Dictionary<Vector2Int, Tile> tiles,
        Dictionary<int, Tile> cornerTiles)
    {
        bool floorAbove = (floorDirectionMask & 1) != 0;
        bool floorBelow = (floorDirectionMask & 2) != 0;
        bool floorLeft = (floorDirectionMask & 4) != 0;
        bool floorRight = (floorDirectionMask & 8) != 0;

        if (floorBelow && floorLeft)
            return cornerTiles[0]; // Arms point to 12 and 3.
        if (floorAbove && floorLeft)
            return cornerTiles[1]; // Arms point to 3 and 6.
        if (floorAbove && floorRight)
            return cornerTiles[2]; // Arms point to 6 and 9.
        if (floorBelow && floorRight)
            return cornerTiles[3]; // Arms point to 9 and 12.

        return GetTile(tiles, 4, 0);
    }

    private static void PlaceOuterCorners(
        Tilemap walls,
        HashSet<Vector3Int> floorCells,
        Dictionary<int, Tile> cornerTiles)
    {
        foreach (Vector3Int floorCell in floorCells)
        {
            bool upOpen = !floorCells.Contains(floorCell + Vector3Int.up);
            bool rightOpen = !floorCells.Contains(floorCell + Vector3Int.right);
            bool downOpen = !floorCells.Contains(floorCell + Vector3Int.down);
            bool leftOpen = !floorCells.Contains(floorCell + Vector3Int.left);

            // The corner sits diagonally outside the floor. Its arms point back to the two side walls.
            if (upOpen && rightOpen)
                SetCornerIfOutsideFloor(walls, floorCells, floorCell + Vector3Int.up + Vector3Int.right, cornerTiles[2]);
            if (rightOpen && downOpen)
                SetCornerIfOutsideFloor(walls, floorCells, floorCell + Vector3Int.right + Vector3Int.down, cornerTiles[3]);
            if (downOpen && leftOpen)
                SetCornerIfOutsideFloor(walls, floorCells, floorCell + Vector3Int.down + Vector3Int.left, cornerTiles[0]);
            if (leftOpen && upOpen)
                SetCornerIfOutsideFloor(walls, floorCells, floorCell + Vector3Int.left + Vector3Int.up, cornerTiles[1]);
        }
    }

    private static void SetCornerIfOutsideFloor(
        Tilemap walls,
        HashSet<Vector3Int> floorCells,
        Vector3Int position,
        Tile cornerTile)
    {
        if (!floorCells.Contains(position))
            walls.SetTile(position, cornerTile);
    }

    private static void PlaceDoors(Tilemap walls, Dictionary<Vector2Int, Tile> tiles)
    {
        Tile verticalDoor = GetTile(tiles, 8, 1);
        Tile horizontalDoor = GetTile(tiles, 8, 7);

        for (int y = -2; y <= 2; y++)
        {
            walls.SetTile(new Vector3Int(-24, y, 0), verticalDoor);
            walls.SetTile(new Vector3Int(23, y, 0), verticalDoor);
        }

        for (int x = -3; x <= 3; x++)
            walls.SetTile(new Vector3Int(x, 23, 0), horizontalDoor);
    }

    private static void PlaceObstacles(Tilemap obstacles, Dictionary<Vector2Int, Tile> tiles)
    {
        // Loot pedestals in the two item rooms.
        obstacles.SetTile(new Vector3Int(-32, 0, 0), GetTile(tiles, 9, 14));
        obstacles.SetTile(new Vector3Int(31, 0, 0), GetTile(tiles, 9, 14));

        // Sparse item-room storage and cover.
        StampAtlasRect(obstacles, tiles, -38, 7, 11, 0, 4, 2);
        StampAtlasRect(obstacles, tiles, -38, -9, 11, 0, 4, 2);
        StampAtlasRect(obstacles, tiles, 35, 7, 11, 0, 4, 2);
        StampAtlasRect(obstacles, tiles, 35, -9, 11, 0, 4, 2);
        obstacles.SetTile(new Vector3Int(-27, 7, 0), GetTile(tiles, 9, 1));
        obstacles.SetTile(new Vector3Int(-27, -7, 0), GetTile(tiles, 9, 3));
        obstacles.SetTile(new Vector3Int(26, 7, 0), GetTile(tiles, 9, 1));
        obstacles.SetTile(new Vector3Int(26, -7, 0), GetTile(tiles, 9, 3));

        // Central room: one major wreck and several separated cover islands.
        StampAtlasRect(obstacles, tiles, -3, -2, 13, 0, 4, 3);
        StampAtlasRect(obstacles, tiles, -13, 6, 11, 0, 4, 2);
        StampAtlasRect(obstacles, tiles, 9, -8, 11, 4, 4, 2);
        StampAtlasRect(obstacles, tiles, 8, 6, 11, 8, 4, 2);
        StampAtlasRect(obstacles, tiles, -12, -8, 11, 8, 4, 2);
        obstacles.SetTile(new Vector3Int(-8, 1, 0), GetTile(tiles, 9, 0));
        obstacles.SetTile(new Vector3Int(8, 1, 0), GetTile(tiles, 9, 2));
        obstacles.SetTile(new Vector3Int(0, 8, 0), GetTile(tiles, 10, 6));

        // Boss arena: sparse mirrored cover and a crashed command module at the far end.
        StampAtlasRect(obstacles, tiles, -3, 42, 13, 4, 4, 3);
        StampAtlasRect(obstacles, tiles, -13, 31, 11, 4, 4, 2);
        StampAtlasRect(obstacles, tiles, 9, 31, 11, 4, 4, 2);
        obstacles.SetTile(new Vector3Int(-11, 26, 0), GetTile(tiles, 10, 1));
        obstacles.SetTile(new Vector3Int(11, 26, 0), GetTile(tiles, 10, 1));
        obstacles.SetTile(new Vector3Int(-7, 37, 0), GetTile(tiles, 9, 4));
        obstacles.SetTile(new Vector3Int(7, 37, 0), GetTile(tiles, 9, 4));
    }

    private static void StampAtlasRect(
        Tilemap target,
        Dictionary<Vector2Int, Tile> tiles,
        int targetX,
        int targetY,
        int sourceTopRow,
        int sourceLeftColumn,
        int width,
        int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sourceRow = sourceTopRow + (height - 1 - y);
                int sourceColumn = sourceLeftColumn + x;
                target.SetTile(
                    new Vector3Int(targetX + x, targetY + y, 0),
                    GetTile(tiles, sourceRow, sourceColumn));
            }
        }
    }

    private static Tilemap GetOrCreateTilemap(Transform grid, string objectName, int sortingOrder)
    {
        Tilemap tilemap = grid.GetComponentsInChildren<Tilemap>(true)
            .FirstOrDefault(candidate => candidate.name == objectName);

        if (tilemap == null)
        {
            GameObject layer = new GameObject(objectName, typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(grid, false);
            Undo.RegisterCreatedObjectUndo(layer, $"Create {objectName} Tilemap");
            tilemap = layer.GetComponent<Tilemap>();
        }

        tilemap.transform.localPosition = Vector3.zero;
        tilemap.transform.localRotation = Quaternion.identity;
        tilemap.transform.localScale = Vector3.one;
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;
        return tilemap;
    }

    private static void AddRect(HashSet<Vector3Int> cells, int x, int y, int width, int height)
    {
        for (int iy = 0; iy < height; iy++)
        {
            for (int ix = 0; ix < width; ix++)
                cells.Add(new Vector3Int(x + ix, y + iy, 0));
        }
    }

    private static Tile GetTile(Dictionary<Vector2Int, Tile> tiles, int row, int column)
    {
        if (!tiles.TryGetValue(new Vector2Int(row, column), out Tile tile))
            throw new KeyNotFoundException($"Missing tile at atlas row {row}, column {column}.");
        return tile;
    }

    private static string SpriteName(int row, int column)
    {
        return $"DSB_r{row:00}_c{column:00}";
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void FrameMapInSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
            return;

        sceneView.orthographic = true;
        sceneView.pivot = new Vector3(0f, 17f, 0f);
        sceneView.size = 48f;
        sceneView.Repaint();
    }
}
