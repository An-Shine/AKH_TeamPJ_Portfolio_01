# 보스 시그니처 VFX 추가

이 파일은 test(5).unitypackage의 Monster 작업을 이어서 만든 보스 전용 업그레이드입니다.

## 새 이미지와 연결된 공격

| 리소스 | 실제 연결 위치 |
| --- | --- |
| Signature/signature_detonation.png | 폭발, 화염 기둥, 지면 파괴, 전장 폭발 |
| Signature/signature_cleave.png | 부채꼴 베기, 피자형 파동의 위험한 원호 |
| Signature/signature_lightning.png | StormPillars 번개 기둥 |
| Signature/signature_collapse.png | GravityCollapse 중력 붕괴 |

이미지는 내장 image_gen 도구로 새로 제작했습니다. 원본 RGBA와 투명한 구멍을 보존했습니다. 프롬프트는 SignatureVfx_Prompts.json에 기록했습니다. 기존 외부 리소스 50종의 출처와 고지는 ResourceProvenance.json / THIRD_PARTY_NOTICES.md를 유지합니다.

## 연출

- 새 이미지는 SpriteRenderer 풀을 사용하고, 공격 메시와 동일한 XY 좌표 및 Quaternion 각도로 표시합니다.
- 폭발과 베기는 0.05초 부근에 크기가 빠르게 치솟고, 짧게 수축하면서 알파가 제곱 곡선으로 사라집니다.
- 번개는 0.065초 뒤에 두 번째 전기 선을 더하고, 충격파와 베기 잔상에도 시간차를 적용합니다.
- 중력 붕괴는 투명한 중심을 유지하는 보라·청록 원형 이미지가 빠르게 안쪽으로 수축합니다.
- 중요한 예고선과 탄환은 장식 이미지보다 위에 표시합니다. 원호의 양끝은 안전한 틈을 가리지 않도록 이미지 높이를 제한합니다.
- 카메라 충격은 보스 코드 안에서 공격 종류에 따라 짧고 강하게 조정합니다. 전역 Time.timeScale과 플레이어 이동에는 연출용 정지를 적용하지 않습니다.

## 피자형 공격 조준

ClosingRing.Angle은 공격 방향이 아니라 안전한 틈의 방향입니다. 모든 파동 호출을 공격 중심각으로 통일하고, 내부에서 안전한 틈을 180도 반대편에 배치했습니다. SerpentWave에서 이미 더하던 180도는 제거하여 두 번 뒤집히지 않도록 했습니다.

근접 부채꼴과 파동의 방향은 플레이어의 현재 위치를 사용합니다. 가까운 플레이어가 보스를 가로지를 때 미래 위치 예측 때문에 반대쪽을 겨누는 현상도 방지합니다. 예고 중에는 회전 속도 제한으로 추적하고, 기존 TargetLockTime 직전부터는 방향을 고정합니다. 발사체의 별도 예측 조준은 유지합니다.

이 프로젝트는 Unity Editor 실행 환경이 제공되지 않았습니다. 실제 Monster 소스를 Unity API 대체 환경에서 컴파일하고 패턴·기하·스타일을 검사했습니다. 실제 플레이 화면과 GPU 셰이더는 Unity에서 최종 확인해야 합니다.
