#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LanguageOptionVerification
{
    private static void Invoke(object instance, string method) => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void RunBatch()
    {
        bool existed = PlayerPrefs.HasKey(EnglishLocalization.LanguagePreferenceKey);
        string saved = PlayerPrefs.GetString(EnglishLocalization.LanguagePreferenceKey);
        try
        {
            LocalizationVerification.RunBatch();
            PlayerPrefs.DeleteKey(EnglishLocalization.LanguagePreferenceKey);
            Check(EnglishLocalization.IsEnglish, "First launch must default to English");
            foreach (string path in new[] { "Assets/Scenes/Start.unity", "Assets/Scenes/Junmo.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var option = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Option>(true)).Single();
                var row = option.GetComponentsInChildren<Transform>(true).Single(t => t.name == "LanguageGroup");
                var previous = row.GetComponentsInChildren<Button>(true).Single(b => b.name == "LanguagePrevButton");
                var next = row.GetComponentsInChildren<Button>(true).Single(b => b.name == "LanguageNextButton");
                var value = row.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "LanguageValueText");
                var labels = option.GetComponentsInChildren<LocalizedText>(true);
                EnglishLocalization.SetLanguage(true);
                foreach (var label in labels) Invoke(label, "OnEnable");
                Invoke(option, "InitializeLanguageControls");
                Check(value.text == "English", "English value missing");
                previous.onClick.Invoke();
                Check(!EnglishLocalization.IsEnglish && value.text == "한국어", "Previous button must change to Korean immediately");
                Check(PlayerPrefs.GetString(EnglishLocalization.LanguagePreferenceKey) == "ko", "Korean preference not saved");
                EnglishLocalization.Reload();
                Check(EnglishLocalization.Get("ui.language", "") == "언어", "Saved language not used after catalog reload");
                Check(row.GetComponentsInChildren<LocalizedText>(true).Single().GetComponent<TMP_Text>().text == "언어", "Fixed label did not update immediately");
                var item = AssetDatabase.LoadAssetAtPath<Item_SO>("Assets/Datas/Items/BloodyBible.asset");
                Check(item.LocalizedName == item.itemName, "Item name did not follow Korean selection");
                var data = AssetDatabase.FindAssets("t:SO_Event").Select(g => AssetDatabase.LoadAssetAtPath<SO_Event>(AssetDatabase.GUIDToAssetPath(g))).First(e => e.titleKey == "event.paranoia.title");
                var choice = data.Selections[1];
                string hint = EventTextFormatter.Localize(choice.selectionUnderTextKey, choice.selectionUnderText, choice.eventToTrigger);
                Check(hint.Contains("60%") && hint.Contains(item.itemName) && !hint.Contains("{"), "Korean reward template failed");
                next.onClick.Invoke();
                Check(EnglishLocalization.IsEnglish && value.text == "English", "Next button must wrap to English");
                Check(item.LocalizedName != item.itemName, "Item name did not follow English selection");
                foreach (var label in labels) Invoke(label, "OnDisable");
                RenderPreview(option, path.EndsWith("Start.unity") ? "options" : "options-game");
                Invoke(option, "OnDestroy");
            }
            Debug.Log("LANGUAGE_OPTION_TESTS_PASSED scenes=2");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
        finally
        {
            if (existed) PlayerPrefs.SetString(EnglishLocalization.LanguagePreferenceKey, saved);
            else PlayerPrefs.DeleteKey(EnglishLocalization.LanguagePreferenceKey);
            PlayerPrefs.Save();
        }
    }

    private static void RenderPreview(Option source, string filename)
    {
        var canvasObject = new GameObject("LanguagePreviewCanvas", typeof(Canvas), typeof(CanvasScaler));
        var cameraObject = new GameObject("LanguagePreviewCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.cullingMask = 1 << 30;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        var copy = UnityEngine.Object.Instantiate(source.gameObject, canvas.transform);
        foreach (Transform child in copy.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        canvasObject.layer = 30;
        var option = copy.GetComponent<Option>();
        option.optionPanel.SetActive(true);
        copy.SetActive(true);
        var texture = new RenderTexture(1280, 960, 24);
        camera.targetTexture = texture;
        var oldActive = RenderTexture.active;
        try
        {
            Directory.CreateDirectory("outputs/language-options");
            foreach (bool english in new[] { true, false })
            {
                EnglishLocalization.SetLanguage(english);
                foreach (var label in copy.GetComponentsInChildren<LocalizedText>(true)) label.Refresh();
                Invoke(option, "InitializeScreenControls");
                Invoke(option, "InitializeLanguageControls");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(copy.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(1280, 960, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0);
                image.Apply();
                File.WriteAllBytes("outputs/language-options/" + filename + "-" + (english ? "en" : "ko") + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
        finally
        {
            RenderTexture.active = oldActive;
            camera.targetTexture = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            Invoke(option, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
#endif
