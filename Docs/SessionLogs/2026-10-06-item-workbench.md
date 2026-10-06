# 2026-10-06 부품 제작·강화 작업대 UI

## 사용자 요청과 근거

현재 사용자 지시가 범위의 기준. 기획은 「러브트레인 2」 및 10월4일 변경 계획을 읽었음.
- https://app.notion.com/p/3f19ca2053b38039a6bcf05e48abab54
- https://app.notion.com/p/3ef9ca2053b380c99ca3caf5e3f715c4

제작은 F로 열고 하단 기차 HUD를 확대하며 상단으로 옮긴 후 세 아이콘 선택지를 가로로 제시. 호버 설명, 드래그 장착, 장착 불가 테두리 붉게 표시. 성공하면 선택지 축소 후 기차 HUD 원위치.
강화는 붉은 이벤트 오브젝트 충돌로 열며 같은 기차 HUD 사용. 아이템 클릭→강화 슬롯 이동, 강화 전/후 설명과 화살표, 강화/제거/취소/닫기. 제거는 이벤트당1회, 강화는 이벤트 횟수 제한 없음. 강화 비용 결정은 보류.

## 구현

- `LevelUpUIManager`는 기존 serialized 데이터/참조를 보존하고 `TrainItemWorkbenchView` 프리팹으로 제작/강화 모드를 조율. 기존 선택 카드 패널은 비활성 유지.
- `Assets/Prefabs/TrainItemWorkbench.prefab`: 상단 TrainHost, 하단 HorizontalLayoutGroup 세 아이콘, 리롤 버튼, 강화 슬롯/양쪽 설명/화살표/액션/큰 닫기, 툴팁과 드래그 아이콘. 새 C# 두 파일은 메타 포함, Junmo 참조 연결.
- 실제 InventoryUI의 TrainEquipmentLayout을 재사용해 위로 이동/1.25배 확대(0.35초). 세 선택지는 이동 뒤0.16초 페이드인. HUD 원래 parent/sibling/anchors/pivot/size/position/scale을 기록·복원.
- 선택지에는 아이콘만 표시. 툴팁은 지역화 이름/해당 레벨 설명을 화면 내부로 보정해 표시. 유효 드래그 중 불가·점유 슬롯 테두리만 붉게 표시하고 드래그 종료/닫기 때 해제.
- 드롭은 Inventory.CanEquipAt 검증 및 wallet.TryPurchaseCreation(Func<bool>) 트랜잭션. 실패 장착은 무과금. 선택지 축소0.12~0.18초 후 기차 HUD 원위치·다운스케일. 취소 후 제안은 유지되어 무료 재추첨을 막음.
- 리롤은 세 선택지를 새로 제시하되 중복 없음, 가능한 대체 아이템이 없으면 버튼 비활성. R키는 새 기획의 향후 필살기에 예약되어 리롤은 버튼으로만 실행.
- 강화 미리보기는 장착 데이터를 이동시키지 않고 원본 슬롯 아이콘만 숨긴 채 떠 있는 아이콘을0.25초 이동. 교체/취소/닫기/강제 중단에서 모두 원본 복구.
- 강화는 `upgradeCostPerCell=-1`로 비용 미정 표시 및 버튼 비활성. 이 값을 추후 지정하면 칸수 메타×가격을 살점에서 차감해 UpgradeItemInstance 실행. 이벤트당 강화 횟수 제한은 없고 아이템 자체 MaxUpgrade는 기존 데이터 상한을 유지.
- 제거는 RemoveItemInstance로 실제 아이템/오브젝트/패시브 수정량을 정리하고 이벤트당1회. 제거 잔여0 및 가능한 강화가 없으면 큰 하단 닫기 표시. Cancel은 선택만 원위치, ESC/우상단X는 창 닫기. 닫기 시 설명/슬롯 등은0.2초 FadeOut, 기차 HUD는0.35초 복귀.
- Inventory/Item_SO: 10개 정확한 슬롯 ID(머리0~2/가운데3~5/꼬리6~8/바퀴9), allowedEquipmentSlots, equipmentCellCount(비용 메타), exact equip/lookup/remove/upgrade. 기존 아이템은 소켓에 해당하는 섹션이 기본 허용 위치. 공통 실체화는 선택한 기차 부착 앵커 사용, 레이저는 기존 총기 교체 위치 유지.
- 제거 시 톱니바퀴 배율, 회복 아이템 최대속도 보너스, 성서 장판 버프/오브젝트, 레이저→기본총기 복귀 처리. 이미 발사한 투사체는 기존 수명대로 동작.

## 이벤트/러브트레인2 수치

- 매60초, 스테이지당 일반/강화3회+기존 보스1회. 테스트용 stage1 두 번째 이벤트 강화(testFirstStageUpgrade=true); stage2 첫 번째 강화; stage3 이후 기본1/3 확률(Inspector 조정 가능). 강화는 일반 이벤트 대체이며 추가 스폰 아님.
- 기존 Station 프리팹을 붉게 칠해 임시 강화 이벤트 오브젝트로 사용. 실제 Train 충돌에서 공유 UI 큐로 열림.
- 스테이지 시계는 이전 단계의 예정 경계를 기준으로 증가해 부동소수점/보스전 시간 정지 때문에 세 번째 이벤트가 누락되지 않음. 마지막900초 보스전에서도 실제 Ending 전에는 이벤트/제작 허용.
- 특정 아이템 보유 이벤트는 후보에서 제외. 랜덤 아이템 이벤트의 살점 보상은 기존50×수량 경로 유지.
- 제작30부터 완료마다+10, 무료리롤5회 이후 영혼1→2→3→4→5(상한5). 살점/리롤횟수는 run 단위, 영혼은 저장. HUD soulText 명시 연결.
- 연료100/기본320/일반최대460/질주+50%. 속도별 초당연료0.25/0.4/0.5/0.6/0.75, 질주 총30을 유지시간에 나눠 소모. 기존 Shift 해제1초 유예 후 감속 유지.
- 일반 충돌 연료10·속도20감소, 엘리트15·30감소. 연료통20초마다 기존 스폰, 처치 시10% 회복 및 살점/영혼 없음.
- 살점: 일반1/2/3 확률50/30/20%, 엘리트10, 보스100. 영혼: 일반0/1 80/20%, 엘리트0/1/2 60/25/15%, 보스1/2/3 50/30/20%.
- 총기 XP 레벨 보너스와 보스 플레이어 레벨 체력 배율 제거. Boss.stageHitPoints로 스테이지 체력 표를 지정할 수 있으며 미정이면 기존 HP 유지.
- 모달 중 피격/충돌 및 중복 처치 방어. 강제 종료는 CancelUIQueue로 대기 요청을 비우고 pause/physics 상태 해제. terminal Ending/Die에서는 신규 UI 차단.

## 검증

- 기존 csproj는 종속 프로젝트 탐색에서 메시지 없이 실패. Unity 생성 csproj는 수정하지 않고 임시 WorkbenchValidation.csproj에서 기존 DLL 참조+추가 소스로 전체 C# 컴파일: 오류0/기존 경고9.
- 실제 소스에 대한 .NET 규칙 검사: 경제·보상·연료249, 인벤토리27, 이벤트/필터26, 이벤트 시계20, UI 큐10 =332개 검증 통과.
- Unity6000.2.3f1 격리 복사본(원본 프로젝트 미실행). 초기 샌드박스 UPM IPC 실패 뒤 검증용 Editor 실행을 auto-review 허용 범위에서 수행. 실제 import/Unity compilation 성공.
- Unity asset/scene+Editor-method interaction103개 검사 통과. 프리팹 필드/씬 연결/누락 스크립트, 제작 등장 순서/세 아이콘/드래그 허용테두리, 실패·성공 결제, 원래 HUD 전체 복원, 선택 교체·취소, 제거1회·큰닫기, 비용미정 비활성, 테스트용 비용10 반복강화, 닫기 FadeOut와 강제 중단/terminal UI큐 복귀를 포함.
- 위 UI 검사는 실제 씬/Unity 컴포넌트에 포인터 핸들러를 직접 호출하고 DOTween.Complete/Goto로 진행한 Editor 메서드 검사. Play Mode, 실제 GraphicRaycaster 경로/물리 충돌, 화면 렌더링/마우스 체감은 검증하지 않음.
- 새 프리팹152개 serialized record/로컬 참조와 Junmo 로컬 참조 유효성, Icon-only 선택지 구조, 지역화 중복키/ko/en, git diff 공백검사 통과.

## 확인 필요/설정

- 강화 비용은 요청대로 미정(Inspector upgradeCostPerCell). 보스 HP 표와 기본 총기 영구 피해 증가량N은 기획에서 수치 미정.
- 모든 현행 아이템은1칸. equipmentCellCount는 비용 메타이며 2칸 footprint 점유 규칙은 별도 정의가 필요.
- 원하는 정확한 부착 위치 허용 마스크 및 기차 슬롯 앵커 좌표는 Inspector에서 조정 가능. 레이저 총기 위치는 기존 GunPivot 유지.
- 같은 워크스페이스에서 별도의 영구 강화 작업이 동시에 추가되어 관련 소스를 건드리지 않고 최신 종속 파일을 C# 검증에 포함. 공유 Soul 저장소/해금 필터 연동 변경은 해당 작업과 함께 유지.
- 실제 플레이에서 F→호버→드래그→장착→복귀, 120초 붉은 이벤트 충돌→선택/취소/제거/ESC, UI창을 반복 열고 닫기, 해상도별 글자/설명/아이콘 간격 확인 필요.

Doc Impact Check: 현재 시스템 계약을 ItemsInventoryWeapons/EventSystemUiQueue/CoreRuntimeGameFlow 메모리에 추가. 원본 scene/prefab/scriptGUID 보존, Package/ProjectSettings 변경 없음. 커밋/푸시는 별도 요청을 받지 않았음.

검증 fixture/결과/snapshot은 `Docs/Validation/2026-10-06-workbench/`에 보관. 테스트용 프로젝트와 package/cache 복사본 및 임시 .NET 실행 소스/산출물은 정리.

## 제목 간격/중앙 배치 조정

사용자 시각 피드백: 기차 아이템 HUD가 제목과 너무 가까워 제작은 중앙쪽, 강화는 기차 HUD와 강화 슬롯을 더 아래로.
- TrainItemWorkbenchView의 mode별 host 위치를 serialized 설정으로 분리: 제작(-27.5,-180), 강화(-27.5,-150). 기존 공통(-24,-100)보다 각각80/50 logical px 내려옴. 전체 기차+바퀴 슬롯의 시각 중심을800화면 x400에 맞춤.
- 강화 슬롯의 localy80→45(화면y210→245), 비교 제목y271, 설명/화살표y328, 설명높이84, 비용y385, 버튼y416. 큰닫기y418. 모두800×450논리좌표 기준.
- 기차/선택지 및 강화 슬롯/비교영역 간격, 주요 요소 겹침 없음 수치검사. 현재 가장 긴 영어/한국어 설명도84높이에 들어가는 글자폭/줄높이 계산 확인. 실제 이미지 재렌더 검증 후샘플 outputs/unity-ui-preview/*-layout-v2.png 제공.

수정된 실제 프리팹/씬 HUD의 Unity6000.2.3f1 렌더링 성공. 제작·강화·호버·드래그V2 이미지 시각검사에서 제목 여백/중앙정렬/선택지·강화슬롯·설명·버튼 겹침 없음 확인. Source/clone View.cs 및 prefab SHA256 일치. 이미지: `outputs/unity-ui-preview/creation-layout-v2.png`, `upgrade-layout-v2.png`. 격리 rendering project는 반복 시각수정용으로 유지하며 해당 출력폴더 `.gitignore`에 project/ 및 *.log 제외. 실제PlayMode는 미검증.
