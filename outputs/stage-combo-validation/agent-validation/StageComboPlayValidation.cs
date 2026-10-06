using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Actual copied-Junmo PlayMode callbacks. No original saves, keyboard injection or player build.
// Run with -batchmode -executeMethod StageComboPlayValidation.Run and omit -quit.
[InitializeOnLoad]
public static class StageComboPlayValidation
{
    private const string Prefix = "LoveTrain.StageComboPlayFixture.";
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static GameManager game;
    private static StageManager stage;
    private static ComboKillUI hud;
    private static Train train;
    private static Spawner sceneSpawner;

    static StageComboPlayValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayStateChanged;
        Application.logMessageReceived += LogMessage;
    }

    public static void Run()
    {
        Guard();
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Begin in EditMode.");
        string destination = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../stage-combo-validation/agent-validation"));
        Directory.CreateDirectory(destination);
        SessionState.SetString(Prefix + "Destination", destination);
        SessionState.SetBool(Prefix + "Active", true); SessionState.SetBool(Prefix + "Done", false);
        SessionState.SetInt(Prefix + "Phase", 0); SessionState.SetInt(Prefix + "Checks", 0);
        SessionState.SetInt(Prefix + "FeatureErrors", 0); SessionState.SetInt(Prefix + "OtherErrors", 0);
        SessionState.SetFloat(Prefix + "Deadline", (float)EditorApplication.timeSinceStartup + 120f);
        File.WriteAllText(ResultPath, "START actual copied Junmo PlayMode stage/combo lifecycle fixture.\n");
        File.WriteAllText(ConsolePath, "Preserved feature errors and other runtime messages for baseline classification.\n");
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
            if (EditorApplication.timeSinceStartup >= SessionState.GetFloat(Prefix + "Deadline", 0f))
                throw new TimeoutException("PlayMode fixture exceeded its 120-second deadline.");
            if (!EditorApplication.isPlaying) return;
            int phase = SessionState.GetInt(Prefix + "Phase", 0);
            if (phase == 0) { Next(1, 0.5f); return; }
            if (Time.frameCount < SessionState.GetInt(Prefix + "WaitFrame", 0) ||
                EditorApplication.timeSinceStartup < SessionState.GetFloat(Prefix + "WaitWall", 0f)) return;
            switch (phase)
            {
                case 1:
                    game = GameManager.Instance; stage = StageManager.Instance;
                    hud = UnityEngine.Object.FindAnyObjectByType<ComboKillUI>(FindObjectsInactive.Include);
                    train = UnityEngine.Object.FindAnyObjectByType<Train>(FindObjectsInactive.Include);
                    Require(game != null && stage != null && hud != null && train != null, "Native Junmo owners and authored combo presenter initialize");
                    Require(stage.StageLength == 57600f && stage.StageNumber == 1, "Native stage starts with 57,600-unit route and first run stage");
                    // Freeze unrelated train input/fuel and enemy spawning inside this fixture only.
                    // StageManager, GameManager and both HUD presenters retain native Update/LateUpdate.
                    train.enabled = false;
                    foreach (Spawner spawner in UnityEngine.Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None)) { spawner.SetSpawning(false); spawner.enabled = false; }
                    game.ChangeState(GameState.Playing); Time.timeScale = 1f;
                    Get<ComboKillState>(game, "comboKills").Reset();
                    for (int index = 0; index < 9; index++) game.AddKillCount(false);
                    Snapshot(); Next(2, 0.15f); break;
                case 2:
                    Require(game.ComboKillCount == 9 && Label() == "Combo Kill 9", "Native LateUpdate displays 9 kills without punctuation");
                    Require(!Message().gameObject.activeInHierarchy && Group().alpha == 1f, "Below 10 kills shows combo without god message");
                    Require(game.gameTime > Stored("GameTime") && stage.StageDistance > Stored("Distance"), "Native Update/LateUpdate advance run clock and route while Playing");
                    Require(Mathf.Abs((stage.StageDistance - Stored("Distance")) - train.CurrentSpeed * (game.gameTime - Stored("GameTime"))) < train.CurrentSpeed * 0.1f,
                        "Native route advancement follows current train speed over elapsed run time");
                    game.AddKillCount(true); Next(3, 0.1f); break;
                case 3:
                    Require(game.ComboKillCount == 10 && Label() == "Combo Kill 10!", "Native elite kill crosses digit punctuation threshold");
                    Require(Message().gameObject.activeInHierarchy && Message().text == "고대신이 당신을 눈 여겨 봅니다.", "Native presenter reveals requested 10-kill message");
                    game.RegisterUIQueue(() => { });
                    Require(game.CurrentState == GameState.Event && Time.timeScale == 0f, "Real modal queue enters Event pause");
                    Snapshot(); Next(4, 0.5f); break;
                case 4:
                    CheckFrozen(true, "Event");
                    game.CloseUI();
                    Require(game.CurrentState == GameState.Playing && Time.timeScale == 1f, "Real queue release resumes prior combat state");
                    Snapshot(); Next(5, 0.15f); break;
                case 5:
                    Require(game.ComboTimeRemaining < Stored("Remaining"), "Combo native clock resumes after modal close");
                    game.PauseGame();
                    Require(game.CurrentState == GameState.Pause && Time.timeScale == 0f, "Actual PauseGame freezes scaled simulation");
                    Snapshot(); Next(6, 0.5f); break;
                case 6:
                    CheckFrozen(true, "Pause");
                    game.ResumeGame(); game.AppearBoss();
                    Require(game.CurrentState == GameState.Boss && Time.timeScale == 1f, "Actual ResumeGame and AppearBoss enter live Boss combat");
                    Snapshot(); Next(7, 0.4f); break;
                case 7:
                    CheckFrozen(false, "Boss");
                    Require(game.ComboTimeRemaining < Stored("Remaining"), "Combo decreases during native Boss combat while run timer stays stopped");
                    game.ChangeState(GameState.Playing);
                    Get<ComboKillState>(game, "comboKills").Reset(); game.AddKillCount(false);
                    SessionState.SetFloat(Prefix + "ExpiryStarted", (float)Time.time);
                    Next(8, 0.2f); break;
                case 8:
                    if (game.ComboKillCount != 0) { Next(8, 0.1f); return; }
                    Require(Time.time - SessionState.GetFloat(Prefix + "ExpiryStarted", 0f) >= 2.8f, "Combo expires after approximately 3 native scaled seconds");
                    Next(9, 0.1f); break;
                case 9:
                    Require(Group().alpha == 0f && !Message().gameObject.activeInHierarchy, "Native presenter hides expired combo and god message");
                    hud.enabled = false; game.AddKillCount(false);
                    Require(Group().alpha == 0f, "Native OnDisable hides presenter while state remains owned by GameManager");
                    hud.enabled = true; Next(10, 0.1f); break;
                case 10:
                    Require(Group().alpha == 1f && Label() == "Combo Kill 1", "Native OnEnable restores current combo immediately");
                    for (int index = 1; index < 100; index++) game.AddKillCount(false);
                    Next(11, 0.1f); break;
                case 11:
                    Require(Label() == "Combo Kill 100!!" && Message().gameObject.activeInHierarchy, "Native 100-kill label has two exclamation marks");
                    game.ChangeState(GameState.StageTransition);
                    Require(game.ComboKillCount == 0, "Actual StageTransition resets combo");
                    game.ChangeState(GameState.Playing);
                    sceneSpawner = UnityEngine.Object.FindAnyObjectByType<Spawner>(FindObjectsInactive.Include);
                    Require(sceneSpawner != null && BossObjectCount() == 0, "Actual scene Spawner exists with no instantiated bosses before warning probe");
                    StageDistanceProgress completedRoute = Get<StageDistanceProgress>(stage, "distanceProgress");
                    completedRoute.Reset(57600f); completedRoute.Advance(320f, 180f, true);
                    SessionState.SetInt(Prefix + "BossIndexBefore", Get<int>(sceneSpawner, "nextBossIndex"));
                    sceneSpawner.SetSpawning(true); sceneSpawner.enabled = true;
                    Call(sceneSpawner, "HandleStageProgress");
                    Require(game.CurrentState == GameState.Boss && Get<bool>(sceneSpawner, "bossSequenceRunning") &&
                        Get<Coroutine>(sceneSpawner, "bossSpawnRoutine") != null,
                        "Actual distance consumer enters Boss and reserves its warning coroutine");
                    Require(Get<StageBossSchedule>(sceneSpawner, "bossSchedule").GetDueRequest(true, stage.StageNumber,
                        stage.StageDistance, stage.StageLength, game.gameTime, game.maxGameTime) == StageBossRequest.None,
                        "Actual warning records the route request before spawning");
                    SessionState.SetFloat(Prefix + "WarningProbeStarted", (float)Time.time);
                    Next(12, 0.1f); break;
                case 12:
                    Require(BossObjectCount() == 0, "No boss prefab is instantiated during the initial warning interval");
                    sceneSpawner.enabled = false;
                    Require(!Get<bool>(sceneSpawner, "bossSequenceRunning") && Get<Coroutine>(sceneSpawner, "bossSpawnRoutine") == null,
                        "Native Spawner.OnDisable cancels warning coroutine and releases running flag");
                    Require(Get<bool>(sceneSpawner, "cancelledWarningNeedsRecovery") && game.CurrentState == GameState.Boss,
                        "Native OnDisable defers gameplay recovery until component is enabled again");
                    Require(Get<int>(sceneSpawner, "nextBossIndex") == SessionState.GetInt(Prefix + "BossIndexBefore", -1) &&
                        Get<StageBossSchedule>(sceneSpawner, "bossSchedule").GetDueRequest(true, stage.StageNumber,
                            stage.StageDistance, stage.StageLength, game.gameTime, game.maxGameTime) == StageBossRequest.Stage,
                        "Cancelled warning restores the same boss index and releases the route reservation");
                    Next(13, 0.1f); break;
                case 13:
                    Require(game.CurrentState == GameState.Boss && Get<bool>(sceneSpawner, "cancelledWarningNeedsRecovery"),
                        "Disabled component keeps warning recovery deferred across native frames");
                    sceneSpawner.enabled = true;
                    Require(game.CurrentState == GameState.Playing && !Get<bool>(sceneSpawner, "cancelledWarningNeedsRecovery"),
                        "Native Spawner.OnEnable restores Playing before route consumers retry");
                    Next(14, 0.1f); break;
                case 14:
                    Require(game.CurrentState == GameState.Boss && Get<bool>(sceneSpawner, "bossSequenceRunning") &&
                        Get<Coroutine>(sceneSpawner, "bossSpawnRoutine") != null,
                        "Native StageManager.LateUpdate reserves the cancelled route boss once after re-enable");
                    BossName[] sequence = Get<BossName[]>(sceneSpawner, "bossSequence");
                    int expectedIndex = (SessionState.GetInt(Prefix + "BossIndexBefore", -1) + 1) % sequence.Length;
                    Require(Get<int>(sceneSpawner, "nextBossIndex") == expectedIndex &&
                        Get<StageBossSchedule>(sceneSpawner, "bossSchedule").GetDueRequest(true, stage.StageNumber,
                            stage.StageDistance, stage.StageLength, game.gameTime, game.maxGameTime) == StageBossRequest.None,
                        "Native retry advances the same boss index exactly once and deduplicates its route request");
                    sceneSpawner.SetSpawning(false);
                    Require(game.CurrentState == GameState.Playing && !Get<bool>(sceneSpawner, "bossSequenceRunning") &&
                        !Get<bool>(sceneSpawner, "cancelledWarningNeedsRecovery"),
                        "Actual SetSpawning(false) cancels retry and immediately restores Playing while enabled");
                    sceneSpawner.enabled = false;
                    Require(BossObjectCount() == 0 && Time.time - SessionState.GetFloat(Prefix + "WarningProbeStarted", 0f) < 4f,
                        "Both real warning attempts cancel before the authored 4-second spawn delay with zero boss instances");
                    Next(15, 0.1f); break;
                case 15:
                    Require(game.CurrentState == GameState.Playing && BossObjectCount() == 0 && !Get<bool>(sceneSpawner, "bossSequenceRunning"),
                        "Stopped and disabled Spawner remains idle across native route notifications");
                    game.AddKillCount(false); game.PlayerDied();
                    Require(game.ComboKillCount == 0 && game.CurrentState == GameState.Die, "Actual PlayerDied resets combo");
                    Require(SessionState.GetInt(Prefix + "FeatureErrors", 0) == 0, "No stage/combo feature error stack traces observed");
                    Finish(true, "PASS: " + SessionState.GetInt(Prefix + "Checks", 0) + " native copied-Junmo PlayMode checks including real warning cancellation and re-enable retry. Other runtime errors requiring baseline classification: " + SessionState.GetInt(Prefix + "OtherErrors", 0) + ". Train input/fuel and unrelated mob spawning disabled only in fixture; route seeded before warning probe. No collisions, physical input, full encounter or player-build claim.");
                    break;
            }
        }
        catch (Exception exception) { Finish(false, "FAIL: " + exception); }
    }

    private static void Snapshot()
    {
        SessionState.SetFloat(Prefix + "Remaining", game.ComboTimeRemaining);
        SessionState.SetFloat(Prefix + "GameTime", game.gameTime);
        SessionState.SetFloat(Prefix + "Distance", stage.StageDistance);
    }
    private static void CheckFrozen(bool combo, string state)
    {
        Require(Mathf.Abs(game.gameTime - Stored("GameTime")) < 0.0001f, "Native run timer stops during " + state);
        Require(Mathf.Abs(stage.StageDistance - Stored("Distance")) < 0.0001f, "Native route progress stops during " + state);
        if (combo) Require(Mathf.Abs(game.ComboTimeRemaining - Stored("Remaining")) < 0.0001f, "Native combo timer stops during " + state);
    }
    private static void Next(int phase, float seconds)
    {
        SessionState.SetInt(Prefix + "Phase", phase); SessionState.SetInt(Prefix + "WaitFrame", Time.frameCount + 2);
        SessionState.SetFloat(Prefix + "WaitWall", (float)EditorApplication.timeSinceStartup + seconds);
    }
    private static void Finish(bool passed, string summary)
    {
        if (SessionState.GetBool(Prefix + "Done", false)) return;
        SessionState.SetBool(Prefix + "Done", true); SessionState.SetInt(Prefix + "ExitCode", passed ? 0 : 1);
        File.AppendAllText(ResultPath, summary + "\n");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else if (!EditorApplication.isPlayingOrWillChangePlaymode) ExitAfterPlay();
    }
    private static void PlayStateChanged(PlayModeStateChange state)
    {
        if (SessionState.GetBool(Prefix + "Active", false) && SessionState.GetBool(Prefix + "Done", false) && state == PlayModeStateChange.EnteredEditMode) ExitAfterPlay();
    }
    private static void ExitAfterPlay()
    {
        SessionState.SetBool(Prefix + "Active", false); EditorApplication.Exit(SessionState.GetInt(Prefix + "ExitCode", 1));
    }
    private static void LogMessage(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Prefix + "Active", false) || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning)) return;
        bool feature = stack.Contains("ComboKill") || stack.Contains("StageProgressUI") || stack.Contains("StageDistanceProgress") ||
            stack.Contains("StageBossSchedule") || stack.Contains("HandleStageProgress") || stack.Contains("Spawner.CancelBossSequence") ||
            stack.Contains("Spawner.RecoverCancelledWarning") || stack.Contains("Spawner.StartBossSequence") || stack.Contains("Spawner.BossSpawnRoutine");
        if (type != LogType.Warning) { string key = Prefix + (feature ? "FeatureErrors" : "OtherErrors"); SessionState.SetInt(key, SessionState.GetInt(key, 0) + 1); }
        File.AppendAllText(ConsolePath, "[" + type + "][" + (feature ? "FEATURE" : "BASELINE_COMPARISON_REQUIRED") + "] " + message + "\n" + stack + "\n");
    }
    private static void Require(bool result, string label)
    {
        if (!result) throw new InvalidOperationException(label);
        SessionState.SetInt(Prefix + "Checks", SessionState.GetInt(Prefix + "Checks", 0) + 1); File.AppendAllText(ResultPath, "PASS " + label + "\n");
    }
    private static void Guard()
    {
        string path = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
        if (!path.EndsWith("/outputs/unity-ui-preview/project/Assets", StringComparison.OrdinalIgnoreCase) ||
            Application.companyName != "CodexUIRenderPreview" || !Application.productName.StartsWith("WorkbenchPreview_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing fixture outside verified disposable path and save identity.");
    }
    private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Members).GetValue(owner);
    private static void Call(object owner, string method) => owner.GetType().GetMethod(method, Members).Invoke(owner, null);
    private static int BossObjectCount() => UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    private static float Stored(string key) => SessionState.GetFloat(Prefix + key, 0f);
    private static string Label() => Get<TMP_Text>(hud, "countText").text;
    private static TMP_Text Message() => Get<TMP_Text>(hud, "godMessage");
    private static CanvasGroup Group() => Get<CanvasGroup>(hud, "visibility");
    private static string ResultPath => Path.Combine(SessionState.GetString(Prefix + "Destination", ""), "unity-stage-combo-play-result.txt");
    private static string ConsolePath => Path.Combine(SessionState.GetString(Prefix + "Destination", ""), "unity-stage-combo-play-console.txt");
}
