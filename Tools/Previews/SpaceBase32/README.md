# SpaceBase native 32px tiles

Runtime atlas: `Assets/04.Sprites/Tilemaps/Map/SpaceBase_Tileset_32px_v1.png`

- Native size: **256 x 160 pixels**, 8 columns x 5 rows.
- Cell size: **32 x 32 pixels**, no spacing, padding or offset.
- 28 occupied cells, each already registered as a sprite in the adjacent `.meta`.
- PPU 32, Multiple, Full Rect, Point, Clamp, no mipmaps, no compression.
- All pixel artwork is drawn at native resolution; only previews use nearest-neighbor enlargement.
- North straight walls and NW/NE corners each occupy **one column x three rows**.
- Floor variants occupy one cell. Dust is a four-cell **2 x 2 set**.
- Side and south wall strips are **6 pixels thick**. Draw floor underneath their transparent interior areas on a separate Tilemap.
- The wall strips reaching the cell borders are intentional; they are not supposed to fill the whole 32px cell with metal.
- Upper/lower corners are square 90-degree joins. No diagonal corner bevels.

## Atlas layout (top-left origin; zero based)

| Row | C0 | C1 | C2 | C3 | C4 | C5 | C6 | C7 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Light top | Electrical top | Divider top | NW top | NE top | empty | empty | empty |
| 1 | Light middle | Electrical middle | Divider middle | NW middle | NE middle | empty | empty | empty |
| 2 | Light bottom | Electrical bottom | Divider bottom | NW bottom | NE bottom | empty | empty | empty |
| 3 | Floor plain | Floor drain | Dust TL | Dust TR | West plain | West vent | SW | SE |
| 4 | empty | empty | Dust BL | Dust BR | East plain | East vent | South | empty |

The two east strips are mirrored variants supplied for convenience. The south strip completes the lower corners.

If re-slicing manually, use Grid by Cell Size **32,32**, offset **0,0**, padding **0,0**. North pieces must retain their top/middle/bottom order. Do not import the enlarged preview as a tileset.

## Verification

`tools/build_spacebase_tiles.py` checks image dimensions, opacity, shared boundary colors, corner alpha geometry and side-wall repeat joins. See `validation.json` for the latest run. `Room_3x.png` assembles the actual sprites to inspect all four corners. Unity rendering itself has not been run by this script.

The earlier Draft_v1 and Draft_v2 images are unrelated generated concept sheets; use the new native-32px file.
