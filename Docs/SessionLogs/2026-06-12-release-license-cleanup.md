---
status: complete
authority: session-log
category: release-docs
task_id: release-license-cleanup
last_reviewed: 2026-06-12
---

# 2026-06-12 Release License Cleanup

## Summary

- Added release-facing third-party notices and credits documents.
- Added an internal license inventory under `Docs/Legal`.
- Updated TMP settings so DungGeunMo is the default font, the default sprite
  asset is cleared, and emoji support is disabled.
- Removed the verified-unused `Assets/Sprites/Particle_FX_1.3 (2)` asset pack
  after an external GUID reference scan found zero references.

## Verification Notes

- Audio files under `Assets/Resources/Sounds/**` were intentionally not
  modified. Their source/license evidence remains planner-owned.
- `Particle_FX_1.3 (2)` had 149 internal meta GUIDs and 0 external references
  before deletion.
- TMP sample files for LiberationSans and EmojiOne remain in the project, so
  they remain conservatively listed in `THIRD_PARTY_NOTICES.txt`.

## Follow-Up

- Confirm DOTween Pro seat/license coverage for every contributor working in
  the Unity project.
- Attach planner-provided audio license evidence before release sign-off.
- Confirm final Steamworks SDK redistributable packaging against Valve
  Steamworks partner terms.
