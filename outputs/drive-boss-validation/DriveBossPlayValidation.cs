using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Copy into the disposable project's Assets/Editor. Omit -quit; Run exits itself.
// Runs native Update/FixedUpdate/coroutines; reflection only arranges test preconditions.
[InitializeOnLoad]
public static class DriveBossPlayValidation
{
    private const string Prefix = "LoveTrain.DriveBossNativePlay.";
    private const string DefaultResult = "C:/Users/nadom/Desktop/졸업작품/LoveTrain/outputs/drive-boss-validation/play-result.txt";
    private const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Train train;
    private static TrainController controller;
    private static TrainDriveState drive;
    private static TrainLevelManager wallet;
    private static GameManager game;
    private static StageManager stage;
    private static ForwardCameraFollow cameraFollow;
    private static PursuingHand hand;
    private static AccelerationWindEffect windEffect;
    private static ParticleSystem wind;
    private static float windLength;
    private static Keyboard keyboard;
    private static InputSettings oldSettings, testSettings;
    private static Key[] heldKeys = Array.Empty<Key>();
    private static bool observedShift;
    private static bool oldBackground;
    private static float a, b, fuel, x, gap, cameraOffset;
    private static int flesh, souls;
    private static RewardPickup pausedPickup;
    private static RewardPickup[] homingPickups;
    private static TrainBoss trainBoss;
    private static EyeBoss eyeBoss;
    private static int reservationCount;
    private static int timedStrikes;
    private static int completedStrikes;
    private static int cancelledWarnings;
    private static bool warningObserved, strikeObserved;
    private static readonly Dictionary<int, EyeObservation> eyeObservations = new Dictionary<int, EyeObservation>();
    private sealed class EyeObservation
    {
        public int Number;
        public float WarningStart;
        public int WarningFrame;
        public float DeclaredDelay;
        public bool SawWarning;
        public bool Struck;
        public bool Closed;
    }

    static DriveBossPlayValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        Application.logMessageReceived += Log;
    }

    public static void staticRun() => Run();

    public static void Run()
    {
        Guard();
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Begin this fixture in EditMode.");
        string result = DefaultResult;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-driveBossPlayResult") result = args[i + 1];
        result = Path.GetFullPath(result);
        Directory.CreateDirectory(Path.GetDirectoryName(result));
        SessionState.SetString(Prefix + "Result", result);
        SessionState.SetBool(Prefix + "Active", true);
        SessionState.SetBool(Prefix + "Done", false);
        SessionState.SetInt(Prefix + "Phase", 0);
        SessionState.SetInt(Prefix + "Checks", 0);
        SessionState.SetInt(Prefix + "ChangedErrors", 0);
        SessionState.SetInt(Prefix + "OtherErrors", 0);
        SessionState.SetFloat(Prefix + "Deadline", (float)EditorApplication.timeSinceStartup + 90f);
        File.WriteAllText(ResultPath, "START native isolated Junmo PlayMode.\nNoise controls: disable ordinary/event spawners and automatic gun/item behaviours; cap deltaTime; keep authored eye warning delay 3 seconds, shorten only strike/cleanup/cooldown and mute eye player damage during sequencing checks. Train enabled except exact pickup resource phase. Eye player-damage amount is not validated here.\n");
        File.WriteAllText(ConsolePath, "Native PlayMode console warnings/errors.\n");
        PlayerPrefs.DeleteKey(PermanentUpgradeProgress.SnapshotSaveKey);
        PlayerPrefs.SetInt(PermanentUpgradeProgress.SoulSaveKey, 0);
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
                if (!EditorApplication.isPlayingOrWillChangePlaymode) Exit();
                return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "Deadline", 0))
                throw new TimeoutException("Native fixture exceeded 90 seconds.");
            if (!EditorApplication.isPlaying) return;
            int phase = SessionState.GetInt(Prefix + "Phase", 0);
            if (phase == 0) { Next(1, 0.25f); return; }
            // Observe from the first native Update after instantiation, including
            // phase 24's settling wait. A phase-entry timestamp is not spawn time.
            if ((phase == 24 || phase == 28) && eyeBoss != null) ObserveEye();
            if (phase == 28) return;
            if (Time.frameCount < SessionState.GetInt(Prefix + "Frame", 0) ||
                EditorApplication.timeSinceStartup < SessionState.GetFloat(Prefix + "Wait", 0)) return;
            switch (phase)
            {
                case 1:
                    Bind();
                    Require(train.BaseSpeed == 320f && train.MaxSpeedValue == 460f && hand.Speed == 320f, "Native Train.Start / PursuingHand initialization uses 320/460 and hand 320");
                    Require(hand.Gap > 0 && hand.gameObject.activeInHierarchy, "Authored hand is active with positive renderer/collider gap");
                    game.ChangeState(GameState.Playing);
                    drive.Speed = 350f;
                    heldKeys = new[] { Key.LeftShift };
                    Next(2, 0.12f); break;
                case 2:
                    Require(observedShift && train.CurrentSpeed > 352f, "Official Dynamic keyboard Shift reaches actual Train.Update and accelerates");
                    if (wind.particleCount == 0) { Next(2, 0.07f); break; }
                    CheckWind();
                    train.TakeDamage(1f);
                    a = train.CurrentSpeed;
                    Next(3, 0.06f); break;
                case 3:
                    Require(train.AccelerationBlocked && Mathf.Abs(train.CurrentSpeed - a) < 0.1f, "Native Update preserves speed during first part of 0.2 second hit lock");
                    Next(4, 0.22f); break;
                case 4:
                    Require(!train.AccelerationBlocked && train.CurrentSpeed > a + 2f, "Native Update resumes held Shift after 0.2 seconds without a new key press");
                    heldKeys = Array.Empty<Key>();
                    drive.Reset(); drive.Speed = 390f;
                    Next(5, 0.35f); break;
                case 5:
                    Require(Mathf.Abs(train.CurrentSpeed - 390f) < 0.2f, "Shift release keeps speed during the 0.5 second delay");
                    windLength = ParticleLength();
                    Next(6, 0.30f); break;
                case 6:
                    Require(train.CurrentSpeed < 388f && train.CurrentSpeed > 320f, "Native Update decelerates after the 0.5 second delay");
                    Require(ParticleLength() < windLength - 0.01f, "Native wind particles shorten as train decelerates");
                    train.enabled = false; hand.enabled = false;
                    train.ModifyFuel(50f - train.CurrentFuel);
                    flesh = wallet.Flesh; souls = wallet.Souls; fuel = train.CurrentFuel;
                    Vector3 dropPosition = train.transform.position + Vector3.right * 20f;
                    homingPickups = new[]
                    {
                        SpawnPickup(RewardPickupKind.Flesh, dropPosition, 7, 0, 0f, 0.5f),
                        SpawnPickup(RewardPickupKind.Soul, dropPosition, 0, 3, 0f, 0.5f),
                        SpawnPickup(RewardPickupKind.Fuel, dropPosition, 0, 0, 0.1f, 0.5f)
                    };
                    Next(61, 0.75f); break;
                case 61:
                    foreach (RewardPickup pickup in homingPickups)
                    {
                        Require(pickup != null && Vector3.Distance(pickup.transform.position, train.transform.position) < 19f,
                            "Native prefab Rigidbody/Transform moves toward train after the actual 0.5 second attraction delay: " + pickup.name);
                        File.AppendAllText(ResultPath, "PICKUP native transform=" + pickup.transform.position + ", body=" + Get<Rigidbody2D>(pickup, "body").position + "\n");
                    }
                    Next(7, 0.9f); break;
                case 7:
                    Require(wallet.Flesh == flesh + 7 && wallet.Souls == souls + 3, "Actual pickup Update attraction collects flesh and soul once");
                    Require(Mathf.Abs(train.CurrentFuel - (fuel + train.MaxFuel * 0.1f)) < 0.01f, "Actual fuel pickup heals 10 percent once; Train drain temporarily disabled for this exact assertion");
                    Require(wallet.TotalExperience == 0f, "Pickup resources do not duplicate kill XP");
                    Next(8, 0.10f); break;
                case 8:
                    Require(wallet.Flesh == flesh + 7 && wallet.Souls == souls + 3 && Mathf.Abs(train.CurrentFuel - fuel - train.MaxFuel * 0.1f) < 0.01f, "Resource totals stay unchanged after collected pickups are destroyed");
                    pausedPickup = SpawnPickup(RewardPickupKind.Soul, train.transform.position + Vector3.right * 15f, 0, 5, 0f, 10f);
                    game.PauseGame();
                    x = pausedPickup.transform.position.x; a = pausedPickup.transform.position.y; b = Get<float>(pausedPickup, "age");
                    souls = wallet.Souls;
                    Next(9, 0.2f); break;
                case 9:
                    Require(game.CurrentState == GameState.Pause && Time.timeScale == 0f && pausedPickup != null &&
                        Mathf.Abs(pausedPickup.transform.position.x - x) < 0.001f && Mathf.Abs(pausedPickup.transform.position.y - a) < 0.001f &&
                        Get<float>(pausedPickup, "age") == b && wallet.Souls == souls, "Real PauseGame / OnGameStateChanged stops pending soul movement and reward");
                    game.ResumeGame();
                    flesh = wallet.Flesh; fuel = train.CurrentFuel;
                    SpawnPickup(RewardPickupKind.Flesh, train.transform.position + Vector3.right * 15f, 9, 0, 0f, 10f);
                    SpawnPickup(RewardPickupKind.Soul, train.transform.position + Vector3.right * 15f, 0, 4, 0f, 10f);
                    SpawnPickup(RewardPickupKind.Fuel, train.transform.position + Vector3.right * 15f, 0, 0, 0.5f, 10f);
                    game.ChangeState(GameState.Ending);
                    Require(wallet.Flesh == flesh + 9 && wallet.Souls == souls + 9 && train.CurrentFuel == fuel, "Actual Ending event settles both pending currency rewards and discards fuel");
                    game.ChangeState(GameState.Ending);
                    Require(wallet.Flesh == flesh + 9 && wallet.Souls == souls + 9, "Repeated Ending cannot pay a pending pickup twice");
                    game.ChangeState(GameState.Playing);
                    wallet.GainExperience(10f);
                    Require(wallet.CurrentLevel == 2 && train.BaseSpeed == 340f && train.MaxSpeedValue == 480f, "Real GainExperience(10) advances Lv2 and raises base/max by 20");
                    Require(hand.Speed == 320f, "Run level change leaves hand speed anchored to run-start base");
                    train.enabled = true;
                    train.transform.position = new Vector3(100f, train.transform.position.y, train.transform.position.z);
                    heldKeys = new[] { Key.D };
                    Next(10, 0.12f); break;
                case 10:
                    Require(cameraFollow.CurrentOffsetX >= 80f && train.transform.position.x > 100f, "Native Controller D and ForwardCameraFollow LateUpdate advance world x and camera beyond old right boundary");
                    heldKeys = Array.Empty<Key>();
                    controller.PushLeft(10000f);
                    Require(Mathf.Abs(train.transform.position.x - controller.MinXPosition) < 0.01f && controller.MinXPosition > 50f, "PushLeft clamps to the moving rear wall rather than old world -20");
                    train.transform.position = new Vector3(cameraFollow.CurrentOffsetX, train.transform.position.y, train.transform.position.z);
                    hand.transform.position = new Vector3(train.transform.position.x - 34.3f, hand.transform.position.y, hand.transform.position.z);
                    hand.InitializeRun(train, 320f); hand.enabled = true;
                    Next(11, 0.08f); break;
                case 11:
                    cameraOffset = cameraFollow.CurrentOffsetX;
                    stage.StartStageTransitionSequence();
                    gap = hand.Gap;
                    Next(12, 0.4f); break;
                case 12:
                    Require(game.CurrentState == GameState.StageTransition && Mathf.Abs(cameraFollow.CurrentOffsetX - cameraOffset) < 0.01f && Mathf.Abs(hand.Gap - gap) < 0.01f,
                        "Native stage transition freezes camera and captured pursuit gap");
                    Next(13, 0.1f); break;
                case 13:
                    if (game.CurrentState == GameState.StageTransition) { Next(13, 0.1f); break; }
                    Require(game.CurrentState == GameState.Playing && stage.StageNumber == 2, "Actual transition coroutine loads the next stage and resumes play");
                    Require(Mathf.Abs(train.transform.position.x - cameraOffset) < 0.05f && Mathf.Abs(cameraFollow.CurrentOffsetX - cameraOffset) < 0.01f,
                        "Actual stage reset uses the current camera origin");
                    Require(Mathf.Abs(hand.Gap - gap) < 5f && hand.Speed == 350f, "Transition restores pursuit spacing; stage two hand speed is 350");
                    game.ChangeState(GameState.Boss);
                    drive.Reset(); drive.Speed = train.MaxSpeedValue;
                    heldKeys = new[] { Key.LeftShift };
                    Next(20, 0.04f); break;
                case 20:
                    Require(train.IsDashing, "Actual held Shift enters a dash before contact");
                    trainBoss = SpawnTrainBoss();
                    Set(trainBoss, "moveSpeed", 7f);
                    AlignBossCollider(trainBoss);
                    fuel = train.CurrentFuel; x = train.transform.position.x;
                    Next(21, 0.25f); break;
                case 21:
                    Require(train.AccelerationBlocked && !train.IsDashing && !train.IsDead, "Native trigger/FixedUpdate boss contact interrupts dash and blocks acceleration without death");
                    Require(train.transform.position.x < x - 0.3f && train.CurrentFuel > fuel - 2f, "Native continuous boss contact pushes left and does not apply old instant-kill fuel damage");
                    a = train.CurrentSpeed;
                    Next(22, 0.25f); break;
                case 22:
                    Require(train.AccelerationBlocked && train.CurrentSpeed < a - 8f, "Continued native boss contact reduces speed at 70 per second while Shift remains held");
                    trainBoss.gameObject.SetActive(false);
                    drive.Speed = 350f;
                    Next(23, 0.20f); break;
                case 23:
                    Require(!train.AccelerationBlocked && train.CurrentSpeed > 355f, "Native boss OnDisable releases contact and held acceleration resumes");
                    heldKeys = Array.Empty<Key>();
                    drive.Reset(); drive.Speed = train.BaseSpeed;
                    eyeBoss = SpawnEyeBoss();
                    Require(Mathf.Abs(Get<float>(eyeBoss, "normalAttackDelay") - 3f) < 0.001f, "Eye fixture retains the authored 3 second warning delay");
                    Set(eyeBoss, "waitTimeAfterPatternEnd", 0.15f);
                    Set(eyeBoss, "normalTentacleDamage", 0f);
                    SessionState.SetFloat(Prefix + "EyeUntil", (float)EditorApplication.timeSinceStartup + 10f);
                    Next(24, 0.1f); break;
                case 24:
                    Require(eyeBoss.GetComponentsInChildren<Enemy>(true).Length == 1 && eyeBoss.GetComponent<EyeBossBelt>() != null,
                        "Authored repeated eye presentation has one Enemy/HP owner and one belt component");
                    Set(eyeBoss, "hasEnteredScreen", true);
                    a = Get<float>(eyeBoss, "calibratedMaxHP");
                    Set(eyeBoss, "currentHP", a * 0.15f);
                    eyeBoss.TakeDamage(1f); eyeBoss.TakeDamage(1f);
                    Require(Mathf.Abs(Get<float>(eyeBoss, "currentHP") - (a * 0.15f - 2f)) < 0.01f,
                        "Two real hits below old enrage threshold both damage the sole eye HP owner");
                    Next(28, 0f); break;
                case 29:
                    Require(reservationCount >= 2 && warningObserved && strikeObserved && timedStrikes >= 2 && completedStrikes >= 2,
                        "Native eye completes at least two non-overlapping authored-3-second warning/strike/cleanup reservations after low-health damage");
                    File.AppendAllText(ResultPath, "EYE completed-strikes=" + completedStrikes + ", timed-strikes=" + timedStrikes + ", cancelled-warnings=" + cancelledWarnings + "\n");
                    eyeBoss.gameObject.SetActive(false);
                    game.ChangeState(GameState.Playing);
                    heldKeys = Array.Empty<Key>(); drive.CancelDash(); drive.Speed = 0f;
                    Get<HandPursuitState>(hand, "pursuit").SetGap(0.01f);
                    Next(30, 0.03f); break;
                case 30:
                    Require(train.IsDead && game.CurrentState == GameState.Die && train.transform.parent == hand.transform,
                        "Native pursuing-hand contact enters the existing parent/drag/player-death path");
                    Require(SessionState.GetInt(Prefix + "ChangedErrors", 0) == 0, "No changed-runtime-path Console exceptions during native fixture");
                    Finish(true, "PASS: " + SessionState.GetInt(Prefix + "Checks", 0) + " native lifecycle/input/physics/pursuit/drop/XP/stage/boss checks. Other Console errors requiring baseline classification=" + SessionState.GetInt(Prefix + "OtherErrors", 0));
                    break;
                default: throw new InvalidOperationException("Unexpected phase " + phase);
            }
        }
        catch (Exception error) { Finish(false, "FAIL phase=" + SessionState.GetInt(Prefix + "Phase", 0) + " " + error); }
    }

    private static void Bind()
    {
        train = UnityEngine.Object.FindFirstObjectByType<Train>();
        game = GameManager.Instance; stage = StageManager.Instance;
        hand = UnityEngine.Object.FindFirstObjectByType<PursuingHand>();
        cameraFollow = UnityEngine.Object.FindFirstObjectByType<ForwardCameraFollow>();
        Require(train != null && game != null && stage != null && hand != null && cameraFollow != null, "Native Junmo scene creates all authored owners/components");
        windEffect = UnityEngine.Object.FindFirstObjectByType<AccelerationWindEffect>();
        Require(windEffect != null, "Authored native acceleration wind component exists");
        wind = Get<ParticleSystem>(windEffect, "wind");
        Require(wind != null, "Authored native wind particle reference is assigned");
        wallet = train.GetComponent<TrainLevelManager>(); controller = train.GetComponent<TrainController>(); drive = Get<TrainDriveState>(train, "drive");
        foreach (Spawner component in UnityEngine.Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None)) component.enabled = false;
        foreach (EventObjectSpawner component in UnityEngine.Object.FindObjectsByType<EventObjectSpawner>(FindObjectsSortMode.None)) component.enabled = false;
        foreach (Gun component in UnityEngine.Object.FindObjectsByType<Gun>(FindObjectsSortMode.None)) component.enabled = false;
        foreach (MonoBehaviour component in train.GetComponentsInChildren<MonoBehaviour>(true))
            if (component.GetType().Namespace != null && component.GetType().Namespace.Contains("Items")) component.enabled = false;
        foreach (Enemy enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        oldSettings = InputSystem.settings; testSettings = UnityEngine.Object.Instantiate(oldSettings);
        testSettings.hideFlags = HideFlags.HideAndDontSave;
        testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = testSettings;
        oldBackground = Application.runInBackground; Application.runInBackground = true;
        Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; Time.maximumDeltaTime = 0.033f;
        keyboard = InputSystem.AddDevice<Keyboard>("DriveBossNativeFixtureKeyboard"); InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
        InputSystem.onBeforeUpdate += BeforeInput;
        InputSystem.onAfterUpdate += AfterInput;
    }

    private static void BeforeInput()
    {
        if (keyboard == null || !EditorApplication.isPlaying || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(heldKeys));
    }
    private static void AfterInput()
    {
        if (keyboard != null && InputState.currentUpdateType == InputUpdateType.Dynamic && keyboard.leftShiftKey.isPressed) observedShift = true;
    }

    private static void CheckWind()
    {
        var particles = new ParticleSystem.Particle[wind.particleCount];
        int count = wind.GetParticles(particles);
        Require(count > 0, "Native acceleration LateUpdate emits actual wind particles");
        foreach (ParticleSystem.Particle particle in particles)
        {
            Color32 color = particle.startColor;
            Require(color.r == 255 && color.g == 255 && color.b == 255 && color.a > 0 &&
                particle.startSize3D.x > particle.startSize3D.y * 3f,
                "Native emitted wind is translucent white and a horizontal line");
        }
    }

    private static float ParticleLength()
    {
        var particles = new ParticleSystem.Particle[wind.particleCount];
        int count = wind.GetParticles(particles);
        Require(count > 0, "Native wind remains available for the deceleration length sample");
        return particles[0].startSize3D.x;
    }

    private static RewardPickup SpawnPickup(RewardPickupKind kind, Vector3 position, int fleshReward, int soulReward, float fuelReward, float delay)
    {
        RewardPickup prefab = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null) continue;
            RewardPickup candidate = asset.GetComponent<RewardPickup>();
            if (candidate != null && Get<RewardPickupKind>(candidate, "kind") == kind) { prefab = candidate; break; }
        }
        if (prefab == null) throw new InvalidOperationException("Authored " + kind + " pickup prefab missing.");
        RewardPickup pickup = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        Set(pickup, "attractionDelay", delay); Set(pickup, "gravity", 0f);
        pickup.Initialize(wallet, fleshReward, soulReward, fuelReward);
        return pickup;
    }

    private static TrainBoss SpawnTrainBoss()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Bosses/TrainBoss.prefab");
        Require(prefab != null, "Authored TrainBoss prefab exists");
        TrainBoss boss = UnityEngine.Object.Instantiate(prefab, train.transform.position, Quaternion.identity).GetComponent<TrainBoss>();
        Set(boss, "hasEnteredScreen", true);
        return boss;
    }
    private static void AlignBossCollider(TrainBoss boss)
    {
        Collider2D bossCollider = Get<Collider2D>(boss, "phase1Collider");
        Collider2D trainCollider = train.GetComponent<Collider2D>();
        Require(bossCollider != null && trainCollider != null, "Authored native contact colliders exist");
        boss.transform.position += trainCollider.bounds.center - bossCollider.bounds.center;
        Physics2D.SyncTransforms();
    }
    private static EyeBoss SpawnEyeBoss()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Bosses/EyeBoss.prefab");
        Require(prefab != null, "Authored EyeBoss prefab exists");
        Vector3 position = new Vector3(Camera.main.transform.position.x, 24f, 0f);
        EyeBoss boss = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<EyeBoss>();
        Set(boss, "hasEnteredScreen", true);
        return boss;
    }

    private static void ObserveEye()
    {
        List<GameObject> list = Get<List<GameObject>>(eyeBoss, "spawnedTentacles");
        var aliveIds = new HashSet<int>();
        foreach (GameObject obj in list) if (obj != null) aliveIds.Add(obj.GetInstanceID());
        if (aliveIds.Count > 1) throw new InvalidOperationException("Eye attack reservations overlapped: " + aliveIds.Count);
        foreach (KeyValuePair<int, EyeObservation> entry in eyeObservations)
        {
            EyeObservation observation = entry.Value;
            if (observation.Closed || aliveIds.Contains(entry.Key)) continue;
            observation.Closed = true;
            if (observation.Struck) completedStrikes++;
            else cancelledWarnings++;
            File.AppendAllText(ResultPath, "EYE END reservation=" + observation.Number + ", kind=" + (observation.Struck ? "strike-complete" : "warning-cancelled") +
                ", time=" + Time.time + ", frame=" + Time.frameCount + "\n");
        }
        foreach (GameObject obj in list)
        {
            if (obj == null) continue;
            Tentacle tentacle = obj.GetComponent<Tentacle>();
            EyeObservation observation;
            if (!eyeObservations.TryGetValue(obj.GetInstanceID(), out observation))
            {
                reservationCount++;
                observation = new EyeObservation
                {
                    Number = reservationCount,
                    WarningStart = Time.time,
                    WarningFrame = Time.frameCount,
                    DeclaredDelay = Get<float>(tentacle, "attackWaitTime"),
                    SawWarning = !tentacle.AttackStarted
                };
                eyeObservations.Add(obj.GetInstanceID(), observation);
                Set(tentacle, "animationLength", 0.12f); Set(tentacle, "toDestroy", 0.03f);
                warningObserved |= observation.SawWarning;
                Require(Mathf.Abs(observation.DeclaredDelay - 3f) < 0.001f, "Native Tentacle.Setup receives the authored 3 second warning");
                File.AppendAllText(ResultPath, "EYE START reservation=" + observation.Number + ", saw-warning=" + observation.SawWarning +
                    ", first-time=" + Time.time + ", first-frame=" + Time.frameCount + ", delay=" + observation.DeclaredDelay + "\n");
            }
            if (tentacle.AttackStarted && !observation.Struck)
            {
                float observedDuration = Time.time - observation.WarningStart;
                // Editor update may observe either side of one native frame;
                // the coroutine also subtracts its starting frame's deltaTime.
                if (observation.SawWarning)
                {
                    float tolerance = Mathf.Max(0.1f, Time.maximumDeltaTime * 3f);
                    Require(observedDuration >= observation.DeclaredDelay - tolerance,
                        "Native eye warning lasts the authored 3 seconds before strike (duration=" + observedDuration + ", frames=" + (Time.frameCount - observation.WarningFrame) + ")");
                    timedStrikes++;
                }
                else File.AppendAllText(ResultPath, "EYE late first observation; duration sample excluded for reservation=" + observation.Number + "\n");
                observation.Struck = true;
                strikeObserved = true;
                File.AppendAllText(ResultPath, "EYE STRIKE reservation=" + observation.Number + ", time=" + Time.time + ", frame=" + Time.frameCount + "\n");
            }
        }
        if (SessionState.GetInt(Prefix + "Phase", 0) == 28 && EditorApplication.timeSinceStartup >= SessionState.GetFloat(Prefix + "EyeUntil", 0)) Next(29, 0f);
    }

    private static void Next(int phase, float wait)
    {
        SessionState.SetInt(Prefix + "Phase", phase);
        SessionState.SetInt(Prefix + "Frame", Time.frameCount + 2);
        SessionState.SetFloat(Prefix + "Wait", (float)EditorApplication.timeSinceStartup + wait);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        SessionState.SetInt(Prefix + "Checks", SessionState.GetInt(Prefix + "Checks", 0) + 1);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }
    private static FieldInfo Field(object owner, string name)
    {
        for (Type type = owner.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, Fields | BindingFlags.DeclaredOnly);
            if (field != null) return field;
        }
        throw new MissingFieldException(owner.GetType().Name, name);
    }
    private static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);
    private static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Prefix + "Active", false)) return;
        File.AppendAllText(ResultPath, "LIFECYCLE " + state + "\n");
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Prefix + "Done", false)) EditorApplication.delayCall += Exit;
    }
    private static void Finish(bool passed, string message)
    {
        if (SessionState.GetBool(Prefix + "Done", false)) return;
        SessionState.SetBool(Prefix + "Done", true);
        SessionState.SetInt(Prefix + "ExitCode", passed ? 0 : 1);
        File.AppendAllText(ResultPath, message + "\n");
        InputSystem.onBeforeUpdate -= BeforeInput; InputSystem.onAfterUpdate -= AfterInput;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (oldSettings != null) InputSystem.settings = oldSettings;
        if (testSettings != null) UnityEngine.Object.Destroy(testSettings);
        Application.runInBackground = oldBackground;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else if (!EditorApplication.isPlayingOrWillChangePlaymode) Exit();
    }
    private static void Log(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Prefix + "Active", false) || type == LogType.Log) return;
        bool affected = stack.Contains("Train.") || stack.Contains("TrainBoss.") || stack.Contains("EyeBoss.") ||
            stack.Contains("Tentacle.") || stack.Contains("RewardPickup.") || stack.Contains("PursuingHand.") ||
            stack.Contains("ForwardCameraFollow.") || stack.Contains("StageManager.");
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            string key = Prefix + (affected ? "ChangedErrors" : "OtherErrors");
            SessionState.SetInt(key, SessionState.GetInt(key, 0) + 1);
        }
        File.AppendAllText(ConsolePath, "[" + type + "][" + (affected ? "CHANGED_PATH" : "BASELINE_COMPARISON_REQUIRED") + "] " + message + "\n" + stack + "\n");
    }
    private static void Guard()
    {
        if (!Application.productName.StartsWith("DriveBossPreview_", StringComparison.Ordinal))
            throw new InvalidOperationException("Disposable DriveBossPreview_ product namespace is required; original project refused.");
    }
    private static void Exit()
    {
        int code = SessionState.GetInt(Prefix + "ExitCode", 1);
        SessionState.SetBool(Prefix + "Active", false);
        EditorApplication.Exit(code);
    }
    private static string ResultPath => SessionState.GetString(Prefix + "Result", DefaultResult);
    private static string ConsolePath => Path.Combine(Path.GetDirectoryName(ResultPath), "play-console.txt");
}
