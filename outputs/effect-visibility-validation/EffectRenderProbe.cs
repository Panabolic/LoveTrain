using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Disposable editor-memory diagnostics only. No scene, prefab, material or settings are saved.
// Launch EffectRenderProbe.staticRun without -quit; this probe exits with its result.
[InitializeOnLoad]
public static class EffectRenderProbe
{
    private const int Width = 1280, Height = 720;
    private static string output;
    private static double deadline;
    private static bool running;
    private static int warmup, checks;
    private static Camera camera;
    private static RenderTexture target;
    private static RenderPipelineAsset qualityPipeline, defaultPipeline;
    private static readonly List<Renderer[]> effects = new List<Renderer[]>();
    private static readonly List<PickupDetails> pickups = new List<PickupDetails>();
    private static readonly string[] Names = { "Flesh", "Soul", "Fuel", "Wind" };
    private static readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private static Color32[] baseline;
    private sealed class PickupDetails
    {
        public RewardPickup Component;
        public SpriteRenderer Body;
        public SpriteRenderer[] Outlines;
        public Sprite[] FleshVariants;
        public Material OriginalBodyMaterial;
    }

    static EffectRenderProbe() { EditorApplication.update += Tick; }
    public static void Run() => staticRun();

    public static void staticRun()
    {
        Guard(); // Refuse the original Editor before writing files or scheduling exit.
        try
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../effect-visibility-validation"));
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-effectProbeOutput") output = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(output);
            File.WriteAllText(ResultPath, "START: actual production URP render request, editor-memory diagnostic objects, sorting-only A/B.\n");
            File.WriteAllText(Path.Combine(output, "console.txt"), "Probe warnings/errors.\n");
            Application.logMessageReceived += Log;
            qualityPipeline = QualitySettings.renderPipeline;
            defaultPipeline = GraphicsSettings.defaultRenderPipeline;
            Require((qualityPipeline != null ? qualityPipeline : defaultPipeline) is UniversalRenderPipelineAsset,
                "Effective production pipeline is URP; QualitySettings/GraphicsSettings overrides remain untouched");
            Write("QUALITY index=" + QualitySettings.GetQualityLevel() + ", quality-pipeline=" + Name(qualityPipeline) + ", default-pipeline=" + Name(defaultPipeline));
            deadline = EditorApplication.timeSinceStartup + 45d;
            Setup();
            running = true;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static void Setup()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) canvas.gameObject.SetActive(false);
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (behaviour != null && behaviour.GetType().Assembly == typeof(Train).Assembly) behaviour.enabled = false;
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) renderer.enabled = false;
        camera = Camera.main;
        Require(camera != null && camera.orthographic, "Authored Junmo orthographic camera is retained");
        foreach (Camera other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) other.enabled = other == camera;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.aspect = (float)Width / Height;
        target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
        {
            name = "TempDiagnostics_EffectRenderTarget",
            hideFlags = HideFlags.HideAndDontSave,
            antiAliasing = 1
        };
        target.Create(); temporary.Add(target); camera.targetTexture = target;
        var database = AssetDatabase.LoadAssetAtPath<StageDatabase>("Assets/Prefabs/Map/StageDatabase.asset");
        Require(database != null && database.stagePrefabs != null && database.stagePrefabs.Count > 0 && database.stagePrefabs[0] != null,
            "Actual StageDatabase first background prefab is available");
        StageManager stage = UnityEngine.Object.FindFirstObjectByType<StageManager>();
        Transform parent = new SerializedObject(stage).FindProperty("bgParent").objectReferenceValue as Transform;
        Require(parent != null, "Authored StageManager background parent preserves production local/world positioning");
        GameObject background = UnityEngine.Object.Instantiate(database.stagePrefabs[0], parent);
        background.name = "TempDiagnostics_" + database.stagePrefabs[0].name;
        background.hideFlags = HideFlags.DontSave; temporary.Add(background);
        AutoScrollBackground scroll = background.GetComponent<AutoScrollBackground>();
        Require(scroll != null, "Actual BeltScroll component exists on the first stage prefab");
        scroll.enabled = false; scroll.cameraTransform = camera.transform;
        typeof(AutoScrollBackground).GetMethod("EnsureViewportCoverage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
        foreach (MonoBehaviour behaviour in background.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        Write("BACKGROUND " + AssetDatabase.GetAssetPath(database.stagePrefabs[0]) + ", world-position=" + background.transform.position + ", camera=" + camera.transform.position + ", size=" + camera.orthographicSize);
        foreach (SpriteRenderer renderer in background.GetComponentsInChildren<SpriteRenderer>())
            Write("BACKGROUND sorting=" + renderer.sortingLayerName + "/" + renderer.sortingOrder + ", sprite=" + Name(renderer.sprite));

        string[] pickupNames = { "FleshPickup", "SoulPickup", "FuelPickup" };
        for (int i = 0; i < pickupNames.Length; i++)
        {
            string path = "Assets/Prefabs/Gameplay/" + pickupNames[i] + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, "Authored " + pickupNames[i] + " prefab exists");
            GameObject pickup = UnityEngine.Object.Instantiate(prefab, new Vector3((i - 1) * 9f, -7.6f, 0f), Quaternion.identity);
            pickup.name = "TempDiagnostics_" + pickupNames[i]; pickup.hideFlags = HideFlags.DontSave; temporary.Add(pickup);
            foreach (MonoBehaviour behaviour in pickup.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (Rigidbody2D body in pickup.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
            foreach (TrailRenderer trail in pickup.GetComponentsInChildren<TrailRenderer>(true)) { trail.Clear(); trail.emitting = false; trail.enabled = false; }
            SpriteRenderer sprite = pickup.GetComponent<SpriteRenderer>();
            Require(sprite != null && sprite.sprite != null, "Original " + pickupNames[i] + " sprite is retained");
            float expected = i == 0 ? 0.5f : 0.38f;
            Require(Mathf.Abs(pickup.transform.localScale.x - expected) < 0.001f, pickupNames[i] + " retains original scale " + expected);
            effects.Add(pickup.GetComponentsInChildren<Renderer>(true));
            RewardPickup component = pickup.GetComponent<RewardPickup>();
            var serialized = new SerializedObject(component);
            var details = new PickupDetails
            {
                Component = component,
                Body = sprite,
                Outlines = ReadSpriteRenderers(serialized.FindProperty("outlineRenderers")),
                FleshVariants = ReadSprites(serialized.FindProperty("fleshSprites")),
                OriginalBodyMaterial = sprite.sharedMaterial
            };
            // Reproduce the runtime flesh sprite choice before synchronizing outlines.
            // No wallet/reward initialization is needed for a frozen render diagnostic.
            if (i == 0 && details.FleshVariants.Length > 0) sprite.sprite = details.FleshVariants[details.FleshVariants.Length / 2];
            RefreshOutline(details);
            pickups.Add(details);
            Write("FX " + Names[i] + ", position=" + pickup.transform.position + ", bounds=" + sprite.bounds + ", original-sorting=" + sprite.sortingLayerName + "/" + sprite.sortingOrder);
        }

        AccelerationWindEffect driver = UnityEngine.Object.FindFirstObjectByType<AccelerationWindEffect>(FindObjectsInactive.Include);
        Require(driver != null, "Authored wind effect component exists");
        ParticleSystem wind = new SerializedObject(driver).FindProperty("wind").objectReferenceValue as ParticleSystem;
        Require(wind != null, "Authored wind ParticleSystem is retained");
        wind.transform.position = Vector3.zero;
        wind.transform.rotation = Quaternion.identity;
        wind.transform.localScale = Vector3.one;
        wind.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = wind.main; main.simulationSpeed = 0f; main.playOnAwake = false; main.startSize3D = true;
        var emission = wind.emission; emission.enabled = false;
        wind.Play();
        for (int index = 0; index < 64; index++)
        {
            wind.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(-26f + (index % 8) * 7f, -12f + (index / 8) * 4f, 0f),
                velocity = Vector3.zero,
                startLifetime = 60f,
                startSize3D = new Vector3(2.5f, 0.035f, 1f),
                startColor = new Color(1f, 1f, 1f, 0.45f)
            }, 1);
        }
        wind.Pause();
        ParticleSystemRenderer windRenderer = wind.GetComponent<ParticleSystemRenderer>();
        Require(windRenderer != null && wind.particleCount == 64, "Manual native wind emission retains 64 fixed original-style white lines");
        effects.Add(new Renderer[] { windRenderer });
        Write("WIND count=" + wind.particleCount + ", style=size3D(2.5,.035,1), white-alpha=.45, original-sorting=" + windRenderer.sortingLayerName + "/" + windRenderer.sortingOrder);
        SetVisible(-1, false);
        TestMethodShaderSupportCurrentPipeline();
    }

    // Public diagnostic entry point requested by the task; reports actual assigned shaders.
    public static void TestMethodShaderSupportCurrentPipeline()
    {
        Write("SHADER PIPELINE effective=" + Name(qualityPipeline != null ? qualityPipeline : defaultPipeline) + ", instance=" + (RenderPipelineManager.currentPipeline != null ? RenderPipelineManager.currentPipeline.GetType().FullName : "not-yet-created"));
        for (int i = 0; i < effects.Count; i++) foreach (Renderer renderer in effects[i])
        {
            Material material = renderer.sharedMaterial;
            Shader shader = material != null ? material.shader : null;
            Write("SHADER " + Names[i] + ", material=" + Name(material) + ", shader=" + Name(shader) +
                ", isSupported=" + (shader != null && shader.isSupported) + ", compile-error=" + (shader != null && ShaderUtil.ShaderHasError(shader)) +
                ", render-pipeline-tag=" + (material != null ? material.GetTag("RenderPipeline", false, "(none)") : "(none)"));
        }
    }

    private static void Tick()
    {
        if (!running) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("URP effect probe exceeded 45 seconds.");
            if (++warmup < 5) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            running = false;
            Require(QualitySettings.renderPipeline == qualityPipeline && GraphicsSettings.defaultRenderPipeline == defaultPipeline,
                "Production render pipeline assets remain unchanged before rendering");
            baseline = Capture("baseline-without-fx.png");
            SetSorting("Default"); SetVisible(-1, true);
            Color32[] before = Capture("before-default.png");
            SetSorting("ForeGround");
            Color32[] after = Capture("after-foreground.png");
            int beforePixels = Difference(baseline, before);
            int afterPixels = Difference(baseline, after);
            Write("COMPOSITE baseline-default=" + beforePixels + ", baseline-foreground=" + afterPixels + ", sorting-only-difference=" + Difference(before, after));
            Require(afterPixels > beforePixels + 100, "Sorting-only ForeGround pass reveals effects above the actual production background");
            var diff = new Color32[baseline.Length];
            for (int i = 0; i < diff.Length; i++)
                diff[i] = new Color32((byte)Math.Abs(after[i].r - before[i].r), (byte)Math.Abs(after[i].g - before[i].g), (byte)Math.Abs(after[i].b - before[i].b), 255);
            Save(diff, "sorting-difference.png");
            for (int i = 0; i < effects.Count; i++)
            {
                SetVisible(i, true); SetSorting("Default");
                Color32[] defaultPixels = Capture(Names[i].ToLowerInvariant() + "-default.png");
                SetSorting("ForeGround");
                Color32[] foregroundPixels = Capture(Names[i].ToLowerInvariant() + "-foreground.png");
                int hidden = Difference(baseline, defaultPixels), visible = Difference(baseline, foregroundPixels);
                Write("PIXEL " + Names[i] + ": Default changed=" + hidden + ", ForeGround changed=" + visible + ", sorting-only=" + Difference(defaultPixels, foregroundPixels));
                Require(visible > (i == 3 ? 50 : 3) && visible > hidden + (i == 3 ? 30 : 3),
                    Names[i] + " changes from background-hidden to visible using sorting layer alone");
            }
            CheckOptionalOutlines();
            TestMethodShaderSupportCurrentPipeline();
            Require(QualitySettings.renderPipeline == qualityPipeline && GraphicsSettings.defaultRenderPipeline == defaultPipeline,
                "Production QualitySettings/default URP assets remain unchanged after all A/B renders");
            Finish(true, "PASS " + checks + " real URP render/sorting/optional-outline checks. No source assets/settings saved. Pickup body shader/material and original .5/.38 scales retained; native RefreshOutline and every authored flesh variant checked when outlines exist; wind assigned material and line style retained.");
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static Color32[] Capture(string file)
    {
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        bool supportedBefore = RenderPipeline.SupportsRenderRequest(camera, request);
        RenderPipeline.SubmitRenderRequest(camera, request);
        bool supportedAfter = RenderPipeline.SupportsRenderRequest(camera, request);
        Write("RENDER " + file + ", supported-before=" + supportedBefore + ", supported-after=" + supportedAfter +
            ", pipeline=" + (RenderPipelineManager.currentPipeline != null ? RenderPipelineManager.currentPipeline.GetType().FullName : "null"));
        Require(supportedAfter && RenderPipelineManager.currentPipeline is UniversalRenderPipeline,
            file + " is submitted through production URP SingleCameraRequest");
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); texture.Apply();
        Color32[] pixels = texture.GetPixels32();
        File.WriteAllBytes(Path.Combine(output, file), texture.EncodeToPNG());
        RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture);
        return pixels;
    }

    private static void Save(Color32[] pixels, string file)
    {
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(Path.Combine(output, file), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }
    private static int Difference(Color32[] left, Color32[] right)
    {
        int changed = 0;
        for (int i = 0; i < left.Length; i++)
            if (Math.Max(Math.Abs(left[i].r - right[i].r), Math.Max(Math.Abs(left[i].g - right[i].g), Math.Abs(left[i].b - right[i].b))) > 3) changed++;
        return changed;
    }

    private static void CheckOptionalOutlines()
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            PickupDetails pickup = pickups[i];
            if (pickup.Outlines.Length == 0)
            {
                Write("OUTLINE " + Names[i] + ": baseline prefab has no outlines; optional outline render checks skipped");
                continue;
            }
            Require(pickup.Outlines.Length == 8, Names[i] + " has eight authored white outline renderers");
            SetVisible(i, true); SetSorting("ForeGround"); RefreshOutline(pickup);
            foreach (SpriteRenderer outline in pickup.Outlines)
            {
                Require(outline != null && outline.sprite == pickup.Body.sprite, Names[i] + " outline follows the selected native body sprite");
                Material material = outline.sharedMaterial;
                Shader shader = material != null ? material.shader : null;
                Require(shader != null && shader.name.Replace(" ", "").StartsWith("LoveTrain/PickupWhite", StringComparison.Ordinal) &&
                    shader.isSupported && !ShaderUtil.ShaderHasError(shader), Names[i] + " assigned white silhouette shader is supported and compiles without errors");
            }
            CompareOutline(i, Names[i].ToLowerInvariant());
            if (i == 0)
            {
                Require(pickup.FleshVariants.Length == 5, "All five authored meat sprites are available to the native outline synchronization check");
                Sprite selected = pickup.Body.sprite;
                for (int variant = 0; variant < pickup.FleshVariants.Length; variant++)
                {
                    Require(pickup.FleshVariants[variant] != null, "Meat variant " + variant + " has a real sprite");
                    pickup.Body.sprite = pickup.FleshVariants[variant];
                    RefreshOutline(pickup);
                    foreach (SpriteRenderer outline in pickup.Outlines)
                        Require(outline.sprite == pickup.Body.sprite, "Native RefreshOutline synchronizes meat variant " + variant + " on each white silhouette");
                    CompareOutline(i, "flesh-variant-" + variant);
                }
                pickup.Body.sprite = selected; RefreshOutline(pickup);
            }
            Require(pickup.Body.sharedMaterial == pickup.OriginalBodyMaterial, Names[i] + " body material/shader identity is unchanged through sorting and outline A/B renders");
        }
    }

    private static void CompareOutline(int index, string name)
    {
        PickupDetails pickup = pickups[index];
        SetVisible(index, true);
        foreach (SpriteRenderer outline in pickup.Outlines) outline.enabled = false;
        Color32[] body = Capture(name + "-body-only.png");
        foreach (SpriteRenderer outline in pickup.Outlines) outline.enabled = true;
        Color32[] outlined = Capture(name + "-outlined.png");
        int difference = Difference(body, outlined);
        int newWhite = 0;
        for (int pixel = 0; pixel < body.Length; pixel++)
        {
            Color32 after = outlined[pixel], before = body[pixel];
            bool changed = Math.Max(Math.Abs(after.r - before.r), Math.Max(Math.Abs(after.g - before.g), Math.Abs(after.b - before.b))) > 3;
            if (changed && after.r > 230 && after.g > 230 && after.b > 230 && !(before.r > 230 && before.g > 230 && before.b > 230)) newWhite++;
        }
        Write("OUTLINE PIXEL " + name + ": body-vs-outline=" + difference + ", added-white=" + newWhite);
        Require(difference > 0 && newWhite > 0, name + " white outlines produce real new white pixels in production URP with other effects disabled");
    }

    private static void RefreshOutline(PickupDetails pickup)
    {
        if (pickup.Outlines.Length == 0) return;
        MethodInfo refresh = typeof(RewardPickup).GetMethod("RefreshOutline", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(refresh != null, "Native RewardPickup.RefreshOutline is available for the frozen diagnostic sprite selection");
        refresh.Invoke(pickup.Component, null);
    }

    private static SpriteRenderer[] ReadSpriteRenderers(SerializedProperty property)
    {
        if (property == null || !property.isArray) return Array.Empty<SpriteRenderer>();
        var result = new SpriteRenderer[property.arraySize];
        for (int i = 0; i < result.Length; i++) result[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer;
        return result;
    }

    private static Sprite[] ReadSprites(SerializedProperty property)
    {
        if (property == null || !property.isArray) return Array.Empty<Sprite>();
        var result = new Sprite[property.arraySize];
        for (int i = 0; i < result.Length; i++) result[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
        return result;
    }
    private static void SetSorting(string layer)
    {
        Require(Array.Exists(SortingLayer.layers, candidate => candidate.name == layer), "Existing sorting layer " + layer + " is available");
        foreach (Renderer[] group in effects) foreach (Renderer renderer in group) renderer.sortingLayerName = layer;
    }
    private static void SetVisible(int only, bool visible)
    {
        for (int i = 0; i < effects.Count; i++) foreach (Renderer renderer in effects[i]) renderer.enabled = visible && (only < 0 || i == only);
    }
    private static void Guard()
    {
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.productName.StartsWith("DriveBossPreview_", StringComparison.Ordinal) ||
            !project.Replace('\\', '/').EndsWith("/outputs/drive-boss-validation/project", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only the existing disposable outputs/drive-boss-validation/project may run this memory-only probe.");
    }
    private static void Finish(bool passed, string message)
    {
        running = false;
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../effect-visibility-validation"));
        Directory.CreateDirectory(output);
        Write((passed ? "PASS " : "FAIL ") + message);
        Application.logMessageReceived -= Log;
        if (camera != null) camera.targetTexture = null;
        foreach (UnityEngine.Object obj in temporary) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        temporary.Clear();
        EditorApplication.Exit(passed ? 0 : 1);
    }
    private static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Log || string.IsNullOrEmpty(output)) return;
        File.AppendAllText(Path.Combine(output, "console.txt"), "[" + type + "] " + message + "\n" + stack + "\n");
    }
    private static string Name(UnityEngine.Object obj) => obj != null ? obj.name : "(null)";
    private static string ResultPath => Path.Combine(output, "render-result.txt");
    private static void Write(string message) => File.AppendAllText(ResultPath, message + "\n");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++; Write("CHECK " + message);
    }
}
