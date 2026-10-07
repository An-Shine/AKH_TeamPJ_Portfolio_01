# 사막 절벽 타일

64×64px, PPU 64. 투명 PNG, Multiple, Point, Full Rect, 압축 없음, Mip Map 비활성화.

## 바깥 경계 시트 (192×192px)

| 왼쪽 | 가운데 | 오른쪽 |
|---|---|---|
| Outer_NW | Edge_N | Outer_NE |
| Edge_W | Sand_Center | Edge_E |
| Outer_SW | Edge_S | Outer_SE |

이름 앞에는 `Cliff_` 접두사가 붙는다. 직선 타일을 반복하고 방향에 맞는 모서리를 연결한다. 아래쪽은 암벽 앞면이 두껍게 보이며, 위쪽은 얇은 절벽 턱으로 표현한다. 단순 회전해서 다른 방향으로 사용하지 않는다.

## 안쪽 경계 시트 (128×128px)

| 왼쪽 | 오른쪽 |
|---|---|
| Inner_VoidSE | Inner_VoidSW |
| Inner_VoidNE | Inner_VoidNW |

위 표는 파일 안의 분할 순서다. 사용자 스케치의 3×3 배치를 기준으로 수정했으며 기존 스프라이트 이름과 ID는 유지했다. 이름을 기준으로 배치할 때는 아래처럼 사용한다.

| 왼쪽 | 가운데 | 오른쪽 |
|---|---|---|
| Inner_VoidNW | Edge_N | Inner_VoidNE |
| Edge_W | Sand_Center | Edge_E |
| Inner_VoidSW | Edge_S | Inner_VoidSE |

Inner는 사용자가 표시한 칸 안의 ㄱ자 절벽 경로를 따른다. 모래는 중앙 지면 쪽에만 남기며 절벽 바깥쪽 모래 덩어리는 제거하고 투명하게 비웠다. 수정 배치 미리보기: `tmp/imagegen/DesertCliff_Inner_UserLayout_4x.png`.

## Unity 배치

PNG의 분할된 스프라이트를 Tile Palette로 드래그해 Tile 에셋을 만들고, 사막 외곽의 경계 칸에 배치한다. 자동 연결 Rule Tile 설정은 포함하지 않았다. 기존 사막 바닥은 내부에 사용하고 경계 칸은 절벽 타일로 교체한다. 절벽 바깥 투명 부분에는 기존 바닥을 칠하지 않는다. 이동 차단은 별도 Collider Tilemap에서 설정한다.

5×5칸으로 확장한 반복 미리보기는 `tmp/imagegen/DesertCliff_RepeatPreview_3x.png`에 있다. PNG 규격과 분할 및 반복 미리보기는 확인했으며, Unity 실제 씬에서 기존 바닥과의 접합 및 콜라이더는 아직 검증하지 않았다.
