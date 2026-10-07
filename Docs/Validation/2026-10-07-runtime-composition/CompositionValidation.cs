using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LoveTrainCompositionValidation
{
    // Copied only into the isolated project's Editor folder. Never imported by the source project.
    [InitializeOnLoad]
    public static class Runner
    {
        private const string Running = "LoveTrain.CompositionValidation.Running";
        private const string Done = "LoveTrain.CompositionValidation.Done";
        private const string Stored = "LoveTrain.CompositionValidation.Result";
        private static readonly BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static Result result;
        private static bool started;
        private static float nativeStart;
        private static int spearCompleted;
        private static LonginusSpear spear;
        private static PoisonMissile missile;
        private static GameObject independent;
        private static float appliedStats;

        [Serializable] private sealed class Result
        {
            public string baseline = "95a7d5e";
            public string scope = "actual source, isolated native Play Mode; no production scene or player build";
            public int passed;
            public List<string> checks = new List<string>();
            public List<string> failures = new List<string>();
        }

        static Runner()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Done, false)) Finish();
            };
        }

        public static void Execute()
        {
            SessionState.SetBool(Running, true);
            SessionState.SetBool(Done, false);
            SessionState.SetString(Stored, JsonUtility.ToJson(new Result()));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static object Create(string name, params object[] arguments) =>
            Activator.CreateInstance(typeof(ItemInstance).Assembly.GetType(name, true), All, null, arguments, null);
        private static object Call(object value, string name, params object[] arguments) =>
            value.GetType().GetMethod(name, All).Invoke(value, arguments);
        private static T Read<T>(object value, string name)
        {
            Type type = value.GetType();
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(name, All | BindingFlags.DeclaredOnly);
                if (property != null) return (T)property.GetValue(value);
                FieldInfo field = type.GetField(name, All | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(value);
                type = type.BaseType;
            }
            throw new MissingMemberException(name);
        }
        private static void Set(object value, string name, object replacement)
        {
            Type type = value.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name, All | BindingFlags.DeclaredOnly);
                if (field != null) { field.SetValue(value, replacement); return; }
                PropertyInfo property = type.GetProperty(name, All | BindingFlags.DeclaredOnly);
                if (property != null) { property.SetValue(value, replacement); return; }
                type = type.BaseType;
            }
            throw new MissingMemberException(name);
        }
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            result.passed++;
            result.checks.Add(name);
        }
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.0001f;
        private static object StatRecorder<T>() => new Action<float, T>((amount, phase) => appliedStats += amount);
        private static void Case(string name, Action test)
        {
            try { test(); }
            catch (Exception exception)
            {
                Exception cause = exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception;
                result.failures.Add(name + ": " + cause);
            }
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (!started)
            {
                started = true;
                result = JsonUtility.FromJson<Result>(SessionState.GetString(Stored, "{}"));
                Application.logMessageReceived += (message, trace, kind) =>
                {
                    if (kind == LogType.Exception || kind == LogType.Error || kind == LogType.Assert)
                        result.failures.Add("Unity " + kind + ": " + message);
                };
                RunCases();
                nativeStart = Time.time;
            }
            else if (Time.time - nativeStart >= 0.25f)
            {
                Case("native frames", () =>
                {
                    Check(spearCompleted == 1 && spear == null, "native spear expires and completes exactly once");
                    Check(missile != null && missile.transform.position.y > 0f, "native independent missile continues moving");
                    Check(independent != null && independent.activeSelf, "independent spawn survives parent release");
                });
                SessionState.SetString(Stored, JsonUtility.ToJson(result));
                SessionState.SetBool(Running, false);
                SessionState.SetBool(Done, true);
                EditorApplication.isPlaying = false;
            }
        }

        private static void RunCases()
        {
            Case("attack timer", () =>
            {
                object cycle = Create("AttackCycle");
                Check(Read<bool>(cycle, "IsReady"), "new attack cycle is ready");
                Call(cycle, "StartCooldown", 1f);
                Call(cycle, "Elapse", 0.6f);
                Check(Near(Read<float>(cycle, "RemainingCooldown"), 0.4f), "cooldown decreases by supplied time");
                Call(cycle, "Hold"); Call(cycle, "Elapse", 2f);
                Check(Near(Read<float>(cycle, "RemainingCooldown"), 0.4f) && !Read<bool>(cycle, "IsReady"), "held cycle does not advance");
                Call(cycle, "ResumeWithCooldown", 0.5f); Call(cycle, "Elapse", 0.6f);
                Check(Read<bool>(cycle, "IsReady"), "normal completion resumes cycle");
            });
            Case("summon sequence", () =>
            {
                int begins = 0, emissions = 0, returns = 0;
                float duration = 2f;
                var completions = new List<Action>();
                object flow = Create("SummonAndAwait", new Func<bool>(() => true), new Func<Vector3>(() => Vector3.right),
                    new Action(() => begins++), new Func<int, Vector3, Action, bool>((port, target, complete) =>
                    { emissions++; completions.Add(complete); return true; }), new Func<int>(() => 3), new Action(() => { }),
                    new Action(() => returns++), new Func<float>(() => duration), new Func<Action, Action>(action => action));
                Call(flow, "Advance", 0f); Call(flow, "Advance", 10f);
                Check(begins == 1, "summon waits for child completion");
                Call(flow, "EmitFromAnimation"); Call(flow, "EmitFromAnimation");
                Check(emissions == 2, "each delivered animation event emits");
                completions[0](); duration = 3f; completions[1]();
                Check(returns == 2 && Near(Read<float>(Read<object>(flow, "Cycle"), "RemainingCooldown"), 3f), "multiple valid completions use latest cooldown");
                Call(flow, "Advance", 4f);
                Check(begins == 1, "launcher does not reattack on timer-crossing frame");
                Call(flow, "Advance", 0f);
                Check(begins == 2, "launcher rechecks readiness next frame");
            });
            Case("port firing", () =>
            {
                int begins = 0; var ports = new List<int>();
                object flow = Create("AnimatedPortFire", new Func<bool>(() => true), new Action(() => begins++),
                    new Func<int, bool>(port => { ports.Add(port); return true; }), new Func<int>(() => 2), new Func<float>(() => 1f));
                Call(flow, "Advance", 0f);
                Check(begins == 1 && Near(Read<float>(Read<object>(flow, "Cycle"), "RemainingCooldown"), 1f), "port fire starts cooldown at presentation start");
                Call(flow, "EmitFromAnimation"); Call(flow, "EmitFromAnimation"); Call(flow, "EmitFromAnimation");
                Check(ports.Count == 3 && ports[0] == 0 && ports[1] == 1 && ports[2] == 0, "animation events retain port cycling");
            });
            Case("health fuel death", () =>
            {
                object health = Create("HealthState"); Call(health, "Reset", 100f); Call(health, "ApplyDamage", 20f);
                Check(Near(Read<float>(health, "Current"), 80f), "health subtracts damage");
                Call(health, "ApplyDamage", 100f);
                Check(Read<bool>(health, "IsDepleted"), "health detects depletion");
                object fuel = Create("FuelState"); Call(fuel, "Reset", 100f); Call(fuel, "Modify", 20f, 100f);
                Check(Near(Read<float>(fuel, "Current"), 100f), "fuel caps at maximum");
                Check((bool)Call(fuel, "Modify", -200f, 100f) && Read<float>(fuel, "Current") == 0f, "fuel depletion clamps to zero");
                object death = Create("DeathLifecycle");
                Check((bool)Call(death, "TryBegin") && !(bool)Call(death, "TryBegin"), "death begins once");
                Check((bool)Call(death, "TryGrantReward") && !(bool)Call(death, "TryGrantReward"), "death reward is granted once");
                Call(death, "Reset"); Check(Read<bool>(death, "IsAlive"), "death state resets for reuse");
            });
            Case("strike", () =>
            {
                object strike = Create("TimedStrike"); Call(strike, "BeginWait", 2f); Call(strike, "Advance", 1f, false);
                Check(Near(Read<float>(strike, "Remaining"), 2f), "strike time pauses when combat stops");
                Call(strike, "BeginAttack", 1f); object target = new object();
                Check((bool)Call(strike, "TryConsumeHit", target) && !(bool)Call(strike, "TryConsumeHit", target), "strike hits each target once");
                Call(strike, "Cancel"); Check(!Read<bool>(strike, "IsActive"), "strike cancellation stops damage");
            });
            Case("stat lease", () =>
            {
                appliedStats = 0f; int level = 1;
                Type phase = typeof(ItemInstance).Assembly.GetType("StatApplicationPhase", true);
                object recorder = typeof(Runner).GetMethod("StatRecorder", All).MakeGenericMethod(phase).Invoke(null, null);
                object modifier = Create("StatModifier", new Func<int>(() => level), new Func<int, float>(value => value * 10f), recorder);
                Call(modifier, "Equip"); Check(Near(appliedStats, 10f), "stat lease applies initial amount");
                level = 2; Call(modifier, "Upgrade", 1); Check(Near(appliedStats, 20f), "stat upgrade applies difference only");
                Call(modifier, "Release"); Call(modifier, "Release"); Check(Near(appliedStats, 0f), "stat lease restores exactly once");
            });
            Case("legacy timed hook", () =>
            {
                GameObject managerObject = new GameObject("Validation GameManager");
                GameManager manager = managerObject.AddComponent<GameManager>(); manager.ChangeState(GameState.Playing);
                GameObject owner = new GameObject("Validation equipment owner");
                var source = ScriptableObject.CreateInstance<CompositionTimedItem>();
                var instance = new ItemInstance(source); instance.HandleEquip(owner);
                instance.Tick(0f, owner); Check(source.Fired == 1 && Near(instance.currentCooldown, 1f), "virtual cooldown hook still executes");
                instance.Tick(0.6f, owner); instance.Tick(0.5f, owner);
                Check(source.Fired == 2, "inventory timed effect activates on timer-crossing frame");
                instance.currentCooldown = 0.2f; instance.Tick(0.25f, owner);
                Check(source.Fired == 3, "external compatibility cooldown edit is imported");
                var manualSource = ScriptableObject.CreateInstance<CompositionTimedItem>(); manualSource.Manual = true;
                var manual = new ItemInstance(manualSource); manual.HandleEquip(owner); manual.Tick(0f, owner); manual.Tick(5f, owner);
                Check(manualSource.Fired == 1 && manual.currentCooldown == float.MaxValue, "manual effect waits for explicit completion");
                manual.StartCooldownManual(0.3f); manual.Tick(0.4f, owner);
                Check(manualSource.Fired == 2, "manual completion resumes inventory clock");
                manual.HandleUnequip(owner); manual.StartCooldownManual(2f);
                Check(manual.currentCooldown == 0f && manual.maxCooldown == 0f, "removed item ignores late cooldown restart");
                Inventory inventory = owner.AddComponent<Inventory>(); inventory.items.Add(instance);
                inventory.ProcessKillEvent(owner); Check(source.Kills == 1, "inventory retains public virtual kill hook");
                inventory.ProcessHitEvent(owner, owner); Check(source.Hits == 1, "inventory retains public virtual hit hook");
                inventory.items.Clear(); instance.HandleUnequip(owner);
                UnityEngine.Object.Destroy(source); UnityEngine.Object.Destroy(manualSource);
                var derivedHeart = ScriptableObject.CreateInstance<CompositionHeartItem>();
                var heartInstance = new ItemInstance(derivedHeart); heartInstance.HandleEquip(owner); heartInstance.Tick(0f, owner);
                Check(derivedHeart.Fired == 1 && heartInstance.currentCooldown == float.MaxValue,
                    "known heart recipe retains derived virtual activation and manual policy");
                heartInstance.HandleUnequip(owner); UnityEngine.Object.Destroy(derivedHeart);
                var derivedBible = ScriptableObject.CreateInstance<CompositionBibleItem>();
                var bibleInstance = new ItemInstance(derivedBible); bibleInstance.HandleEquip(owner); bibleInstance.Tick(0f, owner);
                Check(derivedBible.Fired == 1 && bibleInstance.currentCooldown != float.MaxValue,
                    "known bible recipe retains derived virtual activation and automatic policy");
                bibleInstance.HandleUnequip(owner); UnityEngine.Object.Destroy(derivedBible);
            });
            Case("all item recipes", () =>
            {
                string[] names = { "BlueGear_SO", "RedGear_SO", "HealingItem_SO", "BeatingHeart_SO", "BloodyBible_SO", "MagicBullet_SO",
                    "LaserGun_SO", "Revolver_SO", "RearGun_SO", "PoisonMissileLauncher_SO", "LonginusLauncher_SO", "CrownOfThorns_SO", "GiantMaw_SO" };
                foreach (string name in names)
                {
                    var source = (Item_SO)ScriptableObject.CreateInstance(typeof(ItemInstance).Assembly.GetType(name, true));
                    var instance = new ItemInstance(source); Call(instance, "EnsureEffects");
                    Check(Read<Array>(instance, "effects").Length > 0, name + " creates independent executable effects");
                    UnityEngine.Object.Destroy(source);
                }
            });
            Case("scope and native objects", () =>
            {
                GameObject owner = new GameObject("Scope owner"); object scope = Create("SpawnScope", owner);
                int callbacks = 0; Action guarded = (Action)Call(scope, "Guard", new Action(() => callbacks++)); guarded();
                GameObject bound = new GameObject("Bound child"); Call(scope, "Track", bound);
                int childCleanup = 0;
                bound.AddComponent<CompositionOwnedProbe>().Disabled = () => { childCleanup++; Call(scope, "Forget", bound); };
                independent = new GameObject("Independent child");
                Call(scope, "ReleaseBoundObjects"); guarded();
                Check(callbacks == 1 && !bound.activeSelf && independent.activeSelf, "scope cancels owner callbacks and bound objects only");
                Check(childCleanup == 1, "bound child can unregister during native cleanup");
                Call(scope, "BeginActivation"); guarded();
                Check(callbacks == 1, "old activation callback cannot affect reactivation");
                GameObject launcherObject = new GameObject("Attachment release launcher");
                LonginusLauncher launcher = launcherObject.AddComponent<LonginusLauncher>();
                object launcherScope = Read<object>(Read<object>(launcher, "host"), "Scope");
                int lateCompletions = 0;
                Action lateCompletion = (Action)Call(launcherScope, "Guard", new Action(() => lateCompletions++));
                Call(launcher, "ReleaseAttachment"); lateCompletion();
                Check(lateCompletions == 0, "actual attachment release cancels owner completion before deferred destroy");
                UnityEngine.Object.Destroy(launcherObject);
                GameObject spearObject = new GameObject("Native spear"); spear = spearObject.AddComponent<LonginusSpear>();
                spear.Initialize(5f, 10f, 0.05f, Vector3.zero, Vector3.right, () => spearCompleted++);
                GameObject missileObject = new GameObject("Native independent missile"); missile = missileObject.AddComponent<PoisonMissile>();
                missile.Initialize(2f, 1000f, null, 1f, 0.5f, 1f);
            });
            Case("target capabilities", () =>
            {
                GameObject targetObject = new GameObject("Damage target"); targetObject.SetActive(false);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>(); CreditEnemy enemy = targetObject.AddComponent<CreditEnemy>();
                Set(enemy, "hp", 100f); targetObject.SetActive(true); Set(enemy, "hasEnteredScreen", true);
                TargetHandle target = TargetRegistry.Resolve(collider);
                Check(target != null && target.IsAlive && target.IsTargetable, "real Unity target resolves cached capabilities");
                target.RequestDamage(10f); Check(Near(Read<float>(enemy, "currentHP"), 90f), "capability damage reaches composed health");
                GameObject child = new GameObject("Child collider"); child.transform.SetParent(targetObject.transform);
                Check(TargetRegistry.Resolve(child.AddComponent<BoxCollider2D>()) == null, "target lookup does not widen to parents");
                UnityEngine.Object.DestroyImmediate(targetObject);
                Check(!target.IsAlive && !target.IsActive && target.Owner == null, "target handle respects destroyed Unity owner");
            });
            Case("gas animation gate", () =>
            {
                GameObject gasObject = new GameObject("Native gas"); Collider2D collider = gasObject.AddComponent<BoxCollider2D>();
                PoisonGas gas = gasObject.AddComponent<PoisonGas>(); gas.Initialize(1f, 0.5f, 0f);
                Check(!collider.enabled, "gas initializes without collision damage"); gas.AnimEvent_EnableGas();
                Check(collider.enabled, "gas animation signal enables collision damage");
            });
            Case("area cadence", () =>
            {
                GameObject owner = new GameObject("Area target"); owner.SetActive(false);
                Collider2D collider = owner.AddComponent<BoxCollider2D>(); owner.AddComponent<CreditEnemy>(); owner.SetActive(true);
                TargetHandle target = TargetRegistry.Resolve(collider); int caughtUp = 0, dropped = 0;
                object laser = Create("AreaPresence", true, true); object gas = Create("AreaPresence", false, true);
                Call(laser, "Enter", target); Call(laser, "Enter", target); Call(gas, "Enter", target);
                Call(laser, "Advance", 0.25f, 0.1f, new Action<TargetHandle>(value => caughtUp++));
                Call(gas, "Advance", 0.25f, 0.1f, new Action<TargetHandle>(value => dropped++));
                Check(caughtUp == 2 && dropped == 1, "laser catches up while gas drops elapsed ticks");
                Call(laser, "Advance", 0.06f, 0.1f, new Action<TargetHandle>(value => caughtUp++));
                Call(gas, "Advance", 0.06f, 0.1f, new Action<TargetHandle>(value => dropped++));
                Check(caughtUp == 3 && dropped == 1, "laser preserves residual time and unique occupants");
                Call(laser, "ResetForActivation"); Call(laser, "Advance", 1f, 0.1f, new Action<TargetHandle>(value => caughtUp++));
                Check(caughtUp == 3, "reactivated area waits for its animation hit gate");
                UnityEngine.Object.Destroy(owner);
            });
        }

        private static void Finish()
        {
            result = JsonUtility.FromJson<Result>(SessionState.GetString(Stored, "{}"));
            string path = System.IO.Path.Combine(Application.dataPath, "../composition-results.json");
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(result, true));
            Debug.Log("COMPOSITION VALIDATION: " + result.passed + " checks, " + result.failures.Count + " failures; " + path);
            SessionState.SetBool(Done, false);
            EditorApplication.Exit(result.failures.Count == 0 ? 0 : 1);
        }
    }

    public sealed class CompositionTimedItem : Item_SO
    {
        public int Fired;
        public int Hits;
        public int Kills;
        public bool Manual;
        public override bool IsManualCooldown => Manual;
        public override float GetCooldownForLevel(int level) => 1f;
        public override void OnCooldownComplete(GameObject owner, ItemInstance instance) { Fired++; }
        public override void OnKillEnemy(GameObject owner, GameObject target) { Kills++; }
        public override void OnDealDamage(GameObject owner, GameObject target, GameObject source, ItemInstance instance) { Hits++; }
    }
    public sealed class CompositionHeartItem : BeatingHeart_SO
    {
        public int Fired;
        public override bool IsManualCooldown => true;
        public override void OnCooldownComplete(GameObject owner, ItemInstance instance) { Fired++; }
    }
    public sealed class CompositionBibleItem : BloodyBible_SO
    {
        public int Fired;
        public override bool IsManualCooldown => false;
        public override void OnCooldownComplete(GameObject owner, ItemInstance instance) { Fired++; }
    }
}
