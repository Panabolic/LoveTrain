using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class DriveBossVisualValidation
{
    private static int checks;
    private static string output;
    public static void Run()
    {
        output = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        try
        {
            Require(Application.productName.StartsWith("DriveBossPreview_", StringComparison.Ordinal), "isolated identity");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
            Transform[] all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var train = UnityEngine.Object.FindFirstObjectByType<Train>();
            var meter = UnityEngine.Object.FindFirstObjectByType<SpeedMeterUI>();
            var level = UnityEngine.Object.FindFirstObjectByType<LevelUI>();
            var follow = UnityEngine.Object.FindFirstObjectByType<ForwardCameraFollow>();
            var hand = UnityEngine.Object.FindObjectsByType<PursuingHand>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var route = UnityEngine.Object.FindFirstObjectByType<StageProgressUI>();
            var stage = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            var wind = UnityEngine.Object.FindFirstObjectByType<AccelerationWindEffect>();
            foreach (var pair in new[] {
                Tuple.Create((UnityEngine.Object)meter, new[] { "train", "needleRectTransform", "fuelFill" }),
                Tuple.Create((UnityEngine.Object)level, new[] { "levelManager", "levelText", "xpFill" }),
                Tuple.Create((UnityEngine.Object)route, new[] { "pursuingHand", "pursuitFill", "pursuitPosition" }),
                Tuple.Create((UnityEngine.Object)wind, new[] { "train", "wind", "viewCamera" }),
                Tuple.Create((UnityEngine.Object)stage, new[] { "cameraFollow", "pursuingHand" }),
                Tuple.Create((UnityEngine.Object)follow, new[] { "trainController" }),
                Tuple.Create((UnityEngine.Object)hand, new[] { "train", "stageManager" }) })
                foreach (string field in pair.Item2) Require(new SerializedObject(pair.Item1).FindProperty(field).objectReferenceValue != null, pair.Item1.name + ": " + field);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemy" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Enemy enemy in prefab.GetComponentsInChildren<Enemy>(true))
                {
                    var so = new SerializedObject(enemy);
                    Require(so.FindProperty("fleshPickupPrefab").objectReferenceValue != null, path + " flesh reference");
                    Require(so.FindProperty("soulPickupPrefab").objectReferenceValue != null, path + " soul reference");
                }
            }
            var eye = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Bosses/EyeBoss.prefab");
            Require(eye.GetComponentsInChildren<Enemy>(true).Length == 1, "eye has one Enemy owner");
            Require(eye.GetComponent<EyeBossBelt>() != null, "eye belt authored");
            Require(new SerializedObject(eye.GetComponent<EyeBossBelt>()).FindProperty("repeatTiles").arraySize == 4, "four authored repeats");
            Require(new SerializedObject(eye.GetComponent<EyeBoss>()).FindProperty("tentacleSpawnPoints").arraySize == 8, "explicit eight attack candidates");
            var boss = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Bosses/TrainBoss.prefab");
            Require(boss.transform.localScale == Vector3.one * 6f, "train boss twice original size");
            foreach (string name in new[] { "FleshPickup", "SoulPickup", "FuelPickup" })
            {
                var pickup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/" + name + ".prefab");
                Require(pickup != null && pickup.GetComponent<RewardPickup>() != null, name + " prefab");
                Require(pickup.GetComponent<SpriteRenderer>().sprite != null, name + " authored sprite");
                Require(pickup.GetComponent<Rigidbody2D>() != null, name + " authored body");
                if (name == "SoulPickup") Require(pickup.GetComponent<TrailRenderer>() != null, "soul authored trail");
            }
            GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
            foreach (Camera existing in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) existing.enabled = false;
            Canvas canvas = route.GetComponentInParent<Canvas>();
            foreach (Canvas other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (other != canvas && other.isRootCanvas) other.gameObject.SetActive(false);
            RectTransform hud = all.Single(tr => tr.name == "OtherUI") as RectTransform;
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true)) if (!graphic.transform.IsChildOf(hud)) graphic.enabled = false;
            var camera = new GameObject("DisposableDrivingUiCamera").AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .08f, .105f, 1f);
            camera.cullingMask = 1 << 5; camera.nearClipPlane = .01f; camera.farClipPlane = 100f;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            const int width = 1280, height = 720;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            camera.aspect = (float)width / height;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = width / 800f;
            var members = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            typeof(CanvasScaler).GetMethod("Handle", members).Invoke(scaler, null);
            var aspect = new GameObject("DisposableDrivingAspect").AddComponent<FixedAspectRatioController>();
            typeof(FixedAspectRatioController).GetMethod("ApplyCanvasRoots", members).Invoke(aspect, new object[] { canvas.transform as RectTransform, new Rect(0f, 0f, 1f, 1f) });
            var levelSettings = new SerializedObject(level);
            ((TMP_Text)levelSettings.FindProperty("levelText").objectReferenceValue).text = "Lv. 12";
            ((Image)levelSettings.FindProperty("xpFill").objectReferenceValue).fillAmount = .65f;
            var meterSettings = new SerializedObject(meter);
            ((Image)meterSettings.FindProperty("fuelFill").objectReferenceValue).fillAmount = .72f;
            ((RectTransform)meterSettings.FindProperty("needleRectTransform").objectReferenceValue).localRotation = Quaternion.Euler(0f, 0f, 63f);
            all.Single(tr => tr.name == "SpeedText").GetComponent<TMP_Text>().text = "620";
            var routeSettings = new SerializedObject(route);
            var red = ((Image)routeSettings.FindProperty("pursuitFill").objectReferenceValue).rectTransform;
            red.anchorMax = new Vector2(.35f, 1f);
            var progress = ((Image)routeSettings.FindProperty("progressFill").objectReferenceValue).rectTransform; progress.anchorMax = new Vector2(.6f, 1f);
            var handIcon = (RectTransform)routeSettings.FindProperty("pursuitPosition").objectReferenceValue; handIcon.anchorMin = handIcon.anchorMax = new Vector2(.35f, .5f);
            var playerIcon = (RectTransform)routeSettings.FindProperty("currentPosition").objectReferenceValue; playerIcon.anchorMin = playerIcon.anchorMax = new Vector2(.6f, .5f);
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in hud.GetComponentsInChildren<TMP_Text>(true)) if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            foreach (var item in new[] { level.transform as RectTransform, meter.transform as RectTransform })
            {
                Vector3[] corners = new Vector3[4]; item.GetWorldCorners(corners);
                Vector3 lower = camera.WorldToScreenPoint(corners[0]), upper = camera.WorldToScreenPoint(corners[2]);
                Require(lower.x >= 0 && lower.y >= 0 && upper.x <= width && upper.y <= height, item.name + " fits content");
            }
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(output, "preview.png"), image.EncodeToPNG()); RenderTexture.active = null;
            File.WriteAllText(Path.Combine(output, "visual-result.txt"), "PASS: " + checks + " authored-reference/layout checks and 1280x720 UI-only RenderTexture preview. Production world renderer is not tested by this image.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); File.WriteAllText(Path.Combine(output, "visual-result.txt"), "FAIL: " + exception); EditorApplication.Exit(1); }
    }
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); checks++; }
}
