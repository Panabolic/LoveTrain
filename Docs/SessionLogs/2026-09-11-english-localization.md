# English localization first pass — 2026-09-11

## User scope

Implement English first using stable translation keys and an Excel/CSV editing workflow. Preserve Korean source and gameplay behavior.

## Changes

- Created a 238-row Key/ko/en/Context catalog and the editable Translations workbook at outputs/english-localization/LoveTrain-English.xlsx.
- Added a runtime CSV parser, English lookup with Korean/source fallback, numeric message formatting, and fixed-label binding. No new package dependency.
- Connected 126 event fields, 39 item fields, 19 authored result fields, 26 shared effect templates, 22 UI/setting strings and 6 ending-credit entries. Personal names retain original spelling.
- Added 30 fixed-label components across Start, Junmo and OptionUI, plus keyed ending-credit data. Original serialized source text is retained.
- Preserved Event Maker choice/outcome keys and source logic references across edits, renames, additions and reordering. Newly added choices get unique logic asset paths and new stable keys.
- Added editor CSV import/export/validation menus and build-time validation. Import validates before atomically replacing the runtime catalog. Export retains English for unchanged Korean source and clears stale English when source changes.

## Verification

- Unity 6000.2.3f1 batch compilation and LocalizationVerification.RunBatch completed with exit code 0. Final log markers: LOCALIZATION_DATA_TESTS_PASSED; LOCALIZATION_SCENE_TESTS_PASSED labels=25. The two build scenes contain 25 bindings; OptionUI contains another 5.
- CSV tests cover UTF-8/BOM, quoted commas, doubled quotes, multiline text, reordered headers, missing final newline, empty English, duplicate/blank keys, placeholder mismatches, bad quoting and wrong column counts.
- Runtime checks cover missing-key and blank-English fallback, invariant numeric formatting, and resolved item variables at every authored upgrade level.
- New-event key generation and key stability after reordering were verified. Existing source fields and gameplay asset data were checked against the pre-change files: all 44 changed data assets differ only by added key fields.
- All 238 keys are unique and English cells are non-empty. ko/en placeholder sets match. The only Korean text in en is three personal-name entries.
- XLSX was exported, reopened and compared cell by cell; final data matched all 238 rows. Event/item/UI preview renders were visually checked.
- git diff --check passed. No package manifest, project version, gameplay numbers, probability values or original Korean source fields were changed.

## Limits

The verification was a Unity editor batch/data/scene check, not interactive Play Mode or a player build. Long text layout, typing presentation, tooltip overflow and actual screen rendering still need a visual playthrough. English is the sole target language; live language selection is not part of this pass.

## Doc Impact Check

Category: StructureMemory + SessionLog. Added Localization.md; updated event/item maps and Docs/README routing.
