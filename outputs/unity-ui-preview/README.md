# Actual Unity workbench previews

Rendered 2026-10-06 in Unity6000.2.3f1 from an isolated copy of the current TrainItemWorkbench prefab and actual Junmo InventoryHUD. The four PNGs use original item sprites, fonts, slots and descriptions. Example inventory contains Bible/RearGun/PoisonMissile, with Revolver/BeatingHeart/BlueGear offered; workbench labels use100flesh/7souls. This is a UI-focused Editor render on a neutral backdrop, not an actual PlayMode gameplay capture or input-raycast validation.

The final `creation-layout-v2.png`, `creation-hover-layout-v2.png`, `creation-drag-layout-v2.png` and `upgrade-layout-v2.png` were visually checked. `WorkbenchRenderPreview-layout-v2.cs` is the isolated capture fixture, stored outside Assets and not part of the game. It uses the final lower train/upgrade positions. The reusable disposable project, earlier attempts and raw logs stay local; original Assets/Settings are not changed by rendering.
