# Permanent upgrade validation evidence — 2026-10-06

`ValidationReport.md` records source compilation and isolated behavior/prefab checks. `BehaviorChecks.cs`, `NodeBehaviorChecks.cs` and `FakeUnity.cs` exercise actual catalog, progress and node sources using an in-memory Unity/PlayerPrefs substitute. This checks purchase rules and data restoration, rather than real Unity input or user saves.

`BuildSources.ps1` reconstructs a task-owned compilation project from Unity's existing project/reference assemblies. `audit_upgrade_prefab.py` checks the authored prefab's IDs, hierarchy and references. Generated project/build state, reconstructed baseline copies and raw logs stay local.

`python run_tooltip_checks.py` extracts production `PlaceTooltip`, `ShowTooltip`, `OnPointerEnter`, and `OnSelect` bodies and runs coordinate/selection regression checks through `TooltipRegressionValidation.csproj`. Its doubles verify fixed button-center anchoring, ignored pointer positions, border direction changes, and hover/selection dispatch; they do not run actual Unity transforms, events, or Play Mode.

See [the session log](../../Docs/SessionLogs/2026-10-06-permanent-upgrades.md) for the final 10-node/24-tier UI, validation details and pending effect/item configuration.
