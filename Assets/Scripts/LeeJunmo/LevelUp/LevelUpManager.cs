using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LevelUpUIManager : MonoBehaviour
{
    public static LevelUpUIManager Instance;
    // Preserve the authored legacy references; the old choice cards remain inactive.
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private LevelUpChoiceUI choiceSlot1;
    [SerializeField] private LevelUpChoiceUI choiceSlot2;
    [SerializeField] private LevelUpChoiceUI choiceSlot3;
    [SerializeField] private RectTransform levelUpTitleText;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Inventory playerInventory;
    [Header("Train item workbench")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private TrainItemWorkbenchView workbenchPrefab;
    [SerializeField] private float trainPanelScale = 1.25f;
    [SerializeField] private float panelMoveDuration = 0.35f;
    [Tooltip("Test price: zero allows free upgrades; a negative value disables upgrading.")]
    [SerializeField] private int upgradeCostPerCell = 0;

    private enum Mode { None, Creation, Upgrade }
    private Mode mode;
    private TrainItemWorkbenchView view;
    private TrainLevelManager wallet;
    private Item_SO[] offers = new Item_SO[3];
    private int draggingChoice = -1;
    private int openingFrame;
    private ItemInstance selectedItem;
    private InventorySlotUI selectedSlot;
    private bool removalUsed;
    private bool transitionBusy;
    private bool closing;
    private Sequence transition;
    private RectTransform equipmentRect;
    private Transform savedParent;
    private int savedSibling;
    private Vector2 savedAnchorMin, savedAnchorMax, savedPivot, savedSize, savedPosition;
    private Vector3 savedScale;
    private bool savedLayout;
    private Inventory availabilityInventory;
    private ItemDatabase availabilityDatabase;
    private bool creationAvailabilityDirty = true;
    private bool hasCreationCandidate;
    public bool IsOpen => mode != Mode.None;
    public bool CanShowCreation
    {
        get
        {
            if (IsOpen || !isActiveAndEnabled || inventoryUI == null || inventoryUI.EquipmentRect == null ||
                workbenchPrefab == null || playerInventory == null || itemDatabase == null || itemDatabase.allItems == null) return false;
            if (availabilityDatabase != itemDatabase)
            {
                availabilityDatabase = itemDatabase;
                creationAvailabilityDirty = true;
            }
            if (creationAvailabilityDirty)
            {
                hasCreationCandidate = false;
                foreach (var item in itemDatabase.allItems)
                    if (playerInventory.CanAcquireItem(item)) { hasCreationCandidate = true; break; }
                creationAvailabilityDirty = false;
            }
            return hasCreationCandidate;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    private void Start()
    {
        if (inventoryUI != null) inventoryUI.ConfigureWorkbench(this);
        if (GameManager.Instance != null) GameManager.Instance.OnGameStateChanged += HandleGameState;
    }

    private void OnEnable()
    {
        availabilityInventory = playerInventory;
        if (availabilityInventory != null) availabilityInventory.OnInventoryChanged += InvalidateCreationAvailability;
        InvalidateCreationAvailability();
    }

    private void OnDisable()
    {
        if (availabilityInventory != null) availabilityInventory.OnInventoryChanged -= InvalidateCreationAvailability;
        availabilityInventory = null;
        AbortWorkbench();
    }

    private void InvalidateCreationAvailability() => creationAvailabilityDirty = true;
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStateChanged -= HandleGameState;
        if (Instance == this) Instance = null;
    }
    private void HandleGameState(GameState state)
    {
        if (state == GameState.Die || state == GameState.Ending) AbortWorkbench();
    }

    public void ShowCreation(TrainLevelManager source)
    {
        if (IsOpen) return;
        if (source == null || !source.CanPurchaseCreation || !EnsureView()) { ReleaseQueue(); return; }
        if (!OffersRemainValid()) RollOffers();
        if (offers[0] == null) { ReleaseQueue(); return; }
        Open(Mode.Creation, source);
        ShowOffers();
    }

    public void ShowUpgradeEvent()
    {
        if (IsOpen) return;
        TrainLevelManager source = playerInventory != null ? playerInventory.GetComponent<TrainLevelManager>() : null;
        if (source == null || !EnsureView()) { ReleaseQueue(); return; }
        removalUsed = false;
        Open(Mode.Upgrade, source);
        RefreshUpgrade();
    }

    public void ShowLevelUpChoices() => ShowCreation(playerInventory != null ? playerInventory.GetComponent<TrainLevelManager>() : null);
    public void OnChoiceSelected(Item_SO item) { } // Old cards no longer grant/equip items by click.

    private bool EnsureView()
    {
        if (view != null) return true;
        if (inventoryUI == null || inventoryUI.EquipmentRect == null || workbenchPrefab == null || playerInventory == null || itemDatabase == null)
        {
            Debug.LogError("[ItemWorkbench] Required prefab, inventory HUD or database reference is missing.", this);
            return false;
        }
        equipmentRect = inventoryUI.EquipmentRect;
        inventoryUI.ConfigureWorkbench(this);
        view = Instantiate(workbenchPrefab, inventoryUI.transform.parent);
        view.name = "TrainItemWorkbench";
        var rect = (RectTransform)view.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        view.Bind(this);
        view.gameObject.SetActive(false);
        return true;
    }

    private void Open(Mode nextMode, TrainLevelManager source)
    {
        mode = nextMode; closing = false; transitionBusy = true; openingFrame = Time.frameCount;
        wallet = source;
        wallet.OnResourcesChanged += RefreshResources;
        playerInventory.OnInventoryChanged += HandleInventoryChanged;
        view.gameObject.SetActive(true);
        view.transform.SetAsLastSibling();
        view.SetMode(mode == Mode.Creation);
        if (mode == Mode.Creation) { view.SetCreationControlsVisible(false); view.ChoicesGroup.alpha = 0f; }
        view.RootGroup.alpha = 0f;
        view.RootGroup.interactable = false;
        view.RootGroup.blocksRaycasts = true;
        SaveTrainLayout();
        Vector3 position = equipmentRect.position;
        equipmentRect.SetParent(view.TrainHost, true);
        equipmentRect.anchorMin = equipmentRect.anchorMax = new Vector2(0.5f, 0.5f);
        equipmentRect.position = position;
        transition?.Kill();
        transition = DOTween.Sequence().SetUpdate(true);
        transition.Join(view.RootGroup.DOFade(1f, 0.15f));
        transition.Join(equipmentRect.DOAnchorPos(Vector2.zero, panelMoveDuration).SetEase(Ease.OutCubic));
        transition.Join(equipmentRect.DOScale(savedScale * trainPanelScale, panelMoveDuration).SetEase(Ease.OutCubic));
        if (mode == Mode.Creation)
        {
            transition.AppendCallback(() => view.SetCreationControlsVisible(true));
            transition.Append(view.ChoicesGroup.DOFade(1f, 0.16f));
        }
        transition.OnComplete(() => { transitionBusy = false; view.RootGroup.interactable = true; transition = null; });
        RefreshResources();
        HideLegacyTooltip();
    }

    private void SaveTrainLayout()
    {
        savedParent = equipmentRect.parent; savedSibling = equipmentRect.GetSiblingIndex();
        savedAnchorMin = equipmentRect.anchorMin; savedAnchorMax = equipmentRect.anchorMax;
        savedPivot = equipmentRect.pivot; savedSize = equipmentRect.sizeDelta;
        savedPosition = equipmentRect.anchoredPosition; savedScale = equipmentRect.localScale; savedLayout = true;
    }

    private void ReparentTrainForReturn()
    {
        if (!savedLayout || equipmentRect == null || savedParent == null) return;
        Vector3 position = equipmentRect.position;
        equipmentRect.SetParent(savedParent, true);
        equipmentRect.SetSiblingIndex(savedSibling);
        equipmentRect.anchorMin = savedAnchorMin; equipmentRect.anchorMax = savedAnchorMax;
        equipmentRect.pivot = savedPivot; equipmentRect.sizeDelta = savedSize;
        equipmentRect.position = position;
    }

    public void CloseWorkbench()
    {
        if (!IsOpen || closing) return;
        closing = true; transitionBusy = true;
        transition?.Kill();
        EndCreationDrag();
        ClearSelectionImmediately(false);
        view.HideTooltip();
        view.RootGroup.interactable = false;
        ReparentTrainForReturn();
        transition = DOTween.Sequence().SetUpdate(true);
        transition.Join(view.RootGroup.DOFade(0f, 0.2f));
        transition.Join(equipmentRect.DOAnchorPos(savedPosition, panelMoveDuration).SetEase(Ease.InOutCubic));
        transition.Join(equipmentRect.DOScale(savedScale, panelMoveDuration).SetEase(Ease.InOutCubic));
        transition.OnComplete(() =>
        {
            FinishSession();
            ReleaseQueue();
        });
    }

    private void AbortWorkbench()
    {
        if (!IsOpen && !savedLayout) return;
        bool wasOpen = IsOpen;
        transition?.Kill(); transition = null;
        EndCreationDrag(); ClearSelectionImmediately();
        ReparentTrainForReturn();
        if (savedLayout && equipmentRect != null)
        { equipmentRect.anchoredPosition = savedPosition; equipmentRect.localScale = savedScale; }
        FinishSession();
        if (wasOpen && GameManager.Instance != null) GameManager.Instance.CancelUIQueue();
    }

    private void FinishSession()
    {
        if (wallet != null) wallet.OnResourcesChanged -= RefreshResources;
        if (playerInventory != null) playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        wallet = null; selectedItem = null; selectedSlot = null;
        savedLayout = false; mode = Mode.None; closing = false; transitionBusy = false; transition = null;
        if (view != null) { view.HideTooltip(); view.ClearPreview(); view.gameObject.SetActive(false); }
    }

    private void Update()
    {
        if (!IsOpen || closing || Time.frameCount <= openingFrame || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseWorkbench();
    }

    private bool OffersRemainValid()
    {
        if (offers[0] == null) return false;
        foreach (var offer in offers) if (offer != null && !playerInventory.CanAcquireItem(offer)) return false;
        return true;
    }

    private List<Item_SO> AvailableItems()
    {
        var candidates = new List<Item_SO>();
        if (itemDatabase.allItems == null) return candidates;
        foreach (var item in itemDatabase.allItems)
            if (playerInventory.CanAcquireItem(item) && !candidates.Contains(item)) candidates.Add(item);
        return candidates;
    }

    private void RollOffers()
    {
        var candidates = AvailableItems();
        var newCandidates = candidates.FindAll(item => Array.IndexOf(offers, item) < 0);
        Item_SO fresh = newCandidates.Count > 0 ? newCandidates[UnityEngine.Random.Range(0, newCandidates.Count)] : null;
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int chosen = UnityEngine.Random.Range(0, i + 1);
            Item_SO temp = candidates[i]; candidates[i] = candidates[chosen]; candidates[chosen] = temp;
        }
        if (fresh != null)
        {
            candidates.Remove(fresh); candidates.Insert(0, fresh);
        }
        offers = new Item_SO[3];
        for (int i = 0; i < 3 && i < candidates.Count; i++) offers[i] = candidates[i];
    }

    private void ShowOffers()
    {
        for (int i = 0; i < view.Choices.Length; i++) view.Choices[i].Show(i < offers.Length ? offers[i] : null);
        RefreshResources();
    }

    public void RerollCreation()
    {
        if (mode != Mode.Creation || closing || transitionBusy || draggingChoice >= 0 || !HasAlternativeOffers() || !wallet.CanReroll) return;
        if (!wallet.TryReroll()) return;
        view.HideTooltip(); RollOffers(); ShowOffers();
    }
    private bool HasAlternativeOffers() => AvailableItems().Exists(item => Array.IndexOf(offers, item) < 0);

    private void RefreshResources()
    {
        if (view == null || wallet == null) return;
        view.RefreshResources(wallet);
        view.SetRerollEnabled(mode == Mode.Creation && wallet.CanReroll && HasAlternativeOffers());
        if (mode == Mode.Upgrade) RefreshUpgrade();
    }
    private void HandleInventoryChanged() { if (mode == Mode.Upgrade) RefreshUpgrade(); }

    public bool BeginCreationDrag(int index, PointerEventData pointer)
    {
        if (mode != Mode.Creation || closing || transitionBusy || index < 0 || index >= offers.Length || offers[index] == null) return false;
        draggingChoice = index; view.HideTooltip();
        view.FloatingIcon.sprite = offers[index].iconSprite;
        view.FloatingIcon.color = Color.white; view.FloatingIcon.gameObject.SetActive(true);
        view.FloatingIcon.rectTransform.localScale = Vector3.one;
        view.PlaceFloatingIcon(pointer);
        inventoryUI.MarkDropTargets(offers[index]);
        return true;
    }
    public void MoveCreationDrag(PointerEventData pointer) { if (draggingChoice >= 0 && !transitionBusy) view.PlaceFloatingIcon(pointer); }
    public void EndCreationDrag()
    {
        draggingChoice = -1;
        if (inventoryUI != null) inventoryUI.MarkDropTargets(null);
        if (view != null && !transitionBusy) view.FloatingIcon.gameObject.SetActive(false);
    }

    public void DropCreationItem(int slotIndex)
    {
        if (mode != Mode.Creation || transitionBusy || closing || draggingChoice < 0) return;
        Item_SO item = offers[draggingChoice];
        if (!playerInventory.CanEquipAt(item, slotIndex) || !wallet.CanPurchaseCreation) return;
        ItemInstance equipped = null;
        if (!wallet.TryPurchaseCreation(() => playerInventory.TryEquipAt(item, slotIndex, out equipped))) return;
        int selected = draggingChoice; draggingChoice = -1; transitionBusy = true;
        inventoryUI.MarkDropTargets(null); offers = new Item_SO[3];
        HideItemTooltip();
        InventorySlotUI slot = inventoryUI.GetEquipmentSlot(slotIndex);
        transition?.Kill();
        transition = DOTween.Sequence().SetUpdate(true);
        if (slot != null) transition.Join(view.FloatingIcon.rectTransform.DOMove(slot.transform.position, 0.16f).SetEase(Ease.OutQuad));
        for (int i = 0; i < view.Choices.Length; i++)
            if (view.Choices[i].gameObject.activeSelf)
                transition.Join(view.Choices[i].transform.DOScale(Vector3.zero, i == selected ? 0.12f : 0.18f).SetEase(Ease.InBack));
        transition.OnComplete(() => { view.FloatingIcon.gameObject.SetActive(false); transitionBusy = false; CloseWorkbench(); });
    }

    public void SelectUpgradeItem(ItemInstance item, InventorySlotUI sourceSlot)
    {
        if (mode != Mode.Upgrade || closing || item == null || sourceSlot == null || selectedItem == item) return;
        if (transitionBusy && selectedItem == null) return; // Opening animation.
        transition?.Kill(); transition = null;
        if (selectedSlot != null) selectedSlot.SetPreviewHidden(false);
        selectedItem = item; selectedSlot = sourceSlot; transitionBusy = true;
        selectedSlot.SetPreviewHidden(true);
        view.HideTooltip(); RefreshUpgrade(); view.SetPreviewIconVisible(false);
        var floating = view.FloatingIcon;
        floating.sprite = item.itemData.iconSprite; floating.color = Color.white;
        floating.gameObject.SetActive(true); floating.rectTransform.localScale = Vector3.one;
        floating.rectTransform.position = sourceSlot.transform.position;
        transition = DOTween.Sequence().SetUpdate(true);
        transition.Append(floating.rectTransform.DOMove(view.UpgradeSlot.position, 0.25f).SetEase(Ease.OutCubic));
        transition.OnComplete(() => { floating.gameObject.SetActive(false); transitionBusy = false; view.SetPreviewIconVisible(true); transition = null; });
    }

    public void CancelUpgradeSelection()
    {
        if (mode != Mode.Upgrade || selectedItem == null || closing) return;
        Vector3 iconPosition = view.FloatingIcon.gameObject.activeSelf ? view.FloatingIcon.rectTransform.position : view.UpgradeSlot.position;
        transition?.Kill(); transitionBusy = true;
        var oldSlot = selectedSlot;
        var floating = view.FloatingIcon;
        floating.sprite = selectedItem.itemData.iconSprite; floating.color = Color.white;
        floating.gameObject.SetActive(true); floating.rectTransform.position = iconPosition;
        selectedItem = null; selectedSlot = null;
        view.ClearPreview();
        transition = DOTween.Sequence().SetUpdate(true);
        if (oldSlot != null) transition.Append(floating.rectTransform.DOMove(oldSlot.transform.position, 0.25f).SetEase(Ease.OutCubic));
        transition.OnComplete(() =>
        { oldSlot?.SetPreviewHidden(false); floating.gameObject.SetActive(false); transitionBusy = false; transition = null; RefreshUpgrade(); });
    }

    private void ClearSelectionImmediately(bool clearPreview = true)
    {
        if (selectedSlot != null) selectedSlot.SetPreviewHidden(false);
        if (inventoryUI != null) for (int i = 0; i < 10; i++) inventoryUI.GetEquipmentSlot(i)?.SetPreviewHidden(false);
        selectedItem = null; selectedSlot = null;
        if (view != null) { view.FloatingIcon.gameObject.SetActive(false); if (clearPreview) view.ClearPreview(); }
    }

    private int UpgradeCost(ItemInstance item) => upgradeCostPerCell < 0 ? -1 :
        (int)Math.Min(int.MaxValue, (long)upgradeCostPerCell * playerInventory.OccupiedSlotCount(item));
    private bool CanUpgrade(ItemInstance item) => item != null && playerInventory.items.Contains(item) &&
        item.currentUpgrade < item.itemData.MaxUpgrade && UpgradeCost(item) >= 0 && wallet.Flesh >= UpgradeCost(item);
    private bool NoUpgradeActionsRemain()
    {
        foreach (var item in playerInventory.items)
            if (item != null && item.itemData != null && (CanUpgrade(item) || !removalUsed)) return false;
        return true;
    }
    private void RefreshUpgrade()
    {
        if (mode != Mode.Upgrade || wallet == null) return;
        if (selectedItem != null && !playerInventory.items.Contains(selectedItem)) ClearSelectionImmediately();
        if (selectedItem == null) { view.ClearPreview(); view.SetNoActionsRemain(NoUpgradeActionsRemain()); return; }
        view.ShowPreview(selectedItem, UpgradeCost(selectedItem), CanUpgrade(selectedItem), !removalUsed, NoUpgradeActionsRemain());
        if (transitionBusy) view.SetPreviewIconVisible(false);
    }

    public void UpgradeSelectedItem()
    {
        if (mode != Mode.Upgrade || transitionBusy || closing || !CanUpgrade(selectedItem)) return;
        int cost = UpgradeCost(selectedItem);
        if (!wallet.TrySpendFlesh(cost)) return;
        if (!playerInventory.TryUpgradeItemInstance(selectedItem)) wallet.AddFlesh(cost);
        RefreshUpgrade();
    }
    public void RemoveSelectedItem()
    {
        if (mode != Mode.Upgrade || transitionBusy || closing || removalUsed || selectedItem == null) return;
        var item = selectedItem;
        ClearSelectionImmediately();
        if (playerInventory.RemoveItemInstance(item)) removalUsed = true;
        RefreshUpgrade();
    }
    public void ShowItemTooltip(Item_SO item, int level, PointerEventData pointer)
    { if (IsOpen && !closing && !transitionBusy && draggingChoice < 0 && item != null) view.ShowTooltip(item, level, pointer); }
    public void HideItemTooltip() { if (view != null) view.HideTooltip(); }
    private static void HideLegacyTooltip() { if (TooltipSystem.TryGetInstance(out TooltipSystem tooltip)) tooltip.Hide(); }
    private static void ReleaseQueue() { if (GameManager.Instance != null) GameManager.Instance.CloseUI(); }
}
