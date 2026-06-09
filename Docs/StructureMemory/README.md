---
status: active
authority: structure-memory
category: system-map-index
last_reviewed: 2026-05-18
---

# Structure Memory

`StructureMemory` documents are feature-level maps for fast context reconstruction. They are not official architecture contracts.

Use these documents when a future task needs to quickly understand an existing flow before editing code.

## When To Create Or Update

- A reusable structure or multi-file flow is created or materially changed.
- Ownership, lifecycle, cleanup, or runtime state flow changes.
- A shared service, interface, `MonoBehaviour`, `ScriptableObject`, prefab-facing contract, or scene-facing serialized reference becomes important to future work.
- A date-based `SessionLogs` entry would be too hard to find for the next related task.

## Required Sections

Each focused feature document should include:

- Purpose
- Current Structure
- Key Files
- Ownership And Lifecycle
- Extension Entry Points
- Known Pitfalls
- Promotion Candidate

## Boundaries

- Do not record every task diff here.
- Do not use this folder for unverified guesses.
- Do not treat this folder as more authoritative than the current task or root project rules.
- If a structure becomes stable enough to guide future implementations, propose promoting it to a future `Docs/Architecture/` or `Docs/Contracts/` document.

## Current Documents

- [Script System Map](./ScriptSystemMap.md)
- [Script Systems](./ScriptSystems/README.md)
