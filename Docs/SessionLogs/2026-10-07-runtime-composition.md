# 2026-10-07 Runtime composition comparison branch

The user explicitly requested a branch implementation and comparison of the full proposed composition structure, then rejected a first-change-only scope and confirmed that monsters, projectiles and the player are included.

Baseline: `lovetrain2`, `95a7d5e`. Branch: `codex/runtime-composition`. Starting working tree was clean. Mode: Implementation; risk: High. [Active scope](../ActiveTasks/runtime-composition-comparison.md).

## Changes and reasons

- All 13 Item_SO authoring types create shared runtime recipes. ItemInstance owns actual effect instances, equipment state, bound child cleanup and compatibility cooldown mirrors.
- StatModifier, CombatProc, TimedProc and WeaponOverride hold the actual stat/proc/timer/weapon responsibilities. Shared SOs no longer hold per-instance visual references.
- AttackCycle, SummonAndAwait, AnimatedPortFire and BurstEmission own the corresponding attack progress. Existing Animation Event entry methods forward locally through PresentationLink.
- HomingFlight, FlightLifetime and AreaPresence own flight/expiry/contact/cadence state. Independent fired objects retain their lifetime after equipment removal.
- HealthState, FuelState, DeathLifecycle, KnockbackState and MovementRules are used by monsters and the player. BossContactTracker, AttackReservation and TimedStrike own boss contact/reservation/strike state.
- TargetRegistry/TargetHandle concentrate damage/target/cleanup capability access behind existing Unity adapters. Existing public activeEnemies remains the compatibility source of truth.
- ObjectHost, PresentationLink and SpawnScope are per-owner C# parts. They introduce no new Manager, Singleton, DDOL object, asset-generated UI or mandatory universal object superclass.

Existing serialized authoring fields, asset GUIDs, scenes/prefabs/SO data, Animator/Event names, enums, save IDs, packages/settings/asmdefs and bootstrap are preserved. Existing MonoBehaviour/SO inheritance remains as authoring/API adapters rather than being silently removed without asset migration.

## Important preserved behavior

Longinus captures a target at preparation, reselects at emission and uses latest emission stats; each valid normal spear completion restarts the then-current cooldown. Poison starts cooldown at Fire presentation, emits on each delivered animation signal and passes its emission snapshot through missile flight into gas. Laser catch-up ticks and gas drop-extra-ticks remain different. Inventory timed effects activate after decrement in the same frame; launcher readiness is checked before decrement.

Bound scope cleanup snapshots its list before deactivation because children can unregister during OnDisable. Equipment release invalidates launcher completion/animation callbacks before deferred destruction while leaving fired objects independent. Public virtual cooldown behavior for custom authoring subclasses is retained, including subclasses of Heart/Bible.

## Comparison, validation and limits

Measured C# files: 148 to 177; classes: 167 to 196. This is a complete implementation behind retained adapters, not a completed asset migration or guaranteed file-count reduction. [Detailed UML, metrics and evidence](../Validation/2026-10-07-runtime-composition/README.md).

Source/static and independent cross reviews completed. Actual branch runtime and existing Editor scripts compiled in an isolated Unity 6000.2.3f1 project. Initial native checks: 45 passed; expanded checks: 60 passed; final checks after review repairs: 63 passed, zero failures and zero C# compiler errors. The isolated Editor exited. Final source-copy hash identity and field/GUID checks are recorded in the evidence folder. Production scene/visual/input/full-run/player-build verification remains a separate manual/native follow-up. Do not interpret an isolated component pass as complete game validation.

## Doc Impact Check

SessionLog, ActiveTask/TaskIndex, existing Core/Items/Enemy StructureMemory and RefactorLog. No Architecture/Contracts promotion or Presentation HTML layer. Rollback is this branch's change set relative to `95a7d5e`; unrelated changes must remain intact.
