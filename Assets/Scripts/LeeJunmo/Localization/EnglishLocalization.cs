using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class EnglishLocalization
{
    public const string ResourcePath = "Localization/Strings";
    private static Dictionary<string, LocalizationCsv.Row> rows;
    public const string LanguagePreferenceKey = "LoveTrain.Language";
    public static event Action LanguageChanged;
    public static bool IsEnglish => PlayerPrefs.GetString(LanguagePreferenceKey, "en") != "ko";

    public static void SetLanguage(bool english)
    {
        bool changed = IsEnglish != english;
        PlayerPrefs.SetString(LanguagePreferenceKey, english ? "en" : "ko");
        PlayerPrefs.Save();
        if (changed) LanguageChanged?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { rows = null; LanguageChanged = null; }

    public static string Get(string key, string fallback)
    {
        if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;
        EnsureLoaded();
        if (!rows.TryGetValue(key, out LocalizationCsv.Row row)) return fallback ?? string.Empty;
        if (IsEnglish && !string.IsNullOrWhiteSpace(row.English)) return row.English;
        return string.IsNullOrEmpty(row.Korean) ? (fallback ?? string.Empty) : row.Korean;
    }

    public static bool HasEnglish(string key)
    {
        EnsureLoaded();
        return IsEnglish && !string.IsNullOrEmpty(key) && rows.TryGetValue(key, out LocalizationCsv.Row row) && !string.IsNullOrWhiteSpace(row.English);
    }

    public static string Format(string key, string fallback, params object[] args)
    {
        string text = Get(key, fallback);
        try { return string.Format(CultureInfo.InvariantCulture, text, args); }
        catch (FormatException exception)
        {
            Debug.LogWarning($"[Localization] Invalid format for '{key}': {exception.Message}");
            return string.Format(CultureInfo.InvariantCulture, fallback, args);
        }
    }

    public static void Reload() => rows = null;

    private static void EnsureLoaded()
    {
        if (rows != null) return;
        rows = new Dictionary<string, LocalizationCsv.Row>(StringComparer.Ordinal);
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[Localization] Strings.csv is missing. Using authored source text.");
            return;
        }
        try
        {
            foreach (LocalizationCsv.Row row in LocalizationCsv.Parse(asset.text)) rows.Add(row.Key, row);
        }
        catch (FormatException exception)
        {
            rows.Clear();
            Debug.LogError($"[Localization] {exception.Message} Using authored source text.");
        }
    }
}
