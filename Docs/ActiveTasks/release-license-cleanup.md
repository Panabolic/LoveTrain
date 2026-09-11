---
status: active
authority: active-task
category: docs
task_id: release-license-cleanup
last_reviewed: 2026-06-12
---

# Release License Cleanup

## Goal

Prepare LoveTrain's release-facing license documents, reduce accidental TMP
sample asset usage, and remove the verified-unused Particle_FX graphic pack.

## Active Mode

Implementation / Verification

## Requested Work

- Create public and internal license documentation for current release assets.
- Set TMP defaults to the project's real UI font, DungGeunMo.
- Disable TMP's default EmojiOne sprite asset and emoji support.
- Delete the verified-unused `Particle_FX_1.3 (2)` graphics folder.
- Leave audio files untouched because the planner owns audio license evidence.

## Scope Authority

1. Current user instruction
2. Proposed plan accepted by the user
3. `Docs/README.md` and `Docs/TaskIndex.md` as routing context
4. Prompt-provided Unity safety rules

## Allowed Changes

- `THIRD_PARTY_NOTICES.txt`
- `CREDITS.md`
- `Docs/Legal/LICENSE_INVENTORY.md`
- This ActiveTask and `Docs/TaskIndex.md`
- Narrow TMP settings fields in `Assets/TextMesh Pro/Resources/TMP Settings.asset`
- Delete only `Assets/Sprites/Particle_FX_1.3 (2)` after external GUID scan confirms zero references
- Add one dated SessionLog for the completed work

## Forbidden Changes

- Do not modify, move, or delete `Assets/Resources/Sounds/**`.
- Do not change `EndingManager`, `CreditEnemy`, scenes, or prefabs for in-game credits.
- Do not delete DOTween Pro, Steamworks.NET, Super Pixel Impact FX Pack 2, LiberationSans sample files, or EmojiOne sample files.
- Do not touch existing unrelated dirty worktree changes.

## Required Context

- Prompt-provided Unity safety rules: do not run batchmode while Unity Editor is open; do not claim compile success unless run.
- `Docs/README.md`
- `Docs/TaskIndex.md`
- Existing local license/readme files under the referenced asset folders.

## Risks

- `TMP Settings.asset` is a ScriptableObject asset; changing defaults can affect newly created TMP text and runtime fallback behavior.
- Clearing TMP default sprite/emoji support may affect text that relies on TMP emoji sprite parsing; no direct gameplay use was found during planning.
- `Resources` audio remains a release/legal risk until planner-provided evidence is attached.
- Deleting a Unity asset folder also deletes `.meta` GUIDs; the deletion must be gated by a reference scan.

## Done Criteria

- Release notice, credits, and internal inventory documents exist.
- TMP default font points to DungGeunMo SDF.
- TMP default sprite asset is cleared and emoji support is disabled.
- `Particle_FX_1.3 (2)` no longer exists after zero external GUID references are confirmed.
- Documentation names audio as pending and does not modify audio files.

## Verification Plan

- Run `git diff --check`.
- Re-check TMP settings with `rg`.
- Re-check `Particle_FX_1.3 (2)` path no longer exists.
- Re-check license docs include the expected key assets.
- Because `TMP Settings.asset` is a ScriptableObject asset change, suggest Unity batchmode compile only if Unity Editor is closed.

## Documentation Impact

- ActiveTask created for scope.
- TaskIndex updated as dashboard.
- SessionLog required after implementation.

## Open Questions

- Audio license evidence remains planner-owned and outside this task.
