# Language option — 2026-09-11

## Scope
Implement the approved Language row using the existing option styling, English/Korean switching, immediate option text refresh and saved preferences.

## Changes
- Added a resolution-style Language row in Start and Junmo and the reusable OptionUI prefab.
- Added EnglishLocalization language selection, PlayerPrefs persistence and change notification; English remains the first-run default.
- Active fixed labels and option values update immediately; subsequent item, event and result text uses the selected language, including embedded reward names.
- Added Language to CSV/workbook and Korean names for option labels. Removed fixed-label bindings from dynamic screen/resolution values so opening the panel cannot overwrite current values.

## Verification
- Unity 6000.2.3f1 compilation and final batch exited 0. EVENT_TEXT_FORMAT_TESTS_PASSED, LOCALIZATION_DATA_TESTS_PASSED, LOCALIZATION_SCENE_TESTS_PASSED (23 labels), LANGUAGE_OPTION_TESTS_PASSED (both scenes).
- Rendered the actual title/game option UI in English and Korean at 1280×960; reviewed row spacing, matching arrows/fonts, Korean glyphs and visible controls. Outputs: outputs/language-options/. These are editor renders, not a full interactive Play Mode session.
- Workbook reopen matched all 239 catalog rows. Serialized UI IDs are unique, each surface has one Language row, and git diff --check passed.
- Tests preserve and restore the user’s saved language. Gameplay balance and event data were not changed.

## Doc Impact Check
Updated Localization structure memory and this session log.
