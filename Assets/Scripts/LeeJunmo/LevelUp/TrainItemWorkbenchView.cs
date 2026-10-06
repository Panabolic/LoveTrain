using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Authored by TrainItemWorkbench.prefab. Gameplay transactions remain in Inventory and the wallet.
public sealed class TrainItemWorkbenchView : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform trainHost;
    [SerializeField] private Vector2 creationTrainPosition = new Vector2(-27.5f, -180f);
    [SerializeField] private Vector2 upgradeTrainPosition = new Vector2(-27.5f, -150f);
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text resources;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private RectTransform choicesRoot;
    [SerializeField] private CanvasGroup choicesGroup;
    [SerializeField] private WorkbenchChoiceUI[] choices;
    [SerializeField] private TMP_Text creationHint;
    [SerializeField] private UnityEngine.UI.Button rerollButton;
    [SerializeField] private TMP_Text rerollLabel;
    [SerializeField] private GameObject upgradeRoot;
    [SerializeField] private RectTransform upgradeSlot;
    [SerializeField] private UnityEngine.UI.Image upgradeIcon;
    [SerializeField] private TMP_Text beforeDescription;
    [SerializeField] private TMP_Text afterDescription;
    [SerializeField] private TMP_Text beforeHeading;
    [SerializeField] private TMP_Text afterHeading;
    [SerializeField] private TMP_Text upgradeStatus;
    [SerializeField] private GameObject upgradeActions;
    [SerializeField] private UnityEngine.UI.Button upgradeButton;
    [SerializeField] private UnityEngine.UI.Button removeButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    [SerializeField] private UnityEngine.UI.Button largeCloseButton;
    [SerializeField] private UnityEngine.UI.Image floatingIcon;
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;

    public CanvasGroup RootGroup => rootGroup;
    public RectTransform TrainHost => trainHost;
    public RectTransform UpgradeSlot => upgradeSlot;
    public UnityEngine.UI.Image FloatingIcon => floatingIcon;
    public WorkbenchChoiceUI[] Choices => choices;
    public CanvasGroup ChoicesGroup => choicesGroup;

    public void Bind(LevelUpUIManager owner)
    {
        closeButton.onClick.AddListener(owner.CloseWorkbench);
        largeCloseButton.onClick.AddListener(owner.CloseWorkbench);
        rerollButton.onClick.AddListener(owner.RerollCreation);
        upgradeButton.onClick.AddListener(owner.UpgradeSelectedItem);
        removeButton.onClick.AddListener(owner.RemoveSelectedItem);
        cancelButton.onClick.AddListener(owner.CancelUpgradeSelection);
        SetButtonLabel(upgradeButton, "workbench.upgrade", "강화");
        SetButtonLabel(removeButton, "workbench.remove", "아이템 제거");
        SetButtonLabel(cancelButton, "workbench.cancel", "취소");
        SetButtonLabel(largeCloseButton, "workbench.close", "닫기");
        beforeHeading.text = EnglishLocalization.Get("workbench.before", "강화 전");
        afterHeading.text = EnglishLocalization.Get("workbench.after", "강화 후");
        for (int i = 0; i < choices.Length; i++) choices[i].Configure(owner, i);
    }

    private static void SetButtonLabel(UnityEngine.UI.Button button, string key, string fallback)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = EnglishLocalization.Get(key, fallback);
    }

    public void SetMode(bool creation)
    {
        trainHost.anchoredPosition = creation ? creationTrainPosition : upgradeTrainPosition;
        choicesRoot.gameObject.SetActive(creation);
        creationHint.gameObject.SetActive(creation);
        rerollButton.gameObject.SetActive(creation);
        upgradeRoot.SetActive(!creation);
        title.text = EnglishLocalization.Get(creation ? "workbench.create_title" : "workbench.upgrade_title", creation ? "부품 제작" : "부품 강화");
        ClearPreview();
        HideTooltip();
        floatingIcon.gameObject.SetActive(false);
    }

    public void RefreshResources(TrainLevelManager wallet)
    {
        resources.text = EnglishLocalization.Format("workbench.resources", "살점 {0}   영혼 {1}", wallet.Flesh, wallet.Souls);
        creationHint.text = EnglishLocalization.Format("workbench.creation_hint", "원하는 칸으로 드래그하여 장착 · 제작 비용 {0}", wallet.CreationCost);
        rerollLabel.text = wallet.FreeRerollsRemaining > 0
            ? EnglishLocalization.Format("workbench.free_reroll", "리롤 · 무료 {0}회", wallet.FreeRerollsRemaining)
            : EnglishLocalization.Format("workbench.soul_reroll", "리롤 · 영혼 {0}", wallet.RerollCost);
    }

    public void SetRerollEnabled(bool enabled) => rerollButton.interactable = enabled;

    public void SetCreationControlsVisible(bool visible)
    {
        choicesRoot.gameObject.SetActive(visible);
        creationHint.gameObject.SetActive(visible);
        rerollButton.gameObject.SetActive(visible);
    }

    public void ShowPreview(ItemInstance item, int cost, bool canUpgrade, bool canRemove, bool noActionsRemain)
    {
        upgradeIcon.sprite = item.itemData.iconSprite;
        upgradeIcon.enabled = true;
        beforeDescription.text = item.itemData.LocalizedName + "\n" + item.itemData.GetFormattedDescription(item.currentUpgrade);
        afterDescription.text = item.currentUpgrade >= item.itemData.MaxUpgrade
            ? EnglishLocalization.Get("workbench.max_upgrade", "최대 강화 단계")
            : item.itemData.LocalizedName + "\n" + item.itemData.GetFormattedDescription(item.currentUpgrade + 1);
        upgradeStatus.text = cost < 0 ? EnglishLocalization.Get("workbench.cost_pending", "강화 비용 미정")
            : EnglishLocalization.Format("workbench.upgrade_cost", "강화 비용 · 살점 {0}", cost);
        upgradeButton.interactable = canUpgrade;
        removeButton.interactable = canRemove;
        SetButtonLabel(removeButton, canRemove ? "workbench.remove_available" : "workbench.remove_used",
            canRemove ? "아이템 제거 · 1회" : "아이템 제거 · 0회");
        cancelButton.interactable = true;
        SetNoActionsRemain(noActionsRemain);
    }

    public void ClearPreview()
    {
        upgradeIcon.enabled = false;
        beforeDescription.text = afterDescription.text = string.Empty;
        upgradeStatus.text = EnglishLocalization.Get("workbench.select_item", "기차의 아이템을 클릭하여 선택");
        upgradeButton.interactable = removeButton.interactable = cancelButton.interactable = false;
        SetNoActionsRemain(false);
    }

    public void SetPreviewIconVisible(bool visible) => upgradeIcon.enabled = visible;
    public void SetNoActionsRemain(bool none) { largeCloseButton.gameObject.SetActive(none); upgradeActions.SetActive(!none); }

    public void ShowTooltip(Item_SO item, int level, PointerEventData pointer)
    {
        tooltipTitle.text = item.LocalizedName;
        tooltipDescription.text = item.GetFormattedDescription(level);
        tooltipRoot.gameObject.SetActive(true);
        RectTransform root = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, pointer.position, pointer.enterEventCamera, out Vector2 point);
        Vector2 half = tooltipRoot.rect.size * 0.5f;
        point.y += half.y + 12f;
        if (point.y + half.y > root.rect.yMax - 8f) point.y -= (half.y + 12f) * 2f;
        point.x = Mathf.Clamp(point.x, root.rect.xMin + half.x + 8f, root.rect.xMax - half.x - 8f);
        point.y = Mathf.Clamp(point.y, root.rect.yMin + half.y + 8f, root.rect.yMax - half.y - 8f);
        tooltipRoot.anchoredPosition = point;
        tooltipRoot.SetAsLastSibling();
    }

    public void HideTooltip() => tooltipRoot.gameObject.SetActive(false);

    public void PlaceFloatingIcon(PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, pointer.position, pointer.pressEventCamera, out Vector2 point);
        floatingIcon.rectTransform.anchoredPosition = point;
    }
}
