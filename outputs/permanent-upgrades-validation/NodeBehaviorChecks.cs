using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;

internal static partial class BehaviorChecks
{
    private static PermanentUpgradeNode Node(PermanentUpgradeCatalog catalog, string id) => PermanentUpgradeNode.Build(catalog).Single(node => node.Id == id);
    private static void NodeChecks()
    {
        var catalog = Fresh(100);
        var nodes = PermanentUpgradeNode.Build(catalog);
        var expectedCounts = new Dictionary<string, int>
        {
            { "fuel", 5 }, { "damage", 5 }, { "weapon.laser", 1 }, { "weapon.buckshot", 1 },
            { "weapon.machinegun", 1 }, { "boss.train", 1 }, { "boss.tentacle", 1 }, { "ultimate", 3 },
            { "dash.duration", 3 }, { "dash.speed", 3 }
        };
        Check(nodes.Count == 10 && nodes.Select(node => node.Id).Distinct().Count() == 10, "ten distinct presentation nodes");
        Check(nodes.SelectMany(node => node.Stages).Count() == 24, "all twenty-four original stages mapped once");
        Check(nodes.SelectMany(node => node.Stages).Select(stage => stage.Id).OrderBy(id => id).SequenceEqual(catalog.Upgrades.Select(stage => stage.Id).OrderBy(id => id)), "node presentation preserves original save identifiers");
        foreach (var expected in expectedCounts)
        {
            var node = nodes.Single(candidate => candidate.Id == expected.Key);
            Check(node.Stages.Count == expected.Value, expected.Key + " stage count matches design");
            Check(node.CompletedCount == 0 && !node.IsComplete && node.NextStage != null, expected.Key + " starts incomplete at one next stage");
            Check(node.Stages.All(stage => ReferenceEquals(stage, catalog.Find(stage.Id))), expected.Key + " requests authoritative catalog stage objects");
        }
        Check(PermanentUpgradeNode.Build(null).Count == 0, "missing catalog has no fabricated nodes");

        var fuel = nodes.Single(node => node.Id == "fuel");
        Check(fuel.NextStage.Id == "fuel.1" && fuel.PurchaseStatus == PermanentUpgradePurchaseStatus.Available, "fuel node offers first stage");
        Check(PermanentUpgradeProgress.TryPurchase(fuel.NextStage, out _), "node next-stage purchase accepted");
        Check(PermanentUpgradeProgress.Souls == 95 && fuel.CompletedCount == 1 && fuel.NextStage.Id == "fuel.2", "one click spends only five and advances exactly one stage");
        Check(!PermanentUpgradeProgress.IsPurchased("fuel.2") && !PermanentUpgradeProgress.IsPurchased("fuel.5"), "one next-stage purchase does not bulk-buy later stages");
        Check(fuel.CurrentDescription.Contains("10") && fuel.NextDescription.Contains("20"), "node distinguishes current and next tier effects");
        for (int tier = 2; tier <= 5; tier++)
        {
            Check(fuel.NextStage.Id == "fuel." + tier, "next tier progresses in authored order");
            Check(PermanentUpgradeProgress.TryPurchase(fuel.NextStage, out _), "subsequent node stage purchased once");
        }
        Check(fuel.IsComplete && fuel.CompletedCount == 5 && fuel.NextStage == null && fuel.NextDescription == string.Empty, "completed node has five filled stages and no next effect");
        Check(fuel.PurchaseStatus == PermanentUpgradePurchaseStatus.Purchased && PermanentUpgradeProgress.Souls == 25 && PermanentUpgradeProgress.FuelCapacityBonus == 100f, "completed fuel node reports final total with original cumulative cost");
        string completeSnapshot = Snapshot();
        Check(!PermanentUpgradeProgress.TryPurchase(fuel.NextStage, out _) && Snapshot() == completeSnapshot && PermanentUpgradeProgress.Souls == 25, "completed node cannot debit another purchase");
        using (var saved = JsonDocument.Parse(completeSnapshot))
        {
            var ids = saved.RootElement.GetProperty("purchasedIds").EnumerateArray().Select(value => value.GetString()).ToArray();
            Check(ids.Length == 5 && ids.All(id => id.StartsWith("fuel.", StringComparison.Ordinal)) && !ids.Contains("fuel"), "save stores original five stage IDs rather than presentation node ID");
        }
        ResetProgress();
        fuel = Node(catalog, "fuel");
        Check(fuel.IsComplete && fuel.CompletedCount == 5 && PermanentUpgradeProgress.Souls == 25, "rebuilt nodes restore completed state and wallet from stage save");

        catalog = Fresh(4);
        fuel = Node(catalog, "fuel");
        string insufficientSnapshot = Snapshot();
        Check(fuel.PurchaseStatus == PermanentUpgradePurchaseStatus.InsufficientSouls, "node exposes insufficient next-stage balance");
        Check(!PermanentUpgradeProgress.TryPurchase(fuel.NextStage, out _) && fuel.CompletedCount == 0 && fuel.NextStage.Id == "fuel.1", "insufficient node purchase preserves next stage");
        Check(PermanentUpgradeProgress.Souls == 4 && Snapshot() == insufficientSnapshot, "insufficient node purchase preserves wallet and save");
        PermanentUpgradeProgress.SetSouls(5);
        Check(fuel.PurchaseStatus == PermanentUpgradePurchaseStatus.Available && PermanentUpgradeProgress.TryPurchase(fuel.NextStage, out _), "same node becomes purchasable after soul balance changes");
        Check(fuel.CompletedCount == 1 && fuel.NextStage.Id == "fuel.2" && fuel.PurchaseStatus == PermanentUpgradePurchaseStatus.InsufficientSouls, "successful single purchase refreshes next-stage affordability");

        catalog = Fresh(100);
        var damage = Node(catalog, "damage");
        Check(damage.PurchaseStatus == PermanentUpgradePurchaseStatus.Unconfigured && damage.CompletedCount == 0, "undecided damage node cannot consume currency");
        catalog.Find("damage.1").value = 2;
        catalog.Find("damage.2").value = 3;
        Check(PermanentUpgradeProgress.TryPurchase(damage.NextStage, out _) && PermanentUpgradeProgress.TryPurchase(damage.NextStage, out _), "configured damage node buys individual increments");
        Check(damage.CompletedCount == 2 && damage.CurrentDescription.Contains("5") && damage.NextStage.Id == "damage.3", "damage tooltip exposes cumulative purchased bonus with next unconfigured stage");
        Check(damage.PurchaseStatus == PermanentUpgradePurchaseStatus.Unconfigured, "node keeps further undecided damage tiers locked");
        EnglishLocalization.IsEnglish = true;
        Check(damage.Name == "Base gun damage" && damage.CurrentDescription.Contains("5"), "node titles and cumulative description resolve English language");
        EnglishLocalization.IsEnglish = false;

        var laser = Node(catalog, "weapon.laser");
        Check(PermanentUpgradeProgress.TryPurchase(laser.NextStage, out _) && laser.IsComplete && laser.CompletedCount == 1 && laser.NextStage == null, "single-stage unlock node completes after one purchase");

        catalog = Fresh(40);
        PlayerPrefs.SetString(PermanentUpgradeProgress.SnapshotSaveKey, "{\"version\":1,\"souls\":40,\"purchasedIds\":[\"fuel.1\",\"fuel.3\",\"future.stage\"]}");
        PermanentUpgradeProgress.Reload();
        fuel = Node(catalog, "fuel");
        Check(fuel.CompletedCount == 2 && fuel.NextStage.Id == "fuel.2", "existing partial stage save remains visible without discarding IDs");
        Check(PermanentUpgradeProgress.TryPurchase(fuel.NextStage, out _) && fuel.NextStage.Id == "fuel.4" && fuel.CompletedCount == 3, "next-stage purchase fills earliest gap and preserves already owned tier");
        using (var saved = JsonDocument.Parse(Snapshot()))
            Check(saved.RootElement.GetProperty("purchasedIds").EnumerateArray().Any(value => value.GetString() == "future.stage"), "node purchase retains unknown original save identifiers");

        catalog = Fresh();
        var custom = new PermanentUpgradeDefinition { id = "weapon.future", koreanName = "미래 무기", category = PermanentUpgradeCategory.Weapon, effect = PermanentUpgradeEffect.UnlockItem, cost = 5, unlockedItem = new Item_SO() };
        var customEntries = new List<PermanentUpgradeDefinition> { null, new PermanentUpgradeDefinition(), custom, custom };
        typeof(PermanentUpgradeCatalog).GetField("upgrades", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog, customEntries);
        nodes = PermanentUpgradeNode.Build(catalog);
        Check(nodes.Count == 1 && nodes[0].Id == "weapon.future" && nodes[0].Stages.Count == 1, "presentation handles null blank and duplicate entries while retaining unknown weapon ID");
        Check(customEntries.Count == 4 && nodes[0].Name == "미래 무기", "building nodes does not mutate catalog or invent custom display names");
    }
}
