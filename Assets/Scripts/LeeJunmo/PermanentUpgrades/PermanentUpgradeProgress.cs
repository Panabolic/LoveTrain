using System;
using System.Collections.Generic;
using UnityEngine;

public static class PermanentUpgradeProgress
{
    public const string SoulSaveKey = "LoveTrain.Progression.Souls.v1";
    public const string SnapshotSaveKey = "LoveTrain.Progression.PermanentUpgrades.v1";
    private const int SaveVersion = 1;
    private static readonly HashSet<string> purchasedIds = new HashSet<string>(StringComparer.Ordinal);
    private static int souls;
    private static bool loaded;
    private static bool saveAvailable = true;
    private static string saveError;

    [Serializable]
    private sealed class SaveData
    {
        public int version;
        public int souls;
        public string[] purchasedIds;
    }

    public static int Souls { get { EnsureLoaded(); return souls; } }
    public static bool IsSaveAvailable { get { EnsureLoaded(); return saveAvailable; } }
    public static string SaveError { get { EnsureLoaded(); return saveError; } }
    public static float FuelCapacityBonus => HighestValue(PermanentUpgradeEffect.FuelCapacity);
    public static float BaseGunDamageBonus => SumValues(PermanentUpgradeEffect.BaseGunDamage);
    public static float DashDurationOverride => HighestValue(PermanentUpgradeEffect.DashDuration);
    public static float DashSpeedMultiplierOverride => HighestValue(PermanentUpgradeEffect.DashSpeed);
    public static int UltimateCharges => (int)HighestValue(PermanentUpgradeEffect.UltimateCharges);

    public static void Configure(PermanentUpgradeCatalog catalog) => PermanentUpgradeCatalog.Configure(catalog);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        loaded = false;
        souls = 0;
        saveAvailable = true;
        saveError = null;
        purchasedIds.Clear();
    }

    public static void Reload()
    {
        loaded = true;
        souls = Math.Max(0, PlayerPrefs.GetInt(SoulSaveKey, 0));
        purchasedIds.Clear();
        saveAvailable = true;
        saveError = null;
        if (!PlayerPrefs.HasKey(SnapshotSaveKey)) return;

        try
        {
            // Overwrite retains sentinels for omitted fields, unlike FromJson's zero defaults.
            var data = new SaveData { souls = -1 };
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(SnapshotSaveKey), data);
            if (data.version != SaveVersion || data.souls < 0 || data.purchasedIds == null)
                throw new FormatException("Unsupported or incomplete permanent upgrade save.");
            foreach (string id in data.purchasedIds)
            {
                if (string.IsNullOrWhiteSpace(id)) throw new FormatException("Permanent upgrade save contains an empty identifier.");
                purchasedIds.Add(id);
            }
            souls = data.souls;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is FormatException)
        {
            // Keep the original data intact; never replace an unreadable save with a fresh profile.
            purchasedIds.Clear();
            saveAvailable = false;
            saveError = exception.Message;
            Debug.LogWarning("[PermanentUpgrades] Save could not be loaded. Purchases are disabled and the original save is preserved. " + saveError);
        }
    }

    public static bool IsPurchased(string id)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(id) && purchasedIds.Contains(id);
    }

    public static PermanentUpgradePurchaseStatus GetPurchaseStatus(PermanentUpgradeDefinition definition)
    {
        EnsureLoaded();
        if (definition == null || !PermanentUpgradeCatalog.Load().Contains(definition)) return PermanentUpgradePurchaseStatus.Unconfigured;
        if (IsPurchased(definition.Id)) return PermanentUpgradePurchaseStatus.Purchased;
        if (!saveAvailable || !definition.IsConfigured || !HasValidPrerequisiteChain(definition)) return PermanentUpgradePurchaseStatus.Unconfigured;
        if (!string.IsNullOrEmpty(definition.PrerequisiteId) && !IsPurchased(definition.PrerequisiteId)) return PermanentUpgradePurchaseStatus.PrerequisiteMissing;
        return souls >= definition.Cost ? PermanentUpgradePurchaseStatus.Available : PermanentUpgradePurchaseStatus.InsufficientSouls;
    }

    public static bool TryPurchase(PermanentUpgradeDefinition definition, out PermanentUpgradePurchaseStatus status)
    {
        status = GetPurchaseStatus(definition);
        if (status != PermanentUpgradePurchaseStatus.Available) return false;
        int previousSouls = souls;
        souls -= definition.Cost;
        purchasedIds.Add(definition.Id);
        if (!WriteSnapshot(true))
        {
            souls = previousSouls;
            purchasedIds.Remove(definition.Id);
            status = PermanentUpgradePurchaseStatus.Unconfigured;
            return false;
        }
        status = PermanentUpgradePurchaseStatus.Purchased;
        return true;
    }

    // Run rewards/rerolls already own their amount. Keep the single persistent wallet synchronized.
    // Existing pause/quit saves flush these writes; purchases explicitly flush their transaction.
    public static void SetSouls(int amount)
    {
        EnsureLoaded();
        int previousSouls = souls;
        souls = Math.Max(0, amount);
        if (!saveAvailable)
        {
            PlayerPrefs.SetInt(SoulSaveKey, souls);
            return;
        }
        if (!WriteSnapshot(false)) souls = previousSouls;
    }

    public static bool IsItemUnlocked(Item_SO item)
    {
        if (item == null) return false;
        EnsureLoaded();
        foreach (PermanentUpgradeDefinition definition in PermanentUpgradeCatalog.Load().Upgrades)
            if (definition != null && definition.MatchesItem(item) && !IsPurchased(definition.Id)) return false;
        return true;
    }

    private static bool HasValidPrerequisiteChain(PermanentUpgradeDefinition definition)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { definition.Id };
        string prerequisite = definition.PrerequisiteId;
        PermanentUpgradeCatalog catalog = PermanentUpgradeCatalog.Load();
        while (!string.IsNullOrEmpty(prerequisite))
        {
            if (!seen.Add(prerequisite)) return false;
            PermanentUpgradeDefinition preceding = catalog.Find(prerequisite);
            if (preceding == null || !catalog.Contains(preceding)) return false;
            prerequisite = preceding.PrerequisiteId;
        }
        return true;
    }

    private static float HighestValue(PermanentUpgradeEffect effect)
    {
        EnsureLoaded();
        float result = 0f;
        foreach (PermanentUpgradeDefinition definition in PermanentUpgradeCatalog.Load().Upgrades)
            if (definition != null && definition.Effect == effect && definition.IsConfigured && IsPurchased(definition.Id))
                result = Mathf.Max(result, definition.Value);
        return result;
    }

    private static float SumValues(PermanentUpgradeEffect effect)
    {
        EnsureLoaded();
        float result = 0f;
        foreach (PermanentUpgradeDefinition definition in PermanentUpgradeCatalog.Load().Upgrades)
            if (definition != null && definition.Effect == effect && definition.IsConfigured && IsPurchased(definition.Id))
                result += definition.Value;
        return result;
    }

    private static bool WriteSnapshot(bool flush)
    {
        var ids = new string[purchasedIds.Count];
        purchasedIds.CopyTo(ids);
        Array.Sort(ids, StringComparer.Ordinal);
        string json = JsonUtility.ToJson(new SaveData { version = SaveVersion, souls = souls, purchasedIds = ids });
        bool hadSnapshot = PlayerPrefs.HasKey(SnapshotSaveKey);
        string previousSnapshot = PlayerPrefs.GetString(SnapshotSaveKey);
        int previousSoulMirror = PlayerPrefs.GetInt(SoulSaveKey, 0);
        try
        {
            PlayerPrefs.SetString(SnapshotSaveKey, json);
            PlayerPrefs.SetInt(SoulSaveKey, souls);
            if (flush) PlayerPrefs.Save();
            return true;
        }
        catch (PlayerPrefsException exception)
        {
            saveAvailable = false;
            saveError = exception.Message;
            if (hadSnapshot) PlayerPrefs.SetString(SnapshotSaveKey, previousSnapshot);
            else PlayerPrefs.DeleteKey(SnapshotSaveKey);
            PlayerPrefs.SetInt(SoulSaveKey, previousSoulMirror);
            Debug.LogError("[PermanentUpgrades] Purchase/save failed. " + saveError);
            return false;
        }
    }

    private static void EnsureLoaded()
    {
        if (!loaded) Reload();
    }
}
