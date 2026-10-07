---
status: active
authority: active-task
category: runtime-refactor
task_id: runtime-composition-comparison
last_reviewed: 2026-10-07
---

# Runtime composition branch comparison

## Goal and authority

Mode: Implementation, followed by comparison and verification. Risk: High. Target: Framework/Core.

The user requested an implementation branch for the proposed composition structure, then explicitly expanded the request from a first cooldown change to the entire structure, including monsters, projectiles and the player. Baseline is `lovetrain2` at `95a7d5e`. Work branch is `codex/runtime-composition`.

## Allowed

- Actual per-instance item composition for all 13 current item authoring types.
- Attack cycles, animation-driven emission, flight/lifetime, area effects and weapon state.
- Enemy and player health/fuel, movement, death, targeting, knockback and boss-specific reservation/contact state.
- Shared local Unity bindings, presentation links and owner-scoped cleanup.
- New plain C# parts and their new script metadata; existing component adapters and related project memory.
- Isolated Unity compilation and native tests, source/asset compatibility inspection and baseline comparison.

## Preserved boundaries

Existing scenes, prefabs, SO authoring fields and assets, existing script GUIDs, saved enums/IDs, Animator parameters, Animation Events, asmdefs, packages, ProjectSettings and bootstrap/DDOL remain unchanged. Existing MonoBehaviours and SO inheritance remain compatibility adapters. Asset/schema migration or deletion of those adapters requires a separately specified scope.

No new Manager, Singleton, global event bus, automatic runtime UI hierarchy or general-purpose graph engine is introduced.

## Done criteria

- Every new execution part has actual callers, rather than being an unused UML placeholder.
- All 13 items use an executable recipe and per-equipped-instance state.
- Player, enemy and projectile paths delegate the specified responsibilities to their parts.
- Public virtual item hooks, actual target filters, animation event multiplicity, stat snapshot times, cooldown readiness order and native cleanup remain compatible.
- Bound children end with equipment; independent projectiles keep their own behavior and only callbacks to released owners expire.
- Original assets and serialized field names/types remain stable.
- Comparison distinguishes file/class growth from responsibility reuse and retained compatibility inheritance.

## Verification and remaining questions

Source/static review, new GUID uniqueness, source-copy identity, real Unity compilation and isolated native component/Play Mode checks are required. The original Editor/project is not launched again in batch mode while reported in use. Production scene play, full combat/animation visuals, OS input and player builds must be reported separately.

Recipe storage beyond the existing authoring adapters and eventual prefab/SO migration remain subsequent decisions. Rollback is limited to this branch's changes relative to `95a7d5e`.

## Documentation impact

SessionLog, the existing Core/Items/Enemy StructureMemory maps, RefactorLog and this router entry. No Architecture/Contracts promotion or Presentation HTML work.

See [comparison and evidence](../Validation/2026-10-07-runtime-composition/README.md) and [session log](../SessionLogs/2026-10-07-runtime-composition.md).
