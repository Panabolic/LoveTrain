#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

public static class EventTemplateValidation
{
    public static void Validate(List<LocalizationCsv.Row> rows)
    {
        var catalog = rows.ToDictionary(row => row.Key, StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:SO_Event"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<SO_Event>(AssetDatabase.GUIDToAssetPath(guid));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (asset.Selections != null)
                foreach (var choice in asset.Selections)
                {
                    Id(ids, choice.textId, asset.name);
                    Check(catalog, choice.selectionTextKey, choice.selectionText, asset.name, choice.eventToTrigger, null);
                    Check(catalog, choice.selectionUnderTextKey, choice.selectionUnderText, asset.name, choice.eventToTrigger, null);
                }
            Check(catalog, asset.titleKey, asset.EventTitle, asset.name, null, asset);
            Check(catalog, asset.textKey, asset.EventText, asset.name, null, asset);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:GameEventSO"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameEventSO>(AssetDatabase.GUIDToAssetPath(guid));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (asset.rollGroups == null) continue;
            foreach (var group in asset.rollGroups)
            {
                if (group?.outcomes == null) continue;
                foreach (var outcome in group.outcomes)
                {
                    if (outcome == null) continue;
                    Id(ids, outcome.textId, asset.name);
                    if (float.IsNaN(outcome.weight) || float.IsInfinity(outcome.weight) || outcome.weight < 0)
                        throw new FormatException(asset.name + ": weights must be finite and non-negative.");
                    if (outcome.outputSettings != null)
                        Check(catalog, outcome.outputSettings.specialTextKey, outcome.outputSettings.specialText, asset.name, asset, null);
                }
            }
        }
    }

    private static void Id(HashSet<string> ids, string id, string context)
    {
        if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, @"^[A-Za-z_][A-Za-z0-9_]*$") || !ids.Add(id))
            throw new FormatException(context + ": missing, invalid or duplicate text ID '" + id + "'. Export CSV to assign missing IDs.");
    }

    private static void Check(Dictionary<string, LocalizationCsv.Row> catalog, string key, string source,
        string context, GameEventSO logic, SO_Event asset)
    {
        Resolve(source, false, context, logic, asset);
        if (string.IsNullOrWhiteSpace(key))
        {
            if (!string.IsNullOrWhiteSpace(source)) throw new FormatException(context + ": missing translation key.");
            return;
        }
        if (!catalog.TryGetValue(key, out var row)) throw new FormatException("Missing key: " + key);
        if (!LocalizationCsv.TokensMatch(source, row.Korean) ||
            (!string.IsNullOrWhiteSpace(row.English) && !LocalizationCsv.TokensMatch(source, row.English)))
            throw new FormatException(key + ": translation variables must match the authored event template. Export the updated CSV first.");
        Resolve(row.Korean, false, key + " / ko", logic, asset);
        if (!string.IsNullOrWhiteSpace(row.English)) Resolve(row.English, true, key + " / en", logic, asset);
    }

    private static void Resolve(string template, bool english, string context, GameEventSO logic, SO_Event asset)
    {
        string text, error;
        bool valid = asset != null
            ? EventTextFormatter.TryFormatEvent(template, asset, english, out text, out error)
            : EventTextFormatter.TryFormat(template, logic, english, out text, out error);
        if (!valid) throw new FormatException(context + ": " + error);
    }
}
#endif
