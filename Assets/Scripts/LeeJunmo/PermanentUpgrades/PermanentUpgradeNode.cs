using System;
using System.Collections.Generic;
using System.Globalization;

// A node groups presentation only; purchases and saves retain the original stage IDs.
public sealed class PermanentUpgradeNode
{
    private readonly IReadOnlyList<PermanentUpgradeDefinition> stages;

    private PermanentUpgradeNode(string id, PermanentUpgradeCategory category, List<PermanentUpgradeDefinition> definitions)
    {
        Id = id;
        Category = category;
        stages = definitions.AsReadOnly();
    }

    public string Id { get; }
    public PermanentUpgradeCategory Category { get; }
    public IReadOnlyList<PermanentUpgradeDefinition> Stages => stages;

    public string Name
    {
        get
        {
            bool english = EnglishLocalization.IsEnglish;
            switch (Id)
            {
                case "fuel": return english ? "Maximum fuel" : "연료 최대치";
                case "damage": return english ? "Base gun damage" : "기본총기 데미지";
                case "weapon.laser": return english ? "Laser gun" : "레이저 총기";
                case "weapon.buckshot": return english ? "Shotgun" : "산탄총";
                case "weapon.machinegun": return english ? "Machine gun" : "기관총";
                case "boss.train": return english ? "Train boss item" : "열차보스 아이템";
                case "boss.tentacle": return english ? "Tentacle boss item" : "촉수보스 아이템";
                case "ultimate": return english ? "Ultimate" : "필살기";
                case "dash.duration": return english ? "Dash duration" : "질주 시간";
                case "dash.speed": return english ? "Dash speed" : "질주 속도";
                default: return stages.Count > 0 ? stages[0].Name : Id;
            }
        }
    }

    public int CompletedCount
    {
        get
        {
            int count = 0;
            foreach (PermanentUpgradeDefinition stage in stages)
                if (PermanentUpgradeProgress.IsPurchased(stage.Id)) count++;
            return count;
        }
    }

    public PermanentUpgradeDefinition NextStage
    {
        get
        {
            foreach (PermanentUpgradeDefinition stage in stages)
                if (!PermanentUpgradeProgress.IsPurchased(stage.Id)) return stage;
            return null;
        }
    }

    public bool IsComplete => stages.Count > 0 && NextStage == null;
    public PermanentUpgradePurchaseStatus PurchaseStatus => IsComplete
        ? PermanentUpgradePurchaseStatus.Purchased : PermanentUpgradeProgress.GetPurchaseStatus(NextStage);
    public string NextDescription => NextStage != null ? NextStage.Description : string.Empty;

    public string CurrentDescription
    {
        get
        {
            PermanentUpgradeDefinition latest = null;
            float damageBonus = 0f;
            foreach (PermanentUpgradeDefinition stage in stages)
            {
                if (!PermanentUpgradeProgress.IsPurchased(stage.Id)) continue;
                latest = stage;
                if (stage.Effect == PermanentUpgradeEffect.BaseGunDamage && stage.IsConfigured)
                    damageBonus += stage.Value;
            }

            if (latest == null)
                return EnglishLocalization.IsEnglish ? "No upgrades purchased yet." : "아직 강화하지 않았습니다.";
            if (Category == PermanentUpgradeCategory.BaseGun)
            {
                string value = damageBonus.ToString("0.##", CultureInfo.InvariantCulture);
                return EnglishLocalization.IsEnglish ? "Base gun damage increases by " + value + "."
                    : "기본 총기의 공격력이 총 " + value + " 증가합니다.";
            }
            return latest.Description;
        }
    }

    public static List<PermanentUpgradeNode> Build(PermanentUpgradeCatalog catalog)
    {
        var result = new List<PermanentUpgradeNode>();
        if (catalog == null) return result;
        var groups = new Dictionary<string, List<PermanentUpgradeDefinition>>(StringComparer.Ordinal);
        var categories = new Dictionary<string, PermanentUpgradeCategory>(StringComparer.Ordinal);
        var order = new List<string>();
        var stageIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (PermanentUpgradeDefinition definition in catalog.Upgrades)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || !stageIds.Add(definition.Id)) continue;
            string id = NodeId(definition);
            if (!groups.TryGetValue(id, out List<PermanentUpgradeDefinition> group))
            {
                group = new List<PermanentUpgradeDefinition>();
                groups.Add(id, group);
                categories.Add(id, definition.Category);
                order.Add(id);
            }
            group.Add(definition);
        }
        foreach (string id in order)
            result.Add(new PermanentUpgradeNode(id, categories[id], groups[id]));
        return result;
    }

    private static string NodeId(PermanentUpgradeDefinition definition)
    {
        switch (definition.Category)
        {
            case PermanentUpgradeCategory.Fuel: return "fuel";
            case PermanentUpgradeCategory.BaseGun: return "damage";
            case PermanentUpgradeCategory.Ultimate: return "ultimate";
            case PermanentUpgradeCategory.DashDuration: return "dash.duration";
            case PermanentUpgradeCategory.DashSpeed: return "dash.speed";
            default: return definition.Id;
        }
    }
}
