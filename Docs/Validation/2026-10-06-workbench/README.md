# Workbench validation evidence — 2026-10-06

`result.txt` records the successful Unity 6000.2.3f1 isolated-project run: 103 asset/scene and Editor-method interaction checks. `WorkbenchIsolatedValidation.cs` is the executed fixture, stored outside Assets so it is not part of the game. `source-snapshot.json` identifies the source snapshot copied for validation.

To reproduce, use a separate disposable copy of Assets/Packages/ProjectSettings, place the fixture under that copy's Assets/Editor, and run Unity with `-batchmode -nographics -quit -executeMethod WorkbenchIsolatedValidation.Run`. Do not execute the fixture in the live project. The fixture sets a unique company/product validation namespace before wallet persistence, verifies that identity and restores the copy's settings afterward.

The fixture opens the copied Junmo scene, validates serialized references, invokes the real UI pointer handlers directly, and advances real DOTween sequences explicitly. This is not a Play Mode test, a GraphicRaycaster test, a physics collision test, or a screenshot/visual QA claim. Full raw Editor logs and package/cache copies were temporary and are not retained in the source tree.

The plain .NET checks also passed: 249 economy/reward/fuel assertions, 27 inventory checks, 26 event/filter checks, 20 stage-clock checks and 10 UI queue checks (332 total). See `Docs/SessionLogs/2026-10-06-item-workbench.md` for feature scope and known limitations.
