# 사막 구조물 스프라이트

각각 별도 투명 PNG로 제작. 저장 위치: Assets/04.Sprites/Tilemaps/Obstacles.

| 파일 | 칸 크기 | 픽셀 크기 | 분할 수 |
|---|---|---|---|
| DesertCrashedShip_4x3_64px.png | 4×3 | 256×192 | 12 |
| DesertSatelliteWreck_3x3_64px.png | 3×3 | 192×192 | 9 |
| DesertImpactCrater_4x3_64px.png | 4×3 | 256×192 | 12 |
| DesertSandDune_4x2_64px.png | 4×2 | 256×128 | 8 |
| DesertRockPillars_3x3_64px.png | 3×3 | 192×192 | 9 |
| DesertCommsWreck_3x3_64px.png | 3×3 | 192×192 | 9 |

Unity 설정: Sprite / Multiple, PPU64, Point, Compression None, Mip Maps 비활성화, Full Rect. 64px 격자로 분할 완료.

분할된 스프라이트를 Tile Palette에 등록하고, 구조물 전체 영역을 선택해서 배치한다. 스프라이트 이름의 r0는 최상단, c0는 왼쪽 끝이다. 전체 좌우 8px는 투명하며, 본체 또는 모래의 접지 부분이 캔버스 하단에 닿는다. 주변의 검은 배경이나 흐린 효과는 포함하지 않는다.

이미지 해상도·알파·분할 개수·좌우 여백·하단 위치 및 확대 미리보기 확인 완료. 실제 씬 배치 및 충돌 영역은 미설정.
