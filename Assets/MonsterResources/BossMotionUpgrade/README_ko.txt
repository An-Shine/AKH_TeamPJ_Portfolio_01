보스 본체 모션 업그레이드

test(4).unitypackage의 보스 구조를 기준으로 만든 추가 패치입니다.
이전에 받은 BossMonster_VFX_Upgrade.zip과 함께 사용할 수 있습니다.
이번 ZIP에는 보스 본체 모션 패치만 들어 있습니다.

적용
1. ZIP을 풀고 Assets 폴더의 내용을 Unity 프로젝트 Assets에 같은 경로로 복사합니다.
2. 기존 파일 교체 대상은 Assets/02.Scripts/Monster/BossMonster/BossAnimationDriver.cs 한 개입니다.
   팀에서 이 파일을 추가 수정했다면 ZIP에 포함된 BossAnimationDriver.patch를 먼저 확인하고 병합하세요.
   .meta 파일도 함께 적용하여 새 스프라이트와 클립의 GUID를 유지하세요.
3. Unity의 Tools > Monster > Boss Motion Upgrade > Validate Resources를 실행합니다.
4. Play에서 이동, 8방향 전환, 긴 공격 예고, 발동, 회복, 피격, 사망을 확인합니다.
5. 실제 저장소에서 정해진 Husky 검사와 커밋 훅을 실행합니다.

추가 내용
- 스프라이트 PNG 245개: Blade 62 / Slam 62 / Core 64 / Walk 57.
- 애니메이션 클립 104개: 준비 24 / 대기 24 / 발동 24 / 회복 24 / 보행 8.
- 기존 보스 디자인을 참고한 칼날 동작, 무거운 내려치기, 코어 반동, 보행 포즈.
- 각 보스 인스턴스에 별도 AnimatorOverrideController를 연결합니다.
- 기존 30개 공격 패턴은 칼날, 내려치기, 코어 동작군으로 연결됩니다.
- 긴 예고 시간은 준비 자세를 유지하고, 발동 시 공격 프레임을 처음부터 재생합니다.
- 방향을 바꿀 때 보행·지속 동작의 진행률을 유지합니다.
- Point 필터, 압축 없음, Mip Map 없음. 방향별 PPU와 피벗으로 원본 크기·발 기준을 맞췄습니다.
- 겹쳐진 준비 포즈 4개는 제외하고 해당 위치에서 앞의 정상 포즈를 유지합니다.

변경 범위
보스 코드와 Assets/MonsterResources/BossMotionUpgrade만 포함합니다.
Player, Item, UI, 일반 몬스터, 기존 이미지, 보스 프리팹, 공유 Animator Controller,
씬, 프로젝트 설정, 피해·충돌·이동·공격 판정 코드는 원본 그대로입니다.
이전 VFX 패치 파일도 그대로 유지합니다.

검증
원본 3,038개 파일의 범위와 메타데이터, 프레임 픽셀, PNG 무결성, 클립 참조,
8방향 연결, C# 컴파일 및 애니메이션 API 시뮬레이션을 검사했습니다.
시뮬레이션에는 실제 30개 패턴 설정과 원본 Animator 상태·클립 정보를 사용했습니다.
이 환경에는 Unity Editor와 실제 Git 저장소의 .husky 설정이 없습니다.
Unity Play 및 실제 Husky 실행을 통과했다고 주장하지 않습니다.
세부 결과는 ValidationReport.json에 들어 있습니다.

Preview/BossMotion_Preview.gif는 포함된 스프라이트의 포즈 미리보기입니다.
실제 Unity 전투 녹화가 아닙니다. 이펙트가 합성된 최종 화면은 Play에서 확인하세요.

이미지 제작
기존 보스 리소스를 디자인 참조로 사용하고 built-in image_gen 모드로 새 포즈를 생성했습니다.
최종 프롬프트 세트는 Prompts.json, 출처·픽셀 추출 정보는 ArtworkProvenance.json에 기록했습니다.
프레임 PNG는 원본 생성 이미지의 픽셀을 그대로 잘라 투명 여백만 추가했습니다.
원본 이미지를 다시 그리거나 생산용 프레임을 확대·축소하지 않았습니다.
