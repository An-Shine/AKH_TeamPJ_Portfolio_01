Abandoned Planet ground-detail overlay

Main layout image
- AbandonedPlanet_GroundOverlay_113x66.png
- Canvas: 3616 x 2112 px
- Grid: 113 x 66 cells
- Cell size: 32 x 32 px
- Matching Tilemap origin: (-51, -14)

Unity import
1. Texture Type: Sprite (2D and UI)
2. Sprite Mode: Multiple
3. Pixels Per Unit: 32
4. Filter Mode: Point (no filter)
5. Compression: None
6. Sprite Editor > Slice > Grid By Cell Size > 32 x 32
7. Pivot: Center
8. Keep Empty Rects: Off

Placement
- Create GroundDetails as a child of the existing Grid.
- Set Tilemap Renderer Sorting Order to 1.
- Put the sliced sprites in an empty palette while preserving their sheet layout.
- Stamp the full selection so its bottom-left cell starts at (-51, -14).
- Floor remains at order 0; Walls remain at order 2; Obstacles and Doors remain at order 3.

Files
- AbandonedPlanet_GroundDetails_Atlas.png: reusable individual detail source atlas.
- AbandonedPlanet_GroundOverlay_Preview.png: placement preview only; do not import as the gameplay overlay.
- AbandonedPlanet_GroundOverlay_Layout.txt: generated placement coordinates.
