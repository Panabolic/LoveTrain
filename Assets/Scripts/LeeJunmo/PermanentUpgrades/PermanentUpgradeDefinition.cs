using System;
using System.Globalization;
using UnityEngine;

public enum PermanentUpgradeCategory
{
    Fuel,
    BaseGun,
    Weapon,
    BossItem,
    Ultimate,
    DashDuration,
    DashSpeed
}

public enum PermanentUpgradeEffect
{
    FuelCapacity,
    BaseGunDamage,
    UnlockItem,
    UltimateCharges,
    DashDuration,
    DashSpeed
}

public enum PermanentUpgradePurchaseStatus
{
    Available,
    Purchased,
    PrerequisiteMissing,
    InsufficientSouls,
    Unconfigured
}

[Serializable]
public sealed class PermanentUpgradeDefinition
{
    [Tooltip("Save identifier. Do not change an ID after players have purchased it.")]
    public string id;
    public string koreanName;
    public string englishName;
    [TextArea(2, 5)] public string koreanDescription;
    [TextArea(2, 5)] public string englishDescription;
    [Min(1)] public int cost;
    public PermanentUpgradeCategory category;
    public PermanentUpgradeEffect effect;
    [Tooltip("Fuel/dash/ultimate values are the total at this tier; damage is an additive increase at each tier.")]
    [Min(0f)] public float value;
    [Tooltip("Empty for an independent upgrade; otherwise the preceding tier's stable ID.")]
    public string prerequisiteId;
    [Tooltip("Bind the actual gameplay item for weapon/boss unlocks. Unbound unfinished items cannot be purchased.")]
    public Item_SO unlockedItem;
    [Tooltip("The existing LaserGun_SO can be matched without duplicating item asset references.")]
    public bool useExistingLaserItem;
    public bool enabled = true;
    [Tooltip("Enable only after an ultimate consumer actually implements UltimateCharges.")]
    public bool ultimateImplemented;

    public string Id => id;
    public string Name => EnglishLocalization.IsEnglish && !string.IsNullOrEmpty(englishName) ? englishName : koreanName;
    public string Description
    {
        get
        {
            string description = EnglishLocalization.IsEnglish && !string.IsNullOrEmpty(englishDescription) ? englishDescription : koreanDescription;
            string displayedValue = value > 0f ? value.ToString("0.##", CultureInfo.InvariantCulture)
                : (EnglishLocalization.IsEnglish ? "N (to be decided)" : "N (수치 미정)");
            return (description ?? string.Empty).Replace("{value}", displayedValue);
        }
    }
    public int Cost => cost;
    public PermanentUpgradeCategory Category => category;
    public PermanentUpgradeEffect Effect => effect;
    public float Value => value;
    public string PrerequisiteId => prerequisiteId;

    public bool IsConfigured
    {
        get
        {
            if (!enabled || string.IsNullOrWhiteSpace(id) || cost < 1 || float.IsNaN(value) || float.IsInfinity(value)) return false;
            switch (effect)
            {
                case PermanentUpgradeEffect.FuelCapacity:
                case PermanentUpgradeEffect.BaseGunDamage:
                case PermanentUpgradeEffect.DashDuration:
                    return value > 0f;
                case PermanentUpgradeEffect.DashSpeed:
                    return value > 1f;
                case PermanentUpgradeEffect.UnlockItem:
                    return unlockedItem != null || useExistingLaserItem;
                case PermanentUpgradeEffect.UltimateCharges:
                    return ultimateImplemented && value >= 1f;
                default:
                    return false;
            }
        }
    }

    public bool MatchesItem(Item_SO item)
    {
        if (item == null || effect != PermanentUpgradeEffect.UnlockItem) return false;
        if (unlockedItem != null) return item == unlockedItem;
        return useExistingLaserItem && item is LaserGun_SO;
    }
}
