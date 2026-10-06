using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PermanentUpgrades", menuName = "LoveTrain/Permanent Upgrade Catalog")]
public sealed class PermanentUpgradeCatalog : ScriptableObject
{
    public const string ResourcePath = "PermanentUpgrades";
    [SerializeField] private List<PermanentUpgradeDefinition> upgrades = new List<PermanentUpgradeDefinition>();
    private static PermanentUpgradeCatalog current;

    public IReadOnlyList<PermanentUpgradeDefinition> Upgrades
    {
        get { EnsureDefaults(); return upgrades; }
    }

    public static PermanentUpgradeCatalog Load()
    {
        if (current != null) return current;
        current = Resources.Load<PermanentUpgradeCatalog>(ResourcePath);
        if (current == null)
        {
            current = CreateInstance<PermanentUpgradeCatalog>();
            current.hideFlags = HideFlags.DontSave;
        }
        current.EnsureDefaults();
        return current;
    }

    public static void Configure(PermanentUpgradeCatalog catalog)
    {
        if (catalog == null) return;
        current = catalog;
        current.EnsureDefaults();
    }

    public PermanentUpgradeDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (PermanentUpgradeDefinition upgrade in Upgrades)
            if (upgrade != null && upgrade.Id == id) return upgrade;
        return null;
    }

    public bool Contains(PermanentUpgradeDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.Id)) return false;
        int matches = 0;
        foreach (PermanentUpgradeDefinition candidate in Upgrades)
            if (candidate != null && candidate.Id == definition.Id)
            {
                if (!ReferenceEquals(candidate, definition)) return false;
                matches++;
            }
        return matches == 1;
    }

    private void OnEnable() => EnsureDefaults();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => current = null;

    private void EnsureDefaults()
    {
        if (upgrades != null && upgrades.Count > 0) return;
        upgrades = CreateDefaults();
    }

    private static List<PermanentUpgradeDefinition> CreateDefaults()
    {
        var result = new List<PermanentUpgradeDefinition>(24);
        int[] costs = { 5, 10, 15, 20, 25 };
        int[] fuelValues = { 10, 20, 40, 70, 100 };
        for (int i = 0; i < fuelValues.Length; i++)
        {
            int tier = i + 1;
            result.Add(Create("fuel." + tier, "연료 최대치 +" + fuelValues[i], "Maximum fuel +" + fuelValues[i],
                "기본 최대 연료량이 총 " + fuelValues[i] + " 증가합니다. 이전 단계와 중복 합산되지 않습니다.",
                "Increase maximum fuel by a total of " + fuelValues[i] + ". This replaces the previous tier.",
                costs[i], PermanentUpgradeCategory.Fuel, PermanentUpgradeEffect.FuelCapacity, fuelValues[i], i == 0 ? null : "fuel." + i));
        }
        for (int i = 0; i < costs.Length; i++)
        {
            int tier = i + 1;
            result.Add(Create("damage." + tier, "기본총기 데미지 증가 " + tier, "Base gun damage " + tier,
                "기본 총기의 공격력이 {value} 증가합니다.",
                "Base gun damage increases by {value}.",
                costs[i], PermanentUpgradeCategory.BaseGun, PermanentUpgradeEffect.BaseGunDamage, 0f, i == 0 ? null : "damage." + i));
        }

        PermanentUpgradeDefinition laser = CreateUnlock("weapon.laser", "레이저 총기 해금", "Unlock laser gun", 5, PermanentUpgradeCategory.Weapon);
        laser.useExistingLaserItem = true;
        result.Add(laser);
        result.Add(CreateUnlock("weapon.buckshot", "산탄 총기 해금", "Unlock shotgun", 10, PermanentUpgradeCategory.Weapon));
        result.Add(CreateUnlock("weapon.machinegun", "기관 총기 해금", "Unlock machine gun", 15, PermanentUpgradeCategory.Weapon));
        result.Add(CreateUnlock("boss.train", "기차보스 아이템 해금", "Unlock train boss item", 20, PermanentUpgradeCategory.BossItem));
        result.Add(CreateUnlock("boss.tentacle", "촉수보스 아이템 해금", "Unlock tentacle boss item", 25, PermanentUpgradeCategory.BossItem));

        for (int i = 1; i <= 3; i++)
        {
            result.Add(Create("ultimate." + i, i == 1 ? "필살기 해금" : "필살기 횟수 증가 " + i,
                i == 1 ? "Unlock ultimate" : "Ultimate charges " + i,
                "한 게임에서 필살기를 " + i + "회 사용할 수 있습니다.",
                "Use the ultimate " + i + " time(s) per run.",
                i * 5, PermanentUpgradeCategory.Ultimate, PermanentUpgradeEffect.UltimateCharges, i, i == 1 ? null : "ultimate." + (i - 1)));
        }
        for (int i = 0; i < 3; i++)
        {
            int tier = i + 1;
            int duration = i + 3;
            result.Add(Create("dash.duration." + tier, "질주 시간 " + duration + "초", "Dash duration " + duration + "s",
                "질주의 유지 시간이 " + duration + "초가 됩니다.", "Dash lasts " + duration + " seconds.",
                tier * 5, PermanentUpgradeCategory.DashDuration, PermanentUpgradeEffect.DashDuration, duration,
                i == 0 ? null : "dash.duration." + i));
        }
        int[] percentages = { 60, 70, 100 };
        for (int i = 0; i < percentages.Length; i++)
        {
            int tier = i + 1;
            result.Add(Create("dash.speed." + tier, "질주 속도 +" + percentages[i] + "%", "Dash speed +" + percentages[i] + "%",
                "질주 중 이동 속도가 " + percentages[i] + "% 증가합니다.", "Dash increases movement speed by " + percentages[i] + "%.",
                tier * 5, PermanentUpgradeCategory.DashSpeed, PermanentUpgradeEffect.DashSpeed, 1f + percentages[i] / 100f,
                i == 0 ? null : "dash.speed." + i));
        }
        return result;
    }

    private static PermanentUpgradeDefinition CreateUnlock(string id, string ko, string en, int cost, PermanentUpgradeCategory category)
    {
        return Create(id, ko, en, "해금 후 해당 아이템이 게임 중 획득 목록에 등장할 수 있습니다.",
            "Allow this item to appear in gameplay acquisition pools.",
            cost, category, PermanentUpgradeEffect.UnlockItem, 0f, null);
    }

    private static PermanentUpgradeDefinition Create(string id, string ko, string en, string koDescription, string enDescription,
        int cost, PermanentUpgradeCategory category, PermanentUpgradeEffect effect, float value, string prerequisite)
    {
        return new PermanentUpgradeDefinition
        {
            id = id, koreanName = ko, englishName = en, koreanDescription = koDescription, englishDescription = enDescription,
            cost = cost, category = category, effect = effect, value = value, prerequisiteId = prerequisite
        };
    }
}
