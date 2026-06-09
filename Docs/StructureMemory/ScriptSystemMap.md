---
status: active
authority: structure-memory
category: script-system-map
last_reviewed: 2026-05-18
---

# LoveTrain Script System Map

## Purpose

This map gives future work a quick starting point for LoveTrain's current runtime script structure.

## Top-Level Ownership

| Area | Primary Responsibility | Start Here |
| --- | --- | --- |
| Core game state | Global state, pause/event freeze, UI queue, boss-death transition, restart | [Core Runtime And Game Flow](./ScriptSystems/CoreRuntimeGameFlow.md) |
| Stage flow | Stage background loading and tunnel/fade transition presentation | [Core Runtime And Game Flow](./ScriptSystems/CoreRuntimeGameFlow.md) |
| Train/player | Speed-as-health state, movement bounds, dying/recovery/death presentation, XP and level-up requests | [Core Runtime And Game Flow](./ScriptSystems/CoreRuntimeGameFlow.md) |
| Items and weapons | Item SO data, runtime item state, inventory acquisition/query/dispatch, level-up choice selection/display state, equip/upgrade hooks, cooldown state, visual fallback upgrades, gun strategies | [Items Inventory And Weapons](./ScriptSystems/ItemsInventoryWeapons.md) |
| Events and UI queue | Random event data, weighted effects, result text, popup queueing | [Event System And UI Queue](./ScriptSystems/EventSystemUiQueue.md) |
| Enemies and bosses | Enemy base damage/death, mob movement, spawn phases, boss sequence, pooling | [Enemy Spawn And Boss Flow](./ScriptSystems/EnemySpawnBossFlow.md) |
| Audio and presentation | Sound event bus, sound manager, warning UI, cursor/option/HUD presentation | [Core Runtime And Game Flow](./ScriptSystems/CoreRuntimeGameFlow.md) |
| Editor tools | Event maker and balance dashboard editor windows | Read `Assets/Editor/` directly |

## Important Cross-System Dependencies

- `GameManager` is the central runtime state owner and public UI queue entry point.
- `GameKillCounter` owns kill counter state behind `GameManager`.
- `GameUiQueueController` owns UI queue pending/processing and event-resume bookkeeping behind `GameManager`.
- `GameSimulationController` owns global time scale and physics simulation mode writes.
- `TrainSpeedHealth` owns train speed-as-health state; `Train` remains the Unity presentation and public API facade.
- `Train` speed is player health and drives death/recovery presentation.
- `TrainLevelProgression` owns XP threshold/progress state behind `TrainLevelManager`.
- `TrainLevelManager` publishes level events and requests level-up UI through `GameManager.RegisterUIQueue(...)`.
- `EventManager` requests event UI through the same queue.
- `ItemCooldownState` owns runtime cooldown countdown state behind `ItemInstance`.
- `ItemRuntimeGameStatePolicy` owns the shared item runtime `GameState` gate.
- `ItemVisualUpgradeApplier` owns generic instantiated item visual replacement behind `ItemInstance`.
- `InventoryItemQuery` owns query-only inventory item lookup rules behind `Inventory`.
- `InventoryItemRuntimeDispatcher` owns item tick and item hook dispatch loops behind `Inventory`.
- `InventoryItemAcquirer` owns item acquisition and upgrade execution behind `Inventory`.
- `LevelUpChoiceSelector` owns item availability filtering and random offer selection behind `LevelUpUIManager`.
- `LevelUpChoiceDisplayState` owns new/upgrade display-state calculation behind `LevelUpChoiceUI`.
- `PoolManager` tracks active enemies and exposes pooled mob lookup plus boss prefab lookup.
- `SpawnerRuntimeStateGate` owns spawner runtime `GameState` gating behind `Spawner.Update()`.
- `SpawnerSpawnSchedule` owns basic, elite, and boss spawn schedule due checks behind `Spawner.Update()`.
- `SpawnerSpawnTimerState` owns basic, elite, and boss spawn timer storage, advancement, reset, and next-boss time state behind `Spawner`.
- `SpawnerPhaseSelector` selects spawn phases behind `Spawner`; `Spawner` uses `GameManager.gameTime` for phase and boss timing.
- `SpawnerCurrentPhaseState` owns the selected spawn phase runtime ranges and spawn interval behind `Spawner`.
- `SpawnerBossSequenceCursor` owns cyclic boss sequence index state behind `Spawner`.
- `SpawnerBossSequenceLog` owns boss sequence empty-sequence warnings and reserved-next-boss logging behind `Spawner`.
- `SpawnerBossSettingLookup` owns boss setting lookup and default fallback creation behind `Spawner`.
- `SpawnerBossSpawnPlacement` owns boss spawn point fallback and arrival position resolution behind `Spawner`.
- `SpawnerBossObjectSpawner` owns boss prefab lookup, instantiation, fallback logging, and entrance handoff behind `Spawner`.
- `SpawnerBossStateTransition` owns boss spawn logging and `GameManager.AppearBoss()` entry behind `Spawner`.
- `SpawnerBossWarningRoutine` owns boss warning UI show and post-warning delay sequence behind `Spawner`.
- `SpawnerBossSpawnRoutine` owns boss spawn coroutine setting lookup, state transition, warning wait, and spawn-object request sequence behind `Spawner`.
- `SpawnerSpawnControlState` owns spawn/rear-spawn control flag state behind `Spawner`.
- `SpawnerEnemyPhysicsReset` owns spawned enemy `Rigidbody2D` velocity reset behind `Spawner`.
- `SpawnerMobSpawnSetup` owns mob spawn placement, physics reset, and death callback rewiring behind `Spawner`.
- `SpawnerMobPoolLookup` owns normal, elite, and prefab mob pool lookup selection behind `Spawner`.
- `SpawnerMobBatchRoutine` owns mob batch spawn loop and delay sequence behind `Spawner`.
- `SpawnerSpawnPositionSelector` owns ground/fly spawn point and area position selection behind `Spawner`.
- `SpawnerMobSpawnSelector` owns basic/elite mob type and index selection behind `Spawner`.
- `SpawnerPeriodicSpawnScheduler` owns periodic spawn task interval and timer rules behind `Spawner`.
- `SpawnerPeriodicTaskList` owns periodic spawn task list storage, null-prefab add guard, due-spawn access, and indexed timer reset behind `Spawner`.
- `EnemyDamageGate` owns base enemy damage eligibility behind `Enemy`.
- `EnemyDamageState` owns base enemy HP subtraction and death-threshold state behind `Enemy`.
- `EnemyDeathRewardDispatcher` owns enemy death XP reward and inventory kill-hook dispatch behind `Enemy`.
- `EnemyDeathPresentation` owns base enemy death sprite hide, kill particle request, and zero-duration completion wait behind `Enemy`.
- `EnemyKillParticleSpawner` owns enemy kill particle instantiation behind `Enemy` and `Tentacle`.
- `EnemyTargetResolver` owns enemy player `Rigidbody2D` and `TrainLevelManager` lookup behind `Enemy`.
- `EnemyScreenEntryChecker` owns camera/collider/sprite screen-entry checks behind `Enemy`.
- `EnemyLayerAssignment` owns enable-time enemy layer reset behind `Enemy` and `Boss`.
- `EnemyEnableState` owns base enemy enable-time current HP, alive, and screen-entry reset values behind `Enemy`.
- `EnemyDisableCleanup` owns disable-time hit material cleanup and active-enemy unregister sequencing behind `Enemy`.
- `EnemyDespawnWithoutExpCompletion` owns no-reward despawn active guard and coroutine-stop/deactivate completion behind `Enemy`.
- `EnemyHitMaterialController` owns enemy `_isHit` material flag writes behind `Enemy`.
- `EnemyHitEffectRoutine` owns enemy hit material on/wait/off sequencing behind `Enemy`.
- `EnemySpritePresentation` owns null-safe enemy sprite visibility, color reset, and flip writes.
- `MobMovementStateResolver` owns Mob/FlyMob alive/stunned movement state selection.
- `MobVelocityPlanner` owns ground, flying, and post-death mob velocity formulas behind `Mob` and `FlyMob`.
- `MobDirectionPlanner` owns ground and flying mob direction selection formulas behind `Mob` and `FlyMob`.
- `MobTrainCollisionHandler` owns normal mob train collision damage and default camera shake handling behind `Mob`.
- `MobKnockbackPolicy` owns normal mob knockback force calculation behind `Mob`.
- `MobStunRoutine` owns normal mob stun duration wait sequencing behind `Mob`.
- `MobDeathCompletion` owns normal mob kill count reporting and death completion side effects behind `Mob`.
- `PoolActiveEnemyRegistry` owns active enemy list mutation and despawn iteration behind `PoolManager`.
- `PoolEnemyDespawnFilter` owns active-enemy null and boss-retained despawn eligibility checks behind `PoolActiveEnemyRegistry`.
- `PoolObjectProvider` owns pooled GameObject provider entry points for list initialization, indexed/dynamic pool lookup, and reuse/create orchestration behind `PoolManager`.
- `PoolMobPoolSet` owns indexed mob pool storage and normal/fly/elite getter routing behind `PoolManager`.
- `PoolListFactory` owns pooled list-array allocation and per-index empty list initialization behind `PoolObjectProvider`.
- `PoolIndexedPrefabResolver` owns indexed prefab-array bounds and pool/prefab pair resolution behind `PoolObjectProvider`.
- `PoolDynamicPoolRegistry` owns dynamic pool prefab-name key creation and dictionary get-or-add behavior behind `PoolObjectProvider`.
- `PoolDynamicMobProvider` owns dynamic prefab mob null guard, dynamic pool lookup, and reuse/create request flow behind `PoolManager.GetMob(...)`.
- `PoolReusableObjectSelector` owns inactive pooled object lookup and activation behind `PoolObjectProvider`.
- `PoolObjectFactory` owns pooled object instantiate-on-miss, prefab-name assignment, and pool append behavior behind `PoolObjectProvider`.
- `PoolBossPrefabLookup` owns `BossName` enum-to-boss prefab array lookup behind `PoolManager`.
- `EnemyHpCalibration` owns enemy/boss HP scaling formulas behind `Mob`, `Boss`, and `Tentacle`.
- `BossEntranceMotion` owns boss entrance interpolation behind `Boss`.
- `BossEntranceRoutine` owns boss entrance active-flag timing, movement loop, and final position assignment behind `Boss`.
- `BossEnableState` owns boss enable-time HP/current/alive/screen-entry/entrance reset values behind `Boss`.
- `BossKillEventRequestGate` owns boss kill-event request eligibility behind `EyeBoss` and `TrainBoss`.
- `BossDeathCompletionRouter` owns boss death completion route selection behind `EyeBoss` and `TrainBoss`.
- `BossDeathCompletionExecutor` owns shared boss death completion side effects behind `EyeBoss` and `TrainBoss`.
- `BossDeathExplosionSpawner` owns boss death explosion instantiation behind `EyeBoss` and `TrainBoss`.
- `EyeBossDeathPresentation` owns eye boss death explosion position and completion delay constants behind `EyeBoss`.
- `EyeBossPatternStartGate` owns eye boss normal pattern start eligibility behind `EyeBoss`.
- `EyeBossNormalPatternSelector` owns eye boss normal side/center pattern selection behind `EyeBoss`.
- `EyeBossNormalPatternRoutine` owns eye boss normal pattern post-spawn wait sequence behind `EyeBoss`.
- `EyeBossEnragePatternRoutine` owns eye boss enrage pattern duration-or-clear wait sequence behind `EyeBoss`.
- `EyeBossTentacleSpawnPointCollector` owns eye boss child transform collection for tentacle spawn points behind `EyeBoss`.
- `TrainBossCombatGate` owns train boss damage and train-collision eligibility gates behind `TrainBoss`.
- `TrainBossMovementPolicy` owns train boss forward movement direction, velocity composition, and sprite-facing constants behind `TrainBoss`.
- `TrainBossPhaseTransition` owns train boss phase 2 threshold checks behind `TrainBoss`.
- `TrainBossPhaseState` owns train boss phase 2 entered-state storage behind `TrainBoss`.
- `TrainBossPhasePresentation` owns train boss initial collider state and phase 2 animator/collider presentation behind `TrainBoss`.
- `TrainBossKnockbackPolicy` owns train boss knockback cooldown and force selection behind `TrainBoss`.
- `TrainBossKnockbackCooldownState` owns train boss knockback last-applied timestamp state behind `TrainBoss`.
- `TrainBossKnockbackApplier` owns train boss knockback Rigidbody2D velocity reset and impulse application behind `TrainBoss`.
- `TrainBossStunState` owns train boss stun active-state storage behind `TrainBoss`.
- `TrainBossStunRoutine` owns train boss stun active-state timing and duration wait sequencing behind `TrainBoss`.
- `TrainBossDeathPresentation` owns train boss death explosion offset and completion delay constants behind `TrainBoss`.
- `TrainBossTrainCollisionHandler` owns train boss train collision damage and boss camera shake handling behind `TrainBoss`.
- `TentacleTrainCollisionHandler` owns tentacle player-tag train damage forwarding behind `Tentacle`.
- `TentacleAttackAnimationDurationResolver` owns tentacle attack animation clip duration resolution behind `Tentacle`.
- `TentacleAttackRoutine` owns tentacle attack wait, animation trigger, attack callback, destroy delay sequence, and total duration calculation behind `Tentacle`.
- `TentacleDamageState` owns tentacle HP subtraction and death-threshold state behind `Tentacle`.
- `TentacleDeathCompletion` owns tentacle death particle, death sound, and object destroy side effects behind `Tentacle`.
- `EyeBossTentacleRegistry` owns eye boss spawned tentacle list rules behind `EyeBoss`.
- `EyeBossAttackSoundCooldown` owns the eye boss tentacle attack sound cooldown policy behind `EyeBoss`.
- `EyeBossAttackSoundCooldownState` owns the eye boss tentacle attack sound last-play timestamp state behind `EyeBoss`.
- `EyeBossTentaclePatternPlanner` owns eye boss tentacle spawn/weak index planning behind `EyeBoss`.
- `EyeBossTentacleAttackProfile` owns eye boss tentacle normal/enrage damage and delay selection behind `EyeBoss`.
- `EyeBossTentacleSpawner` owns eye boss tentacle prefab selection, instantiation, setup, attack start, max-duration collection, and spawn sound publishing behind `EyeBoss`.
- `EyeBossEnrageTransition` owns eye boss forced-enrage threshold prediction and HP clamp decision behind `EyeBoss`.
- `SoundManager` listens to `SoundEventBus` and `GameManager.OnGameStateChanged`.

## Third-Party And Package Code

- `Assets/com.rlabrecque.steamworks.net/` is Steamworks.NET package code.
- `Assets/Plugins/Demigiant/` is DOTween/DOTween Pro package code.
- Do not mix package code into LoveTrain structure decisions unless the task specifically targets those packages.

## Known Pitfalls

- Many scene-facing references are public fields or `[SerializeField]` fields. Renames and type changes can break scenes/prefabs.
- Several managers use static instances or `DontDestroyOnLoad`. Adding new global state should be proposed before implementation.
- Some source files include comments with corrupted encoding. Avoid unrelated churn while editing.
- Existing dirty worktree changes may be user-owned; do not revert them without explicit instruction.
