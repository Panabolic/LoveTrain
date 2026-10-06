using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PermanentUpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private string nodeId;
    [SerializeField] private UnityEngine.UI.Button purchaseButton;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private UnityEngine.UI.Image accent;
    [Tooltip("Passive progress rectangles in left-to-right order.")]
    [SerializeField] private UnityEngine.UI.Image[] progressSegments;

    private PermanentUpgradeMenu owner;
    private PermanentUpgradeNode node;
    private bool pointerInside;

    public string NodeId => nodeId;

    public void Bind(PermanentUpgradeMenu menu, PermanentUpgradeNode upgrade)
    {
        if (owner != null && node != null) owner.HideTooltip(node);
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Purchase);
        owner = menu;
        node = upgrade;
        if (purchaseButton != null) purchaseButton.onClick.AddListener(Purchase);
        Refresh();
    }

    public void Refresh()
    {
        if (node == null)
        {
            if (purchaseButton != null) purchaseButton.interactable = false;
            if (nameLabel != null) nameLabel.text = string.Empty;
            if (costLabel != null) costLabel.text = string.Empty;
            if (statusLabel != null) statusLabel.text = string.Empty;
            if (progressSegments != null)
                foreach (UnityEngine.UI.Image segment in progressSegments)
                    if (segment != null) segment.gameObject.SetActive(false);
            return;
        }
        PermanentUpgradePurchaseStatus status = node.PurchaseStatus;
        PermanentUpgradeDefinition next = node.NextStage;
        if (nameLabel != null) nameLabel.text = node.Name;
        if (costLabel != null) costLabel.text = next != null
            ? EnglishLocalization.Format("permanent.cost", "영혼 {0}", next.Cost) : string.Empty;
        if (statusLabel != null)
        {
            statusLabel.text = node.IsComplete ? "MAX" : PermanentUpgradeMenu.StatusText(status);
            statusLabel.color = node.IsComplete
                ? new Color(0.52f, 0.89f, 0.69f) : new Color(0.75f, 0.79f, 0.84f);
        }
        if (purchaseButton != null) purchaseButton.interactable = status == PermanentUpgradePurchaseStatus.Available;
        Color filled = PermanentUpgradeMenu.CategoryColor(node.Category);
        if (accent != null) accent.color = filled;
        int completed = node.CompletedCount;
        if (progressSegments == null) return;
        for (int i = 0; i < progressSegments.Length; i++)
        {
            UnityEngine.UI.Image segment = progressSegments[i];
            if (segment == null) continue;
            segment.gameObject.SetActive(i < node.Stages.Count);
            segment.color = i < completed ? filled : new Color(0.17f, 0.21f, 0.28f);
            segment.raycastTarget = false;
        }
    }

    private void Purchase() { if (owner != null && node != null) owner.Purchase(node); }
    public void OnPointerEnter(PointerEventData pointer)
    {
        pointerInside = true;
        if (owner != null && node != null) owner.ShowTooltip(node, (RectTransform)transform);
    }
    public void OnPointerExit(PointerEventData pointer)
    {
        pointerInside = false;
        if (owner != null && node != null) owner.HideTooltip(node);
    }
    public void OnSelect(BaseEventData eventData)
    {
        if (owner == null || node == null) return;
        owner.ShowTooltip(node, (RectTransform)transform);
    }
    public void OnDeselect(BaseEventData eventData)
    {
        // Buying the last tier disables selection, but the mouse can still be hovering.
        if (!pointerInside && owner != null && node != null) owner.HideTooltip(node);
    }
    private void OnDisable()
    {
        pointerInside = false;
        if (owner != null && node != null) owner.HideTooltip(node);
    }
    private void OnDestroy() { if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Purchase); }
}
