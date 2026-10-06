# Actual Unity workbench previews

Rendered 2026-10-06 in Unity6000.2.3f1 from an isolated copy of the current TrainItemWorkbench prefab and actual Junmo InventoryHUD. The four PNGs use original item sprites, fonts, slots and descriptions. Example inventory contains Bible/RearGun/PoisonMissile, with Revolver/BeatingHeart/BlueGear offered; workbench labels use100flesh/7souls. This is a UI-focused Editor render on a neutral backdrop, not an actual PlayMode gameplay capture or input-raycast validation.

The final `creation-layout-v2.png`, `creation-hover-layout-v2.png`, `creation-drag-layout-v2.png` and `upgrade-layout-v2.png` were visually checked. `WorkbenchRenderPreview-layout-v2.cs` is the isolated capture fixture, stored outside Assets and not part of the game. It uses the final lower train/upgrade positions. The reusable disposable project, earlier attempts and raw logs stay local; original Assets/Settings are not changed by rendering.

## Upgrade/socket checks and final lower HUD alignment

`OctoberFixValidation.cs` and `october-fix-validation-result.txt` record 670 passing Editor-method checks for free upgrades at zero flesh, legacy authored-socket item visuals, and the intermediate 150×78 currency panel. That fixture's panel assertions describe its historical input scene, before the final lower-HUD alignment; they are not the current 150×84 layout.

`HudBottomCenterValidation.cs` and `hud-bottom-center-result.txt` record 413 passing checks for the final scene. Six RenderTexture sizes (1920×1080, 1280×720, 960×540, 800×600, 3440×1440, 640×360) use the production CanvasScaler and FixedAspectRatioController content-root methods. The checks cover lower-section centering, rail-alpha clearance, clipping, currency padding, optional +N glyphs, and exact creation-workbench close restoration. `hud-bottom-centered-1280x720.png` and `hud-bottom-centered-800x600.png` are final static world/UI renders using scene assets; they are not PlayMode captures.

The render fixtures use a temporary graphics path for capture. Actual window resizing, physical input/raycast, hit/camera shake and a player build remain unverified. Preserve the unique preview save identity before running either fixture in a disposable Unity6000.2.3f1 copy outside the open original project. The previously nested copy may be rejected as already open; the external Temp copy was used for these checks. Original Assets/Packages/ProjectSettings were not modified by the fixtures.

`source-snapshot-latest.json` records the final scene/source hashes and distinguishes the earlier stage/combo PlayMode scene from the later HUD layout checks. Raw logs, disposable projects and scene backups remain local.
