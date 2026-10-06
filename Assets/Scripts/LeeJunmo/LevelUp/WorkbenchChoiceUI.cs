using UnityEngine;
using UnityEngine.EventSystems;

public sealed class WorkbenchChoiceUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private CanvasGroup group;
    private LevelUpUIManager owner;
    private int index;
    public Item_SO Item { get; private set; }
    public void Configure(LevelUpUIManager manager, int choiceIndex) { owner = manager; index = choiceIndex; }
    public void Show(Item_SO item)
    {
        Item = item;
        transform.localScale = Vector3.one;
        group.alpha = 1f;
        group.blocksRaycasts = true;
        gameObject.SetActive(item != null);
        if (item != null) { icon.sprite = item.iconSprite; icon.preserveAspect = true; }
    }
    public void SetDragging(bool dragging) { group.alpha = dragging ? 0.3f : 1f; group.blocksRaycasts = !dragging; }
    public void OnPointerEnter(PointerEventData pointer) { if (Item != null) owner.ShowItemTooltip(Item, 1, pointer); }
    public void OnPointerExit(PointerEventData pointer) => owner.HideItemTooltip();
    public void OnBeginDrag(PointerEventData pointer)
    {
        if (pointer.button == PointerEventData.InputButton.Left && owner.BeginCreationDrag(index, pointer)) SetDragging(true);
    }
    public void OnDrag(PointerEventData pointer) => owner.MoveCreationDrag(pointer);
    public void OnEndDrag(PointerEventData pointer) { SetDragging(false); owner.EndCreationDrag(); }
}
