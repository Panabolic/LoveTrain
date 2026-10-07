---
status: active
authority: structure-memory
category: core-runtime-game-flow
last_reviewed: 2026-10-07
---

# Core Runtime And Game Flow

## Current composition branch — 2026-10-07

On `codex/runtime-composition`, Train keeps its scene/public/serialized facade and owns FuelState for fuel, existing TrainDriveState for driving, and DeathLifecycle for life/death presentation state. Train's damage eligibility, dash immunity, acceleration block, death coroutine and scene-return requests remain in their existing execution order. TrainController uses MovementRules for bounded horizontal calculation and applies the result through its ObjectHost Unity binding.

Train car Animator references are bound through PresentationLink and rebound if the public array reference changes. Optional presentation references do not own fuel or death state. Gun keeps IWeaponStrategy while WeaponStatRules holds shared numeric stat calculation. Item/equipment ownership is documented in the [Items map](./ItemsInventoryWeapons.md).

GameManager/StageManager/SceneLoader/TrainLevelManager and permanent save contracts are not replaced by a new universal object or new global Manager. Historical TrainSpeedHealth/TrainLevelProgression/GameSimulationController references below are prior context, not declarations of current files.

See [active scope](../../ActiveTasks/runtime-composition-comparison.md) and [comparison/evidence](../../Validation/2026-10-07-runtime-composition/README.md).

## Purpose

Map the runtime ownership around game state, train/player lifecycle, stage transitions, audio state, and UI/presentation services.

## Current Structure

- `GameManager` owns `GameState`, global play timer, pause/event time freeze side effects, boss-death routing, restart, and quit behavior.
- `GameKillCounter` owns normal, elite, boss, and total kill count state behind `GameManager`.
- `GameUiQueueController` owns queued UI pending/processing state and event-resume bookkeeping behind `GameManager`.
- `GameSimulationController` owns global `Time.timeScale` and `Physics2D.simulationMode` pause/resume writes plus the shared runtime paused-state predicate.
- `StageManager` owns in-scene stage progression: stage prefab loading, tunnel transition presentation, fade sequencing, train repositioning, and resume to `Playing`.
- `TrainSpeedHealth` owns train speed-as-health state, max-speed changes, dying/recovery/depleted transitions, and dead/dying flags.
- `Train` remains the scene-facing MonoBehaviour facade for damage, recovery, dying/death presentation, train control enable/disable, death UI, and return-to-start flow.
- `TrainController` owns horizontal movement bounds.
- `TrainLevelProgression` owns XP totals, level thresholds, display progress, and level advancement state.
- `TrainLevelManager` remains the scene-facing MonoBehaviour facade for train level events and level-up UI queue requests.
- `SceneLoader`, `StartMenuManager`, `StartObject`, `EndingManager`, and UI helper scripts bridge start, death, ending, and scene transitions.
- `SoundEventBus` publishes sound requests; `SoundManager` maps `SoundID` to audio sources and follows `GameManager` state for BGM.
- `FixedAspectRatioController` is runtime-created in `Start.unity` and `Junmo.unity`. It applies a centered 16:9 camera rect, creates runtime letterbox/pillarbox bars, and keeps Canvas UI inside the 16:9 content rect while preserving the `800x600` CanvasScaler baseline.
- `ScreenDisplaySettings` owns runtime display mode persistence and applies windowed, fullscreen, and borderless mode changes through `Screen.SetResolution`.

## Key Files

- `Assets/Scripts/LeeJunmo/GameManager.cs`
- `Assets/Scripts/LeeJunmo/GameKillCounter.cs`
- `Assets/Scripts/LeeJunmo/GameUiQueueController.cs`
- `Assets/Scripts/LeeJunmo/GameSimulationController.cs`
- `Assets/Scripts/LeeJunmo/StageManager.cs`
- `Assets/Scripts/LeeJunmo/Train.cs`
- `Assets/Scripts/LeeJunmo/TrainSpeedHealth.cs`
- `Assets/Scripts/LeeJunmo/TrainController.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/TrainLevelManager.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/TrainLevelProgression.cs`
- `Assets/Scripts/LeeJunmo/FixedAspectRatioController.cs`
- `Assets/Scripts/LeeJunmo/ScreenDisplaySettings.cs`
- `Assets/Scripts/LeeJunmo/Option.cs`
- `Assets/SoundManager.cs`

## Ownership And Lifecycle

- `GameManager` is a `DontDestroyOnLoad` singleton and resets runtime counters on scene load.
- `GameKillCounter` has no Unity scene references and should stay free of UI, singleton, scene, and enemy object calls.
- `GameUiQueueController` has no Unity scene references and should stay free of `Time`, `Physics2D`, singleton, UI object, and scene-transition calls.
- `GameSimulationController` is the central writer for global time scale and physics simulation mode and exposes `IsPaused` for pause-sensitive update gates; it should not own game-state decisions.
- `StageManager` is scene-local and assumes scene-authored references for train, fade UI, stage database, transition transforms, and background parent.
- `Train` assumes a scene-authored train object with controller, animator children, death presentation objects, hand target, overlay animators, and death UI references.
- `TrainSpeedHealth` has no Unity scene references and should stay free of DOTween, coroutines, GameObject access, and singleton calls.
- `TrainLevelProgression` has no Unity scene references and should stay free of UI, singleton, GameObject, and lifecycle calls.
- `TrainLevelManager` keeps public level properties and events for existing UI, enemy scaling, and weapon subscribers while delegating progression state.
- `SoundManager` is a `DontDestroyOnLoad` singleton, subscribes to `SoundEventBus`, and subscribes to `GameManager.OnGameStateChanged` when available.
- `FixedAspectRatioController` is scene-local and runtime-generated. It hooks scene loads, creates a non-persistent controller object only for `Start` and `Junmo`, and avoids serialized scene references for bars or safe-area roots.
- `Option` initializes previous/next selector controls for screen mode and windowed resolution, then delegates display mode persistence/application to `ScreenDisplaySettings`.

## Extension Entry Points

- Add a new global state only by extending `GameState` and reviewing `GameManager`, `Gun`, `ItemInstance`, `Spawner`, `SoundManager`, and UI flow guards.
- Add new kill categories by starting in `GameKillCounter`, then exposing through `GameManager` only if UI or gameplay needs it.
- Add new gameplay-pausing UI through `GameManager.RegisterUIQueue(...)`; keep queue ordering/state bookkeeping in `GameUiQueueController` and side effects in `GameManager`.
- Add new global pause/resume side effects through `GameSimulationController`, while keeping when-to-pause decisions in the caller. Pause-sensitive early returns should read `GameSimulationController.IsPaused`.
- Add new stage transition presentation through `StageManager.StageTransitionSetting` scene references and `StageDatabase`.
- Add new level-up behavior through `TrainLevelManager.OnLevelUp` and the `GameManager` UI queue, not by opening UI directly during gameplay.
- Add or change XP curve rules in `TrainLevelProgression`, while keeping `TrainLevelManager` as the public event facade unless a serialized migration is approved.
- Add new BGM behavior through `SoundID`, `SoundData`, and `SoundManager.HandleGameStateChanged`.
- Add new screen-bound UI presentation against `FixedAspectRatioController.ContentPixelRect` when available, then fall back to full-screen `Screen` bounds for unsupported scenes.

## Known Pitfalls

- `GameManager.InitializeGameData()` resets `gameTime` and `GameKillCounter` on scene load; confirm whether a task needs run persistence before changing it.
- `Time.timeScale` and `Physics2D.simulationMode` writes should go through `GameSimulationController`; raw `Time.timeScale == 0` pause checks should use `GameSimulationController.IsPaused`. Avoid local systems independently freezing time unless the ownership is explicit.
- `GameUiQueueController` should not directly freeze time or start stage transitions; those remain `GameManager` responsibilities.
- `Train` death/recovery uses DOTween and coroutine lifecycles. Interrupted flows need `DOKill`, coroutine, and active object cleanup review.
- Keep train speed-state rules in `TrainSpeedHealth`; keep Unity side effects in `Train`.
- Keep train XP progression rules in `TrainLevelProgression`; keep UI queueing and event publication in `TrainLevelManager`.
- `GameManager.BossDied()` interacts with `PoolManager`, `StageManager`, and `EndingManager`; do not treat boss death as an enemy-only concern.
- The aspect controller reparents root Canvas children at runtime. New UI that is created directly under a Canvas after startup may need either content-rect clamping or explicit parenting review.
- Fullscreen and borderless mode changes use the native/current display resolution; only windowed mode should enable the window-size previous/next selectors.

## Promotion Candidate

This map can become a future architecture document once game state, stage transition, and train death/recovery ownership stabilizes.


## 2026-10-04 Gameplay update (current source)

Train은 이제 연료(CurrentFuel/MaxFuel)가 체력이며 속도로 사망하지 않는다. TrainDriveState(Train.cs 내부 순수 클래스)가 Shift 가속, 해제 후 1초 지연 감속, 5초/1.5배 질주 및 재무장을 담당한다. 전투 상태에서만 주행/연료 시간이 흐른다. 연료 고갈은 기존 사망/엔딩 경로와 연결된다. TrainLevelManager의 GainExperience는 살점을 지급하고 자동 XP 레벨업은 중단한다. 현재 소스에는 과거 메모리의 TrainSpeedHealth/TrainLevelProgression 분리 파일이 없다.

검증/기본 수치/범위: `Docs/SessionLogs/2026-10-04-combat-fuel-creation.md`.

2026-10-04 넓은 화면: Junmo 카메라 size24/y4.22581, 두 BeltScroll 프리팹 Lane 시각1.5배 및 collider 높이/두께 역보정. AutoScrollBackground는 viewport 기준 타일 수/초기 커버리지/재배치를 담당. 지상/비행 스폰 영역 확대. 상세 검증은 `Docs/SessionLogs/2026-10-04-camera-wide-view.md`.

2026-10-04 확대 비율 수정(이전1.5배 설정 대체): 원본 기준1.3배, Junmo 카메라 size20.8/y2.535486591. 선로 시각1.3배/collider 역보정 및 스폰/단색 배경 범위도 원본 기준1.3배로 재계산. 상세는 같은 날짜 camera-wide-view 로그 마지막 항목.

## 2026-10-06 영구 강화 / 런 시작

StartMenuManager.LoadPlayScene은 기존 Start 애니메이션 콜백에서 Resources/PermanentUpgradeMenu 프리팹을 한 번 인스턴스화하여 연다. EnterStartState의 기존 후속 콜백은 하단 게임 시작 확정 전에는 상태를 변경하지 않는다. StartGameFromUpgrades만 기존 playSceneName을 로드하며 Junmo의 레버 시작 흐름을 유지한다. UI Canvas/버튼/TMP/진척도/툴팁은 프리팹에 제작되어 있고 PermanentUpgradeMenu/Card는 직렬화 참조로 표시를 갱신한다. 800×450 콘텐츠 영역, 5열×2행 GridLayoutGroup, 제목 아래 영혼 잔액 한 줄, 호버 툴팁, 하단 시작 버튼으로 구성된다.

PermanentUpgradeNode는 기존 24개 구매 항목을 표시용 10종 노드로 묶으며 상태를 소유하지 않는다. 연료/기본총기 5단계, 필살기/질주 시간/질주 속도 3단계, 레이저/산탄총/기관총/두 보스 아이템 각각 1단계이다. Card.NodeId와 canonical Node.Id로 연결하고 한 클릭은 NextStage 하나만 구매 요청한다. 버튼 아래 별도 비상호작용 Image 배열은 구매한 단계 수만큼 왼쪽부터 채운다. MAX/영혼 부족/미설정 버튼의 구매는 잠기지만 마우스 설명은 유지한다. 프리팹의 기본 안내문은 없고 구매 결과/저장 오류만 하단에 표시한다.

툴팁은 버튼 RectTransform의 실제 중심을 panel 로컬 좌표로 변환한 고정 위치를 기준으로 펼친다. 기본은 우하단이며 오른쪽/아래쪽 경계의 여유 공간이 부족한 축만 왼쪽/위쪽으로 바꾼다. 런타임 pivot과 기준점 간격의 방향을 함께 전환한다. 호버·마우스 클릭·키보드 선택 모두 같은 버튼 중심을 사용하며 포인터 이동으로 위치를 갱신하지 않는다. 상단 pivot의 transform.position을 중심으로 오인하지 않도록 card.TransformPoint(card.rect.center)를 사용한다.

PermanentUpgradeProgress가 영혼 잔액과 구매 ID의 공용 원본을 version1 JSON에 저장한다. 기존 Souls.v1 잔액은 최초 마이그레이션 및 호환 미러에 사용한다. TrainLevelManager는 런 지갑 생성 전 Reload하고 보상·재추첨 후 SetSouls로 동기화한다. Train.Start는 카탈로그의 구매한 연료/질주 효과를 런 초기값에 반영한다. 미정 효과는 구매를 차단한다. 수치 해석과 설정/검증은 SessionLogs/2026-10-06-permanent-upgrades.md 참조.


## 2026-10-06 Item workbench update

2026-10-06 러브트레인2: 속도구간별연료/질주총30, 일반·엘리트충돌피해와감속분리, 몬스터확률살점/영혼보상, 연료통10%회복·재화없음, 제작30+10/무료리롤5+영혼1~5. 모달상태피격차단/강제닫기UI큐복구. Unity격리import+Editor메서드103검사와규칙332개통과; 실제PlayMode미검증. 상세는 `Docs/SessionLogs/2026-10-06-item-workbench.md`.

## 2026-10-06 Distance stages and combo HUD

현재 소스의 스테이지 조우는 고정 180초 간격 대신 `StageManager`의 이동 거리로 결정한다. 내부 `StageDistanceProgress`가 Playing에서 현재 속도 × deltaTime을 누적하며 구간 길이는 57,600(320 × 180초)이다. Boss/Event/Pause/StageTransition에서는 이동 거리가 정지한다. 실제 런의 StageNumber는 반복 배경 CurrentStageIndex와 분리되며, 터널 암전에서 다음 배경을 로드할 때 거리와 이벤트 구간을 초기화한다. StageManager.LateUpdate가 Train/GameManager.Update 이후 `OnProgressAdvanced`를 발행하여 조우 소비자들이 동일한 거리와 런 시간을 사용한다.

15분 타이머는 기존 `GameManager.gameTime`의 Playing 시간이며 보스전에는 정지한다. `Spawner`는 구간 끝 조우와 900초 최종 조우를 별도로 판단하고 같은 프레임에는 최종 조우를 우선한다. 시간 180초 자체는 보스 조우 조건이 아니다.

`GameManager`는 일반·엘리트의 기존 AddKillCount 경로(무기와 충돌 처치)에 연결된 `ComboKillState`를 소유한다. 마지막 처치부터 3초를 Playing/Boss에서 계산하고 Event/Pause에서는 유지한다. 관찰한 Time.timeAsDouble 차이를 Update/처치 등록/상태 전환 전에 한 번 정산하여 콜백 순서와 FixedUpdate 시각 역행에 따른 만료 오차를 방지한다. 전환·사망·엔딩·씬 초기화에서 콤보를 초기화한다. 보스·촉수·연료통은 콤보 집계 대상에 추가하지 않았다.

Junmo의 기존 Canvas/OtherUI 안에 StageProgressHUD와 ComboKillHUD를 씬 제작한다. StageProgressUI는 직렬화 StageManager와 선/위치/이벤트 표시 참조를 읽는다. 최상단 중앙의 경로 아래에는 기존 TimeText가 놓이며 이벤트 노드는 느낌표 아이콘만 표시한다. 노드 설명과 퍼센트 텍스트는 없다. ComboKillUI는 우측 상단, 타이머보다 아래에서 GM의 읽기 값으로 문구·3초 밑줄·10킬 이상 고대신 문구를 표시한다. 자릿수마다 느낌표가 추가된다(9 → 10! → 100!!). 두 UI는 런타임 계층 생성이나 새 Manager/DDOL 객체를 사용하지 않는다.

승인 범위와 검증 결과: [거리 스테이지와 콤보 HUD 세션](../../SessionLogs/2026-10-06-stage-combo.md).

## 2026-10-06 전진 이동 / 주행 성장 / 추격 팔

최신 주행은 Train/TrainDriveState가 소유한다. 실제 Shift 입력과 가속 허용 상태를 분리하고 받아들인 피격마다 0.2초 가속 제한을 갱신한다. 홀드를 유지하면 제한이 끝난 뒤 자동 가속하며, Shift 해제 후 감속 지연은 0.5초이다. 기차 보스 접촉은 source별로 유지되는 별도 제한이며 질주도 취소하고 접촉 동안 70/초로 0까지 감속한다. 일반 질주 무적은 유지한다. SetRunLevel은 런 초기 기본/일반 최고속도에 (level-1)×20을 적용한다.

TrainController는 D의 기존 월드 이동 속도를 유지하면서 앞쪽 clamp를 제거한다. 씬 제작 ForwardCameraRig/ForwardCameraFollow가 기존 앞쪽 경계 이후 앞으로만 이동하며 자식 Main Camera의 기존 local shake를 보존한다. 뒤쪽 한계는 카메라 전진량+기존 minX이다. Spawner/EventObjectSpawner의 생성 좌표와 StageManager의 터널/리셋/새 배경 원점에 같은 전진량을 적용한다. 조우 거리에는 D 이동을 더하지 않고 기존 CurrentSpeed 적분과 보스전 거리/15분 정지를 유지한다. AutoScrollBackground는 실제 viewport의 빈 공간을 기준으로 타일을 재배치한다.

기존 GameOverHand에 씬 제작 PursuingHand가 붙는다. 런 시작 기본속도+완료 구간당30을 사용하며 현재 레벨의 기본속도로 추격 속도를 재계산하지 않는다. 주행속도 차이와 실제 기차 x 변위를 적분한 Gap을 기존 벨트의 0.1 월드 환산으로 표시한다. Playing/Boss에서만 추격하고 전환에는 CaptureTransitionGap/RestoreTransitionGap으로 간격을 유지한다. renderer 끝과 기차 collider의 수평 간격이 닫히면 Train의 기존 끌려가기/게임오버 경로로 진입한다. 사망 끌기 목적지도 현재 카메라 원점을 반영한다.

StageProgressUI는 팔 Gap을 읽어 현재 구간의 팔 아이콘과 플레이어가 지난 경로 위 붉은 Fill을 표시한다. 계기판은 기본45°/일반 최고90°, 질주±5°이며 왼쪽 세로 연료 Fill을 읽는다. SimpleSpeedUI는 실제 속도 숫자만 표시하고 차체 애니메이션은 Train이 갱신한다. 씬 제작 AccelerationWindEffect/ParticleSystem은 기본속도 대비 가속 비율에 따라 흰 직선의 속도·밀도·길이를 조절하며 감속 중 길이를 줄인다. 새 Manager/DDOL, 런타임 UI 계층 생성 또는 저장 형식 변경은 없다.

상세 범위와 실제 검증 결과는 [주행·보스·성장 세션](../../SessionLogs/2026-10-06-driving-boss-progression.md)을 따른다.

후속 가시성 수정에서 Wind의 Sorting Layer를 기존 ForeGround로 변경했다. Default/order25는 BackGround 뒤에 있어 실제 배경에 가려졌으며, 생산 URP를 유지한 동일 재질·입자 A/B에서 레이어만 바꾸면 렌더 픽셀이 나타났다. 별도 설정 변경 없이 같은 카메라·셰이더·주행 규칙을 유지한다.

통로 배치는 카메라 size16→20.8로 확장된 오른 경계 차이8.533333만큼 Junmo의 두 통로 생성/도착/기차 진입6개 point의 X를 옮겼다. 실제 Sprite 크기와 Tunnel2 자식 offset을 기준으로 예전 화면 끝 정렬을 유지하며 프리팹 크기·Y/선로·전환 시간은 유지한다. point는 scene root이고 StageManager가 기존 cameraOffset을 한 번 더한다. 상세 변경/검증은 [통로 배치 기록](../../SessionLogs/2026-10-06-tunnel-camera-layout.md)을 따른다.
