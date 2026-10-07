using UnityEngine;

// Per-equipped-instance effects. Unity clocks stay with Inventory or the attached facade.
internal interface IItemEffect
{
    void Equip();
    void Upgrade(int previousLevel);
    void Release();
}

internal sealed class ItemAttachment : IItemEffect
{
    private readonly ItemInstance instance;
    private readonly System.Func<GameObject> create;
    private readonly System.Func<GameObject, IInstantiatedItem> initialize;
    private readonly System.Action<GameObject> release;
    private IInstantiatedItem behaviour;
    private PresentationLink presentation;

    internal ItemAttachment(ItemInstance instance, System.Func<GameObject> create,
        System.Func<GameObject, IInstantiatedItem> initialize = null, System.Action<GameObject> release = null)
    {
        this.instance = instance;
        this.create = create;
        this.initialize = initialize;
        this.release = release;
    }

    public void Equip()
    {
        GameObject view = create();
        instance.SetInstantiatedObject(view);
        if (view == null) return;
        presentation = PresentationLink.From(view);
        behaviour = initialize != null ? initialize(view) : null;
        if (behaviour != null) behaviour.UpgradeInstItem(instance);
    }

    public void Upgrade(int previousLevel) => Refresh();

    internal void Refresh()
    {
        if (instance.InstantiatedObject == null) return;
        if (presentation == null) presentation = PresentationLink.From(instance.InstantiatedObject);
        if (behaviour == null) behaviour = instance.InstantiatedObject.GetComponent<IInstantiatedItem>();
        if (behaviour != null) behaviour.UpgradeInstItem(instance);
        else presentation?.ApplyUpgrade(instance.itemData, instance.currentUpgrade);
    }

    public void Release()
    {
        if (instance.InstantiatedObject != null) release?.Invoke(instance.InstantiatedObject);
        presentation?.ClearSignals();
        behaviour = null;
    }
}
