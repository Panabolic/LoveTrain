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

## 무료 강화·기존 실체 부착 복귀 및 해상도 조사

현재 사용자 명시 지시 범위: 강화 비용0, 현재선택슬롯에 실제아이템을 붙이는로직잠시비활성/예전소켓사용, 현재UI해상도문제점검. 구현범위와읽기전용조사범위를대화TaskBrief로분리함.
- LevelUpUIManager.upgradeCostPerCell 기본값과 Junmo serialized 값0. 살점0이어도 기존아이템상한미도달은무료강화가능. 이벤트당제거1회, 아이템MaxUpgrade, 제작/리롤비용은보존.
- Item_SO.InstantiateVisual에서 Inventory.GetEquipmentAnchor 호출만제외하여 attachmentSocketName 재귀소켓찾기→없으면user루트의 기존로직을사용. 드래그UI선택슬롯/허용마스크/인벤토리위치는유지. 정확한월드슬롯앵커생성코드는미호출상태로남겨이번범위의광범위삭제/리팩터링을피함. 레이저기본Gun교체경로는기존그대로.
- 검증: 변경된두C#소스가포함된 Unity생성 Assembly-CSharp.csproj의 dotnet build --no-restore 통과(오류0/기존경고9). SO공통호출부및사용자루트/소켓fallback확인, b4e5359 이전구현의동일소켓진입점대조. Junmo localfileID/중복및비용0검사, diff공백검사통과.
- 원본프로젝트의UnityEditor가열려있어서별도원본batchmode는실행하지않음. 이번수정의UnityEditor컴파일/실제장착·무료강화PlayMode는따로실행·관찰하지않았음.

해상도조사는소스·직렬화·수식기반으로실행했으며관련UI는수정하지않음.
- FixedAspectRatioController는모든rootCanvas를800×450논리콘텐츠로맞춤. 1920×1080/1280×720/960×540/800×450/640×360은전체16:9, 배율2.4/1.6/1.2/1/.8.
- 800×600은800×450콘텐츠와상하75px, 1024×768은1024×576와상하96px, 1920×1200은1920×1080와상하60px, 3440×1440은2560×1440와좌우440px. 검은여백은고정16:9설계.
- 확인된P2: PermanentUpgradeMenu.PlaceTooltip은중앙pivot기준clamp인데 Resources/PermanentUpgradeMenu.prefab Tooltip은pivot(0,1)/크기260×170. 논리패널오른쪽아래point에서우측122·하단77만큼콘텐츠밖으로나갈수있음(1920×1080약293/185px). 수정후보는pivot중앙정렬또는실제pivot을반영하는코드, 현재점검범위에서는미수정.
- 가독성P3: workbench설명minimum10logicalpx는640×360에서8px; 영구강화Status7.5logicalpx는640×360에서6px. 낮은해상도최소지원/글자크기결정과native렌더확인필요.
- 제작/강화Tooltip과drag좌표는RectTransformUtility + event camera 및중앙pivot으로콘텐츠영역과일치. OptionLayoutGroup이동적배치하므로YAMLanchoredPosition만으로오버랩을확정하지않음.
- 이전1280×720단일UI렌더는다해상도검증으로주장하지않음. 실제해상도전환중입력/GraphicRaycaster·텍스트overflow·모든화면render/PlayMode검사는미실행.

Doc Impact Check: SessionLog와ItemsInventoryWeapons의현재부착/가격상태만갱신. 해상도발견은Suggested Later로보고; Prefab pivot/Canvas/ProjectSettings/입력은미변경.

## 살점 HUD의 선로 침범 수정

추가 사용자 지시로 살점 HUD 배치 수정은 Implementation 범위에 포함했다. 다른 UI의 해상도 문제는 계속 읽기 전용 조사 범위다.
- `Junmo.unity`의 CurrencyHUD 높이만 92→78 logical px로 줄이고 영혼/살점/게이지/창조 안내의 상단 오프셋을 각각 -5/-23/-43/-58로 조정했다. 폭150, 글자12, 게이지130×11, 우측20/하단17 여백과 경제·입력·표시 로직은 유지했다.
- 실제 `rail.png`의 불투명 픽셀 하단을 기본 카메라로 투영하면 하단에서 약101.7 logical px이다. 기존 박스 상단109는 선로에 침범했으며 수정 후 상단95는 약6.7 logical px 아래에 있다. 콜라이더 상단을 시각적 경계로 사용하지 않았다.
- 카메라 흔들림은 월드 선로만 이동시킬 수 있으므로 흔들리는 순간까지 간격을 보장하는 수정은 아니다. 카메라 흔들림과 다른 HUD·기차 슬롯은 이번 범위에서 변경하지 않았다.
- 동일 프로젝트를 연 원본 Editor에 별도 batchmode를 실행하지 않았다. 워크스페이스 안의 기존 복사본은 project-open 오류로 검증 시작 전에 중단되었고, 원본 Editor와 분리된 Temp 프로젝트에서 검증을 이어갔다.
- Unity 6000.2.3f1 격리 프로젝트의 임포트/컴파일 및 실제 소스 Editor 메서드 검사 670건 통과(실패0). 살점0에서 무료 강화/아이템 상한, 이전 소켓·루트 부착, 1920×1080/1280×720/960×540/800×600/3440×1440/640×360의 배치를 확인했다. 실제 강화 이벤트 열기/아이템 선택/UpgradeSelectedItem과 InstantiateVisual 메서드를 직접 호출했으며 Play Mode 검사는 아니다.
- 해상도 검사는 원본 CanvasScaler의 ScaleWithScreenSize와 FixedAspectRatioController의 실제 콘텐츠 계산/루트 적용 메서드를 사용한 RenderTexture 검사다. 매 해상도에서 투영된800×450 콘텐츠 모서리와 예상 콘텐츠 픽셀영역 일치, 자식 표시 요소/키 그림자 포함, 장비10칸과 겹침0, KO/EN 및 큰 살점 수량의 텍스트 overflow0을 확인했다. 두 스테이지 선로와의 간격은 각각6.677/6.743 logical px로 유지되었다.
- 최초 RT 검사에서는 Camera Canvas의 native 크기가 이전 RT 해상도로 남아 선로 검사4건 실패 및 잘린 캡처가 발생했다. fixture에서 native 렌더 후 크기를 확정하고 생산 CanvasScaler 경로를 유지하도록 고친 뒤 재검증했다. 원본 게임의 해상도 코드/설정을 바꾸지 않았다. 최초 실패는 `outputs/unity-ui-preview/october-fix-validation-temp.log`, 최종 성공은 `october-fix-validation-settled.log`와 `october-fix-validation-result.txt`에 구분했다.
- 최종 `flesh-static-rail-1280x720.png` 및 `flesh-static-rail-800x600.png`에서 선로 아래 여백/재화 박스/하단 안내의 시각 배치를 확인했다. 이는 씬 자산을 사용한 정적 월드·UI 렌더이며 실제 플레이 캡처는 아니다. 원본씬의 변경한7개 record(가격/배치)와 검증 복사본이 같음을 확인했다. 동시에 진행 중인 stage/combo의 다른 씬/소스 변경은 이 작업에 포함하거나 되돌리지 않았다.

Suggested Later: 실제 OS 창 테두리 resize의 갱신 순서도 재현 검사가 필요하다. `FixedAspectRatioController.Apply`는 루트 크기 계산 뒤 `Canvas.ForceUpdateCanvases`를 호출하고, UGUI CanvasScaler는 preWillRenderCanvases에서 scaleFactor를 갱신한다. 옵션 변경은 RequestRefresh의30프레임 재적용이 있으나 OS resize에는 별도 요청이 없다. 이는 소스상 원인 후보이며 현재 런타임 버그로 확정하지 않았다. 영구 강화 툴팁 pivot 문제와 저해상도 가독성은 위 조사와 같이 미수정이다.

미실행: 원본 Editor의 Play Mode, 실제 창 해상도 전환/마우스 raycast·드래그, 피격 카메라 흔들림, 플레이어 빌드. Doc Impact Check: SessionLog + ItemsInventoryWeapons StructureMemory 갱신. RefactorLog/DecisionLog/ErrorLog 및 Architecture/Contracts 승격 불필요. Presentation HTML 계층 없음.

## 하단 HUD 중앙 배치와 재화 박스 여백

사용자 후속 요청은 하단 UI 전체를 아래로 내려 하단 섹션 중앙에 배치하고, 키 안내 아래의 여백을 영혼 텍스트 위의 보이는 여백과 균형 맞추는 것이다. Mode는 Implementation, 씬 변경 위험은 High, 대상은 Leaf UI이다. 대화 브리프에서 Junmo의 기존 RectTransform 위치와 재화 박스 내부 여백만 변경하도록 범위를 명시했다.
- 기본 카메라의 실제 선로 하단 y101.677을 기준으로 하단 섹션 중앙은 y50.839 logical px이다. 미터기와 기차 아이템 표시의 시각 중심이 기존에는 약55~56이었다.
- `SpeedMeter`(356823175)의 Y16.5→11.5. 자식 연료 바도 함께 내려간다.
- `TrainAttachmentLayout`(2700000001)의 Y46→41. 기차 배경/아이템9칸/바퀴칸과의 상대 위치는 그대로이며 별도 `LevelText`(2054807077)는 Y-421.1→-426.1로 함께 내렸다.
- `CurrencyHUD`(2600000001)는 Y17→9, 높이78→84로 변경했다. 내부 상단 오프셋을 유지하여 키 안내 아래 외곽 그림자 여백을2.5→8.5로 늘리고 박스 중심을 y51에 맞췄다. 폭150, 우측20, 재화 글자12, 게이지130×11, 키/안내 크기 및 줄 사이 간격은 유지했다.
- 신규 오브젝트/컴포넌트·프리팹·C#·경제/제작 동작·카메라/선로/CanvasScaler 변경 없음. 상단 진행/콤보/타이머 및 제작·강화창의 열린 배치는 변경하지 않았다.
- 변경 전 스냅샷과 비교한 씬 record 수는1308로 같으며 변경 record는 위4개뿐이다. 관련 리턴 애니메이션은 열 때 현재 authored 위치를 저장하고 닫을 때 복원하는 기존 경로를 사용한다. 롤백은 이4개의 RectTransform 필드만 이전 값으로 복구하며 다른 진행 중인 작업은 보존한다.
- Unity6000.2.3f1 격리 복사본의 씬 임포트 및 Editor 메서드/RenderTexture 검사413건 통과(실패0). 1920×1080/1280×720/960×540/800×600/3440×1440/640×360에서 실제 CanvasScaler·16:9 콘텐츠 루트를 적용하고, 미터기/연료 프레임·기차/슬롯/바퀴/레벨·재화박스의 콘텐츠 내부 배치와 선로 아래 위치를 확인했다. 하단 세 그룹의 시각 bounding 중심은 각각49.8/49.918/51이며 섹션 중앙50.839에서약1.04이내이다.
- 키 그림자 아래 여백은 모든 해상도에서8.5 logical px이다. SoulText의 TMP mesh/textBounds 상단 여백은6.625(차이1.875)로 확인했으며, 폰트의 mesh 여유 영역과 실제 글자 픽셀을 구분한다. 최종1280×720/800×600 네이티브 정적 렌더에서도 글자 위와 키 아래의 보이는 여백 균형을 확인했다. 자동검사가 실제 픽셀 여백의 완전 동일함을 입증하는 것으로 표현하지 않는다.
- 기존+N 숨김개수표시의 Rect는 이동 후 일부 화면 아래에 걸치지만, 테스트용+9 글리프는 콘텐츠 하단에서5 logical px 위에 보이며 잘리지 않는다. 정상 HUD에서 비활성인 기존 표시를 이번 변경으로 재배치하지 않았다.
- 제작 ShowCreation→닫기 메서드와 실제 DOTween 완료를 실행하여 새 authored 위치/스케일/부모/형제순서/앵커/피벗/크기 복원을6개 해상도에서 확인했다. 물리 입력/Play Mode 검사가 아니라 Editor 메서드 검사이다.
- 최종 결과는 `outputs/unity-ui-preview/hud-bottom-center-result.txt`, 실행 로그는 `hud-bottom-center-validation.log`, 렌더는 `hud-bottom-centered-1280x720.png`/`hud-bottom-centered-800x600.png`이다. 원본 Editor가 열린 같은 프로젝트에 별도 batchmode를 실행하지 않았고 이전 외부 Temp 프로젝트만 사용했다. 원본Scene/검증Scene의 동일성 및 diff공백검사도 확인했다.

미실행: 실제 Play Mode/물리 마우스·F키/창 크기 변경/카메라·미터기 피격 흔들림/플레이어 빌드. C# 소스 변경이 없어 별도 MSBuild 반복 검사는 추가하지 않았다. Doc Impact Check: SessionLog + ItemsInventoryWeapons StructureMemory 갱신; 다른 기억 문서·Architecture/Contracts 승격 불필요. Presentation HTML 계층 없음.
