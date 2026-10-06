using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Disposable project only. Invoke methods in EditMode and render the actual copied scene HUD.
// This is not PlayMode, gameplay, keyboard injection, or a player-build fixture.
public static class FleshHudValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly string[] ReferenceFields =
    {
        "wallet", "fleshAmount", "soulAmount", "creationLabel", "costLabel",
        "completedFill", "progressFill", "creationHint"
    };
    private static readonly Color[] Palette =
    {
        Hex("ED6969"), Hex("F59549"), Hex("F2D367"), Hex("6CCE90"), Hex("629BE9"),
        Hex("8A83DE"), Hex("BA85E7"), Hex("F198CC"), Hex("67D3D1"), Hex("F4EEDF")
    };
    private static readonly StringBuilder Evidence = new StringBuilder();
    private static int checks;
    private static int renders;
    private static string destination;
    private static Component presentation;
    private static RectTransform hudRoot;
    private static Canvas canvas;
    private static TrainLevelManager wallet;
    private static LevelUpUIManager workbench;
    private static GameManager game;
    private static RunPartEconomy economy;
    private static Camera camera;
    private static RenderTexture target;
    private static Keyboard fixtureKeyboard;

    public static void Run()
    {
        destination = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../flesh-hud-preview"));
        Directory.CreateDirectory(destination);
        try
        {
            Require(Application.companyName == "CodexUIRenderPreview" &&
                Application.productName.StartsWith("WorkbenchPreview_", StringComparison.Ordinal),
                "Refuse all probes unless disposable PlayerPrefs identity is verified");
            Require(!EditorApplication.isPlaying, "EditMode only");
            fixtureKeyboard = InputSystem.AddDevice<Keyboard>("FleshHudFixtureKeyboard");
            fixtureKeyboard.MakeCurrent();
            EnglishLocalization.SetLanguage(false);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
            hudRoot = FindTransform(scene, "CurrencyHUD") as RectTransform;
            Require(hudRoot != null, "Authored CurrencyHUD exists");
            foreach (Component component in hudRoot.GetComponents<Component>())
                if (component != null && component.GetType().Name == "FleshHud") presentation = component;
            Require(presentation != null, "Authored FleshHud script resolves");
            var serialized = new SerializedObject(presentation);
            foreach (string field in ReferenceFields)
            {
                var property = serialized.FindProperty(field);
                Require(property != null && property.objectReferenceValue != null, "Authored reference: " + field);
            }
            wallet = Get<TrainLevelManager>(presentation, "wallet");
            var walletSerialized = new SerializedObject(wallet);
            Require(walletSerialized.FindProperty("fleshText").objectReferenceValue == null &&
                walletSerialized.FindProperty("soulText").objectReferenceValue == null,
                "Wallet no longer writes over FleshHud's authored labels");
            game = Find<GameManager>();
            workbench = Find<LevelUpUIManager>();
            canvas = hudRoot.GetComponentInParent<Canvas>();
            Require(canvas != null, "HUD has its existing Canvas");
            Require(hudRoot.GetComponentsInChildren<Button>(true).Length == 0, "F instruction remains a passive keyboard hint");
            foreach (Graphic graphic in hudRoot.GetComponentsInChildren<Graphic>(true))
                Require(!graphic.raycastTarget, "Passive HUD graphic: " + graphic.name);
            int missing = 0;
            foreach (Transform child in hudRoot.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            Require(missing == 0, "HUD has no missing scripts");

            SetStatic(typeof(GameManager), "<Instance>k__BackingField", game);
            LevelUpUIManager.Instance = workbench;
            Set(game, "<CurrentState>k__BackingField", GameState.Playing);
            Time.timeScale = 1f;
            var inventory = Get<Inventory>(workbench, "playerInventory");
            inventory.items.Clear();
            Require(wallet.GetComponent<Inventory>() == inventory, "HUD wallet and workbench share the same inventory owner");
            Set(wallet, "creationQueued", false);
            Set(wallet, "committingCreation", false);
            ProbeAvailabilityCache(inventory);

            ProbeAmount(0, 0, 0f, false, "zero");
            ProbeAmount(18, 0, 0.6f, false, "partial");
            ProbeAmount(29, 0, 29f / 30f, false, "one-short");
            ProbeAmount(30, 1, 0f, true, "one-ready");
            ProbeAmount(70, 2, 0f, true, "two-exact");
            ProbeAmount(105, 2, 0.7f, true, "two-and-overflow");
            ProbeAmount(120, 3, 0f, true, "three-exact");
            ProbeAmount(420, 7, 0f, true, "rainbow-complete");
            ProbeAmount(520, 8, 0f, true, "pink-complete");
            ProbeAmount(630, 9, 0f, true, "cyan-complete");
            ProbeAmount(750, 10, 0f, true, "white-complete");
            ProbeAmount(880, 11, 0f, true, "palette-wrap");

            Seed(105);
            Require(economy.TryPurchaseCreation(wallet.CreationCost), "In-memory fixture purchases first cost");
            Refresh();
            CheckHud(75, 1, 0.7f, true, 40, "after-first");
            Require(economy.TryPurchaseCreation(wallet.CreationCost), "In-memory fixture purchases increased cost");
            Refresh();
            CheckHud(35, 0, 0.7f, false, 50, "after-second");

            Seed(105);
            foreach (GameState state in new[] { GameState.Boss, GameState.Pause, GameState.Event, GameState.Die, GameState.StageTransition, GameState.Start })
            {
                Set(game, "<CurrentState>k__BackingField", state);
                Refresh();
                Require(HintVisible() == (state == GameState.Boss), "Hint gate: " + state);
            }
            Set(game, "<CurrentState>k__BackingField", GameState.Playing);
            Time.timeScale = 0f;
            Refresh();
            Require(!HintVisible(), "Paused timescale hides F hint");
            Time.timeScale = 1f;
            Set(wallet, "creationQueued", true);
            Refresh();
            Require(!HintVisible(), "Queued creation hides F hint");
            Set(wallet, "creationQueued", false);
            Set(wallet, "committingCreation", true);
            Refresh();
            Require(!HintVisible(), "Creation transaction hides F hint");
            Set(wallet, "committingCreation", false);
            FieldInfo mode = workbench.GetType().GetField("mode", Members);
            mode.SetValue(workbench, Enum.Parse(mode.FieldType, "Creation"));
            Refresh();
            Require(!HintVisible(), "An open workbench hides F hint");
            mode.SetValue(workbench, Enum.Parse(mode.FieldType, "None"));
            ItemDatabase originalDatabase = Get<ItemDatabase>(workbench, "itemDatabase");
            ItemDatabase emptyDatabase = ScriptableObject.CreateInstance<ItemDatabase>();
            try
            {
                Set(workbench, "itemDatabase", emptyDatabase);
                Refresh();
                Require(!HintVisible(), "No acquisition candidate hides F hint");
            }
            finally
            {
                Set(workbench, "itemDatabase", originalDatabase);
                UnityEngine.Object.DestroyImmediate(emptyDatabase);
            }
            Refresh();
            Require(HintVisible(), "F hint restores when creation is available again");

            EnglishLocalization.SetLanguage(true);
            Refresh();
            Require(Get<TMP_Text>(presentation, "creationLabel").text.Contains("X 2"), "English label preserves available count");
            Require(!Get<TMP_Text>(presentation, "creationLabel").text.Contains("조직"), "English label is localized");
            Require(Get<TMP_Text>(presentation, "fleshAmount").text == "Flesh 105",
                "English flesh label and value");
            Require(Get<TMP_Text>(presentation, "soulAmount").text == "Souls 7", "English soul label and value");
            Require(Get<TMP_Text>(presentation, "costLabel").text == "Cost 30", "English cost label and value");
            EnglishLocalization.SetLanguage(false);
            Refresh();
            Require(Get<TMP_Text>(presentation, "creationLabel").text == "조직 창조 X 2", "Korean label restores");

            PrepareRendering(scene);
            foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1024, 768) })
            {
                ConfigureSize(size.x, size.y);
                Seed(105); Refresh();
                CheckTextBounds();
                Capture("unity-hud-105-" + size.x + "x" + size.y + ".png");
                if (size.x != 1280) continue;
                Require(economy.TryPurchaseCreation(wallet.CreationCost), "Render first creation cost");
                Refresh(); Capture("unity-hud-75-1280x720.png");
                Require(economy.TryPurchaseCreation(wallet.CreationCost), "Render second creation cost");
                Refresh(); Capture("unity-hud-35-1280x720.png");
                EnglishLocalization.SetLanguage(true); Seed(105); Refresh();
                CheckTextBounds(); Capture("unity-hud-105-en-1280x720.png");
                EnglishLocalization.SetLanguage(false);
                foreach (bool english in new[] { false, true })
                {
                    EnglishLocalization.SetLanguage(english);
                    foreach (int flesh in new[] { 1170, int.MaxValue })
                    {
                        Seed(flesh); Refresh();
                        if (size.x == 1280)
                            Capture("unity-hud-high-" + flesh + "-" + (english ? "en" : "ko") + "-1280x720.png");
                        CheckTextBounds();
                    }
                }
                EnglishLocalization.SetLanguage(false);
            }
            Evidence.AppendLine("PASS: " + checks + " actual copied-source / authored-scene EditMode checks; " + renders + " UI-only RenderTexture images.");
            Evidence.AppendLine("No original project changes, PlayMode, gameplay, real F key injection, GraphicRaycaster, or player-build claim.");
            File.WriteAllText(Path.Combine(destination, "unity-hud-validation-result.txt"), Evidence.ToString());
            Debug.Log(Evidence.ToString());
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Evidence.AppendLine("FAIL after " + checks + " checks: " + exception);
            File.WriteAllText(Path.Combine(destination, "unity-hud-validation-result.txt"), Evidence.ToString());
            EditorApplication.Exit(1);
        }
        finally
        {
            if (fixtureKeyboard != null && fixtureKeyboard.added) InputSystem.RemoveDevice(fixtureKeyboard);
        }
    }

    private static void Seed(int flesh)
    {
        economy = new RunPartEconomy(7);
        economy.AddReward(flesh);
        Set(wallet, "economy", economy); // Bypass persistence callbacks entirely.
    }

    private static void ProbeAvailabilityCache(Inventory inventory)
    {
        ItemDatabase originalDatabase = Get<ItemDatabase>(workbench, "itemDatabase");
        var originalItems = new List<ItemInstance>(inventory.items);
        ItemDatabase candidateDatabase = ScriptableObject.CreateInstance<ItemDatabase>();
        ItemDatabase emptyDatabase = ScriptableObject.CreateInstance<ItemDatabase>();
        Item_SO candidate = ScriptableObject.CreateInstance<Item_SO>();
        Item_SO blocker = ScriptableObject.CreateInstance<Item_SO>();
        candidate.allowedEquipmentSlots = EquipmentSlotMask.HeadTop;
        blocker.allowedEquipmentSlots = EquipmentSlotMask.HeadTop;
        candidateDatabase.allItems = new List<Item_SO> { candidate };
        emptyDatabase.allItems = new List<Item_SO>();
        try
        {
            // Manually pair the real callbacks so this EditMode probe uses the actual inventory subscription.
            Call(workbench, "OnDisable");
            Set(workbench, "itemDatabase", candidateDatabase);
            inventory.items.Clear();
            Call(workbench, "OnEnable");
            Seed(105); Refresh();
            Require(HintVisible(), "Candidate cache starts with an available authored slot");
            Require(!Get<bool>(workbench, "creationAvailabilityDirty"), "First availability query resolves dirty cache");
            for (int index = 0; index < 120; index++) Call(presentation, "LateUpdate");
            Require(!Get<bool>(workbench, "creationAvailabilityDirty") && HintVisible(),
                "Repeated LateUpdate queries reuse the resolved availability state");

            inventory.items.Add(new ItemInstance(blocker) { equippedSlotIndex = 0 });
            RaiseInventoryChanged(inventory);
            Require(Get<bool>(workbench, "creationAvailabilityDirty"), "Inventory event invalidates availability cache");
            Call(presentation, "LateUpdate");
            Require(!HintVisible() && !Get<bool>(workbench, "creationAvailabilityDirty"),
                "Occupied candidate slot hides hint on the next real LateUpdate method");
            inventory.items.Clear();
            RaiseInventoryChanged(inventory);
            Call(presentation, "LateUpdate");
            Require(HintVisible(), "Released candidate slot restores hint after inventory event");

            Set(workbench, "itemDatabase", emptyDatabase);
            Call(presentation, "LateUpdate");
            Require(!HintVisible(), "Replacing the database invalidates a previously positive cached result");
            Set(workbench, "itemDatabase", candidateDatabase);
            Call(presentation, "LateUpdate");
            Require(HintVisible(), "Restoring candidate database invalidates a previously negative cached result");

            Call(workbench, "OnDisable");
            Set(workbench, "creationAvailabilityDirty", false);
            RaiseInventoryChanged(inventory);
            Require(!Get<bool>(workbench, "creationAvailabilityDirty"), "OnDisable releases the inventory invalidation subscription");
            Call(workbench, "OnEnable");
            Require(Get<bool>(workbench, "creationAvailabilityDirty"), "OnEnable resets the availability cache");
        }
        finally
        {
            Call(workbench, "OnDisable");
            Set(workbench, "itemDatabase", originalDatabase);
            inventory.items.Clear(); inventory.items.AddRange(originalItems);
            Call(workbench, "OnEnable");
            UnityEngine.Object.DestroyImmediate(candidateDatabase);
            UnityEngine.Object.DestroyImmediate(emptyDatabase);
            UnityEngine.Object.DestroyImmediate(candidate);
            UnityEngine.Object.DestroyImmediate(blocker);
        }
    }

    private static void RaiseInventoryChanged(Inventory inventory)
    {
        Action changed = Get<Action>(inventory, "OnInventoryChanged");
        changed?.Invoke();
    }

    private static void ProbeAmount(int flesh, int count, float fraction, bool hint, string label)
    {
        Seed(flesh);
        Refresh();
        CheckHud(flesh, count, fraction, hint, 30, label);
    }

    private static void CheckHud(int flesh, int count, float fraction, bool hint, int cost, string label)
    {
        TMP_Text amount = Get<TMP_Text>(presentation, "fleshAmount");
        TMP_Text souls = Get<TMP_Text>(presentation, "soulAmount");
        TMP_Text creation = Get<TMP_Text>(presentation, "creationLabel");
        TMP_Text costText = Get<TMP_Text>(presentation, "costLabel");
        Image completed = Get<Image>(presentation, "completedFill");
        Image progress = Get<Image>(presentation, "progressFill");
        Require(amount.text == "살점 " + flesh, label + ": actual flesh quantity at uniform text size");
        Require(souls.text == "영혼 7", label + ": actual soul quantity");
        Require(costText.text == "비용 " + cost, label + ": current cost");
        Require(!amount.text.Contains(flesh + "/") && !costText.text.Contains("/"), label + ": no removed fraction labels");
        Require(Mathf.Abs(progress.fillAmount - fraction) < 0.0001f, label + ": next increasing-cost fraction");
        Require(Mathf.Abs(progress.rectTransform.anchorMax.x - fraction) < 0.0001f,
            label + ": sprite-free Simple gauge width matches progress");
        Require(CloseColor(progress.color, Palette[count % Palette.Length]), label + ": overflow palette color");
        Require(completed.enabled == (count > 0), label + ": completed layer visibility");
        if (count > 0)
            Require(CloseColor(completed.color, Palette[(count - 1) % Palette.Length]), label + ": completed palette color");
        Require(HintVisible() == hint, label + ": creation hint visibility");
        if (hint) Require(creation.text == (count >= 2 ? "조직 창조 X " + count : "조직 창조"), label + ": creation count wording");
    }

    private static void Refresh()
    {
        MethodInfo method = presentation.GetType().GetMethod("RefreshResources", Members);
        if (method == null) throw new MissingMethodException("FleshHud.RefreshResources is missing.");
        method.Invoke(presentation, null);
        presentation.GetType().GetMethod("RefreshCreationHint", Members).Invoke(presentation, new object[] { true });
    }

    private static bool HintVisible()
    {
        UnityEngine.Object hint = Get<UnityEngine.Object>(presentation, "creationHint");
        GameObject owner = hint is GameObject go ? go : ((Component)hint).gameObject;
        return owner.activeSelf;
    }

    private static void PrepareRendering(UnityEngine.SceneManagement.Scene scene)
    {
        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        foreach (Camera existing in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            existing.enabled = false;
        // Keep only the actual HUD; the neutral backdrop is deliberately not gameplay capture.
        Transform ancestor = hudRoot;
        while (ancestor != null && ancestor != canvas.transform)
        {
            Transform parent = ancestor.parent;
            if (parent != null)
                for (int index = 0; index < parent.childCount; index++)
                    if (parent.GetChild(index) != ancestor) parent.GetChild(index).gameObject.SetActive(false);
            ancestor = parent;
        }
        foreach (Canvas other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (other != canvas && other.isRootCanvas) other.gameObject.SetActive(false);
        foreach (Transform child in hudRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
        camera = new GameObject("DisposableHudRenderCamera").AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.075f, 0.08f, 0.105f, 1f);
        camera.cullingMask = 1 << 5;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
    }

    private static void ConfigureSize(int width, int height)
    {
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        target.Create(); camera.targetTexture = target;
        camera.aspect = (float)width / height;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = width / 800f;
        typeof(CanvasScaler).GetMethod("Handle", Members).Invoke(scaler, null);
        Canvas.ForceUpdateCanvases();
        Rect content = (Rect)typeof(FixedAspectRatioController).GetMethod("CalculateContentPixelRect", Members).Invoke(null, new object[] { width, height });
        Rect normalized = (Rect)typeof(FixedAspectRatioController).GetMethod("PixelRectToNormalized", Members).Invoke(null, new object[] { content, (float)width, (float)height });
        var aspect = new GameObject("DisposableHudAspectFixture").AddComponent<FixedAspectRatioController>();
        typeof(FixedAspectRatioController).GetMethod("ApplyCanvasRoots", Members).Invoke(aspect, new object[] { (RectTransform)canvas.transform, normalized });
        Layout();
        Vector3[] corners = new Vector3[4];
        hudRoot.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
        {
            Vector3 point = camera.WorldToScreenPoint(corner);
            Require(point.x >= content.xMin - 1f && point.x <= content.xMax + 1f &&
                point.y >= content.yMin - 1f && point.y <= content.yMax + 1f,
                width + "x" + height + ": HUD corner inside production 16:9 content area");
        }
        Evidence.AppendLine("Rendered size " + width + "x" + height + "; production content pixel rect " + content + "; HUD logical rect " + hudRoot.rect);
    }

    private static void CheckTextBounds()
    {
        Layout();
        TMP_Text flesh = Get<TMP_Text>(presentation, "fleshAmount");
        TMP_Text souls = Get<TMP_Text>(presentation, "soulAmount");
        Require(Mathf.Abs(flesh.fontSize - souls.fontSize) < 0.0001f, "Flesh and soul text use the same font size");
        Require(flesh.alignment == TextAlignmentOptions.MidlineRight && souls.alignment == TextAlignmentOptions.MidlineRight,
            "Flesh and soul text are both right aligned");
        Require(!flesh.text.Contains("<size") && !souls.text.Contains("<size"), "Currency labels and quantities have no smaller rich-text size");
        Vector3[] fleshCorners = new Vector3[4];
        Vector3[] soulCorners = new Vector3[4];
        flesh.rectTransform.GetWorldCorners(fleshCorners);
        souls.rectTransform.GetWorldCorners(soulCorners);
        float fleshRight = camera.WorldToScreenPoint(fleshCorners[2]).x;
        float soulRight = camera.WorldToScreenPoint(soulCorners[2]).x;
        Require(Mathf.Abs(fleshRight - soulRight) < 0.5f, "Flesh and soul text share their screen right edge");
        Evidence.AppendLine("CURRENCY ALIGNMENT font=" + flesh.fontSize + ", fleshRight=" + fleshRight + ", soulRight=" + soulRight);
        foreach (TMP_Text text in hudRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!text.gameObject.activeInHierarchy) continue;
            Evidence.AppendLine("TEXT " + text.name + ": preferred=" + text.preferredWidth + "x" + text.preferredHeight +
                ", rect=" + text.rectTransform.rect.width + "x" + text.rectTransform.rect.height + ", font=" + text.fontSize +
                ", overflow=" + text.isTextOverflowing + ", value=" + text.text);
            Require(!text.isTextOverflowing, "Text fits: " + text.name + " = " + text.text);
            Require(text.textInfo.lineCount <= 1, "One-line HUD label: " + text.name);
        }
    }

    private static void Capture(string name)
    {
        Layout(); camera.Render();
        RenderTexture prior = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(destination, name), texture.EncodeToPNG());
        renders++;
        UnityEngine.Object.DestroyImmediate(texture);
        RenderTexture.active = prior;
    }

    private static void Layout()
    {
        Canvas.ForceUpdateCanvases();
        foreach (TMP_Text text in hudRoot.GetComponentsInChildren<TMP_Text>(true))
            if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();
    }

    private static Transform FindTransform(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
        return null;
    }
    private static T Find<T>() where T : Component
    {
        foreach (T item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (item.gameObject.scene.IsValid()) return item;
        throw new InvalidOperationException("Scene object missing: " + typeof(T).Name);
    }
    private static T Get<T>(object owner, string name) => (T)owner.GetType().GetField(name, Members).GetValue(owner);
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Members).SetValue(owner, value);
    private static void SetStatic(Type type, string name, object value) => type.GetField(name, Members).SetValue(null, value);
    private static void Call(object owner, string name) => owner.GetType().GetMethod(name, Members).Invoke(owner, null);
    private static bool CloseColor(Color one, Color two) => Mathf.Abs(one.r - two.r) < 0.005f && Mathf.Abs(one.g - two.g) < 0.005f && Mathf.Abs(one.b - two.b) < 0.005f;
    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
    private static void Require(bool result, string label)
    {
        if (!result) throw new InvalidOperationException(label);
        checks++; Evidence.AppendLine("PASS " + label);
    }
}
