#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LocalizationEditorTools
{
    public const string CsvPath = "Assets/Resources/Localization/Strings.csv";

    [MenuItem("Tools/LoveTrain/Localization/Import CSV")]
    public static void ImportCsv()
    {
        string path = EditorUtility.OpenFilePanel("Import English translations", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var rows = LocalizationCsv.Parse(File.ReadAllText(path, Encoding.UTF8));
            ValidateRows(rows);
            WriteCatalog(LocalizationCsv.Write(rows));
            AssetDatabase.ImportAsset(CsvPath, ImportAssetOptions.ForceUpdate);
            EnglishLocalization.Reload();
            Debug.Log($"[Localization] Imported {rows.Count} rows. Restart Play Mode to refresh all text.");
        }
        catch (Exception exception) when (exception is FormatException || exception is IOException)
        {
            Debug.LogError($"[Localization] Import rejected; existing CSV was kept. {exception.Message}");
        }
    }

    private static void WriteCatalog(string csv)
    {
        string directory = Path.GetDirectoryName(CsvPath);
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, ".Strings-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporaryPath, csv, new UTF8Encoding(true));
            if (File.Exists(CsvPath)) File.Replace(temporaryPath, CsvPath, null);
            else File.Move(temporaryPath, CsvPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    [MenuItem("Tools/LoveTrain/Localization/Export CSV")]
    public static void ExportCsv()
    {
        string path = EditorUtility.SaveFilePanel("Export translations for Excel", "", "LoveTrain-English", "csv");
        if (string.IsNullOrEmpty(path)) return;
        // Read before changing assets, so a malformed catalog cannot erase translations.
        var existing = LocalizationCsv.Parse(File.ReadAllText(CsvPath, Encoding.UTF8));
        var rows = existing.ToDictionary(row => row.Key, StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:SO_Event"))
        {
            SO_Event asset = AssetDatabase.LoadAssetAtPath<SO_Event>(AssetDatabase.GUIDToAssetPath(guid));
            EnsureEventKeys(asset);
            Merge(rows, asset.titleKey, asset.EventTitle, asset.name + " / title");
            Merge(rows, asset.textKey, asset.EventText, asset.name + " / body");
            if (asset.Selections == null) continue;
            foreach (SO_Event.Selection choice in asset.Selections)
            {
                Merge(rows, choice.selectionTextKey, choice.selectionText, asset.name + " / choice");
                Merge(rows, choice.selectionUnderTextKey, choice.selectionUnderText, asset.name + " / choice hint");
            }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Item_SO"))
        {
            Item_SO asset = AssetDatabase.LoadAssetAtPath<Item_SO>(AssetDatabase.GUIDToAssetPath(guid));
            bool changed = EnsureKey(ref asset.itemNameKey, asset.itemName, "item");
            changed |= EnsureKey(ref asset.itemDescriptionKey, asset.itemScript, "item");
            changed |= EnsureKey(ref asset.itemSimpleDescriptionKey, asset.itemSimpleScript, "item");
            if (changed) EditorUtility.SetDirty(asset);
            Merge(rows, asset.itemNameKey, asset.itemName, asset.name + " / name");
            Merge(rows, asset.itemDescriptionKey, asset.itemScript, asset.name + " / description");
            Merge(rows, asset.itemSimpleDescriptionKey, asset.itemSimpleScript, asset.name + " / summary");
        }
        foreach (string guid in AssetDatabase.FindAssets("t:GameEventSO"))
        {
            GameEventSO asset = AssetDatabase.LoadAssetAtPath<GameEventSO>(AssetDatabase.GUIDToAssetPath(guid));
            EnsureOutcomeKeys(asset);
            if (asset.rollGroups == null) continue;
            foreach (EventRollGroup group in asset.rollGroups)
            {
                if (group == null || group.outcomes == null) continue;
                foreach (WeightedEventOutcome outcome in group.outcomes)
                {
                    EventResultOutput output = outcome?.outputSettings;
                    if (output != null) Merge(rows, output.specialTextKey, output.specialText, asset.name + " / result");
                }
            }
        }
        AssetDatabase.SaveAssets();
        string csv = LocalizationCsv.Write(rows.Values.OrderBy(row => row.Key, StringComparer.Ordinal));
        ValidateRows(LocalizationCsv.Parse(csv));
        File.WriteAllText(path, csv, new UTF8Encoding(true));
        Debug.Log($"[Localization] Exported {rows.Count} rows. Edit en in Excel, save as CSV UTF-8, then Import CSV.");
    }

    public static void EnsureEventKeys(SO_Event asset)
    {
        if (asset == null) return;
        bool changed = EnsureKey(ref asset.titleKey, asset.EventTitle, "event");
        changed |= EnsureKey(ref asset.textKey, asset.EventText, "event");
        if (asset.Selections != null)
        {
            for (int i = 0; i < asset.Selections.Count; i++)
            {
                SO_Event.Selection choice = asset.Selections[i];
                changed |= EnsureTextId(ref choice.textId, "choice");
                changed |= EnsureKey(ref choice.selectionTextKey, choice.selectionText, "choice");
                changed |= EnsureKey(ref choice.selectionUnderTextKey, choice.selectionUnderText, "choice");
                asset.Selections[i] = choice;
                EnsureOutcomeKeys(choice.eventToTrigger);
            }
        }
        if (changed) EditorUtility.SetDirty(asset);
    }

    private static void EnsureOutcomeKeys(GameEventSO asset)
    {
        if (asset == null || asset.rollGroups == null) return;
        bool changed = false;
        foreach (EventRollGroup group in asset.rollGroups)
        {
            if (group == null || group.outcomes == null) continue;
            foreach (WeightedEventOutcome outcome in group.outcomes)
            {
                if (outcome != null) changed |= EnsureTextId(ref outcome.textId, "outcome");
                EventResultOutput output = outcome?.outputSettings;
                if (output != null) changed |= EnsureKey(ref output.specialTextKey, output.specialText, "outcome");
            }
        }
        if (changed) EditorUtility.SetDirty(asset);
    }

    private static bool EnsureTextId(ref string id, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(id)) return false;
        id = prefix + "_" + Guid.NewGuid().ToString("N");
        return true;
    }

    private static bool EnsureKey(ref string key, string source, string prefix)
    {
        if (!string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(source)) return false;
        key = prefix + "." + Guid.NewGuid().ToString("N");
        return true;
    }

    private static void Merge(Dictionary<string, LocalizationCsv.Row> rows, string key, string source, string context)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!rows.TryGetValue(key, out LocalizationCsv.Row row))
        {
            rows.Add(key, new LocalizationCsv.Row { Key = key, Korean = source ?? "", English = "", Context = context });
        }
        else if (Normalize(row.Korean) != Normalize(source))
        {
            row.Korean = source ?? "";
            row.English = "";
            row.Context = context + " / Source changed: translate again";
            Debug.LogWarning($"[Localization] Source changed for '{key}'; cleared the stale English translation in the export.");
        }
    }

    private static string Normalize(string value) => (value ?? "").Replace("\r\n", "\n");

    [MenuItem("Tools/LoveTrain/Localization/Validate CSV")]
    public static void ValidateCsv()
    {
        var rows = LocalizationCsv.Parse(File.ReadAllText(CsvPath, Encoding.UTF8));
        ValidateRows(rows);
        Debug.Log($"[Localization] Validated {rows.Count} rows; {rows.Count(row => string.IsNullOrWhiteSpace(row.English))} use Korean fallback.");
    }

    public static void ValidateRows(List<LocalizationCsv.Row> rows)
    {
        EventTemplateValidation.Validate(rows);
        var keys = new HashSet<string>(rows.Select(row => row.Key), StringComparer.Ordinal);
        var fieldPattern = new Regex(@"^[ \t]*(?:titleKey|textKey|selectionTextKey|selectionUnderTextKey|itemNameKey|itemDescriptionKey|itemSimpleDescriptionKey|specialTextKey|localizationKey|creditKey):[ \t]*(.*?)[ \t\r]*$", RegexOptions.Multiline);
        var callPattern = new Regex(@"EnglishLocalization\.(?:Get|Format)\(\s*""([^""]+)""");
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
            string extension = Path.GetExtension(path);
            bool serialized = extension == ".asset" || extension == ".prefab" || extension == ".unity";
            if (!serialized && extension != ".cs") continue;
            if (extension == ".cs" && path.Contains("/Editor/")) continue;
            if (path.StartsWith("Assets/Plugins/", StringComparison.Ordinal)) continue;
            string text = File.ReadAllText(path);
            foreach (Match match in (serialized ? fieldPattern : callPattern).Matches(text))
            {
                string key = match.Groups[1].Value.Trim().Trim('"');
                if (key.Length > 0 && !keys.Contains(key)) throw new FormatException($"Missing key '{key}' referenced by {path}.");
            }
        }
    }
}

public sealed class LocalizationBuildValidation : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        try { LocalizationEditorTools.ValidateCsv(); }
        catch (Exception exception) when (exception is FormatException || exception is IOException)
        {
            throw new BuildFailedException("Translation CSV validation failed: " + exception.Message);
        }
    }
}
#endif
