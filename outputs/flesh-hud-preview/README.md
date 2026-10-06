# Flesh HUD validation evidence — 2026-10-06

The final authored Junmo HUD is 150×92 logical pixels with a 130×11 gauge. Flesh and souls use the same 12-pixel font size and a shared right edge. The actual Unity UI-only PNGs retain the final Korean/English layout, overflow colors and 105→75→35 examples.

- `Test-FleshCreationProgress.ps1` compiles the actual calculation source in memory and verifies 19 boundaries and 1,000 independent purchase comparisons.
- `FleshHudValidation.cs` opens the actual copied scene and runs 356 serialized-reference, presentation, lifecycle-method, localization and layout checks; its 10 PNGs are Editor RenderTexture captures on a neutral background.
- `FleshHudPlayValidation.cs` runs 36 actual Junmo PlayMode lifecycle, reward, transaction, state, disable/re-enable, language and synthetic Dynamic Input System F checks. All 10 active equipment slots have zero overlaps with the HUD.
- `scene-audit.json` compares pre-task documents with the final scene: no removed IDs, 143 authored HUD records and seven changed existing records.

Run Unity fixtures only in a disposable copy with company name `CodexUIRenderPreview` and a unique product name beginning `WorkbenchPreview_`. Put one fixture under that copy's `Assets/Editor`. Use Unity 6000.2.3f1 with `-batchmode -executeMethod FleshHudValidation.Run` or `-batchmode -executeMethod FleshHudPlayValidation.Run`. The runners exit themselves; omit `-quit` for the asynchronous PlayMode runner. The PlayMode input override is transient and exists only in the guarded copy.

`BuildSources.ps1` builds all current runtime sources and optionally Editor sources with a task-owned temporary project; it does not edit Unity-generated projects. It requires the existing Unity installation/reference assemblies and .NET MSBuild.

Raw `.log` files, generated projects, binaries, the copy's package/cache directories, historical backups and early attempts remain local. Physical keyboard input, a player build and native whole-game Screenshot capture are not claimed. Full scope and remaining warnings are recorded in [the session log](../../Docs/SessionLogs/2026-10-06-flesh-hud.md).
