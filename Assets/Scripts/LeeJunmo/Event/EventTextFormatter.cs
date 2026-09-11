using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Formatting only: never rolls outcomes, executes effects or changes inventory.
public static class EventTextFormatter
{
    private static readonly HashSet<string> warnings = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => warnings.Clear();

    public static string Localize(string key, string source, GameEventSO logic)
        => Format(EnglishLocalization.Get(key, source), logic, EnglishLocalization.HasEnglish(key));

    public static string LocalizeEvent(string key, string source, SO_Event asset)
        => FormatEvent(EnglishLocalization.Get(key, source), asset, EnglishLocalization.HasEnglish(key));

    public static string Format(string template, GameEventSO logic, bool english = true)
    {
        TryFormat(template, logic, english, out string text, out string error);
        if (error != null && warnings.Add(error)) Debug.LogWarning("[EventText] " + error);
        return text;
    }

    public static string FormatEvent(string template, SO_Event source, bool english = true)
    {
        TryFormatEvent(template, source, english, out string text, out string error);
        if (error != null && warnings.Add(error)) Debug.LogWarning("[EventText] " + error);
        return text;
    }

    public static bool TryFormat(string template, GameEventSO logic, bool english, out string text, out string error)
    {
        return Replace(template, token => Resolve(token, logic, english), out text, out error);
    }

    public static bool TryFormatEvent(string template, SO_Event source, bool english, out string text, out string error)
    {
        return Replace(template, token =>
        {
            int separator = token.IndexOf('.');
            if (source == null || source.Selections == null || separator < 1)
                throw new FormatException("Event tokens require choiceId.outcomeId.field: " + token);
            string choiceId = token.Substring(0, separator);
            SO_Event.Selection? found = null;
            foreach (var choice in source.Selections)
            {
                if (choice.textId != choiceId) continue;
                if (found.HasValue) throw new FormatException("Duplicate choice ID: " + choiceId);
                found = choice;
            }
            if (!found.HasValue) throw new FormatException("Unknown choice ID: " + choiceId);
            return Resolve(token.Substring(separator + 1), found.Value.eventToTrigger, english);
        }, out text, out error);
    }

    private static bool Replace(string template, Func<string, string> resolve, out string text, out string error)
    {
        var errors = new List<string>();
        var result = new StringBuilder();
        template = template ?? "";
        for (int i = 0; i < template.Length; i++)
        {
            char current = template[i];
            if ((current == '{' || current == '}') && i + 1 < template.Length && template[i + 1] == current)
            {
                result.Append(current); i++; continue;
            }
            if (current == '{')
            {
                int end = template.IndexOf('}', i + 1);
                if (end < 0 || template.IndexOf('{', i + 1, end - i - 1) >= 0)
                {
                    errors.Add("Unbalanced braces in event template.");
                    result.Append('?'); continue;
                }
                string token = template.Substring(i + 1, end - i - 1);
                try { result.Append(resolve(token)); }
                catch (FormatException exception) { errors.Add(exception.Message); result.Append('?'); }
                i = end;
            }
            else if (current == '}')
            {
                errors.Add("Unbalanced braces in event template."); result.Append('?');
            }
            else result.Append(current);
        }
        text = result.ToString();
        error = errors.Count == 0 ? null : string.Join("; ", errors);
        return errors.Count == 0;
    }

    private static string Resolve(string token, GameEventSO logic, bool english)
    {
        string[] parts = token.Split('.');
        if (parts.Length != 2 || logic == null || logic.rollGroups == null)
            throw new FormatException("Expected outcomeId.field in " + (logic != null ? logic.name : "missing event logic") + ": " + token);
        WeightedEventOutcome found = null;
        EventRollGroup foundGroup = null;
        foreach (var group in logic.rollGroups)
        {
            if (group == null || group.outcomes == null) continue;
            foreach (var outcome in group.outcomes)
            {
                if (outcome == null || outcome.textId != parts[0]) continue;
                if (found != null) throw new FormatException("Duplicate outcome ID in " + logic.name + ": " + parts[0]);
                found = outcome; foundGroup = group;
            }
        }
        if (found == null) throw new FormatException("Unknown outcome ID in " + logic.name + ": " + parts[0]);
        if (parts[1] == "chance") return Number(Chance(foundGroup, found));
        EffectParameters p = found.parameters;
        if (p == null || found.effectLogic == null || found.outputSettings == null)
            throw new FormatException("Outcome has no executable effect parameters: " + token);
        switch (parts[1])
        {
            case "count":
                if (!(found.effectLogic is Effect_AcquireItem || found.effectLogic is Effect_AcquireSpecificItem || found.effectLogic is Effect_AcquireRandomItem || found.effectLogic is Effect_UpgradeRandomItemNTimes || found.effectLogic is Effect_UpgradeRandomItemToMax || found.effectLogic is Effect_SpawnMobBatch))
                    throw new FormatException("This effect has no count: " + token);
                return Number(Math.Max(1, p.intValue));
            case "levels":
                if (!(found.effectLogic is Effect_UpgradeRandomItemNTimes)) throw new FormatException("This effect has no upgrade levels: " + token);
                return Number(Math.Max(1, p.intValue2));
            case "amount":
                if (found.effectLogic is Effect_ModifySpeed) return Number(Math.Abs(p.floatValue));
                if (found.effectLogic is Effect_IncreaseEnemyHealthBuff) return Number(p.intValue);
                throw new FormatException("This effect has no amount: " + token);
            case "value":
                if (found.effectLogic is Effect_ModifySpeed) return Number(p.floatValue);
                if (found.effectLogic is Effect_IncreaseEnemyHealthBuff) return Number(p.intValue);
                throw new FormatException("This effect has no signed value: " + token);
            case "item":
            case "itemSummary":
                if (!(found.effectLogic is Effect_AcquireItem || found.effectLogic is Effect_AcquireSpecificItem) || !(p.soReference is Item_SO item))
                    throw new FormatException("This outcome has no specific item reference: " + token);
                return parts[1] == "item" ? (english ? item.LocalizedName : item.itemName)
                    : (english ? item.LocalizedSimpleDescription : item.itemSimpleScript);
            case "delay":
                if (!(found.effectLogic is Effect_SpawnMobBatch)) throw new FormatException("This effect has no spawn delay: " + token);
                return Number(p.floatValue <= 0.05f ? 0.2f : p.floatValue);
            case "interval":
                if (!(found.effectLogic is Effect_SpawnMobPeriodically)) throw new FormatException("This effect has no spawn interval: " + token);
                return Number(p.floatValue <= 0.1f ? 1f : p.floatValue);
            case "intValue": return Number(p.intValue);
            case "intValue2": return Number(p.intValue2);
            case "floatValue": return Number(p.floatValue);
            case "floatValue2": return Number(p.floatValue2);
            default: throw new FormatException("Unknown event field: " + token);
        }
    }

    // Each roll group is independent. Weights are relative, not percentages.
    private static double Chance(EventRollGroup group, WeightedEventOutcome selected)
    {
        double total = 0;
        foreach (var outcome in group.outcomes)
        {
            if (outcome == null) continue;
            if (float.IsNaN(outcome.weight) || float.IsInfinity(outcome.weight) || outcome.weight < 0)
                throw new FormatException("Chance requires finite, non-negative outcome weights.");
            total += outcome.weight;
        }
        return total <= 0 ? 0 : selected.weight / total * 100d;
    }

    private static string Number(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new FormatException("Event parameter is not a finite number.");
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
