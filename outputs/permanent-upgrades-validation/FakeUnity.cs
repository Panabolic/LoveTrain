using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

// Test doubles only: these never call Unity or touch the user's PlayerPrefs.
namespace UnityEngine
{
    public class Object { public string name; public HideFlags hideFlags; }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }
    public class Sprite : Object { }
    public class TextAsset : Object { public string text; }
    public enum HideFlags { HideAndDontSave, DontSave }
    public enum RuntimeInitializeLoadType { SubsystemRegistration, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName;
        public string fileName;
        public int order;
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TextAreaAttribute : Attribute { public TextAreaAttribute(int min = 3, int max = 3) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
    }
    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void LogWarning(object message) => Warnings.Add(message.ToString());
        public static void LogError(object message) => Warnings.Add(message.ToString());
        public static void Log(object message) { }
    }
    public static class Resources
    {
        public static readonly Dictionary<string, Object> Objects = new Dictionary<string, Object>();
        public static T Load<T>(string path) where T : Object => Objects.TryGetValue(path, out var value) ? value as T : null;
        public static T[] LoadAll<T>(string path) where T : Object
        {
            var found = new List<T>();
            foreach (var item in Objects) if (item.Value is T value) found.Add(value);
            return found.ToArray();
        }
    }
    public sealed class PlayerPrefsException : Exception { public PlayerPrefsException(string message) : base(message) { } }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, int> Integers = new Dictionary<string, int>();
        public static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        public static bool FailSave;
        public static int Saves;
        public static bool HasKey(string key) => Integers.ContainsKey(key) || Strings.ContainsKey(key);
        public static int GetInt(string key, int fallback = 0) => Integers.TryGetValue(key, out var value) ? value : fallback;
        public static string GetString(string key, string fallback = "") => Strings.TryGetValue(key, out var value) ? value : fallback;
        public static void SetInt(string key, int value) => Integers[key] = value;
        public static void SetString(string key, string value) => Strings[key] = value;
        public static void DeleteKey(string key) { Integers.Remove(key); Strings.Remove(key); }
        public static void Save() { if (FailSave) throw new PlayerPrefsException("Simulated save failure"); Saves++; }
        public static void Clear() { Integers.Clear(); Strings.Clear(); FailSave = false; Saves = 0; }
    }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);
        public static void FromJsonOverwrite(string text, object value)
        {
            JsonDocument doc;
            try { doc = JsonDocument.Parse(text); }
            catch (JsonException exception) { throw new ArgumentException(exception.Message, exception); }
            using var document = doc;
            if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected JSON object.");
            foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (doc.RootElement.TryGetProperty(field.Name, out var json))
                    field.SetValue(value, JsonSerializer.Deserialize(json.GetRawText(), field.FieldType, Options));
        }
        public static T FromJson<T>(string text)
        {
            JsonDocument doc;
            try { doc = JsonDocument.Parse(text); }
            catch (JsonException exception) { throw new ArgumentException(exception.Message, exception); }
            using var document = doc;
            if (doc.RootElement.ValueKind == JsonValueKind.Null) return default;
            if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected JSON object.");
            // Unity gives missing JSON fields their type defaults and does not run constructors.
            var value = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (doc.RootElement.TryGetProperty(field.Name, out var json))
                    field.SetValue(value, JsonSerializer.Deserialize(json.GetRawText(), field.FieldType, Options));
            return value;
        }
    }
}

public static class EnglishLocalization { public static bool IsEnglish; }

public class Item_SO : UnityEngine.ScriptableObject { public string itemName; }
public class BloodyBible_SO : Item_SO { }
public class GiantMaw_SO : Item_SO { }
public class Revolver_SO : Item_SO { }
public class CrownOfThorns_SO : Item_SO { }
public class LonginusLauncher_SO : Item_SO { }
public class LaserGun_SO : Item_SO { }
public class RearGun_SO : Item_SO { }
public class PoisonMissileLauncher_SO : Item_SO { }
public class MagicBullet_SO : Item_SO { }
public class BeatingHeart_SO : Item_SO { }
public class RedGear_SO : Item_SO { }
public class BlueGear_SO : Item_SO { }
public class HealingItem_SO : Item_SO { }
