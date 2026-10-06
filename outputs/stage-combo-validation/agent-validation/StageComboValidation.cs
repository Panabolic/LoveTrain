using System;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Copy-only fixture: real sources, scene references and presenter methods in EditMode.
// RenderTexture captures retain the authored HUD on a neutral backdrop; not gameplay captures.
public static class StageComboValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly StringBuilder Evidence = new StringBuilder();
    private static int checks;
    private static int captures;
    private static string destination;
    private static GameManager game;
    private static StageManager stage;
    private static ComboKillUI comboUI;
    private static Component progressUI;
    private static RectTransform routeRoot;
    private static RectTransform comboRoot;
    private static TMP_Text timer;
    private static Canvas canvas;
    private static Camera camera;
    private static RenderTexture target;

    public static void Run()
    {
        destination = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../stage-combo-validation/agent-validation"));
        Directory.CreateDirectory(destination);
        try
        {
            Guard();
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Fixture starts in EditMode");
            ProbeCalculations();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
            game = Find<GameManager>(scene);
            stage = Find<StageManager>(scene);
            routeRoot = FindTransform(scene, "StageProgressHUD") as RectTransform;
            comboRoot = FindTransform(scene, "ComboKillHUD") as RectTransform;
            timer = FindTransform(scene, "TimeText")?.GetComponent<TMP_Text>();
            Require(routeRoot != null && comboRoot != null && timer != null, "Authored route, combo and existing timer exist");
            progressUI = FindComponent(routeRoot, "StageProgressUI");
            comboUI = comboRoot.GetComponent<ComboKillUI>();
            Require(progressUI != null && comboUI != null, "Authored presenter scripts resolve");
            CheckReferences(progressUI, new[] { "stageManager", "progressFill", "currentPosition" });
            CheckArrayReferences(progressUI, "eventMarkers", 3);
            CheckArrayReferences(progressUI, "eventSymbols", 3);
            CheckReferences(comboUI, new[] { "countText", "godMessage", "timeFill", "timeTrack", "visibility" });
            Require(Get<StageManager>(progressUI, "stageManager") == stage, "Route displays scene StageManager");
            canvas = routeRoot.GetComponentInParent<Canvas>();
            Require(canvas != null && comboRoot.GetComponentInParent<Canvas>() == canvas && timer.GetComponentInParent<Canvas>() == canvas,
                "Route, timer and combo share the authored Canvas");
            int missing = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            Require(missing == 0, "Junmo has no missing MonoBehaviour scripts");
            foreach (RectTransform root in new[] { routeRoot, comboRoot })
                foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                    Require(!graphic.raycastTarget, "HUD graphic does not intercept input: " + graphic.name);

            SetStatic(typeof(GameManager), "<Instance>k__BackingField", game);
            Set(game, "<CurrentState>k__BackingField", GameState.Playing);
            Call(stage, "Awake");
            Time.timeScale = 1f;
            ProbeGameManager();
            ProbePresenters();
            PrepareRendering();
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1024, 768) })
            {
                ConfigureSize(size.x, size.y);
                foreach (int count in new[] { 0, 9, 10, 100 })
                {
                    SetPresentationState(count, count == 0 ? 0f : 2.2f, 0.4f);
                    CheckTextBounds();
                    CheckLayout(size.x, size.y);
                    Capture("unity-stage-combo-" + count + "-" + size.x + "x" + size.y + ".png");
                }
                SetPresentationState(18, 0f, 0.4f);
                Require(Get<CanvasGroup>(comboUI, "visibility").alpha == 0f, "Expired combo remains hidden at " + size);
                Capture("unity-stage-combo-expired-" + size.x + "x" + size.y + ".png");
            }
            Evidence.AppendLine("PASS: " + checks + " real-source / authored-scene EditMode checks; " + captures + " UI-only RenderTexture images.");
            Evidence.AppendLine("No PlayMode, collision/input, player build or gameplay screenshot claim.");
            File.WriteAllText(ResultPath, Evidence.ToString());
            Debug.Log("PASS StageComboValidation: checks=" + checks + ", captures=" + captures);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Evidence.AppendLine("FAIL after " + checks + " checks: " + exception);
            File.WriteAllText(ResultPath, Evidence.ToString());
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ProbeCalculations()
    {
        Require(StageDistanceProgress.DefaultStageLength == 320f * 180f, "Default stage length is 57,600 distance units");
        foreach (float speed in new[] { 160f, 320f, 460f, 690f })
        {
            var progress = new StageDistanceProgress();
            progress.Reset(StageDistanceProgress.DefaultStageLength);
            float duration = StageDistanceProgress.DefaultStageLength / speed;
            progress.Advance(speed, duration * 0.5f, true);
            Require(Close(progress.StageDistance, 28800f, 0.01f) && Close(progress.NormalizedProgress, 0.5f),
                "Distance uses actual speed " + speed + " before endpoint");
            progress.Advance(speed, duration, true);
            Require(progress.StageDistance == progress.StageLength && progress.NormalizedProgress == 1f,
                "Distance saturates at endpoint for speed " + speed);
        }
        var fixedSpeed = new StageDistanceProgress();
        fixedSpeed.Reset(57600f);
        fixedSpeed.Advance(320f, 45f, true);
        Require(fixedSpeed.StageDistance == 14400f && fixedSpeed.NormalizedProgress == 0.25f, "25% event point at 14,400");
        fixedSpeed.Advance(320f, 45f, true);
        Require(fixedSpeed.StageDistance == 28800f && fixedSpeed.NormalizedProgress == 0.5f, "50% event point at 28,800");
        fixedSpeed.Advance(320f, 45f, true);
        Require(fixedSpeed.StageDistance == 43200f && fixedSpeed.NormalizedProgress == 0.75f, "75% event point at 43,200");
        fixedSpeed.Advance(320f, 44.9f, true);
        Require(fixedSpeed.NormalizedProgress < 1f, "Base speed remains before endpoint before 180 seconds");
        fixedSpeed.Advance(320f, 0.2f, true);
        Require(fixedSpeed.NormalizedProgress == 1f, "Base speed reaches endpoint around distance-derived 180 seconds");
        fixedSpeed.BeginNextStage(57600f);
        Require(fixedSpeed.StageNumber == 2 && fixedSpeed.StageDistance == 0f, "Next stage increments run counter and resets distance");
        fixedSpeed.Advance(320f, 30f, false);
        fixedSpeed.Advance(-320f, 30f, true);
        fixedSpeed.Advance(320f, -30f, true);
        Require(fixedSpeed.StageDistance == 0f, "Inactive travel and negative speed or time do not advance distance");
        fixedSpeed.Reset(57600f);
        Require(fixedSpeed.StageNumber == 1 && fixedSpeed.StageDistance == 0f, "Restart resets run stage counter");
        var state = new ComboKillState();
        Require(state.Count == 0 && state.RemainingTime == 0f, "Combo begins empty");
        state.RegisterKill(); state.Advance(2.99f);
        Require(state.Count == 1 && state.RemainingTime > 0f, "Combo survives just before 3-second boundary");
        state.Advance(0.02f);
        Require(state.Count == 0 && state.RemainingTime == 0f, "Combo expires after 3 seconds");
        state.RegisterKill(); state.Advance(2f); state.RegisterKill();
        Require(state.Count == 2 && state.RemainingTime == 3f && state.Fraction == 1f, "Each next kill refills its 3-second clock");
        state.Advance(1.5f);
        Require(Close(state.Fraction, 0.5f), "Combo underline fraction at half lifetime");
        state.Advance(-1f); state.Advance(float.NaN); state.Advance(float.PositiveInfinity);
        Require(Close(state.RemainingTime, 1.5f), "Invalid deltas do not corrupt combo lifetime");
        foreach (int count in new[] { 1, 9, 10, 99, 100, 999, 1000, int.MaxValue })
        {
            string digits = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Require(ComboKillState.FormatLabel(count) == "Combo Kill " + digits + new string('!', digits.Length - 1),
                "Digit-based punctuation for " + count);
        }
        var schedule = new StageEventSchedule();
        schedule.BeginStage(1);
        foreach (float fraction in new[] { 0.25f, 0.5f, 0.75f })
        {
            Require(!schedule.IsDue(57600f * fraction - 1f, 57600f), "Event remains pending just before " + fraction);
            Require(schedule.IsDue(57600f * fraction, 57600f), "Event becomes due exactly at " + fraction);
            schedule.RecordSpawn();
            Require(!schedule.IsDue(57600f * fraction, 57600f), "Event is not duplicated at " + fraction);
        }
        Require(schedule.SpawnedEvents == 3 && !schedule.IsDue(57600f, 57600f), "Exactly three route events per stage");
        schedule.BeginStage(2);
        int catchUp = 0;
        while (schedule.IsDue(57600f, 57600f)) { schedule.RecordSpawn(); catchUp++; }
        Require(catchUp == 3 && schedule.StageNumber == 2, "Overshooting multiple route nodes catches each once");
        Require(!StageEventSchedule.IsUpgradeEncounter(1, 1, true, 0f, 1f / 3f) &&
            StageEventSchedule.IsUpgradeEncounter(1, 2, true, 1f, 1f / 3f) &&
            StageEventSchedule.IsUpgradeEncounter(2, 1, true, 1f, 1f / 3f), "Existing first/second-stage upgrade selection preserved");
        var bossSchedule = new StageBossSchedule();
        Require(bossSchedule.GetDueRequest(true, 1, 57599f, 57600f, 180f, 900f) == StageBossRequest.None,
            "Elapsed 180 seconds alone does not request a boss");
        Require(bossSchedule.GetDueRequest(true, 1, 57600f, 57600f, 90f, 900f) == StageBossRequest.Stage,
            "Distance endpoint requests boss even before 180 seconds");
        bossSchedule.RecordRequest(StageBossRequest.Stage, 1);
        Require(bossSchedule.GetDueRequest(true, 1, 57600f, 57600f, 180f, 900f) == StageBossRequest.None,
            "Completed route requests a stage boss only once");
        Require(bossSchedule.GetDueRequest(true, 2, 57600f, 57600f, 400f, 900f) == StageBossRequest.Stage,
            "Independent run stage number permits next route boss");
        Require(bossSchedule.GetDueRequest(false, 2, 57600f, 57600f, 900f, 900f) == StageBossRequest.None,
            "No boss request while game is outside Playing");
        Require(bossSchedule.GetDueRequest(true, 2, 57600f, 57600f, 900f, 900f) == StageBossRequest.Final,
            "Final boss takes priority when 15-minute and route boundaries coincide");
        Require(bossSchedule.GetDueRequest(true, 2, 0f, 57600f, 900f, 900f) == StageBossRequest.Final,
            "Final boss can become due before current route completes");
        bossSchedule.RecordRequest(StageBossRequest.Final, 2);
        Require(bossSchedule.GetDueRequest(true, 3, 57600f, 57600f, 900f, 900f) == StageBossRequest.None,
            "Final boss request suppresses later duplicate requests");
        bossSchedule.Reset();
        Require(bossSchedule.GetDueRequest(true, 1, 57600f, 57600f, 90f, 900f) == StageBossRequest.Stage,
            "New run resets request deduplication");
    }

    private static void ProbeGameManager()
    {
        ComboKillState state = Get<ComboKillState>(game, "comboKills");
        state.Reset();
        int normalBefore = game.NormalKillCount, eliteBefore = game.EliteKillCount;
        game.AddKillCount(false); game.AddKillCount(true);
        Require(game.ComboKillCount == 2 && game.NormalKillCount == normalBefore + 1 && game.EliteKillCount == eliteBefore + 1,
            "Actual normal and elite kill methods feed combo and preserve cumulative statistics");
        game.AddBossKillCount();
        Require(game.ComboKillCount == 2, "Boss kill statistic does not add a combo kill");
        foreach (GameState mode in new[] { GameState.Event, GameState.Pause, GameState.Start, GameState.Title })
        {
            Set(game, "<CurrentState>k__BackingField", mode);
            game.AddKillCount(false);
            Require(game.ComboKillCount == 2, "Kills outside combat do not extend combo in " + mode);
        }
        Set(game, "<CurrentState>k__BackingField", GameState.Boss);
        game.AddKillCount(false);
        Require(game.ComboKillCount == 3, "Kills during Boss combat feed combo");
        foreach (GameState mode in new[] { GameState.StageTransition, GameState.Die, GameState.Ending })
        {
            Set(game, "<CurrentState>k__BackingField", GameState.Playing);
            state.RegisterKill();
            game.ChangeState(mode);
            Require(game.ComboKillCount == 0 && game.ComboTimeRemaining == 0f, "Actual state transition resets combo in " + mode);
        }
        Set(game, "<CurrentState>k__BackingField", GameState.Playing);
    }

    private static void ProbePresenters()
    {
        TMP_Text[] symbols = Get<TMP_Text[]>(progressUI, "eventSymbols");
        Require(symbols.Length == 3, "Three event symbols authored");
        foreach (TMP_Text symbol in symbols) Require(symbol.text == "!", "Event node displays an exclamation icon");
        foreach (TMP_Text text in routeRoot.GetComponentsInChildren<TMP_Text>(true))
            Require(!text.text.Contains("%") && !text.text.Contains("이벤트") && !text.text.Contains("강화"),
                "Route has no removed node explanation or percent text: " + text.name);
        Call(comboUI, "OnEnable");
        Require(!Get<CanvasGroup>(comboUI, "visibility").blocksRaycasts && !Get<CanvasGroup>(comboUI, "visibility").interactable,
            "Combo presenter remains a passive HUD");
        foreach (int count in new[] { 0, 1, 9, 10, 99, 100, 1000 })
        {
            SetPresentationState(count, 2.2f, 0.4f);
            Require(Get<TMP_Text>(comboUI, "countText").text == ComboKillState.FormatLabel(count), "Real presenter formats count " + count);
            Require(Get<TMP_Text>(comboUI, "godMessage").gameObject.activeSelf == (count >= 10), "God message threshold at " + count);
            Require(Get<CanvasGroup>(comboUI, "visibility").alpha == (count > 0 ? 1f : 0f), "Combo visibility at " + count);
            if (count > 0)
            {
                TMP_Text title = Get<TMP_Text>(comboUI, "countText");
                Require(Close(Get<RectTransform>(comboUI, "timeTrack").rect.width, title.GetPreferredValues(title.text).x, 0.1f),
                    "Underline follows the actual text width at " + count);
                Require(Close(Get<Image>(comboUI, "timeFill").rectTransform.anchorMax.x, 2.2f / 3f),
                    "Sprite-free underline visibly scales with lifetime at " + count);
            }
        }
        SetPresentationState(18, 2f, 0.4f);
        Call(comboUI, "OnDisable");
        Require(Get<CanvasGroup>(comboUI, "visibility").alpha == 0f && !Get<TMP_Text>(comboUI, "godMessage").gameObject.activeSelf,
            "Disable hides combo and god message");
        Call(comboUI, "OnEnable");
        Require(Get<CanvasGroup>(comboUI, "visibility").alpha == 1f && Get<TMP_Text>(comboUI, "godMessage").gameObject.activeSelf,
            "Enable refreshes still-current state");
    }

    private static void SetPresentationState(int count, float remaining, float progress)
    {
        ComboKillState combo = Get<ComboKillState>(game, "comboKills");
        combo.Reset();
        for (int index = 0; index < count; index++) combo.RegisterKill();
        if (count > 0) combo.Advance(Mathf.Max(0f, 3f - remaining));
        StageDistanceProgress distance = Get<StageDistanceProgress>(stage, "distanceProgress");
        distance.Reset(57600f); distance.Advance(57600f, progress, true);
        timer.text = "12:47";
        Call(comboUI, "LateUpdate");
        Call(progressUI, "LateUpdate");
        Require(Close(Get<Image>(progressUI, "progressFill").rectTransform.anchorMax.x, progress), "Real route fill position " + progress);
        Require(Close(Get<RectTransform>(progressUI, "currentPosition").anchorMin.x, progress), "Real train marker position " + progress);
    }

    private static void PrepareRendering()
    {
        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        foreach (Camera existing in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) existing.enabled = false;
        foreach (Canvas other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (other != canvas && other.isRootCanvas) other.gameObject.SetActive(false);
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
            if (!graphic.transform.IsChildOf(routeRoot) && !graphic.transform.IsChildOf(comboRoot) && graphic != timer) graphic.enabled = false;
        foreach (Transform child in routeRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
        foreach (Transform child in comboRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
        timer.gameObject.layer = 5;
        camera = new GameObject("DisposableStageComboRenderCamera").AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.075f, 0.08f, 0.105f, 1f);
        camera.cullingMask = 1 << 5;
        camera.nearClipPlane = 0.01f; camera.farClipPlane = 100f;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera; canvas.planeDistance = 1f;
    }

    private static void ConfigureSize(int width, int height)
    {
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        target.Create(); camera.targetTexture = target; camera.aspect = (float)width / height;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = width / 800f;
        Call(scaler, "Handle");
        Canvas.ForceUpdateCanvases();
        Rect content = (Rect)typeof(FixedAspectRatioController).GetMethod("CalculateContentPixelRect", Members).Invoke(null, new object[] { width, height });
        Rect normalized = (Rect)typeof(FixedAspectRatioController).GetMethod("PixelRectToNormalized", Members).Invoke(null, new object[] { content, (float)width, (float)height });
        var aspect = new GameObject("DisposableStageComboAspectFixture").AddComponent<FixedAspectRatioController>();
        typeof(FixedAspectRatioController).GetMethod("ApplyCanvasRoots", Members).Invoke(aspect, new object[] { (RectTransform)canvas.transform, normalized });
        Layout();
        Evidence.AppendLine("LAYOUT capture=" + width + "x" + height + ", production content=" + content);
    }

    private static void CheckLayout(int width, int height)
    {
        Layout();
        Rect content = (Rect)typeof(FixedAspectRatioController).GetMethod("CalculateContentPixelRect", Members).Invoke(null, new object[] { width, height });
        Rect route = ScreenRect(routeRoot), clock = ScreenRect(timer.rectTransform), combo = ScreenRect(comboRoot);
        Require(Close(route.center.x, content.center.x, 1f), "Route remains horizontally centered at " + width + "x" + height);
        Require(route.yMin >= clock.yMax - 1f, "Timer stays below route at " + width + "x" + height);
        Require(combo.yMax <= clock.yMin + 1f, "Combo stays below timer at " + width + "x" + height);
        foreach (Rect rect in new[] { route, clock, combo })
            Require(rect.xMin >= content.xMin - 1f && rect.xMax <= content.xMax + 1f && rect.yMin >= content.yMin - 1f && rect.yMax <= content.yMax + 1f,
                "Authored HUD fits production safe area " + rect);
        Require(combo.xMax < content.xMax - 1f, "Combo retains right edge margin");
    }

    private static Rect ScreenRect(RectTransform transform)
    {
        Vector3[] corners = new Vector3[4]; transform.GetWorldCorners(corners);
        Vector3 lower = camera.WorldToScreenPoint(corners[0]), upper = camera.WorldToScreenPoint(corners[2]);
        return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
    }
    private static void CheckTextBounds()
    {
        Layout();
        foreach (RectTransform root in new[] { routeRoot, comboRoot })
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!text.gameObject.activeInHierarchy || !text.enabled) continue;
                Require(!text.isTextOverflowing, "Text fits authored rectangle: " + text.name + " = " + text.text);
                Require(text.textInfo.lineCount <= 1, "HUD text remains one line: " + text.name);
            }
    }
    private static void Capture(string name)
    {
        Layout(); camera.Render();
        RenderTexture prior = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
        File.WriteAllBytes(Path.Combine(destination, name), texture.EncodeToPNG()); captures++;
        UnityEngine.Object.DestroyImmediate(texture); RenderTexture.active = prior;
    }
    private static void Layout()
    {
        Canvas.ForceUpdateCanvases();
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true)) if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();
    }
    private static void CheckReferences(Component owner, string[] names)
    {
        var serialized = new SerializedObject(owner);
        foreach (string name in names) Require(serialized.FindProperty(name)?.objectReferenceValue != null, "Authored reference " + owner.GetType().Name + "." + name);
    }
    private static void CheckArrayReferences(Component owner, string name, int length)
    {
        var property = new SerializedObject(owner).FindProperty(name);
        Require(property != null && property.arraySize == length, "Authored array " + name + " length " + length);
        for (int index = 0; index < length; index++) Require(property.GetArrayElementAtIndex(index).objectReferenceValue != null, "Authored array " + name + " index " + index);
    }
    private static Component FindComponent(Transform root, string typeName)
    {
        foreach (Component component in root.GetComponents<Component>()) if (component != null && component.GetType().Name == typeName) return component;
        return null;
    }
    private static Transform FindTransform(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }
    private static T Find<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects()) { T item = root.GetComponentInChildren<T>(true); if (item != null) return item; }
        throw new InvalidOperationException("Scene component missing: " + typeof(T).Name);
    }
    private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Members).GetValue(owner);
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Members).SetValue(owner, value);
    private static void SetStatic(Type type, string field, object value) => type.GetField(field, Members).SetValue(null, value);
    private static void Call(object owner, string method) => owner.GetType().GetMethod(method, Members).Invoke(owner, null);
    private static bool Close(float actual, float expected, float tolerance = 0.0001f) => Mathf.Abs(actual - expected) < tolerance;
    private static string ResultPath => Path.Combine(destination, "unity-stage-combo-validation-result.txt");
    private static void Require(bool result, string label) { if (!result) throw new InvalidOperationException(label); checks++; Evidence.AppendLine("PASS " + label); }
    private static void Guard()
    {
        string path = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
        Require(path.EndsWith("/outputs/unity-ui-preview/project/Assets", StringComparison.OrdinalIgnoreCase), "Expected disposable project path verified");
        Require(Application.companyName == "CodexUIRenderPreview" && Application.productName.StartsWith("WorkbenchPreview_", StringComparison.Ordinal), "Unique disposable save identity verified");
    }
}
