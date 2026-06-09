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
