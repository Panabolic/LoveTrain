using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Disposable copied project only. The runner survives EnterPlayMode domain reload through SessionState.
// Launch with -batchmode -executeMethod FleshHudPlayValidation.Run (omit -quit; this runner exits itself).
[InitializeOnLoad]
public static class FleshHudPlayValidation
{
    private const string Prefix = "LoveTrain.FleshHudPlayFixture.";
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static TrainLevelManager wallet;
    private static FleshHud hud;
    private static LevelUpUIManager workbench;
    private static GameManager game;
    private static Keyboard keyboard;
    private static InputSettings originalInputSettings;
    private static InputSettings fixtureInputSettings;
    private static bool originalRunInBackground;
    private static int fInjectionStage;
    private static int inputDebugUpdates;

    static FleshHudPlayValidation()
    {
        EditorApplication.playModeStateChanged += PlayStateChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += LogMessage;
    }

    public static void Run()
    {
        Guard(); // Verify clone identity before any PlayerPrefs read or write.
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Fixture must begin in EditMode.");
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../flesh-hud-preview"));
        Directory.CreateDirectory(directory);
        SessionState.SetString(Prefix + "Directory", directory);
        SessionState.SetBool(Prefix + "Active", true);
        SessionState.SetBool(Prefix + "Done", false);
        SessionState.SetInt(Prefix + "Phase", 0);
        SessionState.SetInt(Prefix + "Checks", 0);
        SessionState.SetInt(Prefix + "HudErrors", 0);
        SessionState.SetInt(Prefix + "OtherErrors", 0);
        SessionState.SetInt(Prefix + "LayoutOverlaps", 0);
        SessionState.SetBool(Prefix + "GameplayCaptureRequested", false);
        SessionState.SetBool(Prefix + "DynamicPressObserved", false);
        SessionState.SetFloat(Prefix + "Deadline", (float)EditorApplication.timeSinceStartup + 120f);
        File.WriteAllText(ResultPath, "START: isolated Junmo PlayMode HUD lifecycle fixture.\n");
        File.WriteAllText(ConsolePath, "All warning/error messages are preserved. Non-HUD runtime errors require baseline classification.\n");
        // Only this verified disposable namespace is reset; no production or user's save identity is used.
        PlayerPrefs.DeleteKey(PermanentUpgradeProgress.SnapshotSaveKey);
        PlayerPrefs.SetInt(PermanentUpgradeProgress.SoulSaveKey, 7);
        EnglishLocalization.SetLanguage(false);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Prefix + "Active", false)) return;
        try
        {
            Guard();
            if (SessionState.GetBool(Prefix + "Done", false))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) ExitAfterPlay();
                return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "Deadline", 0f))
                throw new TimeoutException("PlayMode fixture did not reach its next phase before the 120 second deadline.");
            if (!EditorApplication.isPlaying) return;
            int phase = SessionState.GetInt(Prefix + "Phase", 0);
            if (phase == 0)
            {
                SessionState.SetInt(Prefix + "WaitFrame", Time.frameCount + 40);
                SessionState.SetInt(Prefix + "Phase", 1);
                return;
            }
            if (Time.frameCount < SessionState.GetInt(Prefix + "WaitFrame", 0)) return;
            switch (phase)
            {
                case 1:
                    BindRuntime();
                    Require(wallet.Flesh == 0 && wallet.Souls == 7, "Native wallet Awake loads isolated souls and starts with zero flesh");
                    Require(Get<TrainLevelManager>(hud, "subscribedWallet") == wallet, "Native FleshHud.OnEnable subscribes to authored wallet");
                    Require(Get<TMP_Text>(hud, "soulAmount").text == "영혼 7", "Native FleshHud.Start reflects post-Awake soul initialization");
                    Require(Amount() == "살점 0", "Native startup renders zero flesh");
                    Require(Mathf.Abs(Get<TMP_Text>(hud, "fleshAmount").fontSize - Get<TMP_Text>(hud, "soulAmount").fontSize) < 0.0001f,
                        "Native currency labels use the same authored font size");
                    Require(Get<TMP_Text>(hud, "fleshAmount").alignment == TextAlignmentOptions.MidlineRight &&
                        Get<TMP_Text>(hud, "soulAmount").alignment == TextAlignmentOptions.MidlineRight,
                        "Native flesh and soul labels use authored right alignment");
                    game.ChangeState(GameState.Playing);
                    wallet.AddFlesh(105);
                    Require(Amount() == "살점 105", "Real AddFlesh/OnResourcesChanged refreshes amount without calling HUD methods");
                    Next(2); break;
                case 2:
                    if (!SessionState.GetBool(Prefix + "GameplayCaptureRequested", false))
                    {
                        Require(Hint(), "Native LateUpdate shows F in Playing after reward");
                        Require(Get<TMP_Text>(hud, "creationLabel").text == "조직 창조 X 2", "Native reward shows two available creations");
                        CheckActiveEquipmentOverlap();
                        SessionState.SetString(Prefix + "GameplayCaptureRequestedUtc", DateTime.UtcNow.ToString("O"));
                        ScreenCapture.CaptureScreenshot(GameplayPath);
                        File.AppendAllText(ResultPath, "CAPTURE requested real PlayMode frame: " + Screen.width + "x" + Screen.height + ", frame=" + Time.frameCount + ", flesh=" + wallet.Flesh + "\n");
                        SessionState.SetBool(Prefix + "GameplayCaptureRequested", true);
                        SessionState.SetInt(Prefix + "GameplayCaptureTimeout", Time.frameCount + 12);
                        SessionState.SetInt(Prefix + "WaitFrame", Time.frameCount + 10);
                        return;
                    }
                    if (!File.Exists(GameplayPath) || new FileInfo(GameplayPath).Length < 100 ||
                        File.GetLastWriteTimeUtc(GameplayPath) < DateTime.Parse(SessionState.GetString(Prefix + "GameplayCaptureRequestedUtc", ""),
                            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind))
                    {
                        if (Time.frameCount < SessionState.GetInt(Prefix + "GameplayCaptureTimeout", 0)) return;
                        File.AppendAllText(ResultPath, "NOT AVAILABLE: native ScreenCapture did not write a PNG in this hidden batch GameView. Continue core input/lifecycle checks; no actual gameplay screenshot claim.\n");
                    }
                    else Require(true, "Real gameplay ScreenCapture exists: world and all active HUDs retained");
                    Require(wallet.TryPurchaseCreation(() => true), "Real owner creation transaction succeeds once");
                    Require(wallet.Flesh == 75 && wallet.CreationCost == 40 && wallet.CreatedParts == 1, "Real first transaction leaves 75 and raises cost to 40");
                    Require(Amount() == "살점 75", "First transaction event refreshes native HUD");
                    Next(3); break;
                case 3:
                    Require(Hint() && Get<TMP_Text>(hud, "creationLabel").text == "조직 창조", "One available creation removes X count");
                    Require(wallet.TryPurchaseCreation(() => true), "Real second owner transaction succeeds once");
                    Require(wallet.Flesh == 35 && wallet.CreationCost == 50 && wallet.CreatedParts == 2, "Second transaction leaves 35 and raises cost to 50");
                    Require(Amount() == "살점 35", "Second transaction event refreshes native HUD");
                    Next(4); break;
                case 4:
                    Require(!Hint(), "Native LateUpdate hides F below current cost");
                    wallet.AddFlesh(70);
                    Next(5); break;
                case 5:
                    Require(wallet.Flesh == 105 && Hint(), "Subsequent real reward restores creation availability");
                    game.ChangeState(GameState.Event);
                    Next(6); break;
                case 6:
                    Require(!Hint(), "Actual Event transition hides hint");
                    game.ChangeState(GameState.Playing);
                    game.PauseGame();
                    Next(7); break;
                case 7:
                    Require(game.CurrentState == GameState.Pause && Time.timeScale == 0f && !Hint(), "Actual PauseGame hides hint while native LateUpdate still runs");
                    game.ResumeGame();
                    Next(8); break;
                case 8:
                    Require(game.CurrentState == GameState.Playing && Time.timeScale > 0f && Hint(), "Actual ResumeGame restores hint");
                    hud.enabled = false;
                    wallet.AddFlesh(1);
                    Next(9); break;
                case 9:
                    Require(!Hint() && Get<TrainLevelManager>(hud, "subscribedWallet") == null, "Native OnDisable hides hint and releases wallet subscription");
                    Require(Amount() == "살점 105", "Disabled HUD receives no wallet event");
                    hud.enabled = true;
                    Next(10); break;
                case 10:
                    Require(Amount() == "살점 106" && Hint(), "Native OnEnable refreshes current wallet and restores hint");
                    EnglishLocalization.SetLanguage(true);
                    Next(11); break;
                case 11:
                    Require(Get<TMP_Text>(hud, "creationLabel").text == "Create tissue", "Real LanguageChanged refreshes creation text");
                    Require(Get<TMP_Text>(hud, "costLabel").text == "Cost 50", "Real LanguageChanged refreshes cost text");
                    fInjectionStage = 1; // Arm an event for the official next Dynamic input update.
                    LogInput("armed-before-Dynamic");
                    SessionState.SetInt(Prefix + "OpenTimeoutFrame", Time.frameCount + 120);
                    Next(12); break;
                case 12:
                    if (!workbench.IsOpen)
                    {
                        if (Time.frameCount >= SessionState.GetInt(Prefix + "OpenTimeoutFrame", 0))
                            throw new TimeoutException("Synthetic F input was queued, but native TrainLevelManager.Update did not open the workbench.");
                        return;
                    }
                    Require(SessionState.GetBool(Prefix + "DynamicPressObserved", false), "Official Dynamic input pipeline observed F isPressed and wasPressedThisFrame");
                    Require(game.CurrentState == GameState.Event && Time.timeScale == 0f, "Synthetic F reaches actual wallet Update and UI queue, opening modal Event pause");
                    Require(!Hint(), "Opened native workbench hides F hint");
                    Require(wallet.Flesh == 106 && wallet.CreatedParts == 2, "Opening through F does not charge a creation");
                    workbench.CloseWorkbench();
                    SessionState.SetInt(Prefix + "CloseTimeoutFrame", Time.frameCount + 240);
                    Next(13); break;
                case 13:
                    if (workbench.IsOpen)
                    {
                        if (Time.frameCount >= SessionState.GetInt(Prefix + "CloseTimeoutFrame", 0))
                            throw new TimeoutException("Native DOTween close transition did not complete.");
                        return;
                    }
                    Next(14); break;
                case 14:
                    Require(game.CurrentState == GameState.Playing && Time.timeScale > 0f && Hint(), "Native close animation and queue release restore gameplay and HUD hint");
                    Require(SessionState.GetInt(Prefix + "HudErrors", 0) == 0, "No error stack trace directly references new HUD/progress/availability paths");
                    Require(SessionState.GetInt(Prefix + "LayoutOverlaps", 0) == 0,
                        "CurrencyHUD has zero overlaps with actual active normal equipment slots; measured overlap count=" +
                        SessionState.GetInt(Prefix + "LayoutOverlaps", 0));
                    Finish(true, "PASS: " + SessionState.GetInt(Prefix + "Checks", 0) + " actual Junmo PlayMode HUD lifecycle, reward, transaction, state, disable/re-enable, localization and synthetic F checks. " +
                        "Other world/engine errors preserved for baseline comparison: " + SessionState.GetInt(Prefix + "OtherErrors", 0) + ". No player-build or physical keyboard claim.");
                    break;
            }
        }
        catch (Exception exception) { Finish(false, "FAIL: " + exception); }
    }

    private static void BindRuntime()
    {
        hud = UnityEngine.Object.FindAnyObjectByType<FleshHud>(FindObjectsInactive.Include);
        if (hud == null) throw new InvalidOperationException("Authored FleshHud did not survive scene startup.");
        wallet = Get<TrainLevelManager>(hud, "wallet");
        game = GameManager.Instance;
        workbench = LevelUpUIManager.Instance;
        Require(wallet != null && game != null && workbench != null, "Native scene owners initialize");
        // The package's default Editor policy diverts unfocused keyboards to Editor buffers.
        // Install a transient settings clone so hidden batch GameView focus cannot eat the synthetic event.
        originalInputSettings = InputSystem.settings;
        fixtureInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
        fixtureInputSettings.hideFlags = HideFlags.HideAndDontSave;
        fixtureInputSettings.name = "DisposableFleshHudInputSettings";
        fixtureInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        fixtureInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        fixtureInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        originalRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        InputSystem.settings = fixtureInputSettings;
        keyboard = InputSystem.AddDevice<Keyboard>("FleshHudPlayFixtureKeyboard");
        InputSystem.EnableDevice(keyboard);
        keyboard.MakeCurrent();
        InputSystem.onBeforeUpdate += BeforeInputUpdate;
        InputSystem.onAfterUpdate += AfterInputUpdate;
        LogInput("transient-settings-installed");
    }

    private static void BeforeInputUpdate()
    {
        if (!SessionState.GetBool(Prefix + "Active", false) || SessionState.GetBool(Prefix + "Done", false) ||
            !EditorApplication.isPlaying || keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        Guard();
        if (fInjectionStage == 1)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F));
            fInjectionStage = 2;
            LogInput("Dynamic-press-queued");
        }
        else if (fInjectionStage == 2)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            fInjectionStage = 3;
            LogInput("next-Dynamic-release-queued");
        }
    }

    private static void AfterInputUpdate()
    {
        if (!SessionState.GetBool(Prefix + "Active", false) || SessionState.GetBool(Prefix + "Done", false) ||
            !EditorApplication.isPlaying || keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic ||
            SessionState.GetInt(Prefix + "Phase", 0) != 12) return;
        if (keyboard.fKey.isPressed && keyboard.fKey.wasPressedThisFrame)
            SessionState.SetBool(Prefix + "DynamicPressObserved", true);
        if (inputDebugUpdates++ < 8 || keyboard.fKey.wasPressedThisFrame) LogInput("after-official-Dynamic");
    }

    private static void LogInput(string step)
    {
        if (keyboard == null || wallet == null || game == null) return;
        var queue = Get<System.Collections.Generic.Queue<Action>>(game, "uiRequestQueue");
        File.AppendAllText(ResultPath, "INPUT " + step + ": frame=" + Time.frameCount + ", update=" + InputState.currentUpdateType +
            ", updateCount=" + InputState.updateCount + ", focused=" + Application.isFocused + ", keyboardEnabled=" + keyboard.enabled +
            ", keyboardCurrent=" + (Keyboard.current == keyboard) + ", isPressed=" + keyboard.fKey.isPressed +
            ", wasPressedThisFrame=" + keyboard.fKey.wasPressedThisFrame + ", gameState=" + game.CurrentState +
            ", timeScale=" + Time.timeScale + ", flesh=" + wallet.Flesh + ", cost=" + wallet.CreationCost +
            ", walletActive=" + wallet.isActiveAndEnabled + ", creationQueued=" + Get<bool>(wallet, "creationQueued") +
            ", committing=" + Get<bool>(wallet, "committingCreation") + ", uiQueueCount=" + queue.Count +
            ", uiProcessing=" + Get<bool>(game, "isUIProcessing") + ", workbenchOpen=" + workbench.IsOpen +
            ", canRequest=" + wallet.CanRequestCreation + ", inputMode=" + InputSystem.settings.updateMode +
            ", editorInput=" + InputSystem.settings.editorInputBehaviorInPlayMode + ", background=" + InputSystem.settings.backgroundBehavior + "\n");
    }

    private static void CheckActiveEquipmentOverlap()
    {
        InventoryUI inventoryUI = Get<InventoryUI>(workbench, "inventoryUI");
        Canvas hudCanvas = hud.GetComponentInParent<Canvas>();
        Require(inventoryUI != null && inventoryUI.EquipmentRect != null && hudCanvas != null,
            "Normal scene equipment rectangle and authored currency Canvas resolve in actual PlayMode");
        Require(inventoryUI.EquipmentRect.GetComponentInParent<Canvas>() == hudCanvas,
            "Normal equipment slots and currency HUD share their actual scene Canvas");
        Canvas.ForceUpdateCanvases();
        Rect currency = CanvasRect((RectTransform)hud.transform, hudCanvas.transform);
        int active = 0;
        int overlaps = 0;
        for (int index = 0; index < Inventory.EquipmentSlotCount; index++)
        {
            InventorySlotUI slot = inventoryUI.GetEquipmentSlot(index);
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;
            Rect slotRect = CanvasRect((RectTransform)slot.transform, hudCanvas.transform);
            active++;
            bool overlap = currency.Overlaps(slotRect);
            if (overlap) overlaps++;
            File.AppendAllText(ResultPath, "LAYOUT slot=" + index + ", slotCanvasRect=" + slotRect + ", currencyCanvasRect=" + currency +
                ", overlap=" + overlap + "\n");
        }
        SessionState.SetInt(Prefix + "LayoutOverlaps", overlaps);
        File.AppendAllText(ResultPath, "LAYOUT actual active normal equipment slots measured=" + active +
            ", total overlaps=" + overlaps + ". Collected before input checks; final phase still fails when any overlap exists. " +
            "Geometric overlap evidence; no rendered gameplay appearance claim.\n");
    }

    private static Rect CanvasRect(RectTransform transform, Transform canvasTransform)
    {
        Vector3[] corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        Vector3 lower = canvasTransform.InverseTransformPoint(corners[0]);
        Vector3 upper = canvasTransform.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
    }

    private static void Next(int phase)
    {
        SessionState.SetInt(Prefix + "Phase", phase);
        SessionState.SetInt(Prefix + "WaitFrame", Time.frameCount + 2);
    }
    private static void PlayStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Prefix + "Active", false)) return;
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Prefix + "Done", false)) ExitAfterPlay();
    }
    private static void Finish(bool passed, string summary)
    {
        if (SessionState.GetBool(Prefix + "Done", false)) return;
        SessionState.SetBool(Prefix + "Done", true);
        SessionState.SetInt(Prefix + "ExitCode", passed ? 0 : 1);
        File.AppendAllText(ResultPath, summary + "\n");
        InputSystem.onBeforeUpdate -= BeforeInputUpdate;
        InputSystem.onAfterUpdate -= AfterInputUpdate;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (fixtureInputSettings != null) UnityEngine.Object.Destroy(fixtureInputSettings);
        Application.runInBackground = originalRunInBackground;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else if (!EditorApplication.isPlayingOrWillChangePlaymode) ExitAfterPlay();
    }
    private static void ExitAfterPlay()
    {
        int exitCode = SessionState.GetInt(Prefix + "ExitCode", 1);
        SessionState.SetBool(Prefix + "Active", false);
        EditorApplication.Exit(exitCode);
    }
    private static void LogMessage(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Prefix + "Active", false) || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning)) return;
        bool hudPath = stack.Contains("FleshHud.") || stack.Contains("FleshCreationProgress.") ||
            stack.Contains("get_CanRequestCreation") || stack.Contains("get_CanShowCreation") || stack.Contains("InvalidateCreationAvailability");
        if (type != LogType.Warning)
        {
            string key = Prefix + (hudPath ? "HudErrors" : "OtherErrors");
            SessionState.SetInt(key, SessionState.GetInt(key, 0) + 1);
        }
        File.AppendAllText(ConsolePath, "[" + type + "][" + (hudPath ? "HUD_PATH" : "BASELINE_COMPARISON_REQUIRED") + "] " + message + "\n" + stack + "\n");
    }
    private static void Require(bool result, string text)
    {
        if (!result) throw new InvalidOperationException(text);
        SessionState.SetInt(Prefix + "Checks", SessionState.GetInt(Prefix + "Checks", 0) + 1);
        File.AppendAllText(ResultPath, "PASS " + text + "\n");
    }
    private static void Guard()
    {
        if (Application.companyName != "CodexUIRenderPreview" || !Application.productName.StartsWith("WorkbenchPreview_", StringComparison.Ordinal))
            throw new InvalidOperationException("Disposable PlayerPrefs identity is not verified; refusing PlayMode fixture.");
    }
    private static string ResultPath => Path.Combine(SessionState.GetString(Prefix + "Directory", ""), "unity-hud-play-validation-result.txt");
    private static string ConsolePath => Path.Combine(SessionState.GetString(Prefix + "Directory", ""), "unity-hud-play-console.txt");
    private static string GameplayPath => Path.Combine(SessionState.GetString(Prefix + "Directory", ""), "unity-hud-gameplay.png");
    private static bool Hint() => Get<GameObject>(hud, "creationHint").activeInHierarchy;
    private static string Amount() => Get<TMP_Text>(hud, "fleshAmount").text;
    private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Members).GetValue(owner);
}
