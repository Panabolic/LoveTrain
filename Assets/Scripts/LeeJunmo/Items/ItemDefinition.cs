using System;

// Shared read-only adapter over existing authoring assets. No owner, timer or Unity instance lives here.
internal sealed class ItemDefinition
{
    internal readonly Item_SO Source;
    private readonly CompositionDefinition[] recipe;

    internal ItemDefinition(Item_SO source, params CompositionDefinition[] recipe)
    {
        Source = source;
        this.recipe = (CompositionDefinition[])recipe.Clone();
    }

    internal IItemEffect[] CreateEffects(ItemInstance instance)
    {
        var effects = new IItemEffect[recipe.Length];
        for (int i = 0; i < effects.Length; i++) effects[i] = recipe[i].Create(instance);
        return effects;
    }
}
