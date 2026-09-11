# Event text templates — 2026-09-11

## Request and scope
Convert event description values, probabilities and rewards into data-bound format templates within the existing English-first Excel/CSV workflow. Do not rebalance gameplay.

## Changes
- Added EventTextFormatter for event title/body, choice text/hints and authored outcome text.
- Added stable choice/outcome text IDs; Event Maker preserves IDs and shows syntax help.
- Migrated 38 effect-backed choice hints plus two reward-count dialogue choices and their two event bodies. Updated Korean source, English catalog and workbook together.
- Fixed display/data discrepancies through bindings: Steel Savior reward 2 → configured 3; Paranoia failure 70% → actual 60%. Legacy reward labels now follow actual Item_SO references.
- Import/export/build validate missing or duplicate IDs, bad tokens and stale translation placeholders.

## Verification
- Unity 6000.2.3f1 batch compile and verification exited 0: EVENT_TEXT_FORMAT_TESTS_PASSED, LOCALIZATION_DATA_TESTS_PASSED, LOCALIZATION_SCENE_TESTS_PASSED (25 fixed labels).
- Excel export/reopen matched all 238 catalog rows; 41 event rows contain dynamic templates. Event-sheet rendering reviewed for wrapping.
- Compared affected event assets against the pre-task snapshot excluding the intended text/ID fields: no balance values, effect references or other data changed.
- git diff --check passed; new scripts have Unity .meta files.
- Interactive Play Mode popup layout has not been visually verified. Existing compiler warnings remain unrelated to this change.

## Doc Impact Check
StructureMemory: updated Localization.md for reusable formatting ownership, authoring and validation contracts. This session log records migration scope.
