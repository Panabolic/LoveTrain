# Permanent upgrade validation — prefab migration, 2026-10-06

## Tooltip button-center regression — 2026-10-06

The latest user correction fixes the tooltip to the hovered button's center. Mouse motion does not reposition it. Both `OnPointerEnter` and `OnSelect` pass the same card RectTransform; `ShowTooltip` converts `card.rect.center` through `TransformPoint` and the panel's `InverseTransformPoint`. The existing independent left/up border direction changes remain.

- `python run_tooltip_checks.py` passed 8,559 isolated assertions against four method bodies extracted unchanged from current production: `PlaceTooltip`, `ShowTooltip`, `OnPointerEnter`, and `OnSelect`. Checks include the top-center button pivot's local center at y=-36, translated/scaled transform doubles, three widely differing pointer positions that yield the same tooltip position, identical hover/mouse-selection/keyboard-selection anchoring, unbound-card guards, and all prior border/pivot/position-grid checks. The runner also checks that pointer-move handlers and pointer-based menu positioning APIs are absent. See `tooltip-button-anchor-checks.log`.
- `BuildSources.ps1 -LogName tooltip-button-anchor-build.log` compiled all 142 current runtime sources with exit 0 and 228 current-workspace warnings. Only runtime source compilation was required; no Editor source or asset changed.
- Harness updates passed focused `git diff --check` and Python syntax checks.

**Limits:** transforms and input events are doubles, not actual Unity execution. Asset import, real input, Play Mode, and player builds remain unexecuted. No new native UI automation or batchmode attempt was made for this correction. The first harness run had a fixture expectation that overlooked the border flip for y=-36; correcting that expectation to the above-button anchor (-24) produced the final passing result without production changes.

## Historical tooltip direction regression — 2026-10-06

Before the button-center correction above, the tooltip kept its cursor-adjacent corner twelve panel-local units from the pointer. Its opening direction changed independently on each axis when the default right/down direction would cross the eight-unit panel margin. `PermanentUpgradeCard.OnSelect` retained `PointerEventData` for mouse selection and used the selected card transform for keyboard selection. This pointer-following behavior has been superseded.

- `python run_tooltip_checks.py` passed 8,511 isolated assertions. The runner extracts the actual, unchanged `PlaceTooltip` and `OnSelect` method bodies from the production sources at execution time; it does not maintain a second implementation. Small Unity coordinate/input doubles exercise the central position, each border/corner, both-axis flips, exact margin thresholds, three initial pivots, a screen-position grid, mouse selection dispatch, keyboard fallback, and unbound-card guards. See `tooltip-behavior-checks.log`.
- `BuildSources.ps1 -LogName tooltip-fix-build.log` compiled all 142 current runtime sources against the installed Unity references with exit 0. The current dirty workspace emitted 228 warnings, including the existing Inspector-assigned Menu/Card fields. No comparable same-workspace baseline build was executed for warning attribution; this count must not be compared directly with the earlier migration's smaller source set.
- Validation harness additions and updates passed focused `git diff --check`.

**Limits:** the coordinate/input doubles do not execute Unity's screen-to-local camera conversion, actual Input System events, Editor asset import, Play Mode, or player builds. The running Editor was not batch-started again. Root's native UI verification attempt was denied by the computer-use tool, so actual pointer placement remains a manual Unity check.

## Current implementation and evidence

The current UI uses `Assets/Resources/PermanentUpgradeMenu.prefab`: ten presentation nodes group the original twenty-four purchase stages. The prefab owns Canvas, text, buttons, tooltip, progress rectangles, and fixed-aspect wrappers. Menu code loads the GameObject prefab, checks its menu component, instantiates the authored hierarchy, and requests state changes from the existing progression owner. No production source or asset was modified by this validation work.

The earlier runtime-generated 24-card implementation is historical. Its evidence below must not be treated as validation of the current prefab UI.

- Migration baseline: `BuildSources.ps1 -LogName prefab-migration-baseline-build.log -IncludeEditor` passed with 131 runtime and seven Editor sources, exit 0 for each assembly.
- Final code: `BuildSources.ps1 -LogName prefab-migration-final-build.log -IncludeEditor` passed with 132 runtime and seven Editor sources, exit 0 for each assembly. This includes the new node model and the final explicit prefab-component loading and hover-preservation changes. See `prefab-migration-final-build.log` and `prefab-migration-final-build-editor.log`.
- Actual-source behavior harness: `dotnet run --project PermanentUpgradesBehaviorValidation.csproj` passed 188 isolated assertions. The original 115 purchase/save/unlock/CSV checks remain, plus 68 node checks and five new translation-row checks. See `node-behavior-checks.log`.
- Read-only prefab audit: `python audit_upgrade_prefab.py` passed 5,281 serialized integrity checks across 125 GameObjects and 476 serialized objects. It checked local file IDs and ownership, reciprocal hierarchy links, external GUID resolution and feature GUID uniqueness, typed serialized Menu/Card references, ten distinct node IDs, twenty-four Image progress segments, five grid columns, authored aspect roots/bars, passive tooltip graphics, real button hit graphics, and absence of an extra EventSystem. See `prefab-audit.log`.

Node tests prove that one next-stage request buys exactly one stage; insufficient funds leave stage, wallet, and save unchanged; completed nodes cannot buy again; original stage IDs remain authoritative in saves; five-stage and one-stage completion survive reload; undecided damage stays unavailable; current and next descriptions differ appropriately; configured damage increments sum; and partial/unknown saved stage IDs are preserved. No native Unity APIs or user saves are used in the harness.

Independent source review confirmed that purchases request the captured next stage from PermanentUpgradeProgress, all serialized card IDs map to the default ten-node catalog, and MAX purchase selection loss preserves an existing mouse hover until pointer exit or disable. The existing Start callback guard and final Junmo loading flow remain intact. The Resources load now explicitly resolves a GameObject prefab and its PermanentUpgradeMenu component. No unresolved production review blocker was found.

The runtime warning count changed from 166 at the migration baseline to 184 at final compilation: eighteen additional warnings concern Inspector-assigned menu/card fields whose prefab references passed the typed audit. There were no compiler errors. These builds use independent validation csproj files generated from read-only Unity project templates and existing Unity/package DLLs. Unity-generated csproj files are not edited; package assemblies and Unity generators are not rebuilt.

**Limits:** Unity asset import, Console regression checking, real pointer/selection interaction, visual layout, MonoBehaviour lifecycle, native PlayerPrefs/JsonUtility, and player builds were not executed. The running Editor has no usable automation connection; no second batchmode Editor was started. Static prefab checks do not prove import or appearance. Remaining manual check: open Start, inspect all ten nodes and passive progress bars, hover available/locked/MAX nodes, buy one tier and reload, verify the matching run effects, and start Junmo from the footer button.

## Historical implementation evidence

Repository compilation and isolated behavior validation passed. Live Unity validation is blocked because the running Editor has no usable automation connection; no second batchmode Editor was started. UI interaction, rendering, Unity Console results, and player builds remain unverified.

## Executed evidence

- Baseline: `dotnet msbuild BaselineSourceValidation.csproj /t:Rebuild` with Unity 6000.2.3f1 reference assemblies, exit 0, 126 Assembly-CSharp sources. Four already-dirty runtime files were reconstructed in `baseline-copy` by reversing only this feature's changes; initially clean StartMenuManager came from HEAD. No current production file was reverted. See `baseline-reconstructed-build.log`.
- Final: `./BuildSources.ps1 -LogName final-build.log`, exit 0, 131 Assembly-CSharp sources, including all five new permanent upgrade scripts. The script reuses installed Unity reference assemblies and does not change generated Unity csproj files. See `compiled-sources.txt` and `final-build.log`.
- Behavior: `dotnet run --project PermanentUpgradesBehaviorValidation.csproj`, exit 0, 115 assertions against the actual catalog, definition, progress, and localization CSV parser sources. PlayerPrefs, Resources, and JsonUtility are isolated test doubles. No user save data was read or written. See `behavior-checks.log`.
- Serialized wiring: Start.unity retains animation callbacks LoadPlayScene then EnterStartState, playSceneName Junmo, and an existing InputSystemUIInputModule. Start and Junmo are enabled in Build Settings. All five feature scripts and the folder have meta files with six distinct GUIDs.
- Focused `git diff --check` passed; Git emitted line-ending normalization notices for two files.

## Covered behavior

Legacy soul balance loading; purchase debit and reload; sequential prerequisites; duplicate/insufficient purchase rejection; total fuel and dash tiers; configured additive gun damage; laser and explicitly bound boss-item unlocks; run wallet synchronization; unknown save IDs; duplicate save IDs; malformed, unsupported, incomplete, negative, and blank-ID saves; preservation of unreadable snapshots; failed purchase flush rollback; negative wallet clamping; and all fourteen Korean/English menu translation rows through the production parser.

## Independent review

The original animation's second callback cannot advance GameState while the upgrade menu is open. The final menu start button enables loading and enters the existing Junmo start flow. The grid's four columns fit its 728-unit viewport: 4×173 + 3×8 + 12 = 728. The 780×430 panel fits the project's logical 800×450 safe area, and it requests aspect-controller refresh. Tooltip graphics do not intercept pointer hits; card pointer handlers remain active when purchase buttons are disabled.

The starting gun bonus is gated by the default authored stats and projectile strategy; replacement weapon stats do not receive that bonus, and every update computes from base stats rather than accumulating it. Runtime application is reviewed and compiled, not exercised in Unity.

## Warnings and limits

Baseline compiler warning count was 163; final count was 166. The added feature warning is the expected Inspector-assigned optional StartMenuManager.upgradeCatalog field. The other two added warnings refer to unrelated workbench heading fields changed during the session. No final compiler errors occurred. See `new-warnings.txt`.

The first two attempted baseline builds overlapped in-progress feature edits and reported missing new-type symbols; those attempts are retained in `baseline-build.log` and were superseded by the reconstructed baseline. They are not evidence of baseline regressions.

The source compile validates Assembly-CSharp against existing package/plugin DLLs. It does not rebuild package assemblies, run Unity source generators, import assets, compile shaders, or produce a player build. The behavior harness does not prove native Unity JsonUtility/PlayerPrefs, MonoBehaviour lifecycle, scene loading, pointer interaction, or visual layout.

Remaining acceptance check: in Unity, Start → upgrades; hover locked and available cards; scroll; purchase and reload; verify starting fuel/default-gun/dash values and item acquisition gates; then use the final game-start button and inspect Console output.
