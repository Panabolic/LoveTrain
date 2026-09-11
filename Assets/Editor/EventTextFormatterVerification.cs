#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EventTextFormatterVerification
{
    public static void Run()
    {
        var logic = ScriptableObject.CreateInstance<GameEventSO>();
        var asset = ScriptableObject.CreateInstance<SO_Event>();
        var random = ScriptableObject.CreateInstance<Effect_AcquireRandomItem>();
        var specific = ScriptableObject.CreateInstance<Effect_AcquireSpecificItem>();
        var speed = ScriptableObject.CreateInstance<Effect_ModifySpeed>();
        var item = ScriptableObject.CreateInstance<Item_SO>();
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var reward = new WeightedEventOutcome { textId = "reward", weight = 2, effectLogic = random,
                parameters = new EffectParameters { intValue = 3 }, outputSettings = new EventResultOutput() };
            var nothing = new WeightedEventOutcome { textId = "none", weight = 3, outputSettings = new EventResultOutput() };
            var boost = new WeightedEventOutcome { textId = "speed", weight = 1, effectLogic = speed,
                parameters = new EffectParameters { floatValue = -12.5f }, outputSettings = new EventResultOutput() };
            logic.rollGroups = new List<EventRollGroup> {
                new EventRollGroup { outcomes = new List<WeightedEventOutcome> { reward, nothing } },
                new EventRollGroup { outcomes = new List<WeightedEventOutcome> { boost } }
            };
            Equal(EventTextFormatter.Format("{reward.count}/{reward.chance}/{none.chance}/{speed.chance}/{speed.value}", logic), "3/40/60/100/-12.5", "Independent normalized probabilities and invariant numbers");
            reward.parameters.intValue = 7;
            reward.weight = 6;
            Equal(EventTextFormatter.Format("{reward.count}/{reward.chance}", logic), "7/66.67", "Live data changes");
            logic.rollGroups[0].outcomes.Reverse();
            logic.rollGroups.Reverse();
            Equal(EventTextFormatter.Format("{reward.count}/{speed.value}", logic), "7/-12.5", "Stable outcome IDs after reordering");
            asset.Selections = new List<SO_Event.Selection> {
                new SO_Event.Selection { textId = "choice_a", eventToTrigger = logic },
                new SO_Event.Selection { textId = "choice_b" }
            };
            asset.Selections.Reverse();
            Equal(EventTextFormatter.FormatEvent("{choice_a.reward.count}", asset), "7", "Stable choice IDs in body");
            reward.parameters.intValue = 0;
            Equal(EventTextFormatter.Format("{reward.count}", logic), "1", "Effect default count");
            reward.weight = 0; nothing.weight = 0;
            Equal(EventTextFormatter.Format("{reward.chance}", logic), "0", "Disabled group");
            reward.weight = -1;
            Reject("{reward.chance}", logic);
            reward.weight = 1;
            Reject("{missing.count}", logic);
            Reject("{reward.typo}", logic);
            Reject("{none.count}", logic);
            Reject("{reward.count", logic);
            Reject("{reward.levels}", logic);
            Equal(EventTextFormatter.Format("{{literal}} {reward.count}", logic), "{literal} 1", "Escaped braces");
            Equal(EventTextFormatter.Format("{{{reward.count}}}", logic), "{1}", "Escaped braces around a token");
            Reject("{}", logic);
            Reject("stray }", logic);
            nothing.textId = "reward";
            Reject("{reward.count}", logic);
            nothing.textId = "none";
            reward.effectLogic = specific;
            item.itemName = "테스트 아이템";
            item.itemNameKey = "item.bloody_bible.name";
            reward.parameters.soReference = item;
            Equal(EventTextFormatter.Format("{reward.item}", logic, false), "테스트 아이템", "Korean item name");
            Equal(EventTextFormatter.Format("{reward.item}", logic), item.LocalizedName, "Translated item name");
            item.itemNameKey = "";
            item.itemName = "Changed reward";
            Equal(EventTextFormatter.Format("{reward.item}", logic), "Changed reward", "Changed reward reference data");
            var catalog = LocalizationCsv.Parse(File.ReadAllText(LocalizationEditorTools.CsvPath));
            EventTemplateValidation.Validate(catalog);
            var stale = catalog.First(row => row.Key == "event.steel_savior.choice_2.hint");
            stale.Korean = "아이템 2개"; stale.English = "2 items";
            bool rejected = false;
            try { EventTemplateValidation.Validate(catalog); } catch (FormatException) { rejected = true; }
            if (!rejected) throw new Exception("Stale fixed-string CSV accepted");
            foreach (string guid in AssetDatabase.FindAssets("t:SO_Event"))
            {
                var live = AssetDatabase.LoadAssetAtPath<SO_Event>(AssetDatabase.GUIDToAssetPath(guid));
                if (live.Selections == null) continue;
                foreach (var choice in live.Selections)
                {
                    string hint = EventTextFormatter.Localize(choice.selectionUnderTextKey, choice.selectionUnderText, choice.eventToTrigger);
                    if (hint.Contains("{") || hint.Contains("}")) throw new Exception("Unresolved live hint: " + live.name);
                    if (choice.selectionUnderTextKey == "event.steel_savior.choice_2.hint" && hint != "Gain [3] random items. A special monster appears.") throw new Exception("Steel Savior reward not bound");
                    if (choice.selectionUnderTextKey == "event.paranoia.choice_2.hint" && hint != "40% chance to gain \"Bloodstained Bible\"; 60% chance to lose 70 speed.") throw new Exception("Paranoia probability not bound");
                }
            }
            Debug.Log("EVENT_TEXT_FORMAT_TESTS_PASSED");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            foreach (var instance in new UnityEngine.Object[] { logic, asset, random, specific, speed, item })
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }
    private static void Equal(string actual, string expected, string name)
    {
        if (actual != expected) throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    private static void Reject(string template, GameEventSO logic)
    {
        if (EventTextFormatter.TryFormat(template, logic, true, out _, out _)) throw new Exception("Invalid template accepted: " + template);
    }
}
#endif
