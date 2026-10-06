// The real Spawner.cs is compiled with these dependency doubles. Tests exercise
// only this task's boss reservation/cancellation paths, not unrelated spawn rules.
namespace UnityEngine
{
    public class Object
    {
        public static int Instantiations;
        public static string LastPrefab;
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Instantiations++; LastPrefab = prefab.name;
            return new GameObject { name = prefab.name, BossComponent = new Boss() };
        }
    }
    public class MonoBehaviour : Object
    {
        public bool isActiveAndEnabled = true;
        public Transform transform = new Transform();
        private readonly List<Coroutine> routines = new List<Coroutine>();
        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            var result = new Coroutine { Routine = routine };
            routines.Add(result); result.Completed = !routine.MoveNext(); return result;
        }
        protected void StopCoroutine(Coroutine routine) { routine.Stopped = true; }
        protected void StopAllCoroutines() { foreach (var routine in routines) routine.Stopped = true; }
        public void ResumeWarningWait()
        {
            foreach (var routine in routines.ToArray())
                if (!routine.Stopped && !routine.Completed) routine.Completed = !routine.Routine.MoveNext();
        }
        public int PendingRoutines
        {
            get { int count = 0; foreach (var routine in routines) if (!routine.Stopped && !routine.Completed) count++; return count; }
        }
    }
    public sealed class Coroutine { public IEnumerator Routine; public bool Stopped; public bool Completed; }
    public sealed class WaitForSeconds { public WaitForSeconds(float value) {} }
    public sealed class GameObject : Object
    {
        public string name;
        public Transform transform = new Transform();
        public Boss BossComponent;
        public static GameObject FindWithTag(string tag) { return null; }
        public T GetComponent<T>() where T : class { return BossComponent as T; }
    }
    public sealed class Transform { public Vector3 position; }
    public class Rigidbody2D { public Vector2 linearVelocity; public float angularVelocity; }
    public class BoxCollider2D { public Bounds bounds; }
    public struct Bounds { public Vector3 center; public Vector3 min; public Vector3 max; }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float xValue, float yValue) { x = xValue; y = yValue; }
        public static Vector2 zero => new Vector2(0f, 0f);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float xValue, float yValue, float zValue) { x = xValue; y = yValue; z = zValue; }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public static class Time { public static float timeScale = 1f; public static float deltaTime; }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
    public static class Random
    {
        public static float value => 0.5f;
        public static int Range(int a, int b) => a;
        public static float Range(float a, float b) => a;
    }
    public static class Debug { public static void Log(object value) {} public static void LogWarning(object value) {} public static void LogError(object value) {} }
    public class SerializeField : Attribute {}
    public class HideInInspector : Attribute {}
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) {} }
    public class MinAttribute : Attribute { public MinAttribute(float value) {} }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) {} }
}
public enum GameState { Title, Start, Playing, Event, Boss, Die, Pause, StageTransition, Ending }
public enum BossName { EyeBoss, TrainBoss }
public sealed class GameManager
{
    public static GameManager Instance;
    public GameState CurrentState = GameState.Playing;
    public float gameTime = 179f, maxGameTime = 900f;
    public event Action<GameState> OnGameStateChanged;
    public void ChangeState(GameState state) { if (CurrentState == state) return; CurrentState = state; OnGameStateChanged?.Invoke(state); }
    public void AppearBoss() { ChangeState(GameState.Boss); }
}
public sealed class StageManager
{
    public static StageManager Instance;
    public int StageNumber = 1;
    public float StageDistance = 57600f, StageLength = 57600f;
    public event Action OnProgressAdvanced;
    public void AdvanceCallback() { OnProgressAdvanced?.Invoke(); }
}
public sealed class Train : UnityEngine.MonoBehaviour { public float AccelerationProgress; }
public sealed class Mob { public Action<Mob> OnDied; }
public sealed class Boss
{
    public void StartEntranceRoutine(UnityEngine.Vector3 position, float duration) {}
}
public sealed class PoolManager
{
    public static PoolManager instance;
    public UnityEngine.GameObject[] bosses;
    public UnityEngine.GameObject GetBoss(BossName name) => bosses[(int)name];
    public UnityEngine.GameObject GetMob(UnityEngine.GameObject prefab) => null;
    public UnityEngine.GameObject GetFlyMob(int index) => null;
    public UnityEngine.GameObject GetGroundMob(int index) => null;
    public UnityEngine.GameObject GetFlyEliteMob(int index) => null;
    public UnityEngine.GameObject GetGroundEliteMob(int index) => null;
}
public sealed class BossWarningLoopUI
{
    public static BossWarningLoopUI Instance;
    public int Shown, Hidden;
    public void ShowWarning() { Shown++; }
    public void HideWarning() { Hidden++; }
}
public static class SpawnerCancellationChecks
{
    private const System.Reflection.BindingFlags Members = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
    private static void Invoke(Spawner spawner, string method) { typeof(Spawner).GetMethod(method, Members).Invoke(spawner, null); }
    private static T Get<T>(Spawner spawner, string field) => (T)typeof(Spawner).GetField(field, Members).GetValue(spawner);
    private static void Set(Spawner spawner, string field, object value) { typeof(Spawner).GetField(field, Members).SetValue(spawner, value); }
    private static StageBossRequest Due(Spawner spawner)
        => Get<StageBossSchedule>(spawner, "bossSchedule").GetDueRequest(true, StageManager.Instance.StageNumber,
            StageManager.Instance.StageDistance, StageManager.Instance.StageLength, GameManager.Instance.gameTime, 900f);
    private static Spawner Fixture()
    {
        GameManager.Instance = new GameManager(); StageManager.Instance = new StageManager();
        PoolManager.instance = new PoolManager { bosses = new[] { new UnityEngine.GameObject { name = "Eye" }, new UnityEngine.GameObject { name = "Train" } } };
        BossWarningLoopUI.Instance = new BossWarningLoopUI();
        UnityEngine.Object.Instantiations = 0; UnityEngine.Object.LastPrefab = null;
        UnityEngine.Time.timeScale = 1f; UnityEngine.Time.deltaTime = 0f;
        Spawner.Instance = null;
        var spawner = new Spawner(); Set(spawner, "bossSequence", new[] { BossName.TrainBoss, BossName.EyeBoss });
        Invoke(spawner, "Awake"); Invoke(spawner, "Start");
        return spawner;
    }
    public static string Run()
    {
        checks = 0;
        var spawner = Fixture(); PoolManager.instance = null; StageManager.Instance.AdvanceCallback();
        Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage, "missing pool does not enter Boss or reserve route");
        Check(Get<int>(spawner, "nextBossIndex") == 0 && spawner.PendingRoutines == 0 && BossWarningLoopUI.Instance.Shown == 0, "missing pool keeps boss cursor and coroutine unchanged");
        PoolManager.instance = new PoolManager { bosses = null }; StageManager.Instance.AdvanceCallback();
        Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage, "missing boss array is rejected before reservation");
        PoolManager.instance.bosses = new UnityEngine.GameObject[1]; StageManager.Instance.AdvanceCallback();
        Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage, "out-of-range boss array is rejected before reservation");
        PoolManager.instance.bosses = new UnityEngine.GameObject[2]; StageManager.Instance.AdvanceCallback();
        Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage, "null prefab is rejected before reservation");
        PoolManager.instance.bosses[1] = new UnityEngine.GameObject { name = "Repaired Train" }; StageManager.Instance.AdvanceCallback();
        Check(GameManager.Instance.CurrentState == GameState.Boss && Due(spawner) == StageBossRequest.None && Get<int>(spawner, "nextBossIndex") == 1, "repaired configuration reserves the same boss once");
        Check(BossWarningLoopUI.Instance.Shown == 1 && spawner.PendingRoutines == 1, "null settings use existing warning fallback without exception");

        spawner.isActiveAndEnabled = false; Invoke(spawner, "OnDisable");
        Check(GameManager.Instance.CurrentState == GameState.Boss, "disable does not revive a possibly unloading scene");
        Check(Due(spawner) == StageBossRequest.Stage && Get<int>(spawner, "nextBossIndex") == 0, "disable releases unspawned request and restores boss cursor");
        Check(spawner.PendingRoutines == 0 && BossWarningLoopUI.Instance.Hidden == 1, "disable cancels coroutine and hides warning");
        spawner.isActiveAndEnabled = true; Invoke(spawner, "OnEnable");
        Check(GameManager.Instance.CurrentState == GameState.Playing, "re-enable recovers cancelled warning to Playing");
        StageManager.Instance.AdvanceCallback(); spawner.ResumeWarningWait();
        Check(UnityEngine.Object.Instantiations == 1 && UnityEngine.Object.LastPrefab == "Repaired Train", "re-enable retries the same boss rather than skipping sequence");
        spawner.SetSpawning(false);
        Check(GameManager.Instance.CurrentState == GameState.Boss && Due(spawner) == StageBossRequest.None, "stopping after real spawn preserves Boss and committed request");
        Check(Get<int>(spawner, "nextBossIndex") == 1, "stopping after real spawn keeps committed cursor");

        spawner = Fixture(); StageManager.Instance.AdvanceCallback(); spawner.SetSpawning(false);
        Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage, "SetSpawning false recovers an unspawned warning");
        Check(Get<int>(spawner, "nextBossIndex") == 0 && BossWarningLoopUI.Instance.Hidden == 1 && spawner.PendingRoutines == 0, "SetSpawning false fully releases reservation and warning");
        StageManager.Instance.AdvanceCallback(); Check(BossWarningLoopUI.Instance.Shown == 1, "disabled spawn flag does not request a replacement boss");
        spawner.SetSpawning(true); StageManager.Instance.AdvanceCallback(); spawner.ResumeWarningWait();
        Check(UnityEngine.Object.Instantiations == 1 && UnityEngine.Object.LastPrefab == "Train", "SetSpawning true retries the same boss once");

        foreach (var pause in new[] { GameState.Pause, GameState.Event })
        {
            spawner = Fixture(); StageManager.Instance.AdvanceCallback(); GameManager.Instance.ChangeState(pause); spawner.SetSpawning(false);
            Check(GameManager.Instance.CurrentState == pause, "warning cancellation preserves paused/modal state " + pause);
            spawner.SetSpawning(true); Check(GameManager.Instance.CurrentState == pause, "spawn re-enable does not close modal " + pause);
            GameManager.Instance.ChangeState(GameState.Boss); Invoke(spawner, "Update");
            Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage, "normal modal resume recovers cancelled warning " + pause);
        }

        foreach (var terminal in new[] { GameState.Die, GameState.Ending, GameState.StageTransition, GameState.Title, GameState.Start })
        {
            spawner = Fixture(); StageManager.Instance.AdvanceCallback(); GameManager.Instance.ChangeState(terminal);
            spawner.isActiveAndEnabled = false; Invoke(spawner, "OnDisable"); spawner.isActiveAndEnabled = true; Invoke(spawner, "OnEnable");
            Check(GameManager.Instance.CurrentState == terminal && spawner.PendingRoutines == 0 && UnityEngine.Object.Instantiations == 0, "terminal cancellation never resumes play or spawns " + terminal);
        }
        spawner = Fixture(); StageManager.Instance.AdvanceCallback(); spawner.isActiveAndEnabled = false; Invoke(spawner, "OnDisable"); Invoke(spawner, "OnDestroy");
        Check(GameManager.Instance.CurrentState == GameState.Boss && !Get<bool>(spawner, "cancelledWarningNeedsRecovery"), "scene unload clears recovery without changing shared game state");

        spawner = Fixture(); StageManager.Instance.AdvanceCallback(); PoolManager.instance.bosses[1] = null; spawner.ResumeWarningWait();
        Check(GameManager.Instance.CurrentState == GameState.Playing && Due(spawner) == StageBossRequest.Stage && Get<int>(spawner, "nextBossIndex") == 0, "prefab removal during warning rolls back unspawned encounter");
        Check(UnityEngine.Object.Instantiations == 0 && BossWarningLoopUI.Instance.Hidden == 1, "removed prefab does not create an enemy and warning closes");

        spawner = Fixture(); StageManager.Instance.StageDistance = 100f; GameManager.Instance.gameTime = 900f;
        StageManager.Instance.AdvanceCallback(); spawner.SetSpawning(false);
        Check(Due(spawner) == StageBossRequest.Final && GameManager.Instance.CurrentState == GameState.Playing, "cancelled 900-second warning releases final reservation for retry");
        spawner.SetSpawning(true); StageManager.Instance.AdvanceCallback(); spawner.ResumeWarningWait();
        Check(GameManager.Instance.CurrentState == GameState.Boss && Due(spawner) == StageBossRequest.None && UnityEngine.Object.Instantiations == 1, "final warning retry commits one final boss");

        spawner = Fixture(); var reentrantSpawner = spawner;
        GameManager.Instance.OnGameStateChanged += state => { if (state == GameState.Boss) { reentrantSpawner.isActiveAndEnabled = false; Invoke(reentrantSpawner, "OnDisable"); } };
        StageManager.Instance.AdvanceCallback();
        Check(spawner.PendingRoutines == 0 && Due(spawner) == StageBossRequest.Stage && Get<int>(spawner, "nextBossIndex") == 0, "synchronous disable during Boss transition safely releases request before coroutine handle assignment");
        Check(BossWarningLoopUI.Instance.Shown == 0, "cancelled coroutine cannot reopen warning after synchronous disable");
        spawner.isActiveAndEnabled = true; Invoke(spawner, "OnEnable");
        Check(GameManager.Instance.CurrentState == GameState.Playing, "synchronous-disable cancellation remains recoverable");

        return checks + " actual-source Spawner reservation/cancellation checks passed with dependency doubles. Coroutine first/next MoveNext is exercised; Unity scheduler, real WaitForSeconds timing and actual warning rendering are excluded.";
    }
}
