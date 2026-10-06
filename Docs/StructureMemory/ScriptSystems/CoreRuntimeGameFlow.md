---
status: active
authority: structure-memory
category: core-runtime-game-flow
last_reviewed: 2026-06-08
---

# Core Runtime And Game Flow

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

PermanentUpgradeProgress가 영혼 잔액과 구매 ID의 공용 원본을 version1 JSON에 저장한다. 기존 Souls.v1 잔액은 최초 마이그레이션 및 호환 미러에 사용한다. TrainLevelManager는 런 지갑 생성 전 Reload하고 보상·재추첨 후 SetSouls로 동기화한다. Train.Start는 카탈로그의 구매한 연료/질주 효과를 런 초기값에 반영한다. 미정 효과는 구매를 차단한다. 수치 해석과 설정/검증은 SessionLogs/2026-10-06-permanent-upgrades.md 참조.


## 2026-10-06 Item workbench update

2026-10-06 러브트레인2: 속도구간별연료/질주총30, 일반·엘리트충돌피해와감속분리, 몬스터확률살점/영혼보상, 연료통10%회복·재화없음, 제작30+10/무료리롤5+영혼1~5. 모달상태피격차단/강제닫기UI큐복구. Unity격리import+Editor메서드103검사와규칙332개통과; 실제PlayMode미검증. 상세는 `Docs/SessionLogs/2026-10-06-item-workbench.md`.

## 2026-10-06 Distance stages and combo HUD

현재 소스의 스테이지 조우는 고정 180초 간격 대신 `StageManager`의 이동 거리로 결정한다. 내부 `StageDistanceProgress`가 Playing에서 현재 속도 × deltaTime을 누적하며 구간 길이는 57,600(320 × 180초)이다. Boss/Event/Pause/StageTransition에서는 이동 거리가 정지한다. 실제 런의 StageNumber는 반복 배경 CurrentStageIndex와 분리되며, 터널 암전에서 다음 배경을 로드할 때 거리와 이벤트 구간을 초기화한다. StageManager.LateUpdate가 Train/GameManager.Update 이후 `OnProgressAdvanced`를 발행하여 조우 소비자들이 동일한 거리와 런 시간을 사용한다.

15분 타이머는 기존 `GameManager.gameTime`의 Playing 시간이며 보스전에는 정지한다. `Spawner`는 구간 끝 조우와 900초 최종 조우를 별도로 판단하고 같은 프레임에는 최종 조우를 우선한다. 시간 180초 자체는 보스 조우 조건이 아니다.

`GameManager`는 일반·엘리트의 기존 AddKillCount 경로(무기와 충돌 처치)에 연결된 `ComboKillState`를 소유한다. 마지막 처치부터 3초를 Playing/Boss에서 계산하고 Event/Pause에서는 유지한다. 관찰한 Time.timeAsDouble 차이를 Update/처치 등록/상태 전환 전에 한 번 정산하여 콜백 순서와 FixedUpdate 시각 역행에 따른 만료 오차를 방지한다. 전환·사망·엔딩·씬 초기화에서 콤보를 초기화한다. 보스·촉수·연료통은 콤보 집계 대상에 추가하지 않았다.

Junmo의 기존 Canvas/OtherUI 안에 StageProgressHUD와 ComboKillHUD를 씬 제작한다. StageProgressUI는 직렬화 StageManager와 선/위치/이벤트 표시 참조를 읽는다. 최상단 중앙의 경로 아래에는 기존 TimeText가 놓이며 이벤트 노드는 느낌표 아이콘만 표시한다. 노드 설명과 퍼센트 텍스트는 없다. ComboKillUI는 우측 상단, 타이머보다 아래에서 GM의 읽기 값으로 문구·3초 밑줄·10킬 이상 고대신 문구를 표시한다. 자릿수마다 느낌표가 추가된다(9 → 10! → 100!!). 두 UI는 런타임 계층 생성이나 새 Manager/DDOL 객체를 사용하지 않는다.

승인 범위와 검증 결과: [거리 스테이지와 콤보 HUD 세션](../../SessionLogs/2026-10-06-stage-combo.md).
