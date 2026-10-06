# Driving, bosses, levels, drops and pursuit evidence

Scope and implementation are recorded in [the session log](../../Docs/SessionLogs/2026-10-06-driving-boss-progression.md). User approved the proposed TaskBrief with `ㄱㄱ`.

- Final MSBuild runtime142 / Editor and fixture10 source compilation: errors0. Generated original csproj files are preserved.
- Isolated Unity6000.2.3f1: authored scene/prefab import,113 reference/layout checks and a1280×720 UI-only [preview](./preview.png), visually inspected. This preview is not a gameplay/world-render capture.
- [Native PlayMode result](./play-result.txt):54 checks, changed/runtime Console errors0. Real keyboard input, Update/FixedUpdate/coroutines, drop attraction and settlement, stage transition, camera, train-boss contact, sequential3-second tentacle warnings and pursuing-hand death. Read fixture noise controls in the result/session before treating this as complete gameplay verification.
- [Snapshot](./source-snapshot.json): all142 runtime files and63 changed assets match the tested copy.2992 original Assets meta files have no duplicate GUIDs. Documentation links and whitespace checks passed.

The tools here are confined to the disposable `project/` copy and its distinct persistent-data identity. They never launch batchmode against the open original project. Raw logs, before/after asset backups, merge receipts and transient build/cache files remain locally available but are ignored by Git.

The first native run exposed non-simulated Rigidbody movement failing to update Transform; production RewardPickup now updates Transform in that mode. A subsequent failure came from shortened-warning observation in the fixture; the final test uses production3-second warnings and records three complete, non-overlapping attacks. Both failed-run logs are retained.

Full15-minute gameplay, player builds, visual world-render review, authored drop bounce/trail feel and complete weapon-versus-boss combat remain manual checks. SessionLog, three StructureMemory maps, DecisionLog and ErrorLog were updated; no Architecture/Contracts or Presentation layer was created.

Follow-up: the original snapshot predates the pickup outlines and sorting correction. Use [current effect visibility evidence](../effect-visibility-validation/README.md) for production URP background-inclusive rendering and current hashes. The earlier object/particle existence checks did not detect effects hidden behind BackGround.
