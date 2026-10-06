using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PermanentUpgradeMenu : MonoBehaviour
{
    public const string ResourcePath = "PermanentUpgradeMenu";
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text souls;
    [SerializeField] private TMP_Text message;
    [SerializeField] private TMP_Text startLabel;
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private PermanentUpgradeCard[] cards;
    [SerializeField] private RectTransform tooltip;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;
    [SerializeField] private TMP_Text tooltipStatus;

    private StartMenuManager owner;
    private PermanentUpgradeNode hovered;

    public static PermanentUpgradeMenu Create(StartMenuManager manager, PermanentUpgradeCatalog configuration)
    {
        GameObject prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogError("[PermanentUpgradeMenu] Resources/PermanentUpgradeMenu 프리팹을 찾을 수 없습니다.", manager);
            return null;
        }
        PermanentUpgradeMenu template = prefab.GetComponent<PermanentUpgradeMenu>();
        if (template == null)
        {
            Debug.LogError("[PermanentUpgradeMenu] 프리팹에 PermanentUpgradeMenu 컴포넌트가 없습니다.", manager);
            return null;
        }
        // The inactive prefab owns the entire UI hierarchy and its serialized references.
        PermanentUpgradeMenu menu = Instantiate(template);
        menu.gameObject.SetActive(false);
        menu.owner = manager;
        PermanentUpgradeCatalog catalog = configuration != null ? configuration : PermanentUpgradeCatalog.Load();
        PermanentUpgradeProgress.Configure(catalog);
        List<PermanentUpgradeNode> nodes = PermanentUpgradeNode.Build(catalog);
        foreach (PermanentUpgradeCard card in menu.cards)
        {
            PermanentUpgradeNode node = nodes.Find(candidate => candidate.Id == card.NodeId);
            card.gameObject.SetActive(node != null);
            if (node != null) card.Bind(menu, node);
        }
        menu.startButton.onClick.AddListener(menu.StartGame);
        return menu;
    }

    public void Show()
    {
        PermanentUpgradeProgress.Reload();
        gameObject.SetActive(true);
        FixedAspectRatioController.RequestRefresh();
        hovered = null;
        tooltip.gameObject.SetActive(false);
        Refresh();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    private void OnEnable() => EnglishLocalization.LanguageChanged += Refresh;
    private void OnDisable()
    {
        EnglishLocalization.LanguageChanged -= Refresh;
        hovered = null;
        if (tooltip != null) tooltip.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
    }

    private void Refresh()
    {
        title.text = EnglishLocalization.Get("permanent.title", "영구 강화");
        souls.text = EnglishLocalization.Format("permanent.balance", "보유 영혼 {0}", PermanentUpgradeProgress.Souls);
        startLabel.text = EnglishLocalization.Get("permanent.start", "게임 시작");
        message.text = PermanentUpgradeProgress.IsSaveAvailable ? string.Empty
            : EnglishLocalization.Get("permanent.save_error", "저장 정보를 읽을 수 없어 강화 구매가 잠겼습니다.");
        foreach (PermanentUpgradeCard card in cards)
            if (card.gameObject.activeSelf) card.Refresh();
        if (hovered != null) RefreshTooltip();
    }

    public void Purchase(PermanentUpgradeNode node)
    {
        if (node == null || node.NextStage == null) return;
        PermanentUpgradeDefinition next = node.NextStage;
        PermanentUpgradeProgress.TryPurchase(next, out PermanentUpgradePurchaseStatus status);
        Refresh();
        message.text = status == PermanentUpgradePurchaseStatus.Purchased
            ? EnglishLocalization.Format("permanent.purchase_done", "{0} · 강화 완료", next.Name) : StatusText(status);
    }

    private void StartGame()
    {
        tooltip.gameObject.SetActive(false);
        owner.StartGameFromUpgrades();
    }

    public void ShowTooltip(PermanentUpgradeNode node, PointerEventData pointer)
    {
        hovered = node;
        RefreshTooltip();
        tooltip.gameObject.SetActive(true);
        MoveTooltip(pointer);
    }
    public void ShowTooltip(PermanentUpgradeNode node, RectTransform card)
    {
        hovered = node;
        RefreshTooltip();
        tooltip.gameObject.SetActive(true);
        PlaceTooltip(panel.InverseTransformPoint(card.position));
    }

    private void RefreshTooltip()
    {
        tooltipTitle.text = hovered.Name;
        PermanentUpgradeDefinition current = null;
        foreach (PermanentUpgradeDefinition stage in hovered.Stages)
            if (PermanentUpgradeProgress.IsPurchased(stage.Id)) current = stage;
        string progress = EnglishLocalization.Format("permanent.progress", "진척도: {0} / {1}",
            hovered.CompletedCount, hovered.Stages.Count);
        string currentName = current == null ? EnglishLocalization.Get("permanent.none", "없음")
            : hovered.Category == PermanentUpgradeCategory.BaseGun ? hovered.CurrentDescription : current.Name;
        string currentText = EnglishLocalization.Format("permanent.current", "현재: {0}", currentName);
        PermanentUpgradeDefinition next = hovered.NextStage;
        if (next == null)
        {
            tooltipDescription.text = progress + "\n" + currentText + "\n\n" + hovered.CurrentDescription;
            tooltipStatus.text = EnglishLocalization.Get("permanent.max", "최대 단계");
            return;
        }
        string nextText = EnglishLocalization.Format("permanent.next", "다음: {0}", next.Name);
        tooltipDescription.text = progress + "\n" + currentText + "\n" + nextText + "\n\n" + next.Description;
        tooltipStatus.text = EnglishLocalization.Format("permanent.tooltip_cost", "비용: 영혼 {0} · {1}",
            next.Cost, StatusText(PermanentUpgradeProgress.GetPurchaseStatus(next)));
    }

    public void MoveTooltip(PointerEventData pointer)
    {
        if (hovered == null || !tooltip.gameObject.activeSelf) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, pointer.position, pointer.enterEventCamera, out Vector2 point);
        PlaceTooltip(point);
    }
    private void PlaceTooltip(Vector2 point)
    {
        Vector2 half = tooltip.rect.size * 0.5f;
        point += new Vector2(half.x + 12f, -half.y - 12f);
        point.x = Mathf.Clamp(point.x, panel.rect.xMin + half.x + 8f, panel.rect.xMax - half.x - 8f);
        point.y = Mathf.Clamp(point.y, panel.rect.yMin + half.y + 8f, panel.rect.yMax - half.y - 8f);
        tooltip.anchoredPosition = point;
        tooltip.SetAsLastSibling();
    }
    public void HideTooltip(PermanentUpgradeNode node)
    {
        if (hovered != node) return;
        hovered = null;
        if (tooltip != null) tooltip.gameObject.SetActive(false);
    }

    public static string StatusText(PermanentUpgradePurchaseStatus status)
    {
        switch (status)
        {
            case PermanentUpgradePurchaseStatus.Available: return EnglishLocalization.Get("permanent.available", "강화 가능");
            case PermanentUpgradePurchaseStatus.Purchased: return EnglishLocalization.Get("permanent.purchased", "구매 완료");
            case PermanentUpgradePurchaseStatus.PrerequisiteMissing: return EnglishLocalization.Get("permanent.prerequisite", "선행 강화 필요");
            case PermanentUpgradePurchaseStatus.InsufficientSouls: return EnglishLocalization.Get("permanent.insufficient", "영혼 부족");
            default: return EnglishLocalization.Get("permanent.unconfigured", "준비 중");
        }
    }
    public static Color CategoryColor(PermanentUpgradeCategory category)
    {
        switch (category)
        {
            case PermanentUpgradeCategory.Fuel: return new Color(0.36f, 0.69f, 0.96f);
            case PermanentUpgradeCategory.BaseGun: return new Color(1f, 0.65f, 0.35f);
            case PermanentUpgradeCategory.Weapon: return new Color(0.43f, 0.84f, 0.56f);
            case PermanentUpgradeCategory.BossItem: return new Color(0.97f, 0.43f, 0.46f);
            case PermanentUpgradeCategory.Ultimate: return new Color(0.76f, 0.56f, 1f);
            default: return new Color(0.97f, 0.82f, 0.34f);
        }
    }
}
