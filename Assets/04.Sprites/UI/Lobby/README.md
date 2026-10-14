# Lobby panel sprites

All text, icons, thumbnails, WeaponList and buttons are separate Unity UI objects.

- Lobby_Status_Frame: exploration summary, upper left.
- Lobby_Equipment_Frame: equipment container, lower left; place one WeaponList and your upgrade button inside.
- Lobby_Mission_Frame: central mission container; place banner, TMP texts and your contract selection button inside.
- Lobby_Preparation_Frame: right preparation container.
- Lobby_Log_Frame: bottom log strip; also reusable for inner status/objective boxes with Image Type Sliced.

Import settings: Sprite Single, Point, uncompressed; nine-slice borders are configured.
Use Image Type Sliced, white Image Color, and disable Raycast Target for decorative panels.
The interiors are dark-filled; outside corners are transparent.
