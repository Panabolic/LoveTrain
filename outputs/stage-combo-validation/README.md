# Distance stages and combo validation evidence

The final runtime changes and their scope are described in [the session log](../../Docs/SessionLogs/2026-10-06-stage-combo.md). These files are disposable validation tools and evidence, outside the game's Assets tree.

- Actual-source/dependency-double behavior checks: distance/schedule 70, GameManager/combo 49, Spawner warning cancellation 37. Their `Test-*.ps1` runners and result files are retained.
- [Unity evidence](./agent-validation/README.md): 382 authored-scene EditMode checks and 44 native copied-Junmo PlayMode checks, with 15 UI-only PNGs. The PlayMode fixture seeds route endpoints and disables unrelated train input/fuel and ordinary spawning; it does not test collision kills or an entire boss encounter.
- `source-snapshot.json` preserves the stage/combo validation-time hashes. Its eight runtime source hashes still match the committed sources. Its Junmo hash predates the later currency/lower-HUD layout changes and must not be described as a current whole-scene PlayMode verification.
- The later [HUD evidence](../unity-ui-preview/README.md) records the upgrade/socket probes and final lower-HUD layout checks. See its `source-snapshot-latest.json` for the final scene hash and the separation of validation scopes.

Scene backups, clone/cache/build outputs, raw logs, process identifiers and intermediate authoring tools remain local. The final fixtures use isolated preview save identifiers; never run a second batch Editor against the open original project. Physical input, complete boss spawning/combat, 15-minute full gameplay and player builds remain unverified.
