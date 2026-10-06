# 2026-10-06 주행·보스·레벨·드롭·추격 팔

- Mode: Implementation / Verification. 요구사항과 3개 추가 질문의 답을 반영한 TaskBrief를 제시하고 사용자 `ㄱㄱ`로 승인받았다.
- Risk: High. 카메라 부모/월드 원점, 보스 충돌·공격, 런 성장, 보상 지급 시점 및 Junmo HUD/프리팹을 함께 변경한다.
- Goal: 앞으로 이동하는 화면에서도 벨트·전투·스폰·전환이 이어지고, 성장/드롭/추격 상태를 씬 제작 표시로 전달한다.
- Status: complete — 승인된 코드·씬·프리팹과 격리 검증을 완료했다. 전체 플레이/빌드와 체감 확인은 아래 미실행 항목으로 구분한다.

## 승인 범위와 변경 이유

- Train/TrainDriveState: 받아들인 피격 후 0.2초 가속 제한, Shift 유지 자동 재개, 해제 후 감속 지연0.5초. 레벨당 기본/일반 최고속도+20. 기차 보스 접촉은 연료 즉사 피해 없이 질주 취소·지속 가속 차단·70/초 감속. 일반 질주 무적은 유지한다.
- TrainController/ForwardCameraFollow: D의 앞쪽 한계 제거, 기존 앞쪽 경계 이후 전진만 하는 씬 제작 카메라 부모. 뒤벽은 현재 카메라 전진량+기존 minX이며 기존 자식 카메라 흔들림을 유지한다.
- Spawner/EventObjectSpawner/StageManager/AutoScrollBackground: 생성·터널·리셋·새 배경에 카메라 원점을 반영하고 viewport 커버리지를 유지한다. 기존 조우 거리57,600/이벤트25·50·75%/15분 규칙과 보스전 거리·타이머 정지는 유지한다.
- EyeBoss/EyeBossBelt/Tentacle: 하나의 Enemy/체력/보상과 하나의 반복 collider에 4개 순수 renderer를 붙인다. 기존 배경 기준 CurrentSpeed/10으로 반복한다. 저체력 무적·전체 공격을 제거하고 화면 안에서3초 예고→촉수1개 타격→정리 후 다음 공격을 예약한다. 경고와 타격은 같은 월드 지점이며 화면을 벗어난 경고는 취소한다.
- TrainBoss: prefab scale3→6. 물리 접촉에서7world/s로 기차를 밀고 Train의 접촉 제한을 요청한다. 플레이어 공격에 따른 기존 보스 넉백은 유지한다. phase 교체/다중 collider/비활성화/사망에서 접촉을 정리하며 사망 시작에 두 phase collider를 끈다.
- TrainLevelManager/Gun/LevelUI/두 살점 이벤트: 자동 레벨 제거 직전0d88ce1의 XP 지급/곡선/경험치 벽과 CurrentLevel×2 공격력 공식을 복원한다. 기존 Gun 적용 범위와 영구 기본총 보너스를 유지한다. XP는 처치즉시, 자원은 흡수시 지급한다. 살점 이벤트는 AddFlesh로 분리하고 자동 아이템 선택창 대신 현재 F 작업대를 유지한다.
- Enemy/FuelBarrel/RewardPickup: 기존 roll의 확률·총량을 각각 하나의 살점/영혼 객체에 담는다. 살점은 기존5개 sprite 랜덤·선로 낙하·약한 탄성/화면 테두리 반사, 0.5초 뒤 흡수. 영혼은 상승 뒤 trail과 함께 흡수. 배럴은 갈색이며 처치시 연한 갈색 네모의 흡수로 최대연료10%를 회복한다. Pause/Event/전환에서 정지, Die에서 미수령 폐기, Ending에서 미수령 살점·영혼을 한 번 정산한다.
- PursuingHand: 기존 팔을 계속 활성화하며 런 시작 기본속도320+완료 구간당30으로 추격한다. 레벨은 팔 속도를 올리지 않는다. Playing/Boss에서 주행속도 차이·실제 기차 x 변위를 적분하고 전환 시 간격을 유지한다. 접촉은 기존 끌려가기/게임오버 경로로 연결한다.
- SpeedMeterUI/SimpleSpeedUI/StageProgressUI/AccelerationWindEffect/Junmo: ¼원 계기판(기본45°/일반 최고90°/질주±5°), 왼쪽 세로 연료Fill, 좌상단Lv.n/녹색XP Fill, 팔 아이콘/붉은 경로, 가속량에 따른 흰 직선 이펙트를 제작한다. 숫자 UI는 속도만 읽고 애니메이션은 Train이 소유한다. 런타임 UI 계층 자동 생성이나 새 Manager/DDOL은 없다.

관련 적 prefab39개와 Junmo를 격리 Unity Editor API로 제작했다. 이식은 기존 serialized 문서66개와 새 문서61개에 한정했고 관련 없는 정규화는 제외했다. 새 자산은 Gameplay의 Pickup3종과 Driving의 간단한 도형 sprite/material이며 기존 GUID를 교체하지 않는다. 원본 Packages/ProjectSettings/Input/Tags/Layers/저장 형식과 생성 csproj는 수정하지 않는다.

## 검증과 수정 근거

- 전체 현재 runtime142개와 Editor/fixture10개를 Unity/package 어셈블리에 연결한 최종 MSBuild 소스 컴파일 오류0. 원본 Unity 생성 csproj를 수정하지 않고 외부 검사 프로젝트에 새 소스도 명시했다.
- 실제 순수 주행/팔 규칙18개, XP 경계8개, 보상30,000개 roll 조합과 Pickup 지급/정리 메서드5개 사례를 실행했다. 최소 stub 메서드 검사는 Unity 물리/렌더 검증을 대신하지 않는다.
- 격리 Unity6000.2.3f1에서 실제 임포트·씬/프리팹 제작을 완료했고 참조·레이아웃113개 검사와1280×720 UI-only RenderTexture 이미지를 생성·시각 확인했다. 이 이미지는 production world renderer의 검증이 아니다.
- 최초 native Play Mode에서 Rigidbody2D.simulated=false인 드롭의 body.position 대입이 Transform을 이동시키지 않는 것을 확인했다. 비시뮬레이션 상태에는 Transform.position을 쓰도록 수정했고 재실행에서3종 이동·흡수·1회 지급이 통과했다.
- 다음 native 실행에서 단축한 촉수 예고를 늦게 관측한 fixture의 시간 검사가 실패했다. 실제3초 예고를 생성 직후부터 관측하도록 검사 도구를 고쳤으며 게임 경고 시간을 바꾸지 않았다.
- 마지막 native Play Mode54개 검사 통과, runtime exception/error0. 실제 입력·Update/FixedUpdate로 Shift 유지/피격0.2초/해제0.5초 지연,3종 드롭 Transform 이동·흡수·1회 지급·Pause 정지·Ending 정산, XP/기본·최고속도+20/팔의 런 시작 기준, D/카메라/뒤벽, 실제 터널 전환·다음 구간·팔 간격 유지, 물리 기차 보스 접촉·질주 중단·밀기·감속·해제, 저체력 눈 보스의 연속 피격·단일HP, 촉수3초 경고·공격·정리3회/중첩0, 팔 접촉 게임오버를 확인했다. 실제 바람 particle 생성·흰색 직선·감속 시 길이 감소도 통과했다. 화면 밖에서 생성한 바람은 AlwaysSimulate로 계속 이동한다.
- fixture는 일반/이벤트 스폰과 자동 무기를 잠시 비활성화하고 초기 상태를 직접 배치한다. 정확한 연료 흡수량 검사 때 Train 연료 소모를 잠시 끄며, 드롭 중력과 촉수의 공격/정리 길이·플레이어 피해만 fixture에서 조정했다. 촉수 예고3초는 실제 설정을 유지한다. 따라서 드롭 낙하·탄성의 전체 체감, 전체 보스 생성·공격력 피해·넉백 전투를 이 검사 하나로 검증했다고 주장하지 않는다.
- 최종 runtime142개 및 변경 자산63개의 원본/검증 copy SHA256 일치, Assets meta2992개의 GUID 중복0, 갱신 문서 링크 오류0, 변경 공백 검사 통과. UI-only 이미지는 바람 설정 변경 전의 동일 HUD를 촬영한 것이며 production world 렌더 검증과 구분한다.

검사 도구·중간 실패 로그·이식 보고서는 `outputs/drive-boss-validation/`에 보관한다. 원본 프로젝트 Editor가 열려 있어 원본 batchmode/PlayMode는 실행하지 않았다. 격리 copy는 별도 productName/persistentDataPath로 원본 영구 강화 저장을 사용하지 않는다.

## 남은 확인 / Doc Impact Check

- 15분 전체 플레이·실제 사용자 키보드/해상도 변경·플레이어 빌드는 미실행이다. 조준하며 드롭·경고를 피하는 완전한 전투 체감과 이펙트 밀도는 수동 확인 대상이다.
- 기존 BGM_Boss 미등록과 씬 계층의 DontDestroyOnLoad 경고는 오디오/기존 bootstrap 설정의 별도 확인 대상이다. 이번 변경에서 해당 설정을 바꾸지 않았다.
- SessionLog, CoreRuntimeGameFlow/ItemsInventoryWeapons/EnemySpawnBossFlow StructureMemory, DecisionLog, ErrorLog를 갱신한다. RefactorLog 추가, Architecture/Contracts 승격과 Presentation HTML 생성은 불필요하다.

## 후속: 드롭/바람 비가시성 수정과 흰 테두리

사용자가 드롭과 바람이 안 보인다고 보고하고 드롭 오브젝트의 흰 테두리를 명시적으로 요청했다. 기존 승인 표시 범위의 수정이며, 원본 살점 이미지/보상/속도 수치는 유지한다. 관련 Pickup3개, FuelBarrel, Junmo의 렌더 설정과 RewardPickup의 테두리 동기화, 전용 shader/material만 변경한다.

원인은 Sorting Layer였다. Default/order15의 드롭과 Default/order25의 바람은 BackGround의 불투명 배경보다 먼저 그려져 모두 가려졌다. sortingOrder는 같은 레이어 안에서만 비교한다. 실제 생산 URP/Renderer2D와 Junmo 카메라/첫 BeltScroll을 유지한 A/B에서 재질·크기·오브젝트를 유지하고 레이어만 바꾸자 Default의 변화 픽셀은 종류별0, ForeGround는 살점17/영혼28/연료49/바람1388이었다. 카메라 마스크·클리핑·재질 미지원 가설은 이 검사에서 원인으로 확인되지 않았다. 앞선 particleCount/이동 검사만으로 실제 배경 위 가시성을 확인하지 못한 검증 공백을 보완했다.

Pickup Sprite/영혼 Trail과 Wind Renderer는 기존 ForeGround, 배럴 몬스터는 기존 Monster 레이어로 이동한다. 드롭은 각각 프리팹에 제작된8방향 흰 실루엣 Renderer를 본체 뒤에 배치한다. PickupOutline.shader/material은 원본 Sprite의 알파만 흰색으로 그리며 UV/원본 PNG를 변경하지 않는다. RewardPickup.RefreshOutline은 Initialize에서 선택한 살점 Sprite와 flip/정렬을 직렬화된 테두리 참조에 한 번 동기화한다. 런타임 계층 생성은 없다.

격리 Editor 이식은 관련5자산의 기존 문서12개와 새 문서78개로 한정했다. 실제 URP 최종 렌더/정렬/테두리184검사 통과: 드롭3종과 살점5형태 모두 실제 흰 픽셀이 추가되며 본체 재질과 .5/.38 크기는 유지한다. Shader 지원/컴파일 오류0, 기존 pipeline/QualitySettings 변경0. 이미지를 시각 확인했다. 테두리 적용 후 native Play Mode53검사도 통과해 실제 드롭 이동/흡수/1회 보상·승리 정산·일시정지, 가속/바람·보스·추격 기존 동작의 회귀를 확인했다. runtime exception/error0이며 기존 fixture의 피해/중력 등 조정 한계는 동일하다. 전체 runtime142개 소스와 기존 Editor7개 소스 컴파일 오류0, 새 렌더/제작 fixture는 격리 Unity 임포트에서 실제 컴파일됐다. 원본 Editor Play Mode와 플레이어 빌드는 미실행이다.

후속 증거는 [effect visibility evidence](../../outputs/effect-visibility-validation/README.md)에 있다. 초기 `outputs/drive-boss-validation/source-snapshot.json`은 테두리 변경 전 기록이며 현재 전체 자산 일치 증거로 사용하지 않는다. 최신 스냅샷은 후속 증거 폴더를 따른다.

## 후속 Micro-fix: 드롭 크기3배

사용자가 드롭 아이템 크기를3배로 늘리라고 명시했다. FleshPickup 루트 .5→1.5, SoulPickup/FuelPickup 루트 .38→1.14로 각각 XYZ를3배 변경했다. 기존 흰 테두리 자식도 루트와 함께 확대된다. 배럴 몬스터, 보상량, 이동/흡수 시간, 자식 Transform, GUID와 스크립트는 변경하지 않았다.

세 프리팹의 루트 Transform만 변경했음을 소스/serialized 블록 검토와 공백 검사로 확인했다. 이번 작은 크기 조정에서는 Unity Play Mode·렌더·플레이어 빌드를 재실행하지 않았다. 앞선 가시성/테두리 렌더와 snapshot은 크기 변경 전 증거이며 현재 스케일 검증으로 확대하지 않는다. Doc Impact: 기존 SessionLog만 갱신하고 구조 문서 추가 변경은 불필요하다.

## 후속 Micro-fix: 테두리 두께만 절반

사용자 요청대로 Pickup3종의 WhiteOutline/Edge0~7 로컬 XY 오프셋을 각각 절반으로 줄였다(.16→.08, 대각 .11313708→.05656854). 드롭 루트 크기1.5/1.14, 본체·테두리 Sprite 크기와 나머지 프리팹 값은 유지한다. 24개 테두리 Transform 위치만 바뀌었음을 serialized 블록 비교와 공백 검사로 확인했다. Unity Play Mode·렌더·빌드는 재실행하지 않았다. Doc Impact: 기존 SessionLog만 갱신한다.
