---
status: active
authority: structure-memory
category: localization
last_reviewed: 2026-09-11
---

# English Localization

LoveTrain now resolves player-facing event, item, result and fixed UI strings by stable keys. The option panel selects English or Korean; new installations default to English. Existing Korean fields remain serialized as the source and emergency fallback. No Unity Localization package or runtime XLSX dependency was added.

## Editing translations

- Initial Excel workbook: `outputs/english-localization/LoveTrain-English.xlsx`, sheet `Translations`.
- Runtime catalog: `Assets/Resources/Localization/Strings.csv` (UTF-8 with BOM).
- Columns: `Key`, `ko`, `en`, `Context`. Edit `en`; keep keys and placeholders intact. The workbook is an editing copy, not an automatically watched runtime input.
- Save the Translations sheet from Excel as **CSV UTF-8 (comma delimited)**, then use **Tools > LoveTrain > Localization > Import CSV**. This validates before replacing the runtime catalog. Restart Play Mode to refresh all text.
- **Export CSV** merges current asset source text with existing translations. It assigns keys to new non-empty fields and saves those key assignments. Existing keys and English are preserved when source text is unchanged. Changed source text clears the corresponding English in the exported file and emits a warning, so it can be translated again. Import the completed export to apply it.
- Do not replace the runtime file with an old workbook after a later export; use the latest exported CSV as the next editing source.
- **Validate CSV** checks duplicate/blank keys, malformed quoting, column counts, placeholder agreement and referenced keys. The same validation runs before a player build. Blank English is allowed and falls back to the table's Korean source.
- **Run Data Tests** checks CSV edge cases, numeric formatting, key coverage, and item description variables at every authored upgrade level. Batch verification additionally opens both build scenes and checks fixed labels without saving scenes.

## Runtime ownership

- `LocalizationCsv` parses and writes quoted CSV, including embedded commas, doubled quotes, multiline cells, BOM and reordered headers. It is public because both the runtime and editor assembly share this API.
- `EnglishLocalization` loads Resources/Localization/Strings once per session. With English selected, lookup order is non-blank English, Korean table value, authored fallback. With Korean selected, it uses the Korean table value then authored fallback. A missing key returns the authored fallback. A malformed catalog logs an error and falls back to authored text. Subsystem registration clears the cache, including when domain reload is disabled.
- `SO_Event.titleKey/textKey` and each choice's `selectionTextKey/selectionUnderTextKey` are resolved by EventManager before typing or choice display. Event timing, selection outcomes and UI queue behavior are unchanged.
- `Item_SO.itemNameKey/itemDescriptionKey/itemSimpleDescriptionKey` feed LocalizedName, LocalizedSimpleDescription and GetFormattedDescription. Existing named stat tokens such as {Damage} are replaced after localization.
- `EventResultOutput.specialTextKey` resolves authored outcome messages. Effect scripts use shared `result.*` templates and localized item names; numbered tokens {0}, {1}, etc. are formatted with invariant culture.
- `LocalizedText` is attached only to fixed TMP/legacy Text labels and applies its key on enable. Dynamic item/event text remains owned by its presenter. Screen-mode values are resolved by Option.
- Keys are serialized identifiers, not a hash of the sentence or a runtime list index. Existing choice_1/choice_2 suffixes are initial names only; changing order must retain the stored key. Event Maker copies choice/outcome keys through its temporary data and retains the selected main event asset when its title changes. Newly created non-empty fields receive unique keys.

## Event templates

- Choice button/hint and authored outcome text: `{reward.count}`, `{reward.chance}`, `{reward.item}`, `{upgrade.levels}`, `{speed_loss.value}`.
- Event title/body adds the choice ID: `{choice_1.reward.count}`.
- `Selection.textId` and `WeightedEventOutcome.textId` are stable identifiers. Event Maker preserves them when loading/saving; Export CSV assigns missing IDs. Keep existing IDs when reordering. The editor shows IDs and syntax help.
- `EventTextFormatter` resolves templates after localization, before display. It never rolls, executes effects or changes gameplay state. Values are read each time text is formatted; an already visible popup is not continuously refreshed.
- `chance` is this outcome's weight divided by the total in its own independent roll group, as a percentage with up to two decimals. Zero-total groups return 0; invalid weights are rejected.
- `count` / `levels` follow the effect's minimum-one defaults. Templates retain the original authored wording and punctuation; counts describe configured rewards. Inventory availability and maximum upgrade level can still cap the actual result. `item` / `itemSummary` read the referenced Item_SO in the template's language.
- `value` is signed speed/HP-modifier change; `amount` is absolute speed change or the HP modifier value. `delay` and `interval` follow spawn-effect defaults. Raw `intValue`, `intValue2`, `floatValue`, `floatValue2` are also available. Unsupported fields for an effect are rejected. Double braces escape literal braces.
- Change balance values in GameEventSO, not Excel. Excel stores the sentence and tokens only. Keep token sets identical to the authored template in both ko/en. Changes to template structure require Export CSV before translating again.
- Import/build validation checks IDs, token semantics and agreement with the asset source, so an old workbook cannot silently replace variable descriptions with fixed values. Runtime invalid tokens show ? and log a warning once per error.
- Data tests cover changed parameters, independent probabilities, reordering, default values, item names, invalid tokens, stale CSV rejection and all actual event templates.

## Scope and limitations

- Catalog: 239 keys, including 126 non-empty event fields, 39 item fields, 19 non-empty authored outcome fields, 26 effect-message templates, 23 UI/setting keys and 6 ending-credit entries. There are 29 fixed-label component bindings across the two build scenes and OptionUI prefab. Legacy/test event text is included; the empty test event is unchanged.
- Event descriptions now bind to actual effect data instead of copying authored numbers. Steel Savior shows the configured 3 random rewards; Paranoia shows the actual 40%/60% roll weights. Gameplay values, effect references and roll behavior remain unchanged. Legacy tag-reward hints and narrative/test text retain their original prose; those legacy tag labels do not represent a newly implemented tag-based reward system.
- Ending credit roles are translated through keyed credit entries; personal names retain their original spelling. The legacy developerNames list remains a fallback for unmigrated scenes. Legal credit content and image-embedded text are not rewritten by the string catalog. Existing English button art remains in use.
- English wording is a first translation pass. Language selection is available in title/game options; fixed active labels and option values refresh immediately. New event/item/result displays resolve the selected language. Options cannot be opened during event/level-up popups, and previously assembled result text is not retranslated.
- CSV validation does not prove the absence of font or layout issues in gameplay. Test long event text, choices, item tooltips and ending UI in Play Mode at supported resolutions.

## Language option

- Option exposes previous/next Language buttons beside a native-name value (English / 한국어), using the existing resolution-row styling in Start, Junmo and OptionUI prefab.
- EnglishLocalization.SetLanguage stores en/ko in PlayerPrefs under LoveTrain.Language, saves immediately and raises LanguageChanged on changes. Missing/unrecognized preferences default to English. Reloading the CSV preserves the selected language.
- LocalizedText subscribes while enabled and unsubscribes on disable. Option updates the selected language and screen-mode value on changes. Dynamic value labels have no LocalizedText component to overwrite presenter-owned text on enable.
- EnglishLocalization.HasEnglish routing in EventTextFormatter follows the selected language, so item names inside Korean event templates also remain Korean.
- LanguageOptionVerification.RunBatch checks both scene buttons, default/wrap behavior, saved selection, immediate labels, item/event templates and generates actual panel render previews without saving scenes or changing the user’s saved language.

## Original wording preservation

The 2026-09-11 follow-up restores pre-template Korean and English wording exactly outside the substituted values. Existing brackets, quotes, spaces, punctuation and line breaks are retained. Only original numeric values and specific reward names become data tokens. Extra maximum-count wording, newly listed failure outcomes and added monster counts were removed. Narrative dialogue and test/legacy prose are restored without invented data bindings. Runtime IDs, formatter validation and language selection remain intact.
