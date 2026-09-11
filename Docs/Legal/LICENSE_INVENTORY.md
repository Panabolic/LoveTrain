---
status: final-draft
authority: legal-inventory
category: release-docs
last_reviewed: 2026-06-13
---

# LoveTrain License Inventory

This is LoveTrain's internal checklist for credits and third-party license
evidence. Public-facing notices are controlled by `THIRD_PARTY_NOTICES.txt`;
human-readable credits are controlled by `CREDITS.txt` / `CREDITS.md`.

For this draft, the planner-provided `CREDITS.txt` and
`THIRD_PARTY_NOTICES.txt` are treated as the source of truth. This inventory
tracks the evidence and remaining release actions behind those public files.

## Public Release Notice Items

| Item | Evidence | Status | Public treatment | Required action |
| --- | --- | --- | --- | --- |
| Super Pixel Impact FX Pack 2 | itch.io page; `Assets/Sprites/Super Pixel Impact FX Pack 2/license.txt`; license URL retained | Used | Notice + credits | Keep attribution to Will Tice / unTied Games |
| DOTween Pro | `Assets/Plugins/Demigiant/readme_DOTweenPro.txt`; official license URL | Used | Notice | Confirm every project contributor has a valid DOTween Pro license |
| Steamworks.NET | `Assets/com.rlabrecque.steamworks.net/README.md`; source headers | Used / integration in progress | Notice | Include MIT notice |
| Valve Steamworks SDK runtime | `Assets/com.rlabrecque.steamworks.net/Plugins/steam_api64.dll` | Used / integration in progress | Inventory + conservative notice | Confirm final redistributable packaging against Valve partner terms |
| DungGeunMo / DungGeunMo Fixedsys | `Assets/TextMesh Pro/Fonts/DungGeunMo.ttf`; Cactus / Noonnu source pages | Used | Notice + optional credits | Keep as TMP default font |
| LiberationSans | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`; TMP sample files remain | Present, not intended default | Notice while shipped | Remove from notices only after sample files are removed from release |
| EmojiOne TMP sample | `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`; sample files remain | Present, default disabled | Notice while shipped | Remove from notices only after sample files are removed from release |

## Audio / Music Items

| Item | Evidence | Status | Public treatment | Required action |
| --- | --- | --- | --- | --- |
| 285 Game Sound Effects Vol.1 | itch.io purchase; asset page states CC BY 4.0 and credit example | Used / planner-confirmed | Notice + credits | Include CC BY 4.0 attribution to Filippo Vicarelli / Filippo Game Audio |
| 900 Retro Sound Effects! | itch.io purchase; creator page by brockjroderick; comment confirms royalty-free status | Used / planner-confirmed | Notice + voluntary credits | Keep purchase record and page/comment screenshot |
| 8Bit Arcade Game Sound Pack / 8bitgamesoundpack | itch.io purchase; creator page by SwissDude; creator reply permits commercial game use and modification; planner confirms mapped files | Used / planner-confirmed | Notice + voluntary credits | Keep purchase record, creator permission reply, and mapped-file evidence |
| 8-Bit Sound Effects [100+ SFX] | itch.io purchase; asset page by Beep Yeah! states personal/commercial royalty-free use | Used / planner-confirmed | Notice + credits | Keep purchase record and asset page screenshot |
| Retro-stylized Sound Library | `https://heltonyan.itch.io/pixelcombat`; itch.io purchase/license evidence; CC BY 4.0 attribution required | Used / planner-confirmed | Notice + credits | Include CC BY 4.0 attribution to Helton Yan |
| Suno AI BGM: "Slowing mechanical sound" | Generated 2025-12-14; Suno Pro receipt retained from 2025-12-04 | Used / planner-confirmed | Notice + AI disclosure | Keep generation record and paid subscription receipt |
| Suno AI BGM: "Very slow tempo" | Generated 2025-12-14; Suno Pro receipt retained from 2025-12-04 | Used / planner-confirmed | Notice + AI disclosure | Keep generation record and paid subscription receipt |
| Suno AI BGM: "Heavy mechanical drum beat" | Generated 2025-12-14; Suno Pro receipt retained from 2025-12-04 | Used / planner-confirmed | Notice + AI disclosure | Keep generation record and paid subscription receipt |

## Resolved Metadata Issue

Several audio files contain embedded metadata identifying `8Bit-Pack` /
`Marco Zedler`, which does not match the public author name used in the
release credits. Following the planner-provided `THIRD_PARTY_NOTICES.txt`,
these files are treated as part of the purchased SwissDude
`8Bit Arcade Game Sound Pack` / `8bitgamesoundpack`, not as separate
third-party assets and not as Beep Yeah! assets.

Mapped project files:

- `Assets/Resources/Sounds/ETC/촉수 소환.wav`
- `Assets/Resources/Sounds/Items/레이저 발사.wav`
- `Assets/Resources/Sounds/Items/회복.wav`
- `Assets/Resources/Sounds/Items/BeatingHeart.wav`
- `Assets/Resources/Sounds/UI/보스 경고.wav`

Known original-file evidence supplied by the planner:

- `Assets/Resources/Sounds/ETC/촉수 소환.wav` = `8Bit_NoisyExplosion_11`
- `Assets/Resources/Sounds/Items/레이저 발사.wav` = `8bit_Laser_04`
- `Assets/Resources/Sounds/Items/회복.wav` = `8bit_Coin_07_b`
- `Assets/Resources/Sounds/Items/BeatingHeart.wav` = `8Bit_BumpingMuffled`
- `Assets/Resources/Sounds/UI/보스 경고.wav` = original filename not yet retained

Required evidence to retain before release: the SwissDude
`8bitgamesoundpack` purchase/download record and a screenshot, file list, or
archive listing that ties these renamed Unity files to the purchased pack.

## Inventory-Only Items

| Item | Evidence | Status | Public treatment | Required action |
| --- | --- | --- | --- | --- |
| TextMesh Pro package / TMP settings | `Assets/TextMesh Pro/Resources/TMP Settings.asset` | Used | Inventory only | Track embedded sample assets separately |
| Unity packages | `Packages/manifest.json`; `Packages/packages-lock.json` | Used | Inventory only | Do not list every Unity package in public credits |
| DOTween Pro examples/editor assets | `Assets/Plugins/Demigiant/DOTweenPro Examples`; editor folders | Present | Inventory only | Optional cleanup before release |

## Excluded Or Removed

| Item | Evidence | Status | Public treatment | Required action |
| --- | --- | --- | --- | --- |
| Particle_FX_1.3 | Internal GUID scan found 149 GUIDs and 0 external references | Removed as unused | Exclude | Do not list in public notices unless reintroduced |

## TMP Decision

- Default TMP font asset is DungGeunMo SDF.
- Default TMP sprite asset is cleared.
- TMP emoji support is disabled.
- LiberationSans and EmojiOne sample files are not deleted in this task, so
  they remain conservatively listed in `THIRD_PARTY_NOTICES.txt`.

## Recommended Evidence Folder

Evidence to retain internally:

- `SuperPixelImpactFXPack2_purchase_or_download_record.png`
- `SuperPixelImpactFXPack2_license_capture.png`
- `DOTweenPro_license_or_purchase_evidence.png`
- `SteamworksSDK_partner_terms_reference.txt`
- `FilippoVicarelli_purchase_and_license_capture.png`
- `900Retro_purchase_and_comment_capture.png`
- `SwissDude_purchase_permission_and_mapped_file_evidence.png`
- `BeepYeah_purchase_and_license_capture.png`
- `HeltonYan_purchase_and_CC_BY_license_capture.png`
- `SunoPro_receipt_2025-12-04.png`
- `Suno_generation_records_2025-12-14.png`
- `DungGeunMo_license_capture.png`
