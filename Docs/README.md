---
status: active
authority: docs-router
category: router
last_reviewed: 2026-06-07
---

# LoveTrain Project Docs Guide

This folder is the Markdown project memory for LoveTrain. Markdown is the source of truth for agent-facing project context. No Presentation HTML layer exists.

## Task Routing Order

1. Start from the current user instruction and any prompt Task Brief.
2. If the prompt names or clearly matches an active task, read [ActiveTasks/](./ActiveTasks/).
3. If task scope is unclear, use [TaskIndex.md](./TaskIndex.md) as a router/dashboard.
4. Use this README to choose the task-specific technical documents.
5. Search [ErrorLog.md](./ErrorLog.md) and [DecisionLog.md](./DecisionLog.md) when the task touches known recurring mistakes, durable decisions, lifecycle/serialization risks, or when the user asks for those logs.

[CurrentTask.md](./CurrentTask.md) is deprecated. New active scope belongs in the current prompt or `Docs/ActiveTasks/<task-id>.md`.

## Scope Authority

When task scope conflicts, follow this order:

1. Current user instruction
2. Current prompt Task Brief
3. Matching [ActiveTasks/](./ActiveTasks/) document
4. [TaskIndex.md](./TaskIndex.md) router/dashboard context

Scope authority defines what the thread may change. It does not override Unity safety rules or technical contracts.

## Technical Authority

When implementation or architecture documents conflict, follow this order:

1. Root [AGENTS.md](../AGENTS.md) safety and verification policy
2. Future `Contracts/`, if an approved task creates source-of-truth contracts
3. Future `Architecture/`, if an approved task promotes stable architecture docs
4. Future `Guides/`, if an approved task creates implementation guides
5. [DecisionLog.md](./DecisionLog.md)
6. [ErrorLog.md](./ErrorLog.md)
7. [StructureMemory/](./StructureMemory/)
8. [RefactorLog.md](./RefactorLog.md)
9. [SessionLogs/](./SessionLogs/)

`StructureMemory`, `RefactorLog`, and `SessionLogs` are context/planning documents. They help future work start faster, but they do not override task scope, durable decisions, recurring-error guidance, or Unity safety rules.

## Memory Document Types

- [TaskIndex.md](./TaskIndex.md): router/dashboard for active and proposed task documents. It is not active scope.
- [ActiveTasks/](./ActiveTasks/): task-specific scope, allowed and forbidden changes, done criteria, verification plan, and risk notes.
- [CurrentTask.md](./CurrentTask.md): deprecated compatibility notice only.
- [ErrorLog.md](./ErrorLog.md): recurring mistakes and prevention rules.
- [DecisionLog.md](./DecisionLog.md): durable project decisions.
- [RefactorLog.md](./RefactorLog.md): refactor candidates and structural debt notes. This document is written in Korean by project preference.
- [SessionLogs/](./SessionLogs/): dated task outcome logs.
- [StructureMemory/](./StructureMemory/): feature-level system maps for fast context reconstruction.

## Where To Start By Task Type

### Game State, Pause, Event Queue, Stage Transition, Ending

- [Script System Map](./StructureMemory/ScriptSystemMap.md)
- [Core Runtime And Game Flow](./StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md)

### Train, Speed-As-Health, Level, XP, Death/Recovery

- [Script System Map](./StructureMemory/ScriptSystemMap.md)
- [Core Runtime And Game Flow](./StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md)

### Items, Inventory, Level-Up Choices, Weapons, Item Cooldowns

- [Items Inventory And Weapons](./StructureMemory/ScriptSystems/ItemsInventoryWeapons.md)

### Events, Random Outcomes, Event Effects, Popup UI Queueing

- [Event System And UI Queue](./StructureMemory/ScriptSystems/EventSystemUiQueue.md)

### Enemies, Bosses, Spawning, Pooling, Combat Cleanup

- [Enemy Spawn And Boss Flow](./StructureMemory/ScriptSystems/EnemySpawnBossFlow.md)

### Audio, Warning UI, Cursor, Option UI, HUD Presentation

- [Core Runtime And Game Flow](./StructureMemory/ScriptSystems/CoreRuntimeGameFlow.md)
- [Script System Map](./StructureMemory/ScriptSystemMap.md)

### Localization, Translation CSV, English UI

- [English Localization](./StructureMemory/ScriptSystems/Localization.md)

## Documentation Update Policy

- Small local fixes usually need only a dated session log.
- Reusable structure, ownership changes, lifecycle changes, shared services, interfaces, `MonoBehaviour`s, `ScriptableObject`s, or prefab-facing contracts should update the narrowest matching `StructureMemory` document.
- Active or long-running scope belongs in `ActiveTasks/`, with `TaskIndex.md` updated as the dashboard.
- Durable decisions go in `DecisionLog.md`.
- Recurring mistakes and prevention rules go in `ErrorLog.md`.
- Refactor candidates and structural debt go in `RefactorLog.md`.
- At the end of non-trivial work, report a Doc Impact Check category.
- Do not add broad new documentation folders unless a task needs them.
