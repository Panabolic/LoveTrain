---
status: active
authority: project-log
category: decision-log
last_reviewed: 2026-06-07
---

# Decision Log

## 2026-05-18 - Use Docs As Project Memory

Decision:
Use `Docs/` as LoveTrain's Markdown project memory root.

Reason:
Future Codex work should not rediscover the same Unity script structure, runtime ownership, and verification rules every time a task starts.

Implications:
- Markdown files are the source of truth for agent-facing memory.
- `Docs/README.md` routes task-specific reading.
- Small task outcomes go in `Docs/SessionLogs/`.
- Reusable structure and ownership maps go in `Docs/StructureMemory/`.
- No Presentation HTML layer is added in this first version.

## 2026-05-18 - Use AGENTS.md For Codex Project Instructions

Decision:
Place LoveTrain-specific Codex instructions in the repository root `AGENTS.md`.

Reason:
Codex reads project instructions from `AGENTS.md`, and Unity-specific safety rules should live next to the project.

Implications:
- Required reading order starts from `Docs/CurrentTask.md`, `Docs/ErrorLog.md`, `Docs/DecisionLog.md`, and `Docs/README.md`.
- Unity batchmode, static analysis, serialized-reference, and final-reporting rules live in `AGENTS.md`.
- Task documents can add context, but they should not override current user instructions or root safety rules.

## 2026-05-18 - Keep First Documentation System Lightweight

Decision:
Start with `CurrentTask`, `ErrorLog`, `DecisionLog`, `SessionLogs`, and `StructureMemory` only.

Reason:
LoveTrain does not yet have stable architecture/contracts docs comparable to CapstoneProject. Creating empty official layers would make the docs look more authoritative than the current code review supports.

Implications:
- Add `Architecture/`, `Contracts/`, `Guides/`, `RefactorBacklog/`, or `Presentation/` only when a later task needs them.
- Current script maps stay in `StructureMemory` until stable enough to promote.

## 2026-05-18 - Keep Train State Pure And Presentation In MonoBehaviour

Decision:
Move train speed-as-health state transitions into `TrainSpeedHealth`, while keeping `Train` as the existing scene-facing MonoBehaviour facade.

Reason:
The release-maintenance direction needs clearer responsibility boundaries without breaking scene/prefab references or existing item, enemy, UI, and stage call sites.

Implications:
- `TrainSpeedHealth` should stay free of DOTween, coroutines, GameObject access, singleton calls, and serialized Unity fields.
- `Train` keeps the current public API for `TakeDamage`, speed modification, healing, max-speed changes, death-speed queries, and dead/dying flags.
- Future train refactors should prefer adding pure state/services behind the existing facade before changing serialized or prefab-facing contracts.

## 2026-05-18 - Keep Item Cooldown State Behind ItemInstance

Decision:
Move item cooldown countdown and restart state into `ItemCooldownState`, while keeping `ItemInstance` as the current runtime item facade.

Reason:
Item cooldown logic is a low-editor-touch refactor target: it is gameplay-relevant but does not require scene, prefab, Inspector, ScriptableObject asset, or asmdef changes when the public `ItemInstance` surface is preserved.

Implications:
- `ItemCooldownState` should stay pure C# and should not know about `GameManager`, `Item_SO`, `GameObject`, or Unity lifecycle methods.
- `ItemInstance.currentCooldown` and `ItemInstance.maxCooldown` remain public compatibility mirrors for existing UI/debug usage.
- Manual cooldown behavior continues to use the existing `float.MaxValue` wait state until a broader item contract is approved.

## 2026-05-18 - Keep Generic Item Visual Upgrade Behind A Helper

Decision:
Move the generic Animator/SpriteRenderer upgrade fallback from `ItemInstance` into `ItemVisualUpgradeApplier`.

Reason:
`ItemInstance` should remain a runtime item facade, while fallback visual replacement is presentation logic. This separation does not require prefab, scene, Inspector, or ScriptableObject asset changes.

Implications:
- Item-specific upgrade behaviours should continue to implement `IInstantiatedItem`.
- `ItemVisualUpgradeApplier` is only the fallback for instantiated item objects without `IInstantiatedItem`.
- The fallback order remains Animator controller first, then SpriteRenderer sprite, matching the previous behavior.

## 2026-05-18 - Keep Inventory Queries Behind Inventory Public Methods

Decision:
Move inventory lookup and max/upgradable item query rules into `InventoryItemQuery`, while keeping `Inventory` as the public MonoBehaviour facade.

Reason:
External gameplay, event, and UI code already depends on `Inventory.FindItem(...)`, `Inventory.GetUpgradableItems(...)`, and `Inventory.IsItemMaxed(...)`. Moving only the query implementation reduces `Inventory` responsibility without changing call sites or Inspector data.

Implications:
- `Inventory.items` remains the serialized/public runtime list.
- External code should continue to call the existing `Inventory` methods.
- `InventoryItemQuery` preserves `Item_SO` reference equality matching and current max-level comparisons.

## 2026-05-18 - Keep Inventory Runtime Dispatch Behind Inventory Public Methods

Decision:
Move per-frame item ticking plus kill and hit item hook dispatch loops into `InventoryItemRuntimeDispatcher`, while keeping `Inventory` as the public MonoBehaviour facade.

Reason:
`Inventory` is still the scene-facing component and external call surface, but item loop dispatch is repeatable runtime coordination that can be separated without changing scenes, prefabs, Inspector data, or external call sites.

Implications:
- External code should continue to call `Inventory.ProcessKillEvent(...)` and `Inventory.ProcessHitEvent(...)`.
- `Inventory.Update()` remains the Unity lifecycle entry point.
- `InventoryItemRuntimeDispatcher` preserves list-order iteration and should not own item acquisition or UI change notification.

## 2026-05-18 - Keep Inventory Acquisition Behind Inventory Public Methods

Decision:
Move inventory item acquisition and upgrade execution into `InventoryItemAcquirer`, while keeping `Inventory.AcquireItem(...)` and `Inventory.UpgradeItemInstance(...)` as the public entry points.

Reason:
Acquisition and upgrade execution are gameplay rules, while `Inventory` should remain the scene-facing MonoBehaviour facade and UI change notification owner. This separation preserves external callers and Inspector-facing data.

Implications:
- `Inventory.items` remains the public runtime item list.
- `Inventory` remains responsible for invoking `OnInventoryChanged`.
- `InventoryItemAcquirer` should not own UI notifications or Unity lifecycle methods.

## 2026-05-18 - Keep Level-Up Choice Selection Behind UI Manager Entry Point

Decision:
Move level-up item availability filtering and random offer selection into `LevelUpChoiceSelector`, while keeping `LevelUpUIManager.ShowLevelUpChoices()` as the public UI entry point.

Reason:
Level-up UI wiring is scene-facing and should stay stable. The selection rule is gameplay logic and can be separated without changing serialized UI references, prefabs, ScriptableObject assets, or external callers.

Implications:
- `LevelUpUIManager` remains responsible for panel/slot activation and choice binding.
- `LevelUpChoiceSelector` preserves the current `System.Random` shuffle-and-take behavior.
- Future rarity, weighting, ban lists, or guarantee rules should start in `LevelUpChoiceSelector` rather than UI slot code.

## 2026-05-18 - Keep Level-Up Choice Display State Outside UI Widgets

Decision:
Move new-vs-upgrade and next-level/MAX display-state calculation into `LevelUpChoiceDisplayState`, while keeping `LevelUpChoiceUI.DisplayChoice(...)` as the public UI binding method.

Reason:
Level-up choice UI fields are scene-facing and should stay stable. The display-state rule is small gameplay/presentation logic that can be separated without changing serialized UI references or callers.

Implications:
- `LevelUpChoiceUI` remains responsible for assigning sprites, text, and GameObject active states.
- `LevelUpChoiceDisplayState` preserves the current rule where `nextLevel >= MaxUpgrade` uses the MAX sprite.
- Future display rules should start in `LevelUpChoiceDisplayState` before changing UI widget wiring.

## 2026-05-18 - Keep Train XP Progression Behind TrainLevelManager

Decision:
Move train XP totals, threshold calculation, display progress, and level advancement state into `TrainLevelProgression`, while keeping `TrainLevelManager` as the public MonoBehaviour facade.

Reason:
Enemy scaling, weapons, UI, and level-up flow already depend on `TrainLevelManager` public properties and events. Moving only the state rule reduces responsibility without touching scenes, prefabs, Inspector fields, ScriptableObject assets, or external call sites.

Implications:
- `TrainLevelManager` remains responsible for `OnExperienceGained`, `OnLevelUp`, and `GameManager.RegisterUIQueue(...)` level-up requests.
- `TrainLevelProgression` should stay pure C# and should not call UI, singletons, Unity lifecycle methods, or scene objects.
- Future XP curve or wall changes should start in `TrainLevelProgression`, while public facade changes require a migration plan.

## 2026-05-18 - Keep UI Queue Bookkeeping Behind GameManager

Decision:
Move gameplay-pausing UI queue pending/processing state and event-resume bookkeeping into `GameUiQueueController`, while keeping `GameManager` as the public queue facade and side-effect owner.

Reason:
`GameManager` must still coordinate global state, time scale, physics simulation mode, stage transition start, and public callers. Separating only queue bookkeeping reduces responsibility without changing scenes, prefabs, Inspector data, or external calls to `RegisterUIQueue(...)` and `CloseUI()`.

Implications:
- External systems should continue to call `GameManager.RegisterUIQueue(...)` and `GameManager.CloseUI()`.
- `GameUiQueueController` should stay pure C# and should not call `Time`, `Physics2D`, UI objects, singletons, scene loading, or stage transitions.
- Future popup queue ordering or cancellation rules should start in `GameUiQueueController`; global side effects should stay in `GameManager`.

## 2026-05-18 - Keep Kill Counters Behind GameManager

Decision:
Move normal, elite, boss, and total kill count state into `GameKillCounter`, while keeping `GameManager` as the public facade for existing enemy and ending UI callers.

Reason:
Enemy scripts and ending UI already depend on `GameManager.AddKillCount(...)`, `GameManager.AddBossKillCount()`, and kill count properties. Separating only counter storage and increments reduces `GameManager` responsibility without changing scenes, prefabs, Inspector data, or external call sites.

Implications:
- Enemy scripts should continue to report kills through `GameManager`.
- Ending UI should continue to read kill counts through `GameManager`.
- `GameKillCounter` should stay pure C# and should not know about enemy objects, UI, singletons, scenes, or Unity lifecycle methods.

## 2026-05-18 - Keep Item Runtime State Gate In One Policy

Decision:
Move the shared item runtime `GameState` gate into `ItemRuntimeGameStatePolicy`, while keeping item update methods and instantiated item behaviours as the existing execution entry points.

Reason:
`ItemInstance`, `PoisonMissileLauncher`, `RearGun`, and `Revolver` repeated the same `Playing`, `Boss`, and `Ending` state check. Centralizing that rule reduces future state-change mistakes without changing scenes, prefabs, serialized fields, ScriptableObject assets, or public item APIs.

Implications:
- Item runtime code should call `ItemRuntimeGameStatePolicy.CanRunItem(...)` for this shared gate.
- `ItemRuntimeGameStatePolicy` should stay pure C# and should not know about `GameManager.Instance`, item objects, UI, or Unity lifecycle methods.
- Changing allowed item runtime states is a gameplay/balance change because it affects cooldowns and autonomous item weapons.

## 2026-05-18 - Centralize Runtime Simulation Writes

Decision:
Move global `Time.timeScale` and `Physics2D.simulationMode` pause/resume writes into `GameSimulationController`, while keeping `GameManager`, `SceneLoader`, and `Option` as the existing public entry points.

Reason:
Pause, event UI, restart, and scene loading flows repeated the same global simulation writes. Centralizing the writes reduces future freeze/resume drift without changing scenes, prefabs, Inspector data, ScriptableObject assets, or public APIs.

Implications:
- Callers still decide when to pause or resume simulation.
- `GameSimulationController` should only apply global simulation settings and should not own `GameState`, UI queue ordering, scene loading, or stage transitions.
- New gameplay-pausing flows should route time/physics writes through `GameSimulationController`.

## 2026-05-18 - Centralize Runtime Pause Read Checks

Decision:
Expose runtime pause checks through `GameSimulationController.IsPaused` and route existing `Time.timeScale == 0` early-return checks through it.

Reason:
Simulation pause writes were already centralized, but multiple runtime scripts still repeated the raw time-scale pause predicate. Keeping the predicate behind the same controller reduces drift without changing caller timing, input flow, scenes, prefabs, Inspector data, serialized fields, or public `MonoBehaviour` contracts.

Implications:
- Callers still decide whether pause should suppress their own update work.
- `GameSimulationController.IsPaused` reads current Unity simulation state; it does not decide `GameState`.
- Future pause-sensitive early returns should use `GameSimulationController.IsPaused` instead of directly comparing `Time.timeScale`.

## 2026-05-18 - Keep Spawner Runtime State Gate Behind Spawner

Decision:
Move spawner runtime `GameState` gating into `SpawnerRuntimeStateGate`, while keeping `Spawner.Update()` as the timer and spawn execution owner.

Reason:
`Spawner.Update()` mixed the allowed state rule with phase updates, spawn timing, boss scheduling, and periodic spawn handling. Separating only the state gate makes the spawn-running rule explicit without changing scenes, prefabs, serialized fields, public APIs, timer behavior, or spawn execution order.

Implications:
- `Spawner` still owns `Update()`, phase refresh, spawn calls, and periodic task handling. Later decisions move timer state and selected phase runtime values behind focused helpers.
- `SpawnerRuntimeStateGate` preserves the previous allowed states: `GameState.Playing` and `GameState.Boss`.
- Future spawner runtime state changes should start in `SpawnerRuntimeStateGate`; spawn timing policy/state should stay in focused spawn helpers, while execution side effects stay in `Spawner`.

## 2026-05-18 - Keep Spawner Spawn Schedule Checks Behind Spawner

Decision:
Move basic mob, elite mob, and boss spawn schedule due checks into `SpawnerSpawnSchedule`, while keeping the timer state and spawn execution owners separate from due-check policy.

Reason:
`Spawner.Update()` still needs to own phase refresh, spawn calls, boss sequence starts, and periodic task handling, but the due-check rules are pure timing policy. Separating only those checks reduces `Spawner` responsibility without changing scenes, prefabs, serialized fields, public APIs, timer reset points, or spawn execution order.

Implications:
- At that step, `Spawner` still owned `mobTimer`, `eliteMobTimer`, `nextBossSpawnTime`, timer resets, and calls to `SpawnBasicMobs()`, `SpawnEliteMob()`, and `SpawnNextBoss()`. The later `SpawnerSpawnTimerState` decision moves timer state behind a helper while keeping spawn calls on `Spawner`.
- `SpawnerSpawnSchedule` preserves the previous rules: elite timing starts after `firstEliteSpawnTime`, timers are due when `timer >= interval`, and bosses spawn when `gameTime >= nextBossSpawnTime`.
- Future basic/elite/boss schedule policy changes should start in `SpawnerSpawnSchedule`; actual spawn side effects should stay in `Spawner`.

## 2026-05-18 - Keep Spawn Phase Selection Behind Spawner

Decision:
Move current-time-to-`SpawnPhase` selection into `SpawnerPhaseSelector`, while keeping `Spawner` as the serialized MonoBehaviour facade and spawn execution owner.

Reason:
`Spawner` should keep scene-authored spawn phase data, spawn points, boss settings, and spawn execution. The selection rule is pure and can be separated without changing scenes, prefabs, Inspector data, public methods, or spawn phase field names.

Implications:
- `Spawner.SpawnPhase` remains the serialized phase schema.
- `SpawnerPhaseSelector` should stay pure C# and should not call `GameManager`, `PoolManager`, Unity random, scene transforms, or spawn methods.
- Future phase ordering, fallback, or validation rules should start in `SpawnerPhaseSelector`.

## 2026-05-18 - Keep Current Spawn Phase State Outside Spawner

Decision:
Move selected spawn phase runtime ranges and spawn interval state into `SpawnerCurrentPhaseState`, while keeping `Spawner.SpawnPhase` as the serialized schema, `SpawnerPhaseSelector` as the selection rule, and `Spawner` as the spawn execution owner.

Reason:
`Spawner.UpdatePhase(...)` still copied selected phase values into multiple private fields on `Spawner`. Those fields are runtime bookkeeping, not scene-facing data. Separating them reduces `Spawner` responsibility without changing serialized phase field names, public APIs, scene or prefab references, phase selection behavior, spawn ranges, spawn interval behavior, or spawn execution order.

Implications:
- `Spawner.SpawnPhase` remains the serialized authoring schema.
- `SpawnerPhaseSelector` still decides which phase is selected for the current time.
- `SpawnerCurrentPhaseState` preserves previous current-phase state behavior: selected ranges and interval overwrite prior runtime values only when a phase is selected, and the default spawn interval remains 1.0 seconds before the first successful selection.
- Future selected phase runtime state changes should start in `SpawnerCurrentPhaseState`; phase selection rules should stay in `SpawnerPhaseSelector`.

## 2026-05-18 - Keep Boss Sequence Cursor Behind Spawner

Decision:
Move cyclic boss sequence index state into `SpawnerBossSequenceCursor`, while keeping `Spawner.bossSequence` as serialized data and `Spawner` as the boss spawn execution owner.

Reason:
The cursor state is pure bookkeeping. Separating it reduces `Spawner` responsibility without changing scenes, prefabs, Inspector data, public methods, boss sequence array schema, or boss spawn behavior.

Implications:
- `Spawner.bossSequence` remains the serialized boss order source.
- `SpawnerBossSequenceCursor` should stay pure C# and should not start coroutines, call `GameManager`, call `PoolManager`, instantiate bosses, or read scene transforms.
- Future boss sequence cursor behavior should start in `SpawnerBossSequenceCursor`.

## 2026-05-18 - Keep Boss Setting Lookup Behind Spawner

Decision:
Move boss setting lookup and default fallback creation into `SpawnerBossSettingLookup`, while keeping `Spawner.BossSpawnSetting` as the serialized schema and `Spawner` as the boss spawn execution owner.

Reason:
Boss setting lookup is pure selection/fallback logic. Separating it reduces `Spawner` responsibility without changing scenes, prefabs, Inspector data, public methods, boss setting field names, or boss spawn behavior.

Implications:
- `Spawner.bossSettings` remains the serialized boss configuration source.
- `SpawnerBossSettingLookup` should stay pure C# and should not start coroutines, show warning UI, call `PoolManager`, instantiate bosses, or read scene transforms.
- Future boss setting fallback or validation rules should start in `SpawnerBossSettingLookup`.

## 2026-05-18 - Keep Boss Spawn Placement Resolution Behind Spawner

Decision:
Move boss spawn point fallback and arrival position resolution into `SpawnerBossSpawnPlacement`, while keeping `Spawner.SpawnBossObject(...)` as the prefab lookup, instantiation, logging, and entrance routine side-effect owner.

Reason:
`Spawner.SpawnBossObject(...)` mixed boss prefab lookup, serialized setting lookup, spawn position fallback, missing-spawn-point logging, `Instantiate(...)`, `Boss` component lookup, and entrance routine start. Separating only position resolution makes the authored spawn/arrival rules explicit without changing scenes, prefabs, serialized fields, public APIs, boss prefab lookup, or coroutine flow.

Implications:
- At that step, `Spawner` still owned `PoolManager.instance.GetBoss(...)`, `Instantiate(...)`, missing spawn point logging, and `Boss.StartEntranceRoutine(...)`; those side effects are handled by the later `SpawnerBossObjectSpawner` decision.
- `SpawnerBossSpawnPlacement` preserves the previous fallback rule: use `setting.spawnPoint.position` when assigned, otherwise use the spawner transform position and let `Spawner` log the missing assignment.
- Future boss spawn/arrival placement rules should start in `SpawnerBossSpawnPlacement`; boss object creation and presentation side effects should stay in `Spawner`.

## 2026-05-18 - Keep Spawner Control Flags Behind Spawner Public Methods

Decision:
Move spawner enabled and rear-spawn enabled flag state into `SpawnerSpawnControlState`, while keeping `Spawner.SetSpawning(...)`, `Spawner.SetRearSpawning(...)`, and `StopAllCoroutines()` side effects on `Spawner`.

Reason:
`Spawner` mixed public event/train-facing control methods, private flag storage, periodic spawn gating, rear spawn position input, and coroutine stopping in one component. Separating only flag state and the disable-coroutine decision reduces state responsibility without changing scenes, prefabs, serialized fields, public APIs, event callers, or coroutine ownership.

Implications:
- `Spawner` still exposes `SetSpawning(...)` and `SetRearSpawning(...)` to train and event code.
- `Spawner` still calls `StopAllCoroutines()` only when spawning is disabled.
- `SpawnerSpawnControlState` preserves the previous reset defaults: spawning enabled and rear spawning enabled.
- Future spawn enable, rear-spawn, or disable policy changes should start in `SpawnerSpawnControlState`; actual coroutine stopping should stay in `Spawner`.

## 2026-05-18 - Keep Spawned Enemy Physics Reset Outside Spawner Flow

Decision:
Move spawned enemy `Rigidbody2D` velocity reset into `SpawnerEnemyPhysicsReset`, while keeping `Spawner` as the pool lookup, position assignment, death callback wiring, and batch spawn coroutine owner.

Reason:
`Spawner` should coordinate when and where enemies spawn, but resetting `linearVelocity` and `angularVelocity` is reusable spawned-object cleanup. Separating only that reset keeps spawn flow stable while reducing direct physics component manipulation inside `Spawner`.

Implications:
- `Spawner` still decides which enemy object is used, where it is placed, and when batch coroutine iterations yield.
- `SpawnerEnemyPhysicsReset` preserves the previous reset behavior: when a `Rigidbody2D` exists, set `linearVelocity` to `Vector2.zero` and `angularVelocity` to `0f`; otherwise no-op.
- Future spawned enemy physics cleanup rules should start in `SpawnerEnemyPhysicsReset`; pool lookup and spawn timing should stay in `Spawner`.

## 2026-05-18 - Keep Spawn Position Selection Behind Spawner

Decision:
Move ground and flying mob spawn position selection into `SpawnerSpawnPositionSelector`, while keeping `Spawner` as the serialized spawn point/area owner and spawn execution owner.

Reason:
Position selection is shared by normal mobs, event-spawned mobs, and batch spawns. Separating it reduces `Spawner` responsibility without changing scenes, prefabs, Inspector data, public methods, spawn point arrays, or spawn execution behavior.

Implications:
- `Spawner.groundFrontPoints`, `Spawner.groundRearPoints`, and `Spawner.flyMobSpawnAreas` remain the serialized authoring source.
- `SpawnerSpawnPositionSelector` may read Unity `Transform` and `BoxCollider2D` data, but it should not call `PoolManager`, mutate enemies, start coroutines, or own spawn timers.
- Future spawn-side weighting, safe-zone checks, or offscreen filtering should start in `SpawnerSpawnPositionSelector`.

## 2026-05-18 - Keep Mob Spawn Selection Behind Spawner

Decision:
Move basic and elite mob type/index selection into `SpawnerMobSpawnSelector`, while keeping `Spawner` as the phase-state holder and spawn execution owner.

Reason:
The random choice between ground/flying mobs and their current phase index ranges is gameplay selection logic. Separating it reduces `Spawner` responsibility without changing scenes, prefabs, Inspector data, public methods, spawn phase fields, or pool lookup behavior.

Implications:
- `Spawner` still applies selected phase values to its current index range fields.
- `Spawner` still calls `PoolManager` and owns actual enemy placement, physics reset, and death callback wiring.
- Future spawn weights, type guarantees, or elite selection rules should start in `SpawnerMobSpawnSelector`.

## 2026-05-18 - Keep Periodic Spawn Timing Behind Spawner

Decision:
Move periodic spawn task interval clamping, timer advancement, and timer reset into `SpawnerPeriodicSpawnScheduler`, while keeping `Spawner.AddPeriodicSpawnTask(...)` as the public event-facing API and `Spawner` as the spawn execution owner.

Reason:
Periodic event spawns are timer bookkeeping plus spawn execution. Separating the timer rules reduces `Spawner` responsibility without changing event call sites, public methods, scenes, prefabs, Inspector data, or enemy placement behavior.

Implications:
- Event effects should continue to call `Spawner.AddPeriodicSpawnTask(...)`.
- At that step, `Spawner` still owned the `periodicSpawnTasks` list and called `SpawnMobFromPrefab(...)` when a task was due. The later `SpawnerPeriodicTaskList` decision moves list storage behind a helper while keeping spawn execution in `Spawner`.
- Future periodic task stacking, cancellation, or interval policy should start in `SpawnerPeriodicSpawnScheduler`.

## 2026-05-18 - Keep Base Enemy Damage Eligibility Outside Damage Side Effects

Decision:
Move base enemy damage eligibility into `EnemyDamageGate`, while keeping `Enemy.TakeDamage(...)` as the public combat entry point and damage side-effect owner.

Reason:
`Enemy.TakeDamage(...)` mixed the alive/screen-entry eligibility check with HP subtraction, hit effect coroutine start, sound publishing, and death coroutine start. Separating only the boolean gate makes the base enemy damage rule explicit without changing serialized fields, public APIs, scenes, prefabs, or death behavior.

Implications:
- `Enemy` still owns HP mutation, hit effects, hit sound, and death coroutine start.
- `EnemyDamageGate` preserves the previous base damage rule of `isAlive && hasEnteredScreen`.
- Future base enemy damage eligibility changes should start in `EnemyDamageGate`; boss-specific overrides may keep narrower gates such as `TrainBossCombatGate`.

## 2026-05-18 - Keep Base Enemy Damage State Outside Damage Side Effects

Decision:
Move base enemy HP subtraction and death-threshold state into `EnemyDamageState`, while keeping `Enemy.TakeDamage(...)` as the public combat entry point and Unity side-effect owner.

Reason:
`Enemy.TakeDamage(...)` mixed HP mutation and death threshold checks with hit effect coroutine start, sound publishing, and death coroutine start. Separating only the damage result makes the base enemy damage state explicit without changing serialized fields, public APIs, scenes, prefabs, sound IDs, or death behavior.

Implications:
- `Enemy` still owns the damage gate, hit effect coroutine, hit sound, and death coroutine start.
- `EnemyDamageState` preserves the previous `currentHP - damageAmount` subtraction and `CurrentHp <= 0` death condition.
- Future base enemy damage math, shields, or death-threshold rules should start in `EnemyDamageState`; special enemies can keep separate state helpers when their lifecycle differs.

## 2026-05-18 - Keep Enemy Hit Material Writes In One Helper

Decision:
Move enemy hit material flag writes into `EnemyHitMaterialController`, while keeping `Enemy` as the lifecycle and combat facade.

Reason:
`Enemy` already allows enemies without sprite/material components. Centralizing `_isHit` writes keeps hit effects and disable cleanup on the same null-safe path without changing scenes, prefabs, serialized fields, public APIs, or pooling call sites.

Implications:
- `Enemy` remains responsible for taking damage, starting hit effect coroutines, and unregistering from `PoolManager`.
- `EnemyHitMaterialController` should only apply material hit flags and should not own enemy health, pooling, coroutines, or visuals beyond the material flag.
- Future material property name changes or hit shader guards should start in `EnemyHitMaterialController`.

## 2026-05-18 - Keep Active Enemy Registry Behind PoolManager

Decision:
Move active enemy register/unregister and despawn iteration rules into `PoolActiveEnemyRegistry`, while keeping `PoolManager.activeEnemies` public and keeping `PoolManager` public methods as the call surface.

Reason:
Enemy lifecycle code, item targeting code, train cleanup, and boss-death cleanup already depend on `PoolManager`. Moving only list mutation and cleanup loops reduces `PoolManager` responsibility without changing scenes, prefabs, Inspector data, public methods, or existing direct reads of `activeEnemies`.

Implications:
- External item code may continue to read `PoolManager.instance.activeEnemies`.
- Enemy lifecycle code should continue to call `PoolManager.RegisterEnemy(...)` and `PoolManager.UnregisterEnemy(...)`.
- Future active-enemy filtering, cleanup exclusions, or list consistency rules should start in `PoolActiveEnemyRegistry`.

## 2026-05-18 - Keep Pooled Object Provider Behind PoolManager

Decision:
Move pool list initialization, indexed/dynamic pool lookup, and inactive-object reuse/create rules into `PoolObjectProvider`, while keeping `PoolManager` as the serialized prefab source and public pool facade.

Reason:
`PoolManager` should continue to expose mob/boss lookup methods and hold scene-authored prefab arrays, but indexed pool bounds checks, object reuse, and creation are repeatable pool mechanics. Separating them reduces `PoolManager` responsibility without changing scenes, prefabs, Inspector data, public methods, prefab array fields, or spawned object naming.

Implications:
- `PoolManager` still decides which prefab array and index to use.
- `PoolObjectProvider` may apply the existing prefab-array index bounds rule, instantiate GameObjects under the given parent, set reused objects active, preserve `prefab.name`, and append newly created objects to the pool.
- Future pool prewarm, capacity, inactive cleanup, or naming policy should start in `PoolObjectProvider`.

## 2026-05-18 - Keep Boss Prefab Enum Lookup Behind PoolManager

Decision:
Move `BossName` enum-to-boss prefab array lookup into `PoolBossPrefabLookup`, while keeping `PoolManager.GetBoss(...)` as the public call surface.

Reason:
`PoolManager` should remain the serialized prefab holder and public pool facade, but the boss lookup contract depends on enum order matching the serialized boss array. Naming that contract makes future validation or migration work easier without changing scenes, prefabs, Inspector data, public methods, or current lookup behavior.

Implications:
- `PoolManager.GetBoss(...)` remains the method used by `Spawner`.
- `PoolBossPrefabLookup` preserves the previous lookup behavior: return `bosses[(int)boss]`.
- Future boss enum/prefab order validation should start in `PoolBossPrefabLookup`.

## 2026-05-18 - Keep Enemy HP Calibration In One Helper

Decision:
Move enemy and boss HP calibration formulas into `EnemyHpCalibration`, while keeping `Enemy`, `Mob`, `Boss`, and `Tentacle` as lifecycle/combat facades.

Reason:
Mob, tentacle, and boss HP calculations all depend on base HP plus runtime level/time/event modifiers. Centralizing formulas reduces balance drift without changing scenes, prefabs, Inspector data, serialized fields, public methods, or spawn/lifecycle behavior.

Implications:
- `Mob`, `Boss`, and `Tentacle` still decide when HP is recalculated and where `calibratedMaxHP` is stored.
- Runtime singleton reads remain in the existing MonoBehaviour code to preserve current lifecycle assumptions.
- Future HP scaling, elite multipliers, event debuff interpretation, or level/time scaling rules should start in `EnemyHpCalibration`.

## 2026-05-18 - Keep Boss Entrance Motion Formula Outside Boss

Decision:
Move boss entrance interpolation into `BossEntranceMotion`, while keeping `Boss.StartEntranceRoutine(...)` as the public entrance entry point.

Reason:
Entrance coroutine state and pattern gating are boss lifecycle concerns, but the smooth position interpolation formula is a pure presentation rule. Separating it reduces `Boss` responsibility without changing serialized fields, public entry points, coroutine timing, or Spawner calls.

Implications:
- `Boss` still owns the public `StartEntranceRoutine(...)` entry and stores `isEntranceActive`.
- `BossEntranceMotion` should only evaluate positions and should not start coroutines, set transforms, call `Spawner`, or own boss state.
- Future entrance easing or interpolation changes should start in `BossEntranceMotion`.

## 2026-05-18 - Keep Boss Entrance Routine Sequence Outside Boss

Decision:
Move boss entrance active-flag timing, movement loop, and final position assignment into `BossEntranceRoutine`, while keeping `Boss.StartEntranceRoutine(...)` as the public coroutine start entry.

Reason:
`Boss` should remain the scene-facing lifecycle facade, but the entrance sequence is reusable presentation flow. Moving the sequence behind a helper reduces boss responsibility without changing serialized fields, public methods, `Spawner` entrance handoff, `isEntranceActive` meaning, interpolation formula, or final transform assignment.

Implications:
- `Boss` starts the coroutine and exposes the same entrance API.
- `BossEntranceRoutine` preserves the previous order: set entrance active, interpolate each frame with `BossEntranceMotion`, snap to target position, then clear entrance active.
- Future entrance timing, cancellation, or final-position behavior should start in `BossEntranceRoutine`; easing math should remain in `BossEntranceMotion`.

## 2026-05-18 - Keep Boss Kill Event Request Eligibility Outside Death Side Effects

Decision:
Move boss kill-event request eligibility into `BossKillEventRequestGate`, while keeping `EyeBoss.Die()` and `TrainBoss.Die()` as the event request, kill count, boss-death transition, and stage fallback side-effect owners.

Reason:
`EyeBoss.Die()` and `TrainBoss.Die()` repeated the same condition for requesting kill events: a kill event exists, `GameManager` exists, and the run is not at ending time. Separating only this boolean rule keeps boss death side effects stable while making the event request rule explicit.

Implications:
- `EyeBoss` and `TrainBoss` still own boss-specific death presentation and wait timing.
- `BossKillEventRequestGate` preserves the previous request rule of `hasKillEvent && hasGameManager && !isTimeForEnding`.
- Future boss kill-event request eligibility changes should start in `BossKillEventRequestGate`; side-effect execution should stay behind `BossDeathCompletionExecutor`.

## 2026-05-18 - Keep Boss Death Completion Route Outside Death Side Effects

Decision:
Move boss death completion route selection into `BossDeathCompletionRouter`, while keeping `EyeBoss.Die()` and `TrainBoss.Die()` as the boss kill count, boss-death transition, stage fallback, and destroy side-effect owners.

Reason:
`EyeBoss.Die()` and `TrainBoss.Die()` repeated the same route decision: use `GameManager` when it exists, otherwise fall back to `StageManager`. Separating only the route selection makes the fallback rule explicit without changing the high-risk death side effects.

Implications:
- `EyeBoss` and `TrainBoss` still own boss-specific death presentation and wait timing.
- `BossDeathCompletionRouter` preserves the previous route rule of `hasGameManager ? GameManager : StageManagerFallback`.
- Future boss death completion route changes should start in `BossDeathCompletionRouter`; side-effect execution should stay behind `BossDeathCompletionExecutor`.

## 2026-05-18 - Keep Boss Death Completion Execution In One Helper

Decision:
Move shared boss death completion side effects into `BossDeathCompletionExecutor`, while keeping `EyeBoss.Die()` and `TrainBoss.Die()` as the boss-specific death presentation and wait-timing owners.

Reason:
`EyeBoss.Die()` and `TrainBoss.Die()` repeated the same kill-event request, boss kill count, boss-death transition, stage fallback, and destroy block. Separating the shared execution makes the completion contract explicit without changing scenes, prefabs, serialized fields, public APIs, boss-specific explosion timing, death sounds, or wait ordering.

Implications:
- `EyeBoss` still clears tentacles, spawns its kill explosion, yields base death, and waits before completion.
- `TrainBoss` still yields base death, spawns its offset kill explosion, publishes `SoundID.Boss_Die`, and waits before completion.
- `BossDeathCompletionExecutor` preserves the previous shared completion order: request kill event when allowed, choose `GameManager` or `StageManager` route, then destroy the boss GameObject.
- Future shared boss kill reward, transition, fallback, or destroy behavior should start in `BossDeathCompletionExecutor`.

## 2026-05-18 - Keep Boss Death Explosion Spawning In One Helper

Decision:
Move boss death explosion instantiation into `BossDeathExplosionSpawner`, while keeping `EyeBoss.Die()` and `TrainBoss.Die()` as the boss-specific death timing owners.

Reason:
`EyeBoss.Die()` and `TrainBoss.Die()` both instantiate `killExplosionEffect` during death handling, with TrainBoss applying its existing offset. Separating only the instantiate call names the shared presentation side effect without changing scenes, prefabs, serialized fields, public APIs, explosion timing, sound publishing, wait order, or completion routing.

Implications:
- `EyeBoss` still decides to clear tentacles before spawning its explosion at `transform.position`.
- `TrainBoss` still decides to spawn its explosion after base death at `transform.position + (-5f, 2f)` and publish `SoundID.Boss_Die`.
- `BossDeathExplosionSpawner` preserves the previous spawn behavior by calling `Object.Instantiate(explosionEffect, position, Quaternion.identity)`.
- Future boss death explosion null handling, pooling, or VFX routing should start in `BossDeathExplosionSpawner`.

## 2026-05-18 - Keep Eye Boss Pattern Start Gate Outside Update

Decision:
Move eye boss normal pattern start eligibility into `EyeBossPatternStartGate`, while keeping `EyeBoss.Update()` as the coroutine start owner.

Reason:
`EyeBoss.Update()` mixed lifecycle/pattern-state gating with side/center pattern selection and coroutine starts. Separating the boolean start gate makes the state rule explicit without changing scenes, prefabs, serialized fields, public APIs, pattern coroutine order, tentacle spawning, enrage behavior, or sound publishing.

Implications:
- `EyeBoss` still owns `StartCoroutine(SideAttackPattern())` and `StartCoroutine(CenterAttackPattern())`.
- `EyeBossPatternStartGate` preserves the previous start rule: alive, not busy, no enrage pattern pending, and no entrance motion active.
- Future eye boss normal-pattern gating changes should start in `EyeBossPatternStartGate`; pattern selection should start in `EyeBossNormalPatternSelector`, and coroutine side effects should stay in `EyeBoss`.

## 2026-05-18 - Keep Eye Boss Tentacle Spawn Point Collection In One Helper

Decision:
Move eye boss child-transform collection for tentacle spawn points into `EyeBossTentacleSpawnPointCollector`, while keeping `EyeBoss.Start()` as the Unity lifecycle owner and boss spawn sound publisher.

Reason:
`EyeBoss.Start()` mixed startup sound publishing with the authored child-transform traversal that defines tentacle spawn point order. Separating only the collection rule names the scene-child contract without changing scenes, prefabs, serialized fields, public APIs, child order, or pattern timing.

Implications:
- `EyeBoss` still owns `Start()`, calls `base.Start()`, publishes `SoundID.Boss_Roar`, and stores the resulting spawn point array.
- `EyeBossTentacleSpawnPointCollector` preserves the previous `childCount` length and `GetChild(i)` index-order copy.
- Future eye boss spawn point validation, child filtering, or authoring diagnostics should start in `EyeBossTentacleSpawnPointCollector`.

## 2026-05-18 - Keep Eye Boss Tentacle Registry Behind EyeBoss

Decision:
Move eye boss spawned-tentacle list registration, unregistration, active checks, and cleanup iteration into `EyeBossTentacleRegistry`, while keeping `EyeBoss.RegisterTentacle(...)` and `EyeBoss.UnregisterTentacle(...)` as the public call surface for `Tentacle`.

Reason:
`EyeBoss` should keep pattern coroutine state and serialized tentacle prefab references, but the spawned tentacle list rules are plain runtime bookkeeping. Separating them reduces `EyeBoss` responsibility without changing scenes, prefabs, Inspector data, public methods, or tentacle setup/destruction call sites.

Implications:
- `Tentacle` should continue to call `EyeBoss.RegisterTentacle(...)` and `EyeBoss.UnregisterTentacle(...)`.
- `EyeBossTentacleRegistry` may destroy tentacle GameObjects during cleanup, but should not own pattern selection, damage, timing, or sound publishing.
- Future spawned tentacle cleanup, deduplication, or active-count rules should start in `EyeBossTentacleRegistry`.

## 2026-05-18 - Keep Eye Boss Attack Sound Cooldown Rule And State Outside EyeBoss

Decision:
Move the eye boss tentacle attack sound cooldown check into `EyeBossAttackSoundCooldown` and the last-play timestamp storage into `EyeBossAttackSoundCooldownState`, while keeping `EyeBoss` as the sound publishing owner.

Reason:
The 0.1-second anti-spam rule is a small runtime policy, and the last-play timestamp is runtime bookkeeping. Separating both makes the cooldown ownership explicit without changing scenes, prefabs, serialized fields, public methods, the initial timestamp, mark-after-publish order, or `SoundEventBus` usage.

Implications:
- `EyeBoss` still publishes `SoundID.Boss_TentacleAttack` before marking the sound as played.
- `EyeBossAttackSoundCooldown` should only answer whether the sound can play.
- `EyeBossAttackSoundCooldownState` preserves the previous initial last-play time of `-10f`.
- Future tentacle attack sound interval changes should start in `EyeBossAttackSoundCooldown`; timestamp reset or state changes should start in `EyeBossAttackSoundCooldownState`.

## 2026-05-18 - Keep Eye Boss Tentacle Pattern Planning Outside EyeBoss

Decision:
Move side, center, and enrage tentacle spawn/weak index planning into `EyeBossTentaclePatternPlanner`, while keeping `EyeBoss` as the pattern coroutine owner.

Reason:
The pattern routines should coordinate coroutine timing and boss state, but the spawn index and weak-point index construction is pure pattern planning. Separating it reduces `EyeBoss` responsibility without changing scenes, prefabs, Inspector data, public methods, tentacle prefab references, or actual instantiation behavior.

Implications:
- `EyeBoss` still chooses which coroutine to run; tentacle spawn execution is delegated to `EyeBossTentacleSpawner`.
- `EyeBossTentaclePatternPlanner` should only build pattern plans and should not instantiate, publish sounds, start coroutines, or mutate boss state.
- Future side/center/enrage pattern layout changes should start in `EyeBossTentaclePatternPlanner`.

## 2026-05-18 - Keep Train Boss Combat Rules Behind TrainBoss

Decision:
Move train boss phase 2 threshold checks into `TrainBossPhaseTransition`, and move knockback cooldown/force selection into `TrainBossKnockbackPolicy`, while keeping `TrainBoss` as the stun coroutine, sound, and damage facade.

Reason:
`TrainBoss` is scene-facing boss behavior with serialized phase, collider, and knockback fields. Separating only pure rule checks reduces responsibility without changing scenes, prefabs, Inspector data, public methods, or runtime force/state side effects.

Implications:
- `TrainBoss` still owns phase entry ordering, phase roar sound, and stun coroutine ordering.
- `TrainBossPhaseTransition` should only decide whether phase 2 should start.
- `TrainBossKnockbackPolicy` should only decide whether knockback is allowed for a provided timestamp and which force vector to apply.
- Future phase thresholds or knockback cooldown/force rules should start in these helpers before changing `TrainBoss` wiring.

## 2026-05-18 - Keep Train Boss Phase State Outside TrainBoss

Decision:
Move train boss phase 2 entered-state storage into `TrainBossPhaseState`, while keeping `TrainBoss.CheckPhase()` as the phase entry side-effect owner.

Reason:
`TrainBoss` should coordinate damage, roar sound, phase presentation, stun, and knockback side effects, but the phase 2 entered flag is runtime bookkeeping. Separating that state reduces `TrainBoss` responsibility without changing serialized fields, public APIs, scenes, prefabs, phase threshold math, roar timing, phase presentation, or knockback force selection.

Implications:
- `TrainBossPhaseState` preserves the previous default phase 1 state.
- `TrainBoss.CheckPhase()` still publishes `SoundID.Boss_Roar`, marks phase 2 entered, then applies phase 2 presentation in the same order.
- `TrainBoss.Knockback()` still selects phase 1 or phase 2 force from the current phase state.
- Future phase state reset or multi-phase state changes should start in `TrainBossPhaseState`; threshold math stays in `TrainBossPhaseTransition`.

## 2026-05-18 - Keep Train Boss Knockback Cooldown Timestamp Outside TrainBoss

Decision:
Move train boss last-knockback timestamp storage and update into `TrainBossKnockbackCooldownState`, while keeping `TrainBoss.TakeDamage(...)` as the damage, phase check, and knockback execution entry point.

Reason:
`TrainBoss` should remain the scene-facing boss facade, while cooldown timestamp storage is runtime bookkeeping. Separating that state reduces `TrainBoss` responsibility without changing serialized fields, public APIs, phase checks, stun coroutine ordering, force selection, or the previous mark-after-knockback order.

Implications:
- `TrainBossKnockbackCooldownState` preserves the previous initial last-knockback time of `-999f`.
- `TrainBoss` still decides when to call `Knockback()` and marks the timestamp after knockback starts, matching the previous order.
- Future train boss knockback timestamp reset, reservation, or multi-hit gating state changes should start in `TrainBossKnockbackCooldownState`; cooldown condition math stays in `TrainBossKnockbackPolicy`.

## 2026-05-18 - Keep Train Boss Knockback Physics Application In One Helper

Decision:
Move train boss knockback velocity reset and impulse application into `TrainBossKnockbackApplier`, while keeping `TrainBoss.Knockback()` as the stun coroutine ordering owner.

Reason:
`TrainBoss.Knockback()` still mixed force selection, stun coroutine restart, `Rigidbody2D.linearVelocity` reset, and `AddForce(...)`. Separating only the Rigidbody2D application names the physics side effect without changing scenes, prefabs, Inspector data, serialized fields, public APIs, cooldown timing, force selection, stun timing, or impulse mode.

Implications:
- `TrainBoss` still chooses when knockback runs and restarts the `Stun` coroutine; last-knockback timestamp storage is delegated to `TrainBossKnockbackCooldownState`.
- `TrainBossKnockbackApplier` preserves previous physics application: clear velocity to `Vector2.zero`, then call `AddForce(force, ForceMode2D.Impulse)`.
- Future train boss knockback physics changes should start in `TrainBossKnockbackApplier`; cooldown and force selection should stay in `TrainBossKnockbackPolicy`.

## 2026-05-18 - Keep Train Boss Phase Presentation In One Helper

Decision:
Move train boss initial collider setup and phase 2 animator/collider presentation into `TrainBossPhasePresentation`, while keeping `TrainBoss` as the serialized field owner and phase entry sound owner.

Reason:
`TrainBoss.Awake()` and `CheckPhase()` still directly applied phase collider state and the phase 2 animator trigger. Separating the presentation side effects reduces `TrainBoss` responsibility without changing scenes, prefabs, Inspector data, public methods, serialized collider fields, phase threshold rules, knockback behavior, or sound timing.

Implications:
- Initial collider setup still enables phase 1 and disables phase 2 after `SoundID.Boss_TrainBossSpawn` is published.
- Phase 2 entry still publishes `SoundID.Boss_Roar`, marks phase 2 entered through `TrainBossPhaseState`, then triggers `phase2`, swaps colliders, and logs the same message.
- `TrainBossPhasePresentation` preserves the existing collider null guards and does not own phase state storage, HP thresholds, stun, knockback, Rigidbody2D writes, or damage side effects.
- Future train boss phase animation, collider presentation, or phase debug-log changes should start in `TrainBossPhasePresentation`.

## 2026-05-18 - Keep Train Boss Combat Eligibility Outside Side Effects

Decision:
Move train boss damage eligibility and train-collision eligibility gates into `TrainBossCombatGate`, while keeping `TrainBoss.TakeDamage(...)` and `OnTriggerEnter2D(...)` as the side-effect owners.

Reason:
`TrainBoss.TakeDamage(...)` mixed damage eligibility with base damage, phase transition, knockback cooldown, and knockback side effects. `OnTriggerEnter2D(...)` mixed alive-state gating with train collision handling. Separating only the boolean gates makes the combat eligibility rules explicit without changing scenes, prefabs, serialized fields, public APIs, or collision callbacks.

Implications:
- `TrainBoss` still owns `base.TakeDamage(...)`, phase transition checks, knockback execution, `lastKnockbackTime` updates, and train collision side effects.
- `TrainBossCombatGate` preserves the previous damage rule of `isAlive && hasEnteredScreen`, and the previous train-collision rule of `isAlive`.
- Future train boss combat eligibility rules should start in `TrainBossCombatGate`.

## 2026-05-18 - Keep Train Boss Forward Movement Policy Outside Rigidbody Side Effects

Decision:
Move train boss forward movement direction, velocity composition, and sprite-facing constants into `TrainBossMovementPolicy`, while keeping `TrainBoss.FixedUpdate()` as the Rigidbody2D assignment owner.

Reason:
`TrainBoss.FixedUpdate()` and `SetMoveDirection(...)` encoded the always-left boss movement rule directly beside stun gating and Rigidbody2D writes. Separating only the movement formula reduces `TrainBoss` responsibility without changing serialized fields, public APIs, scenes, prefabs, collider phase behavior, or knockback side effects.

Implications:
- `TrainBoss` still owns target lookup timing, `moveDirection` storage, `Rigidbody2D.linearVelocity` assignment, and sprite write dispatch through `EnemySpritePresentation`; stun state storage is delegated to `TrainBossStunState`.
- `TrainBossMovementPolicy` preserves the previous `Vector2.left` direction, `moveDirection.x * moveSpeed` x velocity, current y velocity preservation, and `flipX = false` rule.
- Future train boss forward movement or sprite-facing rules should start in `TrainBossMovementPolicy`.

## 2026-05-18 - Keep Optional Enemy Sprite Writes In One Helper

Decision:
Move enemy sprite visibility, color reset, and flip writes into `EnemySpritePresentation`, while keeping enemy lifecycle, movement, and combat decisions in `Enemy`, `Boss`, `Mob`, `FlyMob`, and `TrainBoss`.

Reason:
Some enemy-like objects may not have a `SpriteRenderer`, but direct sprite writes remained in death, enable, and movement presentation paths. A single null-safe helper keeps optional presentation handling consistent without changing scenes, prefabs, serialized fields, public APIs, or movement rules.

Implications:
- Enemy movement and death code should continue to decide when sprites are shown, hidden, reset, or flipped.
- `EnemySpritePresentation` should only apply optional `SpriteRenderer` writes.
- Future enemy sprite visibility, color reset, or flip changes should route through `EnemySpritePresentation`.

## 2026-05-18 - Keep Enemy Screen Entry Checks Outside Enemy Lifecycle

Decision:
Move enemy screen-entry targetable checks into `EnemyScreenEntryChecker`, while keeping `Enemy` as the lifecycle and `hasEnteredScreen` state owner.

Reason:
`Enemy.CheckScreenEntry()` mixed lifecycle state updates with camera bounds, collider bounds, sprite bounds, and point fallback calculations. Separating the visibility check makes targetable entry rules explicit without changing scenes, prefabs, serialized fields, public APIs, or the one-way `hasEnteredScreen` transition.

Implications:
- `Enemy` still decides when to check and when to set `hasEnteredScreen`.
- `EnemyScreenEntryChecker` should only answer whether the enemy has entered the camera bounds.
- Future targetable entry rules, offscreen margins, or special fallback behavior should start in `EnemyScreenEntryChecker`.

## 2026-05-18 - Keep Enemy Layer Reset In One Helper

Decision:
Move enemy layer reset on enable into `EnemyLayerAssignment`, while keeping `Enemy.OnEnable()` and `Boss.OnEnable()` as the lifecycle owners.

Reason:
Both base enemies and bosses reset `gameObject.layer` to the `Enemy` layer when enabled. Naming this rule reduces lifecycle duplication and gives future layer-name validation a single entry point without changing scenes, prefabs, Inspector data, public methods, or layer behavior.

Implications:
- `Enemy` and `Boss` still decide when enable-time state resets happen.
- `EnemyLayerAssignment` preserves the previous layer rule: assign `LayerMask.NameToLayer("Enemy")`.
- Future enemy layer validation or fallback behavior should start in `EnemyLayerAssignment`.

## 2026-05-18 - Keep Enemy Target Lookup Outside Enemy Lifecycle Setup

Decision:
Move enemy player target component lookup into `EnemyTargetResolver`, while keeping `Enemy` as the owner of `targetRigid` and `levelManager` fields.

Reason:
`Enemy.Awake()` mixed component setup with player object discovery and target component lookup. Separating the lookup reduces `Enemy` setup responsibility without changing scenes, prefabs, serialized fields, public APIs, or target field usage in enemy subclasses.

Implications:
- `Enemy` still stores and exposes target component references to subclasses through existing protected fields.
- `EnemyTargetResolver` preserves the current `GameObject.FindWithTag("Player")` lookup and component reads.
- Future target lookup fallback, caching, or spawn-time injection should start in `EnemyTargetResolver`.

## 2026-05-18 - Keep Enemy Death Rewards Outside Enemy Death Lifecycle

Decision:
Move enemy death XP reward and inventory kill-hook dispatch into `EnemyDeathRewardDispatcher`, while keeping `Enemy` as the death lifecycle and presentation owner.

Reason:
`Enemy.Die()` mixed the death state transition with XP reward, inventory kill-hook dispatch, sprite hiding, particle creation, and coroutine timing. Separating reward dispatch reduces coupling to `TrainLevelManager` and `Inventory` without changing scenes, prefabs, serialized fields, public APIs, or death order.

Implications:
- `Enemy` still decides when death starts, hides presentation, requests kill particle spawning, and yields coroutine timing.
- `EnemyDeathRewardDispatcher` preserves the current reward order: gain XP first, then call `Inventory.ProcessKillEvent(...)` when available.
- Future reward routing, item kill hooks, or XP suppression rules should start in `EnemyDeathRewardDispatcher`.

## 2026-05-18 - Keep Enemy Kill Particle Spawning In One Helper

Decision:
Move enemy kill particle instantiation into `EnemyKillParticleSpawner`, while keeping `Enemy` and `Tentacle` as the death side-effect ordering owners.

Reason:
Base enemies and tentacles both instantiate `killParticle` during death handling. Separating only the instantiate call names the presentation side effect without changing scenes, prefabs, serialized fields, public APIs, death ordering, sound publishing, or object cleanup behavior.

Implications:
- `Enemy` and `Tentacle` still decide when death presentation happens.
- `EnemyKillParticleSpawner` preserves the previous spawn behavior by calling `Object.Instantiate(killParticle, position, rotation)`.
- Future kill-particle null handling, pooling, or VFX routing should start in `EnemyKillParticleSpawner`.

## 2026-05-18 - Keep Mob Movement State Selection Outside Rigidbody Side Effects

Decision:
Move Mob/FlyMob alive/stunned movement state selection into `MobMovementStateResolver`, while keeping `Mob.FixedUpdate()` and `FlyMob.FixedUpdate()` as the Rigidbody2D assignment, direction selection, and side-effect owners.

Reason:
`Mob.FixedUpdate()` and `FlyMob.FixedUpdate()` repeated the same state branching for death slide versus active movement. Separating only the state selection reduces duplicated control flow without changing scenes, prefabs, serialized fields, public APIs, velocity formulas, or Rigidbody2D writes.

Implications:
- `Mob` and `FlyMob` still own `moveDirection` writes, target-position reads, velocity application, sprite-facing calls, and death-slide return flow.
- `MobMovementStateResolver` preserves the previous rule: stunned means no movement update, alive and not stunned means active movement, dead and not stunned means death slide.
- Future mob movement-state rules should start in `MobMovementStateResolver`; velocity formulas should stay in `MobVelocityPlanner`.

## 2026-05-18 - Keep Mob Velocity Formulas Outside Mob Movement State

Decision:
Move ground mob, flying mob, and post-death mob velocity formulas into `MobVelocityPlanner`, while keeping `Mob` and `FlyMob` as the `FixedUpdate`, direction selection, stun/death state, and Rigidbody2D assignment owners.

Reason:
`Mob.FixedUpdate()` and `FlyMob.FixedUpdate()` repeated the post-death slide velocity and directly encoded ground/flying velocity formulas. Separating the formulas reduces duplicated movement math without changing scenes, prefabs, serialized fields, public APIs, or state checks.

Implications:
- `Mob` and `FlyMob` still decide whether movement is alive, stunned, or post-death.
- `MobVelocityPlanner` preserves the previous 30.0f leftward death slide speed, ground x-axis velocity, and flying normalized velocity.
- Future mob velocity formula changes should start in `MobVelocityPlanner`.

## 2026-05-18 - Keep Mob Direction Formulas Outside Mob Presentation

Decision:
Move ground mob and flying mob direction selection formulas into `MobDirectionPlanner`, while keeping `Mob` and `FlyMob` as the `FixedUpdate`, movement state, sprite flip, and Rigidbody2D assignment owners.

Reason:
`Mob.SetMoveDirection(...)` and `FlyMob.SetMoveDirection(...)` encoded target-position math directly inside scene-facing behaviours. Separating only the direction formulas makes movement rules easier to test and extend without changing scenes, prefabs, serialized fields, public APIs, or movement entry points.

Implications:
- `Mob` and `FlyMob` still decide when direction should be recalculated and when sprites should flip.
- `MobDirectionPlanner` preserves the previous ground x-axis choice, zero direction on equal x, flying `diveDistance` threshold, and normalized dive direction.
- Future ground/flying direction rules should start in `MobDirectionPlanner`.

## 2026-05-18 - Keep Mob Train Collision Handling Behind Mob Trigger

Decision:
Move mob train-layer collision handling, train damage application, and default camera shake into `MobTrainCollisionHandler`, while keeping `Mob.OnTriggerEnter2D(...)` as the Unity trigger entry point and keeping `Mob` responsible for starting its death coroutine.

Reason:
`Mob.OnTriggerEnter2D(...)` mixed Unity trigger entry, layer filtering, train lookup, player damage, camera shake, and death start. Separating the interaction side effects reduces `Mob` responsibility without changing scenes, prefabs, serialized fields, public APIs, collision callbacks, or the existing die-on-train-layer behavior.

Implications:
- `Mob` still starts `Die()` when a collision is on the `Train` layer, including the previous case where no `Train` component is found.
- `MobTrainCollisionHandler` preserves the previous `LayerMask.NameToLayer("Train")`, `GetComponentInParent<Train>()`, `Train.TakeDamage(damage)`, and default `CameraShakeManager.Instance.ShakeCamera()` behavior.
- Future normal-mob train collision damage or shake rules should start in `MobTrainCollisionHandler`.

## 2026-05-18 - Keep Mob Knockback Force Calculation Outside Mob Lifecycle

Decision:
Move normal mob knockback force calculation into `MobKnockbackPolicy`, while keeping `Mob.Knockback(...)` as the public call surface and keeping `Mob` responsible for starting stun and applying the Rigidbody2D impulse.

Reason:
`Mob.Knockback(...)` is called by item code and mixed public gameplay entry, stun lifecycle, force-vector math, and physics side effects. Separating only the vector rule reduces `Mob` responsibility without changing public APIs, serialized fields, scenes, prefabs, stun timing, or `Rigidbody2D.AddForce(...)` ownership.

Implications:
- Item code should continue to call `Mob.Knockback(direction, power)`.
- `MobKnockbackPolicy` preserves the previous `direction.normalized * power` rule.
- Future normal mob knockback direction or power rules should start in `MobKnockbackPolicy`.

## 2026-05-18 - Keep Mob Death Completion Behind Mob Coroutine

Decision:
Move normal mob kill count reporting and post-base-death completion side effects into `MobDeathCompletion`, while keeping `Mob.Die()` as the coroutine ordering owner.

Reason:
`Mob.Die()` mixed kill count reporting, base enemy death yielding, death sound publishing, sprite hiding, GameObject deactivation, and respawn callback invocation. Separating the completion side effects makes the normal mob death contract explicit without changing scenes, prefabs, serialized fields, public APIs, pooling, or callback order.

Implications:
- `Mob.Die()` still reports the kill before yielding `base.Die()`, then completes mob-specific death after the base death flow.
- `MobDeathCompletion` preserves the previous order: publish `SoundID.Enemy_Die`, hide the sprite, deactivate the GameObject, then invoke `OnDied`.
- Future normal mob death sound, kill count reporting, deactivation, or respawn callback changes should start in `MobDeathCompletion`.

## 2026-05-18 - Keep TrainBoss Train Collision Handling Behind TrainBoss Trigger

Decision:
Move train boss train-layer collision handling, boss-damage application, and boss camera shake into `TrainBossTrainCollisionHandler`, while keeping `TrainBoss.OnTriggerEnter2D(...)` as the Unity trigger entry point and keeping `TrainBoss` responsible for alive-state gating.

Reason:
`TrainBoss.OnTriggerEnter2D(...)` mixed the trigger entry point with layer filtering, train lookup, boss damage application, and boss-specific camera shake. Separating the side effects reduces `TrainBoss` responsibility without changing scenes, prefabs, serialized fields, public APIs, collision callbacks, boss damage semantics, or phase/knockback behavior.

Implications:
- `TrainBoss` still ignores collisions when it is not alive.
- `TrainBossTrainCollisionHandler` preserves the previous `LayerMask.NameToLayer("Train")`, `GetComponentInParent<Train>()`, `Train.TakeDamage(damage, true)`, and `CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f)` behavior.
- Future train boss train-collision damage or shake rules should start in `TrainBossTrainCollisionHandler`.

## 2026-05-18 - Keep Tentacle Train Collision Handling Behind Tentacle Trigger

Decision:
Move tentacle player-tag collision handling and train damage forwarding into `TentacleTrainCollisionHandler`, while keeping `Tentacle.OnTriggerEnter2D(...)` as the Unity trigger entry point.

Reason:
`Tentacle.OnTriggerEnter2D(...)` mixed Unity trigger entry, `Player` tag filtering, `Train` component lookup, and train damage forwarding. Separating the side effect reduces `Tentacle` responsibility without changing scenes, prefabs, serialized fields, public APIs, collision callbacks, attack timing, owner registration, or destroy flow.

Implications:
- `Tentacle` still owns attack coroutine timing, animation trigger, owner registration, HP scaling, damage intake, and destruction.
- `TentacleTrainCollisionHandler` preserves the previous `CompareTag("Player")`, `GetComponent<Train>()`, and `Train.TakeDamage(damage)` behavior.
- Future eye-boss tentacle collision damage rules should start in `TentacleTrainCollisionHandler`.

## 2026-05-18 - Keep Tentacle Attack Duration Resolution Behind Tentacle Setup

Decision:
Move tentacle attack animation duration resolution into `TentacleAttackAnimationDurationResolver`, while keeping `Tentacle.Setup(...)`, `BeginAttack()`, `AttackRoutine()`, and `GetTotalDuration()` as the existing lifecycle and timing call surface.

Reason:
`Tentacle.Setup(...)` mixed owner registration, damage/timing setup, sound callback storage, and Animator clip scanning. Separating only the clip-duration lookup reduces `Tentacle` setup responsibility without changing scenes, prefabs, serialized fields, public APIs, attack coroutine order, owner registration, or destruction.

Implications:
- At that step, `Tentacle` still owned attack wait time, animation trigger, callback invocation, attack-duration wait, destroy wait, owner register/unregister, and `GetTotalDuration()`; later `TentacleAttackRoutine` took the attack sequence and total duration calculation.
- `TentacleAttackAnimationDurationResolver` preserves the previous `runtimeAnimatorController` gate, `Attack`/`attack` clip-name search, first-match return, and 1.0-second default only when an animator controller exists.
- Future tentacle attack clip naming or fallback duration rules should start in `TentacleAttackAnimationDurationResolver`.

## 2026-05-18 - Keep Tentacle Damage State Outside Unity Side Effects

Decision:
Move tentacle HP subtraction and death-threshold state into `TentacleDamageState`, while keeping `Tentacle.TakeDamage(...)` as the public combat entry point and Unity side-effect owner.

Reason:
`Tentacle.TakeDamage(...)` mixed HP mutation, death threshold checks, hit effect coroutine start, sound publishing, kill particle instantiation, and destruction. Separating only the state rule reduces `Tentacle` combat responsibility without changing scenes, prefabs, serialized fields, public APIs, sound IDs, particle placement, or destroy behavior.

Implications:
- `Tentacle` still owns the `isAlive` guard, hit effect coroutine, hit/death sound publishing, kill particle spawn timing, and `Destroy(gameObject)`.
- `TentacleDamageState` preserves the previous `currentHP - damageAmount` subtraction and `currentHP <= 0` death condition.
- Future tentacle damage math, shields, or death-threshold rules should start in `TentacleDamageState`.

## 2026-05-18 - Keep Tentacle Attack Sequence Outside Tentacle

Decision:
Move tentacle attack wait, animation trigger, attack callback, destroy delay sequencing, and total duration calculation into `TentacleAttackRoutine`, while keeping `Tentacle.BeginAttack()`, `Tentacle.AttackRoutine()`, and `Tentacle.GetTotalDuration()` as the lifecycle/timing entry points.

Reason:
`Tentacle.AttackRoutine()` mixed coroutine entry, warning delay, animation trigger, boss sound callback, hit-window wait, destroy delay, and object destruction, while `GetTotalDuration()` still encoded the same timing formula used by `EyeBoss` pattern waits. Separating the sequence and total duration calculation reduces `Tentacle` responsibility without changing serialized fields, public APIs, prefabs, scenes, setup values, callback timing, trigger name, total duration formula, or destroy timing.

Implications:
- `Tentacle` still starts the attack coroutine through `BeginAttack()` and still exposes `GetTotalDuration()`.
- `TentacleAttackRoutine` preserves previous order: wait, trigger `attack` when possible, invoke callback, wait for animation length, wait for destroy delay, then destroy the tentacle object.
- `TentacleAttackRoutine.CalculateTotalDuration(...)` preserves the previous formula: attack wait plus animation length when positive, otherwise 1.0 second, plus destroy delay.
- Future tentacle attack timing, callback, animation trigger, total duration, or destroy sequence changes should start in `TentacleAttackRoutine`.

## 2026-05-18 - Keep EyeBoss Tentacle Attack Profile Selection Outside Spawn Side Effects

Decision:
Move eye boss normal/enrage tentacle damage and attack-delay selection into `EyeBossTentacleAttackProfile`, while passing the resolved profile to `EyeBossTentacleSpawner` for spawn execution.

Reason:
Normal/enrage damage and delay selection is a pure rule that can be separated from both pattern timing and tentacle spawn side effects without changing scenes, prefabs, serialized fields, or public APIs.

Implications:
- `EyeBoss` still owns the serialized normal/enrage damage and delay fields and resolves the profile before spawn execution.
- `EyeBossTentacleSpawner` consumes the resolved profile for `Tentacle.Setup(...)`.
- `EyeBossTentacleAttackProfile` preserves the previous `isEnrage ? enrage : normal` selection for both damage and attack delay.
- Future eye boss tentacle damage/delay profile rules should start in `EyeBossTentacleAttackProfile`.

## 2026-05-18 - Keep Eye Boss Tentacle Spawn Execution In One Helper

Decision:
Move eye boss tentacle prefab selection, instantiation, setup, attack start, max-duration collection, and spawn sound publishing into `EyeBossTentacleSpawner`, while keeping `EyeBoss` as the pattern coroutine and profile-resolution owner.

Reason:
`EyeBoss.SpawnTentaclesAndGetDuration(...)` still mixed pattern coroutine support with prefab selection, `Object.Instantiate(...)`, `Tentacle.Setup(...)`, `BeginAttack()`, `GetTotalDuration()`, and `SoundID.Boss_TentacleSpawn` publishing. Separating the spawn execution side effects reduces `EyeBoss` responsibility without changing scenes, prefabs, Inspector data, serialized fields, public APIs, pattern timing, null behavior, or sound timing.

Implications:
- `EyeBoss` still owns pattern coroutine timing and passes the selected normal/enrage attack profile to the spawner.
- `EyeBossTentacleSpawner` preserves previous spawn behavior: skip out-of-range indices, choose weak versus normal prefab by weak index set, skip null prefabs, instantiate at the spawn point with identity rotation, setup/start `Tentacle` when present, collect max duration, and publish `SoundID.Boss_TentacleSpawn`.
- Future eye boss tentacle pooling, spawn null handling, spawn sound routing, or setup changes should start in `EyeBossTentacleSpawner`.

## 2026-05-18 - Keep EyeBoss Enrage Transition State Outside Unity Side Effects

Decision:
Move eye boss enrage threshold prediction and forced-enrage HP clamp decision into `EyeBossEnrageTransition`, while keeping `EyeBoss.TakeDamage(...)` as the public combat entry point and side-effect owner.

Reason:
`EyeBoss.TakeDamage(...)` mixed damage prediction, enrage threshold calculation, forced-enrage eligibility, HP clamping, invincibility state, roar sound publishing, coroutine start, and fallback boss damage. Separating only the threshold decision reduces `EyeBoss` combat responsibility without changing scenes, prefabs, serialized fields, public APIs, sound IDs, coroutine flow, or base damage handling.

Implications:
- `EyeBoss` still owns the `isInvincible` guard, `currentHP` assignment, `isInvincible` and `enragePatternReady` side effects, `SoundID.Boss_Roar`, `ForceEnrageRoutine()`, and fallback `base.TakeDamage(...)`.
- `EyeBossEnrageTransition` preserves the previous `currentHP - damageAmount` prediction, `calibratedMaxHP * EnragePatternThreshold` threshold, and `!hasEnraged && !enragePatternReady && predictedHP <= thresholdHP` condition.
- Future eye boss enrage threshold, clamp, or damage prediction rules should start in `EyeBossEnrageTransition`.

## 2026-05-18 - Keep Train Boss Death Presentation Constants In One Helper

Decision:
Move train boss death explosion offset and completion delay constants into `TrainBossDeathPresentation`, while keeping `TrainBoss.Die()` as the coroutine ordering, death sound, and completion execution owner.

Reason:
`TrainBoss.Die()` still encoded the `(-5f, 2f)` explosion offset and `2.0f` completion wait directly beside base death yielding, sound publishing, and boss completion. Separating only the constants names the boss-specific death presentation contract without changing scenes, prefabs, serialized fields, public APIs, explosion prefab reference, sound ID, wait duration, or completion order.

Implications:
- `TrainBoss` still yields `base.Die()`, spawns the configured explosion, publishes `SoundID.Boss_Die`, waits, and delegates shared completion to `BossDeathCompletionExecutor`.
- `TrainBossDeathPresentation` preserves the previous explosion offset and 2.0-second wait.
- Future train boss death offset or wait-duration changes should start in `TrainBossDeathPresentation`.

## 2026-05-18 - Keep Eye Boss Normal Pattern Selection Outside Update

Decision:
Move the eye boss normal side-versus-center pattern selection into `EyeBossNormalPatternSelector`, while keeping `EyeBoss.Update()` as the lifecycle gate and coroutine start owner.

Reason:
`EyeBoss.Update()` already delegates normal-pattern start eligibility, but still encoded the 50:50 side/center random selection directly beside coroutine startup. Separating only the selection rule names the normal-pattern choice contract without changing serialized fields, public APIs, scenes, prefabs, coroutine names, or pattern timing.

Implications:
- `EyeBoss` still decides when Update can start a pattern and still starts `SideAttackPattern()` or `CenterAttackPattern()`.
- `EyeBossNormalPatternSelector` preserves the previous `Random.Range(0, 2) == 0` side-pattern rule; center remains the other branch.
- Future normal-pattern weighting or sequencing changes should start in `EyeBossNormalPatternSelector`.

## 2026-05-18 - Keep Eye Boss Death Presentation Constants In One Helper

Decision:
Move eye boss death explosion position and completion delay constants into `EyeBossDeathPresentation`, while keeping `EyeBoss.Die()` as the tentacle cleanup, explosion spawn timing, base death yielding, and completion execution owner.

Reason:
`EyeBoss.Die()` still encoded the death explosion position as `transform.position` and the `2.0f` completion wait directly beside tentacle cleanup, base death yielding, and boss completion. Separating only these constants names the eye-boss-specific death presentation contract without changing scenes, prefabs, serialized fields, public APIs, explosion prefab reference, wait duration, or completion order.

Implications:
- `EyeBoss` still clears tentacles, spawns the configured explosion before yielding `base.Die()`, waits, and delegates shared completion to `BossDeathCompletionExecutor`.
- `EyeBossDeathPresentation` preserves the previous explosion position of `transform.position` and 2.0-second wait.
- Future eye boss death position or wait-duration changes should start in `EyeBossDeathPresentation`.

## 2026-05-18 - Keep Eye Boss Normal Pattern Wait Sequence In One Helper

Decision:
Move the eye boss normal pattern post-spawn wait sequence into `EyeBossNormalPatternRoutine`, while keeping `EyeBoss` as the busy-state owner and tentacle spawn execution caller.

Reason:
`SideAttackPattern()` and `CenterAttackPattern()` repeated the same flow after resolving their pattern plan: spawn tentacles, wait for the returned duration, wait for `waitTimeAfterPatternEnd`, then clear busy state. Separating only the wait sequence reduces duplicated coroutine timing code without changing serialized fields, public APIs, scenes, prefabs, pattern plans, spawn execution, busy-state ownership, or timing values.

Implications:
- `EyeBoss` still sets and clears `isBusy`, creates the side/center plans, and calls `SpawnTentaclesAndGetDuration(...)`.
- `EyeBossNormalPatternRoutine` preserves the previous wait order: pattern duration first, then `waitTimeAfterPatternEnd`.
- Future normal-pattern wait behavior should start in `EyeBossNormalPatternRoutine`; pattern plan selection should stay in `EyeBossTentaclePatternPlanner`.

## 2026-05-18 - Keep Eye Boss Enrage Pattern Wait Loop In One Helper

Decision:
Move the eye boss enrage pattern duration-or-all-tentacles-cleared wait loop into `EyeBossEnragePatternRoutine`, while keeping `EyeBoss` as the enrage state, post-pattern wait, and busy-state owner.

Reason:
`EyeBoss.EnragePatternRoutine()` still mixed enrage state writes, pattern plan execution, a duration loop, all-tentacles-cleared early exit, invincibility reset, post-pattern wait, and busy-state reset. Separating only the duration-or-clear wait loop names the enrage timing contract without changing serialized fields, public APIs, scenes, prefabs, enrage pattern plans, spawn execution, state write order, log text, or timing values.

Implications:
- `EyeBoss` still sets `isBusy`, `hasEnraged`, `isInvincible`, `enragePatternReady`, and post-pattern wait order.
- `EyeBossEnragePatternRoutine` preserves the previous loop rule: advance by `Time.deltaTime` until duration expires or `EyeBossTentacleRegistry.HasAny(...)` is false.
- Future enrage pattern duration or early-exit wait behavior should start in `EyeBossEnragePatternRoutine`.

## 2026-05-18 - Keep Train Boss Stun Routine Behind TrainBoss

Decision:
Move the train boss stun active-state timing and duration wait into `TrainBossStunRoutine`, while keeping `TrainBoss` as the coroutine restart and knockback flow owner.

Reason:
`TrainBoss.Stun()` still mixed stun state writes with the duration wait. Moving the full true-wait-false sequence names the stun timing contract without changing serialized fields, public APIs, scenes, prefabs, coroutine name, `StopCoroutine("Stun")` / `StartCoroutine("Stun")` order, `isStunned` write order, or `stunDuration` value.

Implications:
- `TrainBoss` still starts the `"Stun"` coroutine and reads stun state for movement gating through `TrainBossStunState`.
- `TrainBossStunRoutine` preserves the previous order: set stunned true, wait `stunDuration`, then set stunned false.
- `TrainBoss.Knockback()` still stops and starts the `"Stun"` coroutine before applying the Rigidbody2D impulse.
- Future train boss stun state timing or wait changes should start in `TrainBossStunRoutine`; stun state storage changes should start in `TrainBossStunState`, while Rigidbody2D movement gating stays in `TrainBoss`.

## 2026-05-18 - Keep Train Boss Stun State Outside TrainBoss

Decision:
Move train boss stun active-state storage into `TrainBossStunState`, while keeping `TrainBoss.FixedUpdate()` as the Rigidbody2D movement gate and `TrainBoss.Knockback()` as the coroutine restart owner.

Reason:
The stun boolean is runtime bookkeeping used by movement gating and the stun coroutine. Separating it reduces `TrainBoss` state responsibility without changing serialized fields, public APIs, scenes, prefabs, coroutine name, coroutine restart order, stun duration, or movement velocity behavior.

Implications:
- `TrainBossStunState` preserves the previous default not-stunned state.
- `TrainBossStunRoutine` still owns the true-wait-false timing sequence and writes through the state setter.
- `TrainBoss.FixedUpdate()` still decides whether to apply velocity, but it reads `TrainBossStunState.IsStunned`.
- Future stun reset or state-storage changes should start in `TrainBossStunState`; timing changes should stay in `TrainBossStunRoutine`.

## 2026-05-18 - Keep Boss Enable State Reset Values In One Helper

Decision:
Move boss enable-time HP/current/alive/screen-entry/entrance reset values into `BossEnableState`, while keeping `Boss.OnEnable()` as the Unity lifecycle and side-effect owner.

Reason:
`Boss.OnEnable()` mixed runtime reset values with sprite visibility, enemy layer assignment, and active-enemy registration. Separating only the value bundle names the boss enable-state contract without changing serialized fields, public APIs, scenes, prefabs, PoolManager registration, layer assignment, sprite visibility, HP calibration formula, or entrance coroutine behavior.

Implications:
- `Boss` still calls `CalculateCalibratedHP()`, updates fields, shows the sprite, applies the enemy layer, and registers with `PoolManager`.
- `BossEnableState` preserves previous reset values: current HP equals calibrated max HP, alive is true, screen-entry is false, and entrance-active is false.
- Future boss enable reset value changes should start in `BossEnableState`; Unity side effects should stay in `Boss.OnEnable()`.

## 2026-05-18 - Keep Boss Warning Sequence Behind Spawner

Decision:
Move boss warning UI show and post-warning delay into `SpawnerBossWarningRoutine`, while keeping boss coroutine ordering separate from warning UI behavior.

Reason:
`Spawner.BossSpawnRoutine(...)` mixed boss timing, `GameManager.AppearBoss()`, warning UI presentation, wait timing, and boss object spawn execution. Separating only warning presentation and delay names the boss warning sequence without changing serialized fields, public APIs, scenes, prefabs, boss sequence timing, warning UI singleton usage, or boss prefab creation.

Implications:
- At that step, `Spawner` still resolved `BossSpawnSetting`, logged the sequence, called `GameManager.Instance.AppearBoss()`, and spawned the boss object after the warning sequence; later decisions moved object spawn execution to `SpawnerBossObjectSpawner`, boss-state entry to `SpawnerBossStateTransition`, and sequence ordering to `SpawnerBossSpawnRoutine`.
- `SpawnerBossWarningRoutine` preserves previous behavior: show `BossWarningLoopUI` only when an instance exists, then wait for `spawnDelayAfterWarning`.
- Future warning UI presentation or post-warning delay sequencing should start in `SpawnerBossWarningRoutine`; boss selection stays in `SpawnerBossSequenceCursor`/`Spawner`, routine ordering stays in `SpawnerBossSpawnRoutine`, and object spawn side effects stay in `SpawnerBossObjectSpawner`.

## 2026-05-18 - Keep Mob Batch Spawn Loop Behind Spawner Public API

Decision:
Move mob batch spawn loop and delay sequencing into `SpawnerMobBatchRoutine`, while keeping `Spawner.SpawnMobBatch(...)` as the public event-facing entry point.

Reason:
`Spawner.SpawnMobBatch(...)` and its coroutine mixed the public spawn request, one-time batch position selection, per-count pooled mob lookup, placement, physics reset, and delay waits. Separating only the loop names the batch spawn contract without changing serialized fields, public APIs, scenes, prefabs, spawn position rules, pooling calls, physics reset behavior, or delay timing.

Implications:
- External callers still use `Spawner.SpawnMobBatch(...)`.
- `SpawnerMobBatchRoutine` preserves previous behavior: choose one spawn position before the loop, request `PoolManager.instance.GetMob(prefab)` each iteration, place and physics-reset non-null enemies, and wait `delay` after every iteration.
- Future batch count, delay, or fixed-position reuse rules should start in `SpawnerMobBatchRoutine`; public spawn API and spawn-position selection should stay in `Spawner`.

## 2026-05-18 - Keep Mob Spawn Setup Behind Spawner Pool Lookup

Decision:
Move mob spawn placement, physics reset, and `Mob.OnDied` callback rewiring into `SpawnerMobSpawnSetup`, while keeping mob pool lookup behavior unchanged at that step.

Reason:
`Spawner.SpawnMobCommon(...)` mixed pooled enemy null handling, spawn position selection, transform placement, physics reset, `Mob` component lookup, and death callback rewiring. Separating only setup side effects names the mob spawn setup contract without changing serialized fields, public APIs, scenes, prefabs, spawn position rules, pooling calls, physics reset behavior, or respawn callback semantics.

Implications:
- At that step, `Spawner` still chose normal/elite/event pool lookup paths and passed `GetSpawnPosition` plus `RespawnMob` to the helper.
- `SpawnerMobSpawnSetup` preserves previous behavior: skip null enemies, place at the selected spawn position, clear physics through `SpawnerEnemyPhysicsReset`, then remove and re-add the same `Mob.OnDied` callback.
- Future mob spawn setup, placement composition, physics reset composition, or death callback rewiring should start in `SpawnerMobSpawnSetup`; pool lookup selection is handled by the later `SpawnerMobPoolLookup` decision.

## 2026-05-18 - Keep Mob Pool Lookup Selection Outside Spawner

Decision:
Move normal, elite, and prefab mob pool lookup selection into `SpawnerMobPoolLookup`, while keeping `Spawner` as the public mob spawn call-site and setup delegation owner.

Reason:
`Spawner` still encoded which `PoolManager` getter to call for normal ground/fly mobs, elite ground/fly mobs, and prefab-driven event spawns. Separating only this lookup selection reduces direct `PoolManager` branching in `Spawner` without changing serialized fields, public APIs, scenes, prefabs, spawn selection rules, setup timing, pooling behavior, null behavior, or callback wiring.

Implications:
- `Spawner` still selects basic/elite spawn type and index through `SpawnerMobSpawnSelector`, then asks `SpawnerMobPoolLookup` for the pooled object.
- `SpawnerMobPoolLookup` preserves the previous calls: `GetFlyMob` or `GetGroundMob` for basic mobs, `GetFlyEliteMob` or `GetGroundEliteMob` for elite mobs, and `GetMob(prefab)` for prefab spawns.
- Future normal, elite, or prefab mob pool lookup selection changes should start in `SpawnerMobPoolLookup`; spawn scheduling and setup should stay in their existing helpers.

## 2026-05-18 - Keep Boss Object Spawn Execution Outside Spawner

Decision:
Move boss prefab lookup, instantiation, missing-spawn-point logging, and entrance handoff into `SpawnerBossObjectSpawner`, while keeping `Spawner` as the boss sequence, warning, and spawn call-site owner.

Reason:
`Spawner.SpawnBossObject(...)` still mixed the post-warning call site with `PoolManager` boss prefab lookup, spawn position fallback logging, `Object.Instantiate(...)`, `Boss` component lookup, and entrance routine handoff. Separating only object spawn execution reduces `Spawner` responsibility without changing serialized fields, public APIs, scenes, prefabs, boss settings schema, boss sequence timing, warning timing, prefab lookup semantics, spawn position fallback, null behavior, log text, or entrance behavior.

Implications:
- `Spawner` still supplies boss setting lookup through `SpawnerBossSettingLookup`, runs the warning sequence, and calls `SpawnerBossObjectSpawner` after the warning delay.
- `SpawnerBossObjectSpawner` preserves previous behavior: return when `PoolManager.instance` or boss prefab is null, log the same missing spawn-point error, instantiate at the resolved spawn position with identity rotation, and start entrance only when a `Boss` component and arrival point exist.
- Future boss object spawn execution, prefab lookup side effects, missing-spawn-point logging, or entrance handoff changes should start in `SpawnerBossObjectSpawner`; routine sequence decisions should stay in `SpawnerBossSpawnRoutine`, and warning UI timing should stay in `SpawnerBossWarningRoutine`.

## 2026-05-18 - Keep Boss State Transition Outside Spawner

Decision:
Move boss appearance logging and `GameManager.Instance.AppearBoss()` entry into `SpawnerBossStateTransition`, while keeping boss coroutine ordering separate from state-entry side effects.

Reason:
`Spawner.BossSpawnRoutine(...)` still mixed boss setting lookup, boss appearance logging, `GameManager` boss-state entry, warning wait, and boss object spawn call-site sequencing. Separating only the state-transition side effect names the boss-state entry contract without changing serialized fields, public APIs, scenes, prefabs, boss settings schema, sequence timing, warning timing, log text, `GameManager` state transition, or boss object spawn behavior.

Implications:
- At that step, `Spawner` still resolved the boss setting before the transition, waited for `SpawnerBossWarningRoutine`, and called `SpawnBossObject(...)` after the warning delay. The later `SpawnerBossSpawnRoutine` decision moves routine ordering behind a helper.
- `SpawnerBossStateTransition` preserves the previous order: log using `GameManager.Instance.gameTime`, then call `GameManager.Instance.AppearBoss()`.
- Future boss appearance logging or `GameManager` boss-state entry rules should start in `SpawnerBossStateTransition`; warning timing should stay in `SpawnerBossWarningRoutine`.

## 2026-05-18 - Keep Boss Spawn Coroutine Sequence Outside Spawner

Decision:
Move boss spawn coroutine setting lookup, boss-state transition, warning wait, and spawn-object request ordering into `SpawnerBossSpawnRoutine`, while keeping `Spawner.StartBossSequence(...)` as the coroutine start entry point and `Spawner.SpawnBossObject(...)` as the object-spawn call site.

Reason:
`Spawner.BossSpawnRoutine(...)` still coordinated multiple already-separated boss helpers in order. Moving only that ordering into a focused routine helper reduces `Spawner` responsibility without changing public boss spawn APIs, serialized boss settings, scene/prefab references, warning delay values, state transition order, boss object spawn behavior, or coroutine start ownership.

Implications:
- Public `Spawner.SpawnBoss(...)`, `SpawnTrainBoss()`, and `SpawnEyeBoss()` still start the same boss sequence through `Spawner`.
- `SpawnerBossSpawnRoutine` preserves the previous order: resolve setting, enter boss state, yield warning wait using `spawnDelayAfterWarning`, then request boss object spawn for the same boss name.
- Future boss spawn coroutine ordering changes should start in `SpawnerBossSpawnRoutine`; state-entry side effects, warning UI behavior, and object spawn execution stay in their focused helpers.

## 2026-05-18 - Keep Boss Sequence Reporting Outside Spawner

Decision:
Move boss sequence empty-sequence warnings and reserved-next-boss logging into `SpawnerBossSequenceLog`, while keeping `Spawner.SpawnNextBoss()` as the boss sequence selection and spawn execution call site.

Reason:
`Spawner.SpawnNextBoss()` still mixed boss sequence cursor usage, spawn sequence startup, and debug reporting. Separating only reporting keeps the runtime sequence behavior unchanged while making sequence state and sequence reporting distinct responsibilities.

Implications:
- `Spawner` still decides when to spawn the next boss and still starts the selected boss sequence.
- `SpawnerBossSequenceCursor` remains the cyclic index state owner.
- `SpawnerBossSequenceLog` preserves previous warning/log text and should be the starting point for future boss sequence reporting changes.

## 2026-05-18 - Keep Normal Mob Stun Wait Behind Mob

Decision:
Move normal mob stun duration waiting into `MobStunRoutine`, while keeping `Mob.Knockback(...)` as the public call surface and `Mob.Stun()` as the stun state write owner.

Reason:
`Mob.Stun()` mixed state writes with duration waiting. Separating only the wait step names the stun timing contract without changing serialized/private fields, public APIs, scenes, prefabs, coroutine start order, `isStunned` write order, or `stunDuration` value.

Implications:
- `Mob` still starts stun from `Knockback(...)`, applies Rigidbody2D impulse, and writes `isStunned = true/false`.
- `MobStunRoutine` preserves the previous wait of `stunDuration`.
- Future normal mob stun wait timing changes should start in `MobStunRoutine`.

## 2026-05-18 - Keep Tentacle Death Completion Behind Tentacle

Decision:
Move tentacle death particle, death sound, and object destroy side effects into `TentacleDeathCompletion`, while keeping `Tentacle.TakeDamage(...)` as the public damage entry point and hit feedback owner.

Reason:
`Tentacle.TakeDamage(...)` already delegates HP subtraction and death-threshold state, but still mixed hit feedback with death completion side effects. Separating only the death completion step names the kill-presentation contract without changing serialized fields, public APIs, scenes, prefabs, hit effect timing, hit sound timing, death particle position, death sound, or destroy timing.

Implications:
- `Tentacle` still owns `TakeDamage(...)`, hit effect start, and `SoundID.Enemy_Hit`.
- `TentacleDeathCompletion` preserves the previous death order: spawn kill particle, publish `SoundID.Enemy_Die`, then destroy the tentacle GameObject.
- Future tentacle death particle, death sound, or destroy behavior changes should start in `TentacleDeathCompletion`.

## 2026-05-18 - Keep Enemy Hit Effect Timing Behind Enemy

Decision:
Move enemy hit material on/wait/off sequencing into `EnemyHitEffectRoutine`, while keeping `Enemy.HitEffect()` as the coroutine entry point and `EnemyHitMaterialController` as the material flag writer.

Reason:
`Enemy.HitEffect()` still mixed coroutine timing with `_isHit` material writes. Separating only the timing sequence names the hit flash contract without changing serialized fields, public APIs, scenes, prefabs, hit sound timing, damage flow, death flow, or material cleanup behavior.

Implications:
- `Enemy` still decides when to start hit feedback from `TakeDamage(...)`.
- `EnemyHitEffectRoutine` preserves the previous sequence: set hit true, wait `hitEffectDuration`, then set hit false.
- Future enemy hit flash timing changes should start in `EnemyHitEffectRoutine`; direct material writes should stay in `EnemyHitMaterialController`.

## 2026-05-18 - Keep Base Enemy Death Presentation Behind Enemy

Decision:
Move base enemy death sprite hide, kill particle request, and zero-duration completion wait creation into `EnemyDeathPresentation`, while keeping `Enemy.Die()` as the death state and reward-dispatch ordering owner.

Reason:
`Enemy.Die()` still mixed death state, XP/inventory reward dispatch, sprite presentation, particle request, and coroutine wait creation. Separating only the presentation/timing detail reduces base enemy responsibility without changing serialized fields, public APIs, scenes, prefabs, reward order, kill particle position, or coroutine yield behavior.

Implications:
- `Enemy` still sets `isAlive = false` before reward dispatch and presentation.
- `EnemyDeathRewardDispatcher` still owns XP and inventory kill-hook dispatch.
- `EnemyDeathPresentation` preserves the previous order: hide sprite, request kill particle spawn at the enemy transform position/rotation, then create `WaitForSeconds(0)`.
- Future base enemy death presentation or zero-duration wait changes should start in `EnemyDeathPresentation`.

## 2026-05-18 - Keep Base Enemy Enable State Reset Values In One Helper

Decision:
Move base enemy enable-time current HP, alive, and screen-entry reset values into `EnemyEnableState`, while keeping `Enemy.OnEnable()` as the Unity lifecycle and side-effect owner.

Reason:
`Enemy.OnEnable()` mixed pure reset values with sprite presentation, layer assignment, and active-enemy registration. Separating only the value bundle aligns base enemy lifecycle with the existing `BossEnableState` pattern without changing serialized fields, public APIs, scenes, prefabs, HP calibration, sprite reset, layer assignment, or pool registration.

Implications:
- `Enemy` still calls `CalculateCalibratedHP()`, updates fields, resets sprite presentation, applies the enemy layer, and registers with `PoolManager`.
- `EnemyEnableState` preserves previous reset values: current HP equals calibrated max HP, alive is true, and screen-entry is false.
- Future base enemy enable reset value changes should start in `EnemyEnableState`; Unity side effects should stay in `Enemy.OnEnable()`.

## 2026-05-18 - Keep Enemy Disable Cleanup Behind Enemy

Decision:
Move enemy disable-time hit material cleanup and active-enemy unregister sequencing into `EnemyDisableCleanup`, while keeping `Enemy.OnDisable()` as the Unity lifecycle entry point.

Reason:
`Enemy.OnDisable()` mixed lifecycle dispatch with `_isHit` material cleanup and `PoolManager.UnregisterEnemy(...)`. Separating only the sequence names the cleanup contract without changing serialized fields, public APIs, scenes, prefabs, null-material behavior, `PoolManager` null behavior, or active-list mutation ownership.

Implications:
- `Enemy.OnDisable()` still runs from Unity lifecycle and delegates with the current `PoolManager.instance`.
- `EnemyDisableCleanup` preserves previous behavior: do nothing when `PoolManager` is null; otherwise reset hit material first, then unregister the enemy.
- Actual hit material writes remain behind `EnemyHitMaterialController`.
- Active enemy list mutation remains behind `PoolManager` and `PoolActiveEnemyRegistry`.

## 2026-05-18 - Keep No-Reward Enemy Despawn Behind Enemy

Decision:
Move no-reward despawn active guard and coroutine-stop/deactivate completion into `EnemyDespawnWithoutExpCompletion`, while keeping `Enemy.DespawnWithoutExp()` as the public entry point.

Reason:
`Enemy.DespawnWithoutExp()` is used by active-enemy cleanup paths and mixed public cleanup entry, active hierarchy guard, alive-state mutation, coroutine stopping, and GameObject deactivation. Separating only the guard/completion side effects reduces base enemy responsibility without changing serialized fields, public APIs, scenes, prefabs, pool cleanup callers, active guard behavior, or deactivation order.

Implications:
- `Enemy.DespawnWithoutExp()` still writes `isAlive = false` before stopping coroutines and deactivating the GameObject.
- `EnemyDespawnWithoutExpCompletion` preserves previous behavior: skip objects outside the active hierarchy, stop all coroutines, then set the GameObject inactive.
- Future no-reward cleanup or despawn completion changes should start in `EnemyDespawnWithoutExpCompletion`; active-enemy list iteration stays behind `PoolActiveEnemyRegistry`.

## 2026-05-18 - Keep Active Enemy Despawn Filters Outside Registry Iteration

Decision:
Move active-enemy null and boss-retained despawn eligibility checks into `PoolEnemyDespawnFilter`, while keeping `PoolActiveEnemyRegistry` as the active list mutation and reverse-iteration owner.

Reason:
`PoolActiveEnemyRegistry` mixed list mutation, reverse cleanup iteration, null skipping, and boss exclusion in the same loop. Separating only the eligibility checks names the cleanup policy without changing `PoolManager` public APIs, `activeEnemies` storage, iteration order, boss exclusion behavior, or no-reward despawn calls.

Implications:
- `PoolActiveEnemyRegistry` still owns duplicate-preventing register, remove-if-present unregister, and reverse despawn iteration.
- `PoolEnemyDespawnFilter` preserves previous cleanup behavior: all-enemy cleanup skips null entries, and boss-retained cleanup skips null entries plus enemies with a `Boss` component.
- Future boss-retained cleanup rules should start in `PoolEnemyDespawnFilter`; list mutation stays behind `PoolActiveEnemyRegistry`.

## 2026-05-18 - Keep Pooled Object Reuse Selection Outside Provider

Decision:
Move inactive pooled object lookup and activation into `PoolReusableObjectSelector`, while keeping `PoolObjectProvider` as the pool creation, indexed/dynamic lookup, instantiate-on-miss, naming, and pool-add owner.

Reason:
`PoolObjectProvider.GetOrCreate(...)` mixed reusable object selection and activation with instantiate-on-miss behavior. Separating only the reusable-selection step names the reuse policy without changing `PoolManager` public APIs, serialized prefab arrays, dynamic pool keys, first-inactive selection order, activation timing, instantiation parent, object renaming, or pool append behavior.

Implications:
- `PoolReusableObjectSelector` preserves the previous reusable selection rule: return the first non-null inactive object after setting it active.
- `PoolObjectProvider` still instantiates the prefab under the provided parent, renames the object to `prefab.name`, and appends it to the same pool when no reusable object exists.
- Future pooled object reuse selection or activation changes should start in `PoolReusableObjectSelector`; pool lookup and instantiate-on-miss behavior stays in `PoolObjectProvider`.

## 2026-05-18 - Keep Dynamic Pool Registry Rules Outside Provider

Decision:
Move dynamic pool prefab-name key creation and dictionary get-or-add behavior into `PoolDynamicPoolRegistry`, while keeping `PoolObjectProvider.GetDynamicPool(...)` as the current pool lookup entry point.

Reason:
`PoolObjectProvider` still owned multiple pooling responsibilities after reusable-object selection was separated. Dynamic event-spawned mob pools have a distinct registry rule that can be named without changing `PoolManager` public APIs, serialized prefab arrays, scene references, dynamic pool keys, or object creation behavior.

Implications:
- `PoolObjectProvider.GetDynamicPool(...)` remains the caller-facing helper used behind `PoolManager`.
- `PoolDynamicPoolRegistry` preserves the current key rule: use `prefab.name`, add a new `List<GameObject>` only when missing, then return `dynamicPools[key]`.
- Future dynamic pool key, capacity, or registry lookup behavior should start in `PoolDynamicPoolRegistry`; instantiate-on-miss behavior stays in `PoolObjectProvider`.

## 2026-05-18 - Keep Indexed Pool Resolution Outside Provider

Decision:
Move indexed prefab-array bounds and pool/prefab pair resolution into `PoolIndexedPrefabResolver`, while keeping `PoolObjectProvider.GetIndexed(...)` as the current indexed pool lookup entry point.

Reason:
`PoolObjectProvider.GetIndexed(...)` still mixed the invalid-index rule with pooled object reuse/create behavior. The index validation and `pools[index]` / `prefabs[index]` pairing are lookup policy and can be separated without changing `PoolManager` public getters, serialized prefab arrays, scene references, invalid-index null behavior, or instantiate/reuse behavior.

Implications:
- `PoolObjectProvider.GetIndexed(...)` remains the helper used by `PoolManager.GetGroundMob(...)`, `GetFlyMob(...)`, `GetGroundEliteMob(...)`, and `GetFlyEliteMob(...)`.
- `PoolIndexedPrefabResolver` preserves the current bounds rule: invalid indexes outside `prefabs.Length` fail and keep the caller returning null.
- Future indexed prefab validation or pool/prefab pair selection rules should start in `PoolIndexedPrefabResolver`; object reuse and instantiate-on-miss behavior stays in `PoolObjectProvider`.

## 2026-05-18 - Keep Pooled Object Creation Outside Provider

Decision:
Move pooled object instantiate-on-miss, prefab-name assignment, and pool append behavior into `PoolObjectFactory`, while keeping `PoolObjectProvider.GetOrCreate(...)` as the reuse-first entry point.

Reason:
`PoolObjectProvider.GetOrCreate(...)` mixed reuse selection with new object creation side effects. Separating creation gives instantiate/naming/registration a single owner without changing `PoolManager` public APIs, serialized prefab arrays, dynamic pool keys, first-inactive reuse order, parent transform, object naming, or pool append behavior.

Implications:
- `PoolObjectProvider.GetOrCreate(...)` still checks for inactive reusable objects before creating a new one.
- `PoolObjectFactory` preserves current creation behavior: instantiate under the provided parent, set the new object's name to `prefab.name`, append it to the same pool, and return it.
- Future pooled object creation, naming, parent, prewarm, or registration behavior should start in `PoolObjectFactory`; reusable-object selection stays in `PoolReusableObjectSelector`.

## 2026-05-18 - Keep Pool List Initialization Outside Provider

Decision:
Move pooled list-array allocation and per-index empty list initialization into `PoolListFactory`, while keeping `PoolObjectProvider.CreatePools(...)` as the current pool initialization entry point.

Reason:
`PoolObjectProvider` had become the pooling facade after indexed lookup, dynamic lookup, reuse selection, and object creation were split out. Moving the remaining list-array allocation into a focused helper completes that responsibility split without changing `PoolManager.Start()`, serialized prefab arrays, pool array length, or empty-list initialization behavior.

Implications:
- `PoolManager` still initializes ground, fly, ground-elite, and fly-elite pools through `PoolObjectProvider.CreatePools(...)`.
- `PoolListFactory` preserves the current initialization rule: allocate a `List<GameObject>[]` with `prefabs.Length` and assign a new empty `List<GameObject>` to every index.
- Future pool list allocation, prewarm-list shape, or per-index initialization behavior should start in `PoolListFactory`; caller-facing pool initialization stays in `PoolObjectProvider`.

## 2026-05-18 - Keep Dynamic Prefab Mob Request Outside PoolManager

Decision:
Move `PoolManager.GetMob(GameObject)` dynamic prefab null guard, dynamic pool lookup, and reuse/create request flow into `PoolDynamicMobProvider`, while keeping `PoolManager.GetMob(...)` as the public event/prefab-spawn entry point.

Reason:
`PoolManager` should remain the scene-facing pooling facade and serialized prefab owner. Dynamic event-spawned mob request flow is a focused pooling coordination rule that can move behind the facade without changing serialized fields, public APIs, dynamic pool keys, or object reuse/create behavior.

Implications:
- `PoolManager.GetMob(GameObject)` still returns null for a null prefab and otherwise requests pooled objects under `PoolManager.transform`.
- `PoolDynamicMobProvider` preserves the existing call order: null guard, `PoolObjectProvider.GetDynamicPool(...)`, then `PoolObjectProvider.GetOrCreate(...)`.
- Future dynamic prefab spawn request rules should start in `PoolDynamicMobProvider`; dynamic key creation stays in `PoolDynamicPoolRegistry`, and instantiate-on-miss behavior stays in `PoolObjectFactory`.

## 2026-05-18 - Keep Indexed Mob Pool Set Outside PoolManager

Decision:
Move private indexed mob pool storage and normal/fly/elite getter routing into `PoolMobPoolSet`, while keeping `PoolManager` serialized prefab arrays and public indexed mob getter methods stable.

Reason:
`PoolManager` should remain the scene-facing pooling facade and serialized prefab owner. The four indexed pool arrays and their matching public getter routing are cohesive private pooling state that can be owned together without changing scenes, prefabs, Inspector data, public APIs, invalid-index behavior, or pooled object reuse/create behavior.

Implications:
- `PoolManager.Start()` still initializes normal, fly, ground-elite, and fly-elite pools from the same serialized prefab arrays.
- `PoolManager.GetGroundMob(...)`, `GetFlyMob(...)`, `GetGroundEliteMob(...)`, and `GetFlyEliteMob(...)` keep their signatures and route through `PoolMobPoolSet`.
- Future indexed mob pool storage, initialization, or normal/fly/elite routing changes should start in `PoolMobPoolSet`; prefab-array bounds remain in `PoolIndexedPrefabResolver`.

## 2026-05-18 - Keep Periodic Spawn Task List Outside Spawner

Decision:
Move periodic spawn task list storage, null-prefab add guard, due-spawn access, and indexed timer reset into `SpawnerPeriodicTaskList`, while keeping `Spawner.AddPeriodicSpawnTask(...)` and actual prefab spawn execution on `Spawner`.

Reason:
`Spawner` should remain the scene-facing spawn facade and execution owner. The periodic task list is private bookkeeping that can be separated without changing event call sites, public methods, `PeriodicSpawnTask` field schema, scenes, prefabs, Inspector data, timer rules, spawn execution order, or enemy placement behavior.

Implications:
- Event effects still call `Spawner.AddPeriodicSpawnTask(...)`; null prefab requests are still ignored.
- `Spawner.HandlePeriodicTasks()` still gates work through `SpawnerSpawnControlState`, then asks the task list for due prefab/fly pairs and calls `SpawnMobFromPrefab(...)` before resetting the task timer.
- Future periodic task list storage, stacking, cancellation, or due-task access changes should start in `SpawnerPeriodicTaskList`; interval clamping and timer math remain in `SpawnerPeriodicSpawnScheduler`.

## 2026-05-18 - Keep Spawn Timer State Outside Spawner

Decision:
Move basic, elite, and boss spawn timer storage, advancement, reset, and next-boss time mutation into `SpawnerSpawnTimerState`, while keeping phase refresh and spawn execution on `Spawner`.

Reason:
`Spawner.Update()` should remain the scene-facing execution owner, but private timer state is pure bookkeeping. Separating timer state reduces `Spawner` responsibility without changing serialized fields, public APIs, scenes, prefabs, due-check policy, spawn call order, timer reset points, or boss interval behavior.

Implications:
- `SpawnerSpawnSchedule` still owns due-check policy for elite timer start, timer due checks, and boss due checks.
- `SpawnerSpawnTimerState` preserves previous timer initialization and mutation: mob timer starts at 0, elite timer starts at `eliteMobSpawnInterval`, next boss time starts at `bossSpawnInterval`, and boss time advances by `bossSpawnInterval` after a boss spawn is requested.
- Future timer state, reset, or next-spawn-time mutation changes should start in `SpawnerSpawnTimerState`; actual spawn side effects should stay in `Spawner`.

## 2026-05-18 - Keep Pure Refactor Helpers Internal By Default

Decision:
New pure helper types created for low-editor-touch refactors should use `internal` accessibility unless they are an existing public contract or a deliberately approved cross-assembly API.

Reason:
Most refactor helpers are implementation details behind existing `MonoBehaviour` facades. Keeping them internal preserves encapsulation without changing scenes, prefabs, Inspector data, serialized fields, or current same-assembly call sites.

Implications:
- Existing scene-facing public methods and serialized/public fields remain the compatibility surface.
- Helper methods can remain public inside an internal type when the effective API is still assembly-internal.
- If future asmdefs or test assemblies need direct access, expose those helpers intentionally through an approved contract rather than by default.

## 2026-06-07 - Split Active Scope From Project Memory

Decision:
Use `Docs/ActiveTasks/` and `Docs/TaskIndex.md` for active task routing, and keep `Docs/CurrentTask.md` only as a deprecated compatibility notice.

Reason:
`Docs/CurrentTask.md` had grown into a mixed active-scope, done-criteria, verification, and historical-risk file. Separating task scope from durable memory makes future threads start from the prompt or a focused ActiveTask instead of treating one large file as global policy.

Implications:
- New task scope belongs in the current prompt Task Brief or `Docs/ActiveTasks/<task-id>.md`.
- `Docs/TaskIndex.md` is a dashboard/router, not active scope by itself.
- `Docs/README.md` routes technical context after scope is identified.
- `Docs/CurrentTask.md` should not receive new task scope.
- Historical low-editor-touch runtime refactor scope is preserved in `Docs/ActiveTasks/low-editor-touch-runtime-refactor.md`.

## 2026-06-07 - Keep StartObject Free Of Immediate Event Requests

Decision:
`StartObject` should start gameplay and run its authored start-object UI/exit sequence only. It should not request an `SO_Event` immediately when the start object is hit.

Reason:
The first event encounter should come from the event-object spawn flow after gameplay begins, so start timing can be tuned through the first event object appearance time instead of a synchronous `StartObject` event popup.

Implications:
- `EventObjectSpawner.firstSpawnTime` remains the tuning point for the early first event-object appearance.
- `EventTriggerObject` remains the collision path that calls `EventManager.RandomEventStart()`.
- Removed `StartObject.startEvent` scene/prefab Inspector references become obsolete serialized data until affected Unity assets are resaved.

## 2026-06-08 - Preserve 16:9 Content Rect With 800x600 UI Scale

Decision:
Keep runtime camera content and primary UI inside a centered 16:9 content rect while preserving the existing `800x600` CanvasScaler baseline for UI scale.

Reason:
The current 1920x1080 fullscreen presentation looks natural and should have no bars. The previous UI-size problem came from treating 1920x1080 as the UI logical baseline, not from using 16:9 as the content aspect.

Implications:
- `FixedAspectRatioController` is runtime-created for `Start.unity` and `Junmo.unity`; it should not require scene-authored `AspectSafeAreaRoot` or `LetterboxBars`.
- The serialized CanvasScaler baseline should remain `800x600`; runtime content roots should preserve the 1920x1080 visual size and layout feel.
- 1920x1080 should show no bars; ultrawide displays should pillarbox; taller-than-16:9 displays should letterbox.
- Tooltip and mouse-aim screen bounds should use the controller content rect when available, with full-screen fallback for scenes without the controller.
- Windowed resolution selection is only active in windowed mode. Fullscreen and borderless use the native/current display resolution.
- `Option` screen controls should use previous/next selector buttons for screen mode and windowed resolution instead of legacy dropdown controls.
