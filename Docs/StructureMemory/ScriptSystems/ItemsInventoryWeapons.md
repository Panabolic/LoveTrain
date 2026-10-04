---
status: active
authority: structure-memory
category: items-inventory-weapons
last_reviewed: 2026-05-18
---

# Items Inventory And Weapons

## Purpose

Map item data, inventory state, equip/upgrade hooks, cooldown execution, weapon strategy switching, and level-up choices.

## Current Structure

- `Item_SO` is the base item data and hook surface for equip, take-damage, deal-damage, kill, cooldown, upgrade, and formatted description behavior.
- `ItemInstance` stores runtime item state: current upgrade level, public cooldown mirrors, and instantiated item object.
- `ItemCooldownState` owns the item cooldown countdown, manual-cooldown wait, and cooldown restart state.
- `ItemRuntimeGameStatePolicy` owns the shared `GameState` gate for item ticks and instantiated item update loops.
- `ItemVisualUpgradeApplier` owns the generic Animator/SpriteRenderer fallback upgrade application for instantiated item visuals.
- `Inventory` owns the player's list of `ItemInstance`s and exposes item acquisition, upgrade, hit, and kill hooks.
- `InventoryItemQuery` owns inventory item lookup, max-level checks, and upgradable item list queries.
- `InventoryItemRuntimeDispatcher` owns per-frame item tick dispatch plus kill and hit hook dispatch loops.
- `InventoryItemAcquirer` owns inventory item acquisition and upgrade execution against the runtime item list.
- Instantiated item behaviours implement `IInstantiatedItem` to receive upgrade synchronization.
- `ItemDatabase` is the item list used by level-up and event reward logic.
- `LevelUpUIManager` filters available items and opens three choice slots through the `GameManager` UI queue.
- `LevelUpChoiceSelector` owns level-up item availability filtering and random choice selection.
- `LevelUpChoiceDisplayState` owns new-vs-upgrade display-state calculation for level-up choice slots.
- `Gun` owns base/current gun stats, fire input, holder/muzzle visual swapping, and `IWeaponStrategy` execution.
- `ProjectileStrategy` and `LaserSpriteStrategy` are strategy implementations for weapon processing.

## Key Files

- `Assets/Scripts/LeeJunmo/Item_SO.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemInstance.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemCooldownState.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemRuntimeGameStatePolicy.cs`
- `Assets/Scripts/LeeJunmo/Items/ItemVisualUpgradeApplier.cs`
- `Assets/Scripts/LeeJunmo/Inventory/Inventory.cs`
- `Assets/Scripts/LeeJunmo/Inventory/InventoryItemQuery.cs`
- `Assets/Scripts/LeeJunmo/Inventory/InventoryItemRuntimeDispatcher.cs`
- `Assets/Scripts/LeeJunmo/Inventory/InventoryItemAcquirer.cs`
- `Assets/Scripts/LeeJunmo/Items/IInstantiatedItem.cs`
- `Assets/Scripts/LeeJunmo/Items/IWeaponStrategy.cs`
- `Assets/Scripts/LeeJunmo/Gun.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpManager.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpChoiceSelector.cs`
- `Assets/Scripts/LeeJunmo/LevelUp/LevelUpChoiceDisplayState.cs`

## Ownership And Lifecycle

- Item acquisition starts through `Inventory.AcquireItem(...)`.
- `Inventory.AcquireItem(...)` and `Inventory.UpgradeItemInstance(...)` remain the public entry points and delegate execution to `InventoryItemAcquirer`.
- Inventory query callers continue to use `Inventory.FindItem(...)`, `Inventory.GetUpgradableItems(...)`, and `Inventory.IsItemMaxed(...)`; those methods delegate query-only work to `InventoryItemQuery`.
- Inventory runtime callers continue to use `Inventory.ProcessKillEvent(...)` and `Inventory.ProcessHitEvent(...)`; `Inventory.Update()` and those public methods delegate item loops to `InventoryItemRuntimeDispatcher`.
- New items create an `ItemInstance`, call `HandleEquip(...)`, and let `Item_SO.OnEquip(...)` instantiate visual or logic prefabs.
- Existing items call `ItemInstance.UpgradeLevel()`, which delegates to `Item_SO.UpgradeLevel(...)`.
- Instantiated item upgrade sync first tries `IInstantiatedItem`; when that interface is absent, generic Animator/SpriteRenderer replacement is handled by `ItemVisualUpgradeApplier`.
- Runtime item cooldowns tick from `Inventory.Update()` through each `ItemInstance`; countdown state is delegated to `ItemCooldownState`.
- Item runtime state gating is delegated to `ItemRuntimeGameStatePolicy`, currently allowing `Playing`, `Boss`, and `Ending`.
- Pause-sensitive item and weapon early returns use `GameSimulationController.IsPaused` instead of raw `Time.timeScale` comparisons.
- Manual-cooldown items set `Item_SO.IsManualCooldown`, wait with a `float.MaxValue` sentinel, and restart cooldown through `ItemInstance.StartCooldownManual(...)`.
- Gun firing is separate from item cooldowns; `Gun.Update()` delegates to the current `IWeaponStrategy`.
- Level-up UI opens through `LevelUpUIManager.ShowLevelUpChoices()`, which delegates offer filtering and random selection to `LevelUpChoiceSelector` before binding slots.
- `LevelUpChoiceUI.DisplayChoice(...)` still binds serialized UI elements directly, but delegates new/upgrade state calculation to `LevelUpChoiceDisplayState`.

## English text lookup

See [Localization](./Localization.md) for the keyed CSV workflow. Item_SO resolves localized names and descriptions; GetFormattedDescription replaces existing stat variables after translation. Level-up choices and inventory tooltips use these accessors.

## Extension Entry Points

### Train attachment HUD (2026-10-04)

- Junmo uses `InventoryUI.useTrainLayout` with serialized `headSlots`, `middleSlots`, `tailSlots` (three each), and `wheelSlots` (one). Other scenes retain the legacy layoutGroup1/layoutGroup2 presentation by default.
- The HUD reuses InventoryCell prefabs; InventorySlotUI continues to own icon, level marker, cooldown, and tooltip presentation.
- InventoryCell owns its dark Backdrop child (first sibling, stretch anchors, 2px inset, no raycast). Scene instances no longer need separate background objects. Existing child names remain intact for InventorySlotUI's named image lookup.
- `InventoryUI` projects items by `Item_SO.attachmentSocketName`: TrainF to head, TrainR to tail, and other/root attachments to middle. Explicit `wheelItems` references override socket grouping; Junmo currently assigns BlueGear and RedGear.
- Within each section, display follows inventory order. Precise top/front/center placement and multi-slot occupancy require a future equipment-position contract; these UI locations do not change the item's actual gameplay socket.
- `hiddenItemCountText` shows +N for equipped items beyond display capacity. This is a UI count only; item acquisition and effects remain owned by Inventory.
- TrainAttachmentLayout uses the existing train textures as cropped RawImages behind nine equally sized attachment cells and a shared wheel cell. Legacy grids remain disabled in Junmo rather than being deleted.
- Generated C# project compilation and isolated presenter checks passed; Unity scene import and Play Mode visuals remain unverified.

- Add a new passive or active item by deriving from `Item_SO`, adding a `CreateAssetMenu`, and implementing only the needed hooks.
- Add or change item-active game states in `ItemRuntimeGameStatePolicy`, then review cooldowns and instantiated item update loops.
- Add an instantiated item prefab by assigning `Item_SO.instantiatedPrefab` and implementing `IInstantiatedItem` on its behaviour when upgrade sync is needed.
- Add a new gun strategy through `IWeaponStrategy` and switch it through `Gun.SetWeapon(...)`.
- Add new level-up display data through `Item_SO` icon, sprites/controllers by level, descriptions, and `GetStatReplacements(...)`.

## Known Pitfalls

- `Item_SO` fields are public/serialized and likely referenced by assets. Renames or type changes require asset migration review.
- `ItemInstance` stores only runtime state and does not persist across scenes by itself.
- `ItemInstance.currentCooldown` and `ItemInstance.maxCooldown` remain public compatibility mirrors; keep them synchronized if cooldown logic changes.
- `ItemRuntimeGameStatePolicy` currently allows item runtime behavior in `Playing`, `Boss`, and `Ending`; changing it affects cooldowns and autonomous item weapons.
- `ItemVisualUpgradeApplier` uses `Item_SO.controllersByLevel` before `Item_SO.spritesByLevel`, matching the previous fallback order.
- `InventoryItemQuery` preserves reference equality checks for matching `Item_SO` assets.
- `InventoryItemRuntimeDispatcher` preserves the existing dispatch order by iterating `Inventory.items` in list order.
- `InventoryItemAcquirer.AcquireOrUpgrade(...)` preserves the existing behavior where existing items upgrade through `ItemInstance.UpgradeLevel()`, while new items create an `ItemInstance`, add it to the list, then call `HandleEquip(...)`.
- `Inventory.Update()` ticks every item every frame; long-running item logic should stay cheap.
- Several item SOs index arrays by `currentUpgrade - 1`; new item data must keep array lengths aligned with `MaxUpgrade`.
- Some item and weapon scripts depend on `GameManager.Instance.CurrentState`; new states may pause item behavior unexpectedly.
- Some item and weapon scripts also depend on `GameSimulationController.IsPaused`; changing simulation pause behavior affects projectile, beam, launcher, and rear-gun update gates.
- `LevelUpChoiceSelector` uses the current `System.Random` shuffle behavior; changing randomness should be treated as a gameplay/balance change.
- `LevelUpChoiceDisplayState` preserves the existing rule where `nextLevel >= MaxUpgrade` shows the MAX sprite.

## Promotion Candidate

This map can become a future item/weapon contract after content authoring rules and serialized item schemas stabilize.


## 2026-10-04 Gameplay update (current source)

Gun은 전투 중 자동 발사하고 조준은 기존 입력을 유지한다. Inventory.CanAcquireItem은 중복 및 머리/가운데/꼬리 3칸, 공용 바퀴 1칸을 제한한다. AcquireItem 중복 업그레이드는 제거하고 전용 UpgradeItemInstance를 유지한다. TrainLevelManager는 살점/창조 비용을 제공하고 LevelUpUIManager.ShowCreation은 F 확정, R 유료 재추첨, Esc 취소 및 취소 후 제안 유지를 담당한다. Junmo FleshText는 명시적으로 연결된다.

검증/기본 수치/범위: `Docs/SessionLogs/2026-10-04-combat-fuel-creation.md`.
