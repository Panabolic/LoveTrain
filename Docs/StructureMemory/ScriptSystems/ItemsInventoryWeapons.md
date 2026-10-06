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

Gun은 전투 중 자동 발사하고 조준은 기존 입력을 유지한다. Inventory.CanAcquireItem은 중복 및 머리/가운데/꼬리 3칸, 공용 바퀴 1칸을 제한한다. AcquireItem 중복 업그레이드는 제거하고 전용 UpgradeItemInstance를 유지한다. TrainLevelManager는 살점/창조 비용을 제공한다. 현행 LevelUpUIManager.ShowCreation은 F로 작업대 진입, 드래그 장착으로 확정, 리롤 버튼, Esc 닫기 및 취소 후 제안 유지를 담당한다.

검증/기본 수치/범위: `Docs/SessionLogs/2026-10-04-combat-fuel-creation.md`.

## 2026-10-06 영구 강화 / 해금

PermanentUpgradeCatalog의 UnlockItem 항목은 특정 Item_SO 참조에 대응한다. 기존 LaserGun_SO는 기본 항목의 명시적 타입 매칭을 사용하며, 미지정 산탄총·기관총·보스 아이템은 구매가 잠겨 있다. Inventory.CanAcquireItem과 CanEquipAt에서 PermanentUpgradeProgress.IsItemUnlocked를 검사하므로 제작 제안과 이벤트 획득은 같은 해금 조건을 따른다.

Gun은 런 시작의 영구 데미지 보너스를 원본 GunStats와 기본 ProjectileStrategy에만 적용한다. 레이저로 교체하면 빠지고 기본총으로 복원하면 한 번 적용된다. ScriptableObject 원본 스탯은 변경하지 않는다. 공격력 단계별 증가량은 아직 0이며 카탈로그에서 확정할 수 있다. 카탈로그 ID는 저장 계약이므로 구매 후 변경하면 안 된다.


## 2026-10-06 Item workbench update

2026-10-06 제작/강화 작업대: TrainItemWorkbench.prefab + TrainItemWorkbenchView/WorkbenchChoiceUI, 기존LevelUpUIManager가 F제작3선택지 드래그 및 강화미리보기/제거를 조율. 기존기차HUD 재부모화/확대/복귀, Inventory 정확한10슬롯/허용마스크/장착앵커/제거정리. 강화costPerCell=-1 미정, 이벤트제거1회/강화회수제한없음(아이템MaxUpgrade유지). 상세및검증은 `Docs/SessionLogs/2026-10-06-item-workbench.md`.

## 2026-10-06 살점 HUD / 조직 창조

- Junmo `CurrencyHUD`는 씬에 작성된 `FleshHud`와 TMP/Image/키 안내 참조를 사용한다. 런타임 UI 자동 생성 없이 오른쪽 아래의 기존 우측·하단 여백을 유지하고, 영혼을 상단에 배치한다.
- 살점과 영혼의 라벨·숫자는 모두 같은 크기(12 logical px)와 오른쪽 정렬을 사용하며 두 줄의 오른쪽 경계를 맞춘다.
- 장비 HUD와의 겹침을 피하도록 최종 박스 150×92/게이지 130×11 logical px를 사용한다. 기존 우측 20/하단 17 여백을 유지하고 장비 슬롯 10개와의 실제 PlayMode 교차 0을 확인했다.
- 자원·제작 횟수·비용은 기존 `TrainLevelManager`/`RunPartEconomy`가 소유한다. `FleshCreationProgress`는 현재 살점과 증가하는 제작 비용의 누적 합만 계산하는 표시용 값이다. 경제·보상·F 입력·실제 장착 거래는 변경하지 않는다.
- `FleshHud`는 `OnResourcesChanged`와 `EnglishLocalization.LanguageChanged`를 OnEnable/OnDisable에서 균형 있게 구독·해제하고 Start에서 초기 영혼을 다시 표시한다. 기존 wallet의 fleshText/soulText 필드는 보존하되 Junmo에서는 참조를 비워 이 뷰가 동일한 TMP를 단독 갱신한다.
- 완성한 제작분의 색을 바탕으로, 다음 제작분의 색을 잔여 살점/다음 비용 비율만큼 겹친다. 빨주노초파남보·분홍·청록·백색을 순환하며 Sprite 없는 Simple Image의 가로 앵커를 변경한다. 비용 상한과 증가량 0에서도 계산은 유한하다.
- 게이지 바로 아래에 `F 조직 창조`와 현재 1회 비용을 표시한다. 자원 기준 2회 이상이면 `조직 창조 X N`으로 표시하며 별도 횟수·추가 획득 안내·분수는 없다.
- `CanRequestCreation`은 Playing/Boss, 양수 timeScale, 키보드, 큐·거래·열린 작업대 여부와 후보 가용성을 표시용으로 조회한다. 후보 조회는 `LevelUpUIManager.CanShowCreation`에 캐시하며 Inventory 변경/재활성화/Database 변경 시 다시 계산한다. 매 프레임에는 캐시와 상태만 조회한다.
- 승인 범위와 검증·남은 확인은 [살점 HUD 작업 기록](../../SessionLogs/2026-10-06-flesh-hud.md)을 참조한다.
