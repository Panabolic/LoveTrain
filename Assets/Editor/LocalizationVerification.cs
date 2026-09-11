#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LocalizationVerification
{
    [MenuItem("Tools/LoveTrain/Localization/Run Data Tests")]
    public static void RunDataTests()
    {
        var source = new List<LocalizationCsv.Row> {
            new LocalizationCsv.Row { Key = "test.quoted", Korean = "쉼표, 따옴표 \"문장\"\n다음 줄 {0}", English = "Comma, quote \"text\"\nNext line {0}", Context = "CSV round trip" },
            new LocalizationCsv.Row { Key = "test.blank", Korean = "원문", English = "", Context = "Fallback" }
        };
        var actual = LocalizationCsv.Parse("\uFEFF" + LocalizationCsv.Write(source));
        Check(actual.Count == 2 && actual[0].Korean == source[0].Korean && actual[0].English == source[0].English, "Quoted/multiline UTF-8 round trip");
        Check(actual[1].English == "", "Blank English preserved");
        Reject("Key,ko,en\na,가,A\na,나,B\n", "Duplicate key");
        Reject("Key,ko,en\na,{Damage},{Count}\n", "Placeholder mismatch");
        Reject("Key,ko,en\na,가,\"unfinished", "Unclosed quote");
        Reject("Key,ko,en\na,가,\"A\"tail\n", "Trailing characters after quote");
        Reject("Key,ko,en\na,가,A,extra\n", "Wrong column count");
        Reject("Key,ko,en\n,가,A\n", "Blank key");
        Check(LocalizationCsv.Parse("en,Key,ko\nEnglish,test,한국어")[0].Key == "test", "Reordered columns and no final newline");
        Check(EnglishLocalization.Get("missing.test.key", "원문") == "원문", "Missing-key fallback");
        var catalog = typeof(EnglishLocalization).GetField("rows", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var originalCatalog = catalog.GetValue(null);
        try
        {
            catalog.SetValue(null, new Dictionary<string, LocalizationCsv.Row> {
                { "test.blank", new LocalizationCsv.Row { Key = "test.blank", Korean = "원문", English = "" } }
            });
            Check(EnglishLocalization.Get("test.blank", "다른 원문") == "원문", "Blank-English Korean fallback");
        }
        finally { catalog.SetValue(null, originalCatalog); }
        Check(EnglishLocalization.Format("result.speed_recover", "기차의 속도를 {0}만큼 회복했습니다.", 12.5f) == "Restored 12.5 train speed.", "Runtime numeric formatting");
        LocalizationEditorTools.ValidateCsv();
        var rows = LocalizationCsv.Parse(File.ReadAllText(LocalizationEditorTools.CsvPath));
        var keys = new HashSet<string>(rows.Select(row => row.Key));
        foreach (string guid in AssetDatabase.FindAssets("t:Item_SO"))
        {
            Item_SO item = AssetDatabase.LoadAssetAtPath<Item_SO>(AssetDatabase.GUIDToAssetPath(guid));
            Check(keys.Contains(item.itemNameKey), item.name + " name key");
            Check(keys.Contains(item.itemDescriptionKey), item.name + " description key");
            Check(keys.Contains(item.itemSimpleDescriptionKey), item.name + " summary key");
            Check(!string.IsNullOrWhiteSpace(item.LocalizedName), item.name + " translated name");
            for (int level = 1; level <= item.MaxUpgrade; level++)
                Check(!item.GetFormattedDescription(level).Contains("{"), item.name + " unresolved description variable at level " + level);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:SO_Event"))
        {
            SO_Event asset = AssetDatabase.LoadAssetAtPath<SO_Event>(AssetDatabase.GUIDToAssetPath(guid));
            Check(string.IsNullOrWhiteSpace(asset.EventTitle) || keys.Contains(asset.titleKey), asset.name + " title key");
            Check(string.IsNullOrWhiteSpace(asset.EventText) || keys.Contains(asset.textKey), asset.name + " body key");
            if (asset.Selections == null) continue;
            foreach (var choice in asset.Selections)
            {
                Check(string.IsNullOrWhiteSpace(choice.selectionText) || keys.Contains(choice.selectionTextKey), asset.name + " choice key");
                Check(string.IsNullOrWhiteSpace(choice.selectionUnderText) || keys.Contains(choice.selectionUnderTextKey), asset.name + " hint key");
            }
        }
        var temporaryEvent = ScriptableObject.CreateInstance<SO_Event>();
        temporaryEvent.EventTitle = "Test";
        temporaryEvent.Selections = new List<SO_Event.Selection> {
            new SO_Event.Selection { selectionText = "First" },
            new SO_Event.Selection { selectionText = "Second" }
        };
        LocalizationEditorTools.EnsureEventKeys(temporaryEvent);
        string firstKey = temporaryEvent.Selections[0].selectionTextKey;
        string secondKey = temporaryEvent.Selections[1].selectionTextKey;
        temporaryEvent.Selections.Reverse();
        temporaryEvent.EventTitle = "Renamed";
        LocalizationEditorTools.EnsureEventKeys(temporaryEvent);
        Check(temporaryEvent.Selections[0].selectionTextKey == secondKey && temporaryEvent.Selections[1].selectionTextKey == firstKey, "Keys survive reordering");
        UnityEngine.Object.DestroyImmediate(temporaryEvent);
        EventTextFormatterVerification.Run();
        Debug.Log("LOCALIZATION_DATA_TESTS_PASSED");
    }

    // Batch-only scene inspection never saves scenes or source assets.
    public static void RunBatch()
    {
        bool hadLanguage = PlayerPrefs.HasKey(EnglishLocalization.LanguagePreferenceKey);
        string previousLanguage = PlayerPrefs.GetString(EnglishLocalization.LanguagePreferenceKey);
        try
        {
            EnglishLocalization.SetLanguage(true);
            RunDataTests();
            int labels = 0;
            foreach (string path in new[] { "Assets/Scenes/Start.unity", "Assets/Scenes/Junmo.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (LocalizedText label in root.GetComponentsInChildren<LocalizedText>(true))
                    {
                        Check(!string.IsNullOrEmpty(label.localizationKey), "Missing fixed-label key");
                        label.Refresh();
                        var tmp = label.GetComponent<TMPro.TMP_Text>();
                        if (tmp != null) Check(tmp.text == EnglishLocalization.Get(label.localizationKey, label.fallbackText), "Fixed label binding");
                        labels++;
                    }
                }
            }
            Check(labels >= 23, "Expected fixed labels in both build scenes");
            Debug.Log("LOCALIZATION_SCENE_TESTS_PASSED labels=" + labels);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
        finally
        {
            if (hadLanguage) PlayerPrefs.SetString(EnglishLocalization.LanguagePreferenceKey, previousLanguage);
            else PlayerPrefs.DeleteKey(EnglishLocalization.LanguagePreferenceKey);
            PlayerPrefs.Save();
        }
    }

    private static void Reject(string csv, string name)
    {
        try { LocalizationCsv.Parse(csv); }
        catch (FormatException) { return; }
        throw new Exception("Expected rejection: " + name);
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Localization verification failed: " + name);
    }
}
#endif
