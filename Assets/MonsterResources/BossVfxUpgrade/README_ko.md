# BossMonster VFX Upgrade

`test(4).unitypackage`의 보스 전용 패치입니다. 기존 보스 연결을 유지하면서 50개 텍스처와 1,266개 애니메이션 프레임을 패턴별로 합성합니다. 실제 플레이 영상 또는 측정된 10배 성능·품질 수치는 아닙니다.

## 적용

1. 팀 작업 중인 Unity 프로젝트에서 `Assets > Import Package > Custom Package`로 `BossMonster_VFX_Upgrade.unitypackage`를 가져옵니다.
2. 패키지의 항목을 모두 Import합니다. 기존 보스 코드 6개의 GUID는 유지합니다. 새 전용 리소스가 빠지면 Console에 누락 경로가 표시됩니다.
3. `Tools > Monster > Boss VFX Upgrade > Validate Resources`를 실행해 텍스처 설정·셰이더·기존 보스 연결을 확인합니다.
4. `Tools > Monster > Boss VFX Upgrade > Resource Gallery`에서 원본 프레임을 확인하고, Play에서 실제 보스 합성·타이밍·프레임 시간을 확인합니다.

이미 수정한 팀의 보스 코드가 있다면 아래 6개 파일의 변경을 비교·병합하세요. 이 패키지의 기준 버전은 제공된 `test(4).unitypackage`입니다.

## 작업 범위

기존 파일 변경은 `Assets/02.Scripts/Monster/BossMonster/`의 다음 6개뿐입니다.

- `BossBattleEffects.cs`: 보스 전용 리소스 풀과 패턴별 연출 연결.
- `BossBattleWorld.cs`: 경고·투사체의 시각 요소, 패턴별 이펙트 분기.
- `BossDamageZone.cs`: 경고선·디테일·사전 연출의 수명 관리.
- `BossShapeVisual.cs`: 윤곽선·경고 눈금·진행 아크.
- `BossVisualPolish.cs`: 보스 전용 몸체·림 셰이더와 전용 잔광 연결.
- `BossPatternExecutor.cs`: ExecutionNova의 중복 중앙 폭발만 생략. 전용 전장 폭발이 안전지대 주변에서 재생되며 기존 음향·카메라·애니메이션 호출은 유지.

신규 코드도 위 보스 폴더 안에만 있으며, 신규 리소스·문서는 `Assets/MonsterResources/BossVfxUpgrade/` 안에만 있습니다. Player·Item·UI·일반 몬스터 코드·기존 프리팹·씬·전투 설정·기존 메타 파일은 원본 해시와 동일합니다. 데미지·충돌 판정·공격 범위·패턴 스케줄 값은 유지합니다.

## 패턴별 변화

| 패턴 | 강화 연출 |
| --- | --- |
| CrescentCleave / DoubleClaw / RotatingScythes | 실제 부채꼴 각도에 맞는 칼날 아크, 흰색 날, 적중 섬광, 방향성 파편 |
| RiftLanes / SerpentWave / AlternatingMaze | 지면 균열, 돌 파편, 빔 윤곽과 경고 눈금 |
| HellfireRain / MeteorLattice | 충돌 직전 낙하 화염, 불기둥, 다중 폭발, 재·불티 |
| CrossSweep / SanguineLances | 전기 빔, 펄스 표면, 전장 안의 점광과 스파크 |
| GroundSlam / ThornFan / RadialSpikes / TrackingEruption | 지면 분출, 흙 충격파, 균열, 잔해 |
| ExpandingNova / TwinNova | 위험 구간만 따라가는 선명한 노바 아크와 스파크; 회피 틈 유지 |
| SpiralBarrage / TargetedVolley / HomingOrbs / EchoTurrets / OrbitRelease / PerimeterBarrage | 흰색 중심과 어두운 외곽의 투사체, 플라스마 잔상, 발사 섬광 |
| Cataclysm / ApocalypseSequence / BurrowAmbush / DashRend | 짧은 중심광, 폭발 시트, 두 겹 충격파, 파편과 잔광 |
| BloodPools | 별도 혈색 바닥 효과와 수렴 입자 |
| StormPillars | 분기 번개, 전용 낙뢰 애니메이션, 전기 구체, 접지 아크 |
| GravityCollapse | 안쪽으로 수렴하는 와류·붕괴 애니메이션과 역방향 충격파 |
| ExecutionNova | 안전 원의 전체 시각 면적을 비우는 전장 폭발, 안전 경계 최상단 표시 |

경고 경계는 실제 판정 수치를 사용하며 이펙트 위에 표시됩니다. 밝은 중심은 짧게, 색상·어두운 윤곽·파편은 서로 다른 층으로 배치했습니다. Point 필터·무압축·Mip Map OFF·원본 해상도 유지로 텍스처 선명도를 보존합니다.

## 수명·부하 제어

보스별 50개 파티클 시스템을 재사용하며 텍스처별 상한, 프레임별 중요 입자 128개·장식 입자 320개 상한을 둡니다. 링·번개·균열은 192개 슬롯과 재사용 메시 2개로 관리합니다. VFX는 별도 난수를 사용하며 Time.timeScale을 변경하지 않습니다. 일시 정지에서 입자 발행과 메시 시간이 멈춥니다. 추가 콜라이더·피해 호출·전역 후처리를 만들지 않습니다.

압축하지 않은 텍스처 원본의 RGBA 합계는 약 73 MiB입니다. 실제 GPU 메모리와 프레임 시간은 플랫폼·렌더 파이프라인·전투 밀도에 따라 Play/Profiler에서 확인해야 합니다.

## 검증 범위와 남은 확인

제공된 모든 원본 파일의 SHA-256을 비교했고, 위 6개 외의 파일 및 기존 메타 데이터가 보존되었음을 확인했습니다. C# 컴파일러와 Unity API 계약 테스트에서 30개 실제 패턴 스케줄의 이펙트 호출, 50개 리소스 로딩, 번개 분기, 안전지대, 정지, 별도 난수, 메시 상한을 확인했습니다.

이 작업 환경에는 Unity Editor가 없어 실제 Unity 컴파일·셰이더 GPU 컴파일·Play 화면·실제 프레임 시간을 실행하지 못했습니다. 포함된 검사 메뉴와 Unity Play 확인이 필요합니다. 첨부에는 `.husky`·Git 저장소·프로젝트 lint 설정이 없어 실제 Husky hook 실행도 수행할 수 없었습니다. 보스 범위·GUID·추가 코드 공백·줄바꿈을 별도로 검사했으며 hook 통과로 표현하지 않습니다. 팀 저장소에 병합한 뒤 해당 저장소의 기존 Husky 절차를 실행하세요.

리소스 출처는 `THIRD_PARTY_NOTICES.md`, 파일별 원본 및 SHA-256은 `ResourceProvenance.json`에 있습니다. 이 문서는 Player·Item·UI 또는 공유 프로젝트 설정 변경을 요구하지 않습니다.
