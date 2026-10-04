# 2026-10-04 HUD layout

Scope: Junmo gameplay HUD layout only.

- Moved SpeedMeter from bottom center to bottom left.
- Initially centered the existing inventory groups, then replaced their visible presentation with the user-approved connected train layout. The original grids remain disabled in Junmo to preserve references.
- Added enlarged, connected tail/middle/head backgrounds using cropped RawImages of the existing train textures; no new image assets or packages.
- Added nine attachment slots (three per train section) and one shared wheel slot, all 36 x 36 Canvas units. Reused InventoryCell prefabs to retain icons, upgrade markers, cooldown fill, and hover tooltips. No text is placed inside slots.
- Extended InventoryUI with optional serialized train layout bindings. TrainF items display in the head, TrainR in the tail, and TrainM/GunPivot/root items in the middle. BlueGear and RedGear are explicitly bound to the wheel group. Within each group, items occupy slots in acquisition order because detailed top/front/center attachment metadata does not yet exist.
- Items exceeding a group's display capacity are counted in an external +N indicator; inventory acquisition and equipped effects are unchanged. The current economy still permits more than these ten displayed slots.
- Other scenes default to the existing sequential inventory UI through useTrainLayout=false.
- Added CurrencyHUD at bottom right with FleshText (살점 0) and SoulText (영혼 0), using the existing Korean TMP font. These are display placeholders; no currency economy or acquisition logic was added.
- InventoryUI presentation changed; item acquisition/equipment/gameplay scripts and other scenes are unchanged.

Validation:

- Generated Assembly-CSharp project compiled with dotnet build --no-restore: zero errors, the same four pre-existing warnings as the baseline.
- Nine isolated C# presenter behavior checks passed against the actual InventoryUI source with minimal Unity/UI doubles: section mapping, wheel overrides, empty slots, overflow without deletion, refresh, destruction cleanup, legacy fallback, missing data, and missing slot references. This is not Unity Play Mode validation.
- Unique IDs, preservation of original scene objects, 3+3+3+1 bindings, prefab override targets, equal slot dimensions, absence of slot text, disabled legacy grids, passive background raycasts, local references, and HUD separation at the 800px reference width passed static checks.
- git diff --check passed.

Screenshot-driven visibility correction:

- Muted the train background to slate gray and added a dark, non-raycast backdrop behind each existing 36px slot, separating the white frame from train linework.
- Lowered and compressed TrainAttachmentLayout. All ten slots now fit in the bottom 96 Canvas units (previous highest slot reached 154 units).
- Moved the shared wheel cell into the lower gap next to the front carriage's wheels. Removed the visible long wheel bracket and unused stems instead of reserving an extra row below the train.
- Statically checked that slot rectangles do not overlap, remain within the bottom band, and preserve their prefab/presenter references. Actual Unity Game view confirmation remains pending.

Follow-up: moved the wheel slot and its dark backdrop to the right of the train (layout-local x=204, y=-6), between the train and currency UI. Kept the 36px size and wheel binding. At the 800px reference width, the train ends at x=578, the wheel occupies x=586..622, and currency text starts at x=628.

Prefab consolidation: moved the dark Backdrop into InventoryCell.prefab as its first child, before ItemIcon/Level/CoolDownFill. It stretches with a 2px inset and has raycastTarget=false. Removed all ten separate scene backdrop objects and references. The prefab GUID and existing component/child IDs remain intact; slot position overrides and presenter bindings are unchanged. Other InventoryCell instances inherit this shared background. Static hierarchy/reference checks and git diff --check passed; Unity import/visual verification remains pending.

Unity Editor live connection was unavailable. Scene import, Game view appearance, and Play Mode behavior remain unverified. The temporary Pipeline package addition used while checking connectivity was removed.

Doc Impact Check: updated the inventory StructureMemory for the new optional serialized presentation contract, plus this session log.
