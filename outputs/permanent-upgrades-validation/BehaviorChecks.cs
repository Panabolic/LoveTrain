using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;

internal static partial class BehaviorChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException("FAIL: " + message);
    }
    private static void ResetProgress() => typeof(PermanentUpgradeProgress).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    private static PermanentUpgradeCatalog Fresh(int souls = 0)
    {
        PlayerPrefs.Clear();
        PlayerPrefs.SetInt(PermanentUpgradeProgress.SoulSaveKey, souls);
        ResetProgress();
        var catalog = ScriptableObject.CreateInstance<PermanentUpgradeCatalog>();
        PermanentUpgradeProgress.Configure(catalog);
        PermanentUpgradeProgress.Reload();
        return catalog;
    }
    private static PermanentUpgradePurchaseStatus Status(PermanentUpgradeCatalog catalog, string id) => PermanentUpgradeProgress.GetPurchaseStatus(catalog.Find(id));
    private static void Buy(PermanentUpgradeCatalog catalog, string id)
    {
        Check(PermanentUpgradeProgress.TryPurchase(catalog.Find(id), out var result), id + " purchase succeeds");
        Check(result == PermanentUpgradePurchaseStatus.Purchased, id + " status purchased");
    }
    private static string Snapshot() => PlayerPrefs.GetString(PermanentUpgradeProgress.SnapshotSaveKey);
    private static void RejectSave(string raw, string name)
    {
        var catalog = Fresh(37);
        PlayerPrefs.SetString(PermanentUpgradeProgress.SnapshotSaveKey, raw);
        PermanentUpgradeProgress.Reload();
        Check(!PermanentUpgradeProgress.IsSaveAvailable, name + " blocked");
        Check(!string.IsNullOrEmpty(PermanentUpgradeProgress.SaveError), name + " reason reported");
        Check(!PermanentUpgradeProgress.TryPurchase(catalog.Find("fuel.1"), out _), name + " purchase rejected");
        PermanentUpgradeProgress.SetSouls(40);
        Check(Snapshot() == raw, name + " original snapshot preserved");
        Check(PlayerPrefs.GetInt(PermanentUpgradeProgress.SoulSaveKey) == 40, name + " run soul mirror remains usable");
    }
    private static void Main()
    {
        var localizedRows = LocalizationCsv.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Strings.csv")));
        string[] requiredTranslationKeys = { "permanent.title", "permanent.cost", "permanent.balance", "permanent.hint", "permanent.start", "permanent.saved", "permanent.save_error", "permanent.purchase_done", "permanent.tooltip_cost", "permanent.available", "permanent.purchased", "permanent.prerequisite", "permanent.insufficient", "permanent.unconfigured", "permanent.progress", "permanent.current", "permanent.next", "permanent.none", "permanent.max" };
        Check(localizedRows.Count > 0, "actual localization CSV passes production parser without duplicate keys or token mismatch");
        foreach (string translationKey in requiredTranslationKeys)
            Check(localizedRows.Count(row => row.Key == translationKey && !string.IsNullOrWhiteSpace(row.Korean) && !string.IsNullOrWhiteSpace(row.English)) == 1, translationKey + " has one complete Korean/English row");
        var catalog = Fresh(100);
        Check(catalog.Upgrades.Count == 24, "all planned upgrade cards available");
        Check(catalog.Upgrades.Select(x => x.Id).Distinct().Count() == 24, "stable unique default IDs");
        Check(PermanentUpgradeProgress.Souls == 100 && PermanentUpgradeProgress.IsSaveAvailable, "legacy wallet read without lost souls");
        Check(!PlayerPrefs.HasKey(PermanentUpgradeProgress.SnapshotSaveKey), "legacy read does not mutate save");
        Check(Status(catalog, "fuel.1") == PermanentUpgradePurchaseStatus.Available, "first tier available");
        Check(Status(catalog, "fuel.2") == PermanentUpgradePurchaseStatus.PrerequisiteMissing, "later tier requires predecessor");
        Check(Status(catalog, "damage.1") == PermanentUpgradePurchaseStatus.Unconfigured, "undecided damage cannot consume currency");
        Check(Status(catalog, "weapon.buckshot") == PermanentUpgradePurchaseStatus.Unconfigured, "unbound weapon cannot consume currency");
        Check(Status(catalog, "ultimate.1") == PermanentUpgradePurchaseStatus.Unconfigured, "unimplemented ultimate cannot consume currency");
        var laser = new LaserGun_SO();
        var ordinary = new Item_SO();
        Check(!PermanentUpgradeProgress.IsItemUnlocked(laser), "laser locked before unlock");
        Check(PermanentUpgradeProgress.IsItemUnlocked(ordinary), "unrelated item remains available");
        Check(!PermanentUpgradeProgress.TryPurchase(catalog.Find("fuel.2"), out _) && PermanentUpgradeProgress.Souls == 100, "skipped prerequisite purchase preserves balance");
        Buy(catalog, "fuel.1");
        Check(PermanentUpgradeProgress.Souls == 95 && PermanentUpgradeProgress.FuelCapacityBonus == 10f, "first fuel purchase applies and spends once");
        string purchasedSnapshot = Snapshot();
        Check(!PermanentUpgradeProgress.TryPurchase(catalog.Find("fuel.1"), out var already) && already == PermanentUpgradePurchaseStatus.Purchased, "duplicate purchase rejected");
        Check(PermanentUpgradeProgress.Souls == 95 && Snapshot() == purchasedSnapshot, "duplicate does not charge or write");
        Buy(catalog, "fuel.2");
        Check(PermanentUpgradeProgress.FuelCapacityBonus == 20f, "fuel tiers replace previous total rather than adding");
        Buy(catalog, "weapon.laser");
        Check(PermanentUpgradeProgress.IsItemUnlocked(laser), "laser available after unlock");
        ResetProgress();
        Check(PermanentUpgradeProgress.Souls == 80 && PermanentUpgradeProgress.IsPurchased("fuel.2") && PermanentUpgradeProgress.IsItemUnlocked(laser), "reload restores wallet, tiers and item unlock");
        Check(PlayerPrefs.GetInt(PermanentUpgradeProgress.SoulSaveKey) == 80, "legacy mirror synchronized after purchases");

        catalog.Find("damage.1").value = 2;
        catalog.Find("damage.2").value = 3;
        Buy(catalog, "damage.1");
        Buy(catalog, "damage.2");
        Check(PermanentUpgradeProgress.BaseGunDamageBonus == 5f, "configured damage tiers sum authored increments");
        Buy(catalog, "dash.duration.1");
        Buy(catalog, "dash.duration.2");
        Check(PermanentUpgradeProgress.DashDurationOverride == 4f, "dash duration uses purchased total");
        Buy(catalog, "dash.speed.1");
        Check(Math.Abs(PermanentUpgradeProgress.DashSpeedMultiplierOverride - 1.6f) < .0001f, "dash multiplier supplied to runtime");
        var boss = new GiantMaw_SO();
        catalog.Find("boss.train").unlockedItem = boss;
        Check(!PermanentUpgradeProgress.IsItemUnlocked(boss) && PermanentUpgradeProgress.IsItemUnlocked(new GiantMaw_SO()), "explicit bound asset locks only its own item");
        Buy(catalog, "boss.train");
        Check(PermanentUpgradeProgress.IsItemUnlocked(boss), "bound boss item available after purchase");
        Check(PlayerPrefs.Saves == 9, "every accepted purchase flushed once");
        PermanentUpgradeProgress.SetSouls(91);
        Check(PlayerPrefs.Saves == 9, "run resource changes defer disk flush");
        ResetProgress();
        Check(PermanentUpgradeProgress.Souls == 91 && PermanentUpgradeProgress.IsPurchased("boss.train"), "run wallet synchronization preserves upgrades");

        catalog = Fresh(4);
        string before = Snapshot();
        Check(Status(catalog, "fuel.1") == PermanentUpgradePurchaseStatus.InsufficientSouls, "insufficient soul status");
        Check(!PermanentUpgradeProgress.TryPurchase(catalog.Find("fuel.1"), out _) && PermanentUpgradeProgress.Souls == 4 && Snapshot() == before, "insufficient funds purchase atomic");
        Check(PermanentUpgradeProgress.GetPurchaseStatus(new PermanentUpgradeDefinition { id = "fuel.1", cost = 1, value = 100 }) == PermanentUpgradePurchaseStatus.Unconfigured, "external definition cannot spoof catalog cost");
        catalog.Find("fuel.1").prerequisiteId = "fuel.2";
        Check(Status(catalog, "fuel.1") == PermanentUpgradePurchaseStatus.Unconfigured, "prerequisite cycle rejected");
        catalog.Find("fuel.1").prerequisiteId = "missing.id";
        Check(Status(catalog, "fuel.1") == PermanentUpgradePurchaseStatus.Unconfigured, "missing prerequisite rejected");
        catalog.Find("fuel.1").prerequisiteId = null;
        catalog.Find("fuel.1").cost = 0;
        Check(Status(catalog, "fuel.1") == PermanentUpgradePurchaseStatus.Unconfigured, "zero cost misconfiguration rejected");
        catalog.Find("fuel.1").cost = 5;
        catalog.Find("fuel.1").value = float.NaN;
        Check(Status(catalog, "fuel.1") == PermanentUpgradePurchaseStatus.Unconfigured, "nonfinite stat rejected");

        catalog = Fresh(90);
        PlayerPrefs.SetString(PermanentUpgradeProgress.SnapshotSaveKey, "{\"version\":1,\"souls\":70,\"purchasedIds\":[\"future.special\",\"fuel.1\",\"fuel.1\"]}");
        PermanentUpgradeProgress.Reload();
        Check(PermanentUpgradeProgress.Souls == 70 && PermanentUpgradeProgress.FuelCapacityBonus == 10f, "snapshot is authority and duplicate save IDs do not double stats");
        Buy(catalog, "fuel.2");
        using (var saved = JsonDocument.Parse(Snapshot()))
        {
            var ids = saved.RootElement.GetProperty("purchasedIds").EnumerateArray().Select(x => x.GetString()).ToArray();
            Check(ids.Contains("future.special"), "unknown future IDs retained when saving");
            Check(ids.Count(x => x == "fuel.1") == 1, "duplicate IDs normalized on successful write");
        }
        PermanentUpgradeProgress.SetSouls(-9);
        ResetProgress();
        Check(PermanentUpgradeProgress.Souls == 0 && PermanentUpgradeProgress.IsPurchased("fuel.2"), "negative resource set clamps without losing upgrades");

        RejectSave("not valid json", "malformed save");
        RejectSave("{}", "empty object save");
        RejectSave("{\"version\":2,\"souls\":50,\"purchasedIds\":[]}", "future version save");
        RejectSave("{\"version\":1,\"souls\":-1,\"purchasedIds\":[]}", "negative balance save");
        RejectSave("{\"version\":1,\"souls\":50}", "missing purchase list save");
        RejectSave("{\"version\":1,\"purchasedIds\":[]}", "missing wallet save");
        RejectSave("{\"version\":1,\"souls\":50,\"purchasedIds\":[\" \"]}", "empty identifier save");

        catalog = Fresh(20);
        PermanentUpgradeProgress.SetSouls(20);
        before = Snapshot();
        PlayerPrefs.FailSave = true;
        Check(!PermanentUpgradeProgress.TryPurchase(catalog.Find("fuel.1"), out _), "failed disk flush rejects purchase");
        Check(PermanentUpgradeProgress.Souls == 20 && !PermanentUpgradeProgress.IsPurchased("fuel.1"), "failed flush rolls back in-memory spend and upgrade");
        Check(Snapshot() == before && PlayerPrefs.GetInt(PermanentUpgradeProgress.SoulSaveKey) == 20, "failed flush rolls back both PlayerPrefs values");
        Check(!PermanentUpgradeProgress.IsSaveAvailable, "failed save reports blocked persistence");
        PlayerPrefs.FailSave = false;
        PermanentUpgradeProgress.Reload();
        Check(PermanentUpgradeProgress.IsSaveAvailable && PermanentUpgradeProgress.Souls == 20, "reload recovers preserved snapshot");
        Buy(catalog, "fuel.1");

        NodeChecks();
        Console.WriteLine("PASS: " + checks + " isolated catalog, node, purchase, unlock, persistence and production CSV assertions. Unity test doubles only; no Unity runtime exercised.");
    }
}
