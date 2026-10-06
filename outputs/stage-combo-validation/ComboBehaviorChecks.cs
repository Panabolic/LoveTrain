using System;
using System.Reflection;

// Dependencies are doubles; GameManager and ComboKillState come from the actual runtime sources.
public static class ComboBehaviorChecks
{
    private static int passed;

    public static string Run()
    {
        passed = 0;
        var combo = new ComboKillState();
        Check(combo.Count == 0 && combo.RemainingTime == 0f && combo.Fraction == 0f, "Initial state is empty");
        combo.Advance(9f);
        Check(combo.Count == 0, "Empty clock remains empty");
        combo.RegisterKill();
        Check(combo.Count == 1 && combo.RemainingTime == 3f && combo.Fraction == 1f, "First kill starts three seconds");
        combo.Advance(2.75f);
        Check(combo.Count == 1 && combo.RemainingTime == 0.25f, "Combo survives until the exact boundary");
        combo.RegisterKill();
        Check(combo.Count == 2 && combo.RemainingTime == 3f, "Next kill refreshes the full window");
        combo.Advance(3f);
        Check(combo.Count == 0 && combo.RemainingTime == 0f, "Exact three seconds expires the combo");
        combo.RegisterKill();
        combo.Advance(10f);
        Check(combo.Count == 0 && combo.Fraction == 0f, "Overshoot resets without a negative fill");
        combo.RegisterKill();
        for (int i = 0; i < 180; i++) combo.Advance(1f / 60f);
        Check(combo.Count == 0, "Three seconds of sixty-Hz frames expire without float subtraction drift");
        combo.RegisterKill();
        for (int i = 0; i < 30; i++) combo.Advance(0.1f);
        Check(combo.Count == 0, "Repeated tenth-second frames expire at three seconds");
        combo.RegisterKill();
        combo.Advance(0f);
        combo.Advance(-1f);
        combo.Advance(float.NaN);
        combo.Advance(float.PositiveInfinity);
        Check(combo.Count == 1 && combo.RemainingTime == 3f, "Invalid clock deltas cannot damage the state");
        combo.Reset();
        for (int i = 0; i < 10; i++) combo.RegisterKill();
        Check(combo.Count == 10 && combo.RemainingTime == 3f, "Distinct simultaneous kills all count");
        Check(ComboKillState.FormatLabel(9) == "Combo Kill 9", "Single digit has no punctuation");
        Check(ComboKillState.FormatLabel(10) == "Combo Kill 10!", "Ten has one exclamation mark");
        Check(ComboKillState.FormatLabel(99) == "Combo Kill 99!", "Ninety-nine retains one exclamation mark");
        Check(ComboKillState.FormatLabel(100) == "Combo Kill 100!!", "Hundred has two exclamation marks");
        Check(ComboKillState.FormatLabel(1000) == "Combo Kill 1000!!!", "Thousand has three exclamation marks");
        Check(ComboKillState.FormatLabel(int.MaxValue) == "Combo Kill 2147483647!!!!!!!!!", "Large counts retain exact digits");

        var manager = new GameManager();
        Invoke(manager, "Awake");
        Invoke(manager, "OnEnable");
        Invoke(manager, "Start");
        Check(manager.ComboKillCount == 0 && manager.CurrentState == GameState.Start, "Scene initialization clears combos");
        manager.AddKillCount(false);
        Check(manager.NormalKillCount == 1 && manager.ComboKillCount == 0, "Non-combat kills preserve counters without combos");
        manager.StartGame();
        manager.AddKillCount(false);
        manager.AddKillCount(true);
        Check(manager.NormalKillCount == 2 && manager.EliteKillCount == 1 && manager.ComboKillCount == 2,
            "Existing normal and elite kill route registers exactly once");
        manager.AddBossKillCount();
        Check(manager.BossKillCount == 1 && manager.ComboKillCount == 2, "Boss kills do not join the combo");
        Tick(manager, 0.5f);
        Check(manager.ComboTimeRemaining == 2.5f && manager.gameTime == 0.5f, "Playing advances both existing game time and combo time");
        manager.ChangeState(GameState.Event);
        Tick(manager, 9f);
        Check(manager.ComboKillCount == 2 && manager.ComboTimeRemaining == 2.5f, "Event freezes the combo");
        manager.ChangeState(GameState.Pause);
        Tick(manager, 9f);
        Check(manager.ComboKillCount == 2 && manager.ComboTimeRemaining == 2.5f, "Pause freezes the combo");
        manager.ChangeState(GameState.Boss);
        Tick(manager, 0.5f);
        Check(manager.ComboTimeRemaining == 2f && manager.gameTime == 0.5f, "Boss advances combo without changing existing game time");
        manager.AddKillCount(true);
        Check(manager.ComboKillCount == 3 && manager.ComboTimeRemaining == 3f, "Boss-phase mobs refresh the window");
        Tick(manager, 3f);
        Check(manager.ComboKillCount == 0 && manager.ComboTimeRemaining == 0f, "Manager expires the combo at three seconds");
        foreach (GameState state in new[] { GameState.StageTransition, GameState.Die, GameState.Ending })
        {
            manager.ChangeState(GameState.Playing);
            manager.AddKillCount(false);
            manager.ChangeState(state);
            Check(manager.ComboKillCount == 0 && manager.ComboTimeRemaining == 0f, state + " clears the combo");
        }
        manager.ChangeState(GameState.Playing);
        manager.AddKillCount(false);
        bool observedReset = false;
        manager.OnGameStateChanged += state => { if (state == GameState.StageTransition) observedReset = manager.ComboKillCount == 0; };
        manager.ChangeState(GameState.StageTransition);
        Check(observedReset, "State listeners see the already reset combo");
        manager.ChangeState(GameState.Playing);
        manager.AddKillCount(false);
        manager.RestartGame();
        Check(manager.ComboKillCount == 0 && manager.TotalKillCount == 0 && manager.gameTime == 0f,
            "Scene reload resets combo and preserves existing run reset behavior");
        Invoke(manager, "OnDisable");
        manager.ChangeState(GameState.Playing);
        manager.AddKillCount(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
        Check(manager.ComboKillCount == 1, "Disabled manager unsubscribes from scene reload callbacks");

        double sampleTime = UnityEngine.Time.timeAsDouble + 10d;
        BeginCombo(manager, sampleTime);
        ObserveTime(sampleTime + 2.995d);
        Invoke(manager, "Update");
        ObserveTime(sampleTime + 3.01d);
        manager.AddKillCount(false);
        Invoke(manager, "Update");
        Check(manager.ComboKillCount == 1 && manager.ComboTimeRemaining == 3f,
            "Kill before Update expires the previous near-boundary combo before registering");

        sampleTime += 10d;
        BeginCombo(manager, sampleTime);
        ObserveTime(sampleTime + 2.995d);
        Invoke(manager, "Update");
        ObserveTime(sampleTime + 3.01d);
        Invoke(manager, "Update");
        manager.AddKillCount(false);
        Check(manager.ComboKillCount == 1 && manager.ComboTimeRemaining == 3f,
            "Kill after Update gives the same boundary result");

        sampleTime += 10d;
        BeginCombo(manager, sampleTime);
        ObserveTime(sampleTime + 3d);
        manager.AddKillCount(false);
        Invoke(manager, "Update");
        Check(manager.ComboKillCount == 1 && manager.ComboTimeRemaining == 3f,
            "Exact expiry is checked even when the kill precedes Update");

        sampleTime += 10d;
        BeginCombo(manager, sampleTime);
        ObserveTime(sampleTime + 3d);
        Invoke(manager, "Update");
        manager.AddKillCount(false);
        Check(manager.ComboKillCount == 1 && manager.ComboTimeRemaining == 3f,
            "Exact expiry after Update matches expiry before Update");

        sampleTime += 10d;
        BeginCombo(manager, sampleTime);
        ObserveTime(sampleTime + 2.99d);
        manager.AddKillCount(true);
        Invoke(manager, "Update");
        Check(manager.ComboKillCount == 2 && manager.ComboTimeRemaining == 3f,
            "A kill inside the live boundary extends the current combo before Update");

        sampleTime += 10d;
        BeginCombo(manager, sampleTime);
        ObserveTime(sampleTime + 2.99d);
        Invoke(manager, "Update");
        manager.AddKillCount(true);
        Check(manager.ComboKillCount == 2 && manager.ComboTimeRemaining == 3f,
            "A live boundary kill after Update has the same result");
        for (int i = 0; i < 8; i++) manager.AddKillCount(false);
        Invoke(manager, "Update");
        Check(manager.ComboKillCount == 10 && manager.ComboTimeRemaining == 3f,
            "Many kills at one observed timestamp retain the full window");

        ObserveTime(sampleTime + 3.49d);
        manager.ChangeState(GameState.Event);
        Check(manager.ComboTimeRemaining == 2.5f,
            "Entering a modal accounts for combat time elapsed before the state change");
        ObserveTime(sampleTime + 80d);
        manager.ChangeState(GameState.Playing);
        Check(manager.ComboTimeRemaining == 2.5f,
            "Modal time is excluded even when Update was not called while the modal was open");
        ObserveTime(sampleTime + 80.5d);
        Invoke(manager, "Update");
        Check(manager.ComboTimeRemaining == 2f, "Resumed combat uses only newly elapsed combat time");
        manager.ChangeState(GameState.Pause);
        Invoke(manager, "Update");
        Invoke(manager, "Update");
        Check(manager.ComboTimeRemaining == 2f, "A frozen scaled timestamp does not drain during pause");
        ObserveTime(sampleTime + 100d);
        manager.ChangeState(GameState.Boss);
        Check(manager.ComboTimeRemaining == 2f, "Pause to Boss excludes inactive time");
        ObserveTime(sampleTime + 100.5d);
        Invoke(manager, "Update");
        ObserveTime(sampleTime + 100.25d);
        manager.AddKillCount(false);
        Check(manager.ComboKillCount == 11 && manager.ComboTimeRemaining == 3f,
            "An earlier FixedUpdate observation cannot rewind the combo clock");
        ObserveTime(sampleTime + 100.5d);
        Invoke(manager, "Update");
        Check(manager.ComboTimeRemaining == 3f, "Returning to the previous timestamp does not subtract time twice");
        ObserveTime(sampleTime + 101d);
        Invoke(manager, "Update");
        Check(manager.ComboTimeRemaining == 2.5f, "Combat resumes from the newest observed clock");

        manager.ChangeState(GameState.StageTransition);
        ObserveTime(sampleTime + 200d);
        manager.ChangeState(GameState.Start);
        ObserveTime(sampleTime + 300d);
        manager.StartGame();
        manager.AddKillCount(false);
        Invoke(manager, "Update");
        Check(manager.ComboKillCount == 1 && manager.ComboTimeRemaining == 3f,
            "Initial Start to Playing excludes waiting time before the first kill");
        return passed + " actual-source combo / GameManager behavior checks passed (dependency doubles; not Unity Play Mode).";
    }

    private static void Tick(GameManager manager, float delta)
    {
        UnityEngine.Time.deltaTime = delta;
        UnityEngine.Time.timeAsDouble += delta;
        Invoke(manager, "Update");
    }

    private static void ObserveTime(double timestamp)
    {
        UnityEngine.Time.timeAsDouble = timestamp;
        UnityEngine.Time.deltaTime = 0f;
    }

    private static void BeginCombo(GameManager manager, double timestamp)
    {
        manager.ChangeState(GameState.StageTransition);
        ObserveTime(timestamp);
        manager.ChangeState(GameState.Playing);
        manager.AddKillCount(false);
    }

    private static void Invoke(GameManager manager, string method) =>
        typeof(GameManager).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Failed: " + name);
        passed++;
    }
}

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public readonly object gameObject = new object();
        protected static void Destroy(object value) { }
        protected static void DontDestroyOnLoad(object value) { }
    }
    public static class Time { public static float deltaTime; public static double timeAsDouble; public static float timeScale = 1f; }
    public static class Debug { public static void Log(object value) { } }
    public static class Application { public static void Quit() { } }
    public enum SimulationMode2D { FixedUpdate, Script }
    public static class Physics2D { public static SimulationMode2D simulationMode; }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public enum LoadSceneMode { Single }
    public static class SceneManager
    {
        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        private static string activeScene = "Junmo";
        public static Scene GetActiveScene() => new Scene { name = activeScene };
        public static void LoadScene(string name)
        {
            activeScene = name;
            sceneLoaded?.Invoke(GetActiveScene(), LoadSceneMode.Single);
        }
    }
}
namespace DG.Tweening { public static class DOTween { public static void Init() { } public static void KillAll() { } } }
public static class Option { public static void SetOptionInputBlocked(bool value) { } }
public class BossWarningLoopUI { public static BossWarningLoopUI Instance; public void SetPauseState(bool value) { } }
public class PoolManager { public static PoolManager instance = new PoolManager(); public void DespawnAllEnemiesExceptBoss() { } }
public class EndingManager { public static EndingManager Instance; public void StartEnding() { } }
public class StageManager { public static StageManager Instance; public void StartStageTransitionSequence() { } }
public enum SoundID { UI_Option }
public static class SoundEventBus { public static void Publish(SoundID id) { } }
