# Stage / combo validation

Run fixtures only in a disposable Unity 6000.2.3f1 copy outside the open original project. The original project is open in Unity; do not use batchmode against it. The copy must keep its unique `CodexUIRenderPreview` / `WorkbenchPreview_*` save identity. Do not copy the original `ProjectSettings.asset` over that identity.

Before root starts Unity, sync the final source files, metadata, scene and required assets to the copy, preserve the original scene snapshot, and record matching SHA256 digests. Copy fixtures from this directory to the copy's `Assets/Editor`; never put them in the original Assets tree. Use a task-specific `-logFile` and `Start-Process -WindowStyle Hidden`.

`BuildSources.ps1` reconstructs a task-owned MSBuild project with installed Unity and package assembly references, including every current runtime source under Assets/Scripts and the Assets root. It never edits generated Unity csproj files. Its result is source compilation, not confirmation of Unity import or Play Mode.

`StageComboValidation.Run` is a synchronous guarded Unity EditMode fixture: pure runtime calculations, current GameManager kill methods, authored serialized references, presenter methods, layout bounds, and actual authored UI-only RenderTexture PNGs. Use `-batchmode -executeMethod StageComboValidation.Run`; rendering requires graphics, so omit `-nographics`. The runner exits itself. PNGs are neutral-background UI renders, not gameplay captures.

`StageComboPlayValidation.Run` exercises the copied Junmo scene's native GameManager/StageManager/HUD callbacks: combo thresholds and expiry, modal/pause freeze and resume, Boss run-clock/route freeze while combo runs, and presenter disable/enable and terminal reset. The fixture alone disables train input/fuel and mob spawning to focus native clock behavior. Use `-batchmode -executeMethod StageComboPlayValidation.Run` without `-quit`; the asynchronous runner survives domain reload and exits itself. It does not claim collision kills, physical input, or a full gameplay screenshot.

Its final warning probe seeds the real route owner at the endpoint, enables the actual scene Spawner, calls its distance-consumer method, cancels through native OnDisable, verifies deferred recovery, re-enables through native OnEnable, waits for native StageManager.LateUpdate to retry that same boss once, and stops spawning before the authored 4-second warning delay. It checks no Boss instances were created and preserves the same boss index/reservation through cancellation. This is real warning lifecycle coverage with a seeded route, not a full encounter playthrough.

For fixture compilation before copying to Unity, run `BuildSources.ps1 -IncludeEditor -IncludeFixtures`. This compiles the real runtime sources and task fixtures with actual installed Unity/package references.

Read the emitted result and raw Editor log. Preserve failures. Unity fixture invocation is coordinated by root; preparing these files does not execute Unity. Player build and physical input are outside these fixtures.

Completed evidence: `unity-stage-combo-validation-result.txt` records 382 passing EditMode checks and 15 UI-only PNGs; `unity-stage-combo-play-result.txt` records 44 passing native PlayMode checks, including actual warning cancellation/re-enable retry. `unity-stage-combo-play-console.txt` preserves baseline warnings and the existing unregistered `BGM_Boss` warning separately from runtime errors.

The validation-time scene hash in the parent `source-snapshot.json` is historical: later currency/HUD placement edits changed Junmo. The eight stage/combo runtime sources remain identical. Final HUD checks are documented in [the newer UI evidence](../../unity-ui-preview/README.md); this stage/combo PlayMode run was not repeated after that layout-only update.
