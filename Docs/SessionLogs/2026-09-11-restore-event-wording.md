# Restore original event wording — 2026-09-11

## Request
Restore the exact original text style while retaining data-driven format substitution.

## Changes
- Restored 42 changed Korean/English catalog rows and matching SO_Event source fields from the pre-format migration snapshot.
- Original numeric values and specific item names are replaced by existing formatter tokens, preserving brackets, spacing, punctuation and wording.
- Removed added maximum/upgrade language, explicit failure branches and newly introduced monster counts. Restored narrative, test and legacy tag descriptions where no original runtime value existed.
- Preserved actual effect values, probability weights, references, stable IDs and the Language option. Consequently data-bound values still reflect the actual reward/probability even when the old literal was incorrect.

## Verification
- Replacing tokens with their original literals reproduces all 126 original event rows in both languages exactly (252 comparisons).
- Validated 68 placeholder references against serialized outcome IDs and matched source/CSV token sets. Gameplay data outside text fields is unchanged.
- Workbook export/reopen matched all 239 rows; 29 event rows retain original-value placeholders. Render reviewed. Scoped git diff --check passed.
- Unity batch rerun was blocked because this project is open in another editor instance (HandleProjectAlreadyOpenInAnotherInstance). The existing editor was left running; no new runtime compile/test pass is claimed. Updated exact-output assertions for the restored Steel Savior and Paranoia prose.

## Doc Impact Check
Updated Localization structure memory and added this session log.
