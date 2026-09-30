Collapsed ground chasm set

Contents
- Four independent 320 x 320 transparent PNG sprites
- One combined 672 x 672 transparent sheet
- One preview image over the approximate desert-floor color

Unity import
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single for the four individual files
- Pixels Per Unit: 32
- Filter Mode: Point (no filter)
- Compression: None
- Mesh Type: Full Rect

Suggested scene setup
- Put the sprites under a CollapsedChasms parent object.
- Use the same Sorting Layer and Sorting Order as Walls (currently order 2).
- Keep collision separate from the artwork.
- Add PolygonCollider2D or stepped BoxCollider2D shapes over the dark opening.
- If several collider pieces are used, combine them with a Static Rigidbody2D and CompositeCollider2D.
- Keep roughly a three-cell-wide blocked band so the visual opening and collision agree.

Sheet slicing
- Cell size: 320 x 320
- Padding: 32 x 32
- Offset: 0 x 0
