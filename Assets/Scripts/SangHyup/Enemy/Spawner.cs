using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance;

    [System.Serializable]
    public struct SpawnPhase
    {
        public float startTime;
        public int groundMinIndex; public int groundMaxIndex;
        public int flyMinIndex; public int flyMaxIndex;
        public int groundEliteMinIndex; public int groundEliteMaxIndex;
        public int flyEliteMinIndex; public int flyEliteMaxIndex;
        public float spawnInterval;
    }

    [System.Serializable]
    public class BossSpawnSetting
    {
        public BossName bossName;

        [Tooltip("이 보스가 처음 생성될 위치 (전투 위치 혹은 등장 시작 위치)")]
        public Transform spawnPoint;

        [Tooltip("등장 연출 후 도착할 위치 (비워두면 연출 없이 바로 패턴 시작)")]
        public Transform arrivalPoint;

        [Tooltip("이동하는 데 걸리는 시간")]
        public float entranceDuration = 2.0f;

        public float spawnDelayAfterWarning = 4.0f;
        public float soundPlayDelay = 0.5f;
    }

    [System.Serializable]
    public class PeriodicSpawnTask
    {
        public GameObject prefab; public float interval; public bool isFly; public float timer;
        public PeriodicSpawnTask(GameObject prefab, float interval, bool isFly)
        {
            this.prefab = prefab; this.interval = interval; this.isFly = isFly; this.timer = 0f;
        }
    }

    [Header("Phase Settings")]
    [SerializeField] private SpawnPhase[] spawnPhases;

    [Header("Boss Settings")]
    [Tooltip("등장할 보스 순서 (예: Train -> Eye -> Train)")]
    [SerializeField] private BossName[] bossSequence;

    [Tooltip("각 보스별 세부 설정 (위치, 딜레이 등)")]
    [SerializeField] private BossSpawnSetting[] bossSettings;
    // Retain the serialized name for existing scenes; ordinary bosses no longer
    // use a fixed run-time interval.
    [HideInInspector, SerializeField] private float bossSpawnInterval = 180.0f;

    [Header("Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] groundFrontPoints;
    [SerializeField] private Transform[] groundRearPoints;
    [SerializeField] private BoxCollider2D[] flyMobSpawnAreas;
    [SerializeField] private ForwardCameraFollow cameraFollow;

    // --- 제어 플래그 ---
    private bool isSpawningEnabled = true;
    private bool isRearSpawnEnabled = true;

    // --- 내부 변수 ---
    private int currentGroundMin, currentGroundMax;
    private int currentFlyMin, currentFlyMax;
    private int currentGroundEliteMin, currentGroundEliteMax;
    private int currentFlyEliteMin, currentFlyEliteMax;
    private float currentSpawnInterval = 1.0f;

    [Header("Fuel supply")]
    [SerializeField] private GameObject fuelBarrelPrefab;
    [SerializeField] private float fuelBarrelInterval = 20f;
    private float fuelBarrelTimer;
    [Header("Acceleration spawn balance")]
    [SerializeField] private Train playerTrain;
    [SerializeField, Min(1f)] private float maxBasicSpawnRateMultiplier = 1.35f;
    [SerializeField, Range(0f, 1f)] private float maxFrontSpawnProbability = 0.8f;
    private float AccelerationProgress => playerTrain != null ? playerTrain.AccelerationProgress : 0f;
    private float mobTimer;
    private float eliteMobTimer;
    private int nextBossIndex = 0;
    private readonly StageBossSchedule bossSchedule = new StageBossSchedule();
    private StageManager stageManager;
    private GameManager gameManager;
    private Coroutine bossSpawnRoutine;
    private bool bossSequenceRunning;
    private StageBossRequest pendingBossRequest;
    private int pendingBossStageNumber;
    private int bossIndexBeforeWarning;
    private bool cancelledWarningNeedsRecovery;

    private List<PeriodicSpawnTask> periodicSpawnTasks = new List<PeriodicSpawnTask>();

    private void Awake() { if (Instance == null) Instance = this; }

    private void Start()
    {
        if (playerTrain == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) playerTrain = player.GetComponent<Train>();
        }
        mobTimer = 0f;
        eliteMobTimer = eliteMobSpawnInterval;

        nextBossIndex = 0;
        bossSchedule.Reset();
        gameManager = GameManager.Instance;
        stageManager = StageManager.Instance;
        if (stageManager != null) stageManager.OnProgressAdvanced += HandleStageProgress;
        if (gameManager != null) gameManager.OnGameStateChanged += HandleGameStateChanged;

        periodicSpawnTasks.Clear();
        UpdatePhase(0f);

        isSpawningEnabled = true;
        isRearSpawnEnabled = true;
    }

    private void OnDestroy()
    {
        if (stageManager != null) stageManager.OnProgressAdvanced -= HandleStageProgress;
        if (gameManager != null) gameManager.OnGameStateChanged -= HandleGameStateChanged;
        CancelBossSequence();
        cancelledWarningNeedsRecovery = false;
        if (Instance == this) Instance = null;
    }

    // OnDisable also runs during scene teardown. Defer any gameplay recovery
    // until this component is actually enabled again, rather than reviving an unload.
    private void OnDisable() { CancelBossSequence(true); }
    private void OnEnable() { RecoverCancelledWarning(); }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Die || state == GameState.Ending || state == GameState.StageTransition ||
            state == GameState.Title || state == GameState.Start)
        {
            CancelBossSequence();
            cancelledWarningNeedsRecovery = false;
        }
    }

    private void CancelBossSequence(bool recoverOnEnable = false)
    {
        bool wasPending = bossSequenceRunning;
        if (bossSpawnRoutine != null) StopCoroutine(bossSpawnRoutine);
        bossSpawnRoutine = null;
        bossSequenceRunning = false;
        if (!wasPending) return;

        bossSchedule.CancelRequest(pendingBossRequest, pendingBossStageNumber);
        nextBossIndex = bossIndexBeforeWarning;
        pendingBossRequest = StageBossRequest.None;
        if (BossWarningLoopUI.Instance != null) BossWarningLoopUI.Instance.HideWarning();
        GameManager manager = GameManager.Instance;
        cancelledWarningNeedsRecovery = recoverOnEnable && manager != null &&
            (manager.CurrentState == GameState.Boss || manager.CurrentState == GameState.Pause || manager.CurrentState == GameState.Event);
    }

    private void RecoverCancelledWarning()
    {
        if (!cancelledWarningNeedsRecovery || !isActiveAndEnabled || GameManager.Instance == null) return;
        GameState state = GameManager.Instance.CurrentState;
        // Preserve the modal's queue/time-scale bookkeeping. Its normal resume
        // restores Boss first; this Update then recovers the cancelled encounter.
        if (state == GameState.Pause || state == GameState.Event) return;
        cancelledWarningNeedsRecovery = false;
        if (state == GameState.Boss) GameManager.Instance.ChangeState(GameState.Playing);
    }

    private void HandleStageProgress()
    {
        if (!isActiveAndEnabled || !isSpawningEnabled || bossSequenceRunning || gameManager == null || stageManager == null) return;
        StageBossRequest request = bossSchedule.GetDueRequest(gameManager.CurrentState == GameState.Playing,
            stageManager.StageNumber, stageManager.StageDistance, stageManager.StageLength,
            gameManager.gameTime, gameManager.maxGameTime);
        if (request != StageBossRequest.None) SpawnNextBoss(request);
    }

    private void Update()
    {
        RecoverCancelledWarning();
        if (GameManager.Instance == null || Time.timeScale <= 0f ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss)) return;

        if (isSpawningEnabled && fuelBarrelPrefab != null)
        {
            fuelBarrelTimer += Time.deltaTime;
            if (fuelBarrelTimer >= Mathf.Max(1f, fuelBarrelInterval))
            {
                fuelBarrelTimer = 0f;
                Instantiate(fuelBarrelPrefab, GetSpawnPosition(false), Quaternion.identity);
            }
        }

        float gameTime = GameManager.Instance.gameTime;
        UpdatePhase(gameTime);

        if (isSpawningEnabled) mobTimer += Time.deltaTime * Mathf.Lerp(1f, Mathf.Max(1f, maxBasicSpawnRateMultiplier), AccelerationProgress);
        if (isSpawningEnabled && gameTime >= firstEliteSpawnTime) eliteMobTimer += Time.deltaTime;

        if (isSpawningEnabled && mobTimer >= Mathf.Max(0.1f, currentSpawnInterval))
        {
            SpawnBasicMobs();
            mobTimer %= Mathf.Max(0.1f, currentSpawnInterval);
        }

        if (isSpawningEnabled && gameTime >= firstEliteSpawnTime && eliteMobTimer >= eliteMobSpawnInterval)
        {
            SpawnEliteMob();
            eliteMobTimer = 0f;
        }

        HandlePeriodicTasks();
    }

    private bool SpawnNextBoss(StageBossRequest request)
    {
        if (bossSequence == null || bossSequence.Length == 0)
        {
            Debug.LogWarning("[Spawner] BossSequence가 설정되지 않았습니다!");
            return false;
        }

        int safeIndex = nextBossIndex % bossSequence.Length;
        BossName bossToSpawn = bossSequence[safeIndex];

        if (!StartBossSequence(bossToSpawn, request)) return false;

        Debug.Log($"[Spawner] 다음 보스 인덱스 예약: {nextBossIndex} ({bossSequence[nextBossIndex]})");
        return true;
    }

    private bool StartBossSequence(BossName bossName, StageBossRequest request = StageBossRequest.None)
    {
        if (!isActiveAndEnabled || !isSpawningEnabled || bossSequenceRunning || GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.Playing) return false;
        // Validate before entering Boss or reserving the sequence/schedule. A
        // missing pool/prefab must remain retryable in Playing when repaired.
        if (GetBossPrefab(bossName) == null) return false;
        pendingBossRequest = request;
        pendingBossStageNumber = stageManager != null ? stageManager.StageNumber : 0;
        bossIndexBeforeWarning = nextBossIndex;
        if (request != StageBossRequest.None)
        {
            bossSchedule.RecordRequest(request, pendingBossStageNumber);
            nextBossIndex = (nextBossIndex + 1) % bossSequence.Length;
        }
        bossSequenceRunning = true;
        Coroutine startedRoutine = StartCoroutine(BossSpawnRoutine(bossName));
        // A state listener may disable this component synchronously during the
        // coroutine's first MoveNext, before StartCoroutine returns its handle.
        if (!bossSequenceRunning)
        {
            if (startedRoutine != null) StopCoroutine(startedRoutine);
            return false;
        }
        bossSpawnRoutine = startedRoutine;
        return true;
    }

    private IEnumerator BossSpawnRoutine(BossName bossName)
    {
        BossSpawnSetting setting = GetBossSetting(bossName);
        Debug.Log($"[Spawner] {GameManager.Instance.gameTime}초: 보스({bossName}) 등장 시퀀스!");

        GameManager.Instance.AppearBoss();

        if (!bossSequenceRunning || !isActiveAndEnabled || !isSpawningEnabled) yield break;

        if (BossWarningLoopUI.Instance != null)
        {
            BossWarningLoopUI.Instance.ShowWarning();
        }

        yield return new WaitForSeconds(setting.spawnDelayAfterWarning);

        // Pausing/event UI suspends the existing scaled warning wait. Terminal
        // states cancel it, preventing a boss from appearing after a lost run.
        while (GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Pause || GameManager.Instance.CurrentState == GameState.Event))
            yield return null;
        if (!isSpawningEnabled || !isActiveAndEnabled || GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.Boss || !SpawnBossObject(bossName))
        {
            // The prefab can be removed while the warning is running. Release
            // only an unspawned encounter, preserving a real active boss.
            CancelBossSequence(true);
            RecoverCancelledWarning();
            yield break;
        }
        bossSpawnRoutine = null;
        bossSequenceRunning = false;
        pendingBossRequest = StageBossRequest.None;
    }

    // ✨ [수정] arrivalPoint 유무에 따른 분기 처리
    private bool SpawnBossObject(BossName boss)
    {
        GameObject bossPrefab = GetBossPrefab(boss);
        if (bossPrefab != null)
        {
            BossSpawnSetting setting = GetBossSetting(boss);

            if (bossPrefab != null)
            {
                // 1. 스폰 위치 결정 (SpawnPoint가 없으면 Spawner 위치)
                Vector3 spawnPos = CameraRelativePosition(transform);
                if (setting.spawnPoint != null)
                {
                    spawnPos = CameraRelativePosition(setting.spawnPoint);
                }
                else
                {
                    Debug.LogError($"[Spawner] {boss}의 Spawn Point가 BossSettings에 할당되지 않았습니다!");
                }

                // 2. 보스 생성 (SpawnPoint 위치에)
                // Once instantiation begins, disable/stop must not roll back the
                // committed encounter or return to Playing with a live boss.
                bossSequenceRunning = false;
                GameObject bossObj = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

                // 3. 등장 연출 처리
                Boss bossScript = bossObj.GetComponent<Boss>();
                if (bossScript != null)
                {
                    // ✨ arrivalPoint가 존재할 때만 연출 실행
                    if (setting.arrivalPoint != null)
                    {
                        bossScript.StartEntranceRoutine(CameraRelativePosition(setting.arrivalPoint), setting.entranceDuration);
                    }
                    else
                    {
                        // arrivalPoint가 없으면 아무것도 안 함.
                        // Boss.cs의 OnEnable에서 isEntranceActive = false로 초기화되므로
                        // 즉시 패턴 로직이 작동함 (기존 방식)
                    }
                }
                return true;
            }
        }
        return false;
    }

    private void SpawnBasicMobs()
    {
        if (UnityEngine.Random.value < 0.5f)
        {
            int idx = UnityEngine.Random.Range(currentGroundMin, currentGroundMax + 1);
            SpawnMobInternal(idx, isFly: false);
        }
        else
        {
            int idx = UnityEngine.Random.Range(currentFlyMin, currentFlyMax + 1);
            SpawnMobInternal(idx, isFly: true);
        }
    }

    private void SpawnEliteMob()
    {
        bool isFly = UnityEngine.Random.value > 0.5f;
        GameObject enemy = null;

        if (isFly)
        {
            int idx = UnityEngine.Random.Range(currentFlyEliteMin, currentFlyEliteMax + 1);
            enemy = PoolManager.instance.GetFlyEliteMob(idx);
        }
        else
        {
            int idx = UnityEngine.Random.Range(currentGroundEliteMin, currentGroundEliteMax + 1);
            enemy = PoolManager.instance.GetGroundEliteMob(idx);
        }

        if (enemy != null) SpawnMobCommon(enemy, isFly);
    }

    private void SpawnMobInternal(int index, bool isFly)
    {
        GameObject enemy = isFly ? PoolManager.instance.GetFlyMob(index) : PoolManager.instance.GetGroundMob(index);
        SpawnMobCommon(enemy, isFly);
    }

    private void SpawnMobFromPrefab(GameObject prefab, bool isFly)
    {
        GameObject enemy = PoolManager.instance.GetMob(prefab);
        SpawnMobCommon(enemy, isFly);
    }

    private void SpawnMobCommon(GameObject enemy, bool isFly)
    {
        if (enemy == null) return;
        enemy.transform.position = GetSpawnPosition(isFly);
        InitEnemyPhysics(enemy);
        Mob mob = enemy.GetComponent<Mob>();
        if (mob != null) { mob.OnDied -= RespawnMob; mob.OnDied += RespawnMob; }
    }

    private float SideWeight(bool front, int frontCount, int rearCount)
    {
        if (frontCount == 0) return !front && isRearSpawnEnabled ? 1f : 0f;
        if (rearCount == 0 || !isRearSpawnEnabled) return front ? 1f : 0f;
        float baseline = (float)frontCount / (frontCount + rearCount);
        float probability = Mathf.Lerp(baseline, Mathf.Max(baseline, maxFrontSpawnProbability), AccelerationProgress);
        return front ? probability / frontCount : (1f - probability) / rearCount;
    }

    private Vector3 GetSpawnPosition(bool isFly)
    {
        if (isFly && flyMobSpawnAreas != null && flyMobSpawnAreas.Length > 0)
        {
            float playerX = playerTrain != null ? playerTrain.transform.position.x : transform.position.x;
            int fronts = 0, rears = 0;
            foreach (var area in flyMobSpawnAreas)
                if (area != null) { if (area.bounds.center.x + CameraOffsetFor(area.transform) >= playerX) fronts++; else rears++; }
            float total = fronts * SideWeight(true, fronts, rears) + rears * SideWeight(false, fronts, rears);
            float roll = UnityEngine.Random.value * total;
            foreach (var area in flyMobSpawnAreas)
            {
                if (area == null) continue;
                float weight = SideWeight(area.bounds.center.x + CameraOffsetFor(area.transform) >= playerX, fronts, rears);
                if (weight <= 0f) continue;
                roll -= weight;
                if (roll > 0f) continue;
                Bounds bounds = area.bounds;
                return new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x) + CameraOffsetFor(area.transform), UnityEngine.Random.Range(bounds.min.y, bounds.max.y), 0f);
            }
        }
        int frontCount = 0, rearCount = 0;
        if (groundFrontPoints != null) foreach (var point in groundFrontPoints) if (point != null) frontCount++;
        if (groundRearPoints != null) foreach (var point in groundRearPoints) if (point != null) rearCount++;
        float frontWeight = SideWeight(true, frontCount, rearCount);
        float rearWeight = SideWeight(false, frontCount, rearCount);
        float sum = frontCount * frontWeight + rearCount * rearWeight;
        float sample = UnityEngine.Random.value * sum;
        if (groundFrontPoints != null && frontWeight > 0f)
            foreach (var point in groundFrontPoints)
            {
                if (point == null) continue;
                sample -= frontWeight;
                if (sample <= 0f) return CameraRelativePosition(point);
            }
        if (groundRearPoints != null && rearWeight > 0f)
            foreach (var point in groundRearPoints)
            {
                if (point == null) continue;
                sample -= rearWeight;
                if (sample <= 0f) return CameraRelativePosition(point);
            }
        return CameraRelativePosition(transform);
    }

    private float CameraOffsetFor(Transform point)
    {
        return cameraFollow != null && !point.IsChildOf(cameraFollow.transform) ? cameraFollow.CurrentOffsetX : 0f;
    }

    private Vector3 CameraRelativePosition(Transform point)
    {
        return point.position + Vector3.right * CameraOffsetFor(point);
    }

    private void UpdatePhase(float currentTime)
    {
        if (spawnPhases == null) return;
        for (int i = spawnPhases.Length - 1; i >= 0; i--)
        {
            if (currentTime >= spawnPhases[i].startTime)
            {
                currentGroundMin = spawnPhases[i].groundMinIndex; currentGroundMax = spawnPhases[i].groundMaxIndex;
                currentFlyMin = spawnPhases[i].flyMinIndex; currentFlyMax = spawnPhases[i].flyMaxIndex;
                currentGroundEliteMin = spawnPhases[i].groundEliteMinIndex; currentGroundEliteMax = spawnPhases[i].groundEliteMaxIndex;
                currentFlyEliteMin = spawnPhases[i].flyEliteMinIndex; currentFlyEliteMax = spawnPhases[i].flyEliteMaxIndex;
                currentSpawnInterval = spawnPhases[i].spawnInterval;
                return;
            }
        }
    }

    private void HandlePeriodicTasks()
    {
        if (!isSpawningEnabled) return;
        for (int i = 0; i < periodicSpawnTasks.Count; i++)
        {
            var task = periodicSpawnTasks[i];
            task.timer += Time.deltaTime;
            if (task.timer >= task.interval) { SpawnMobFromPrefab(task.prefab, task.isFly); task.timer = 0f; }
        }
    }

    public void AddPeriodicSpawnTask(GameObject prefab, float interval, bool isFly)
    {
        if (prefab == null) return;
        if (interval <= 0.1f) interval = 0.1f;
        periodicSpawnTasks.Add(new PeriodicSpawnTask(prefab, interval, isFly));
    }

    public void SpawnMobBatch(GameObject prefab, int count, float delay, bool isFly) { StartCoroutine(SpawnBatchRoutine(prefab, count, delay, isFly)); }
    private IEnumerator SpawnBatchRoutine(GameObject prefab, int count, float delay, bool isFly)
    {
        Vector3 spawnPos = GetSpawnPosition(isFly);
        for (int i = 0; i < count; i++)
        {
            GameObject enemy = PoolManager.instance.GetMob(prefab);
            if (enemy != null) { enemy.transform.position = spawnPos; InitEnemyPhysics(enemy); }
            yield return new WaitForSeconds(delay);
        }
    }

    public void SetSpawning(bool enabled)
    {
        isSpawningEnabled = enabled;
        if (!enabled)
        {
            CancelBossSequence(true);
            StopAllCoroutines();
        }
        RecoverCancelledWarning();
    }
    public void SetRearSpawning(bool enabled) { isRearSpawnEnabled = enabled; }
    private void InitEnemyPhysics(GameObject enemy) { Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>(); if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; } }

    private BossSpawnSetting GetBossSetting(BossName name)
    {
        if (bossSettings != null)
            foreach (var s in bossSettings)
                if (s != null && s.bossName == name) return s;

        return new BossSpawnSetting { bossName = name, spawnDelayAfterWarning = 3.0f };
    }

    private GameObject GetBossPrefab(BossName name)
    {
        if (PoolManager.instance == null) return null;
        try { return PoolManager.instance.GetBoss(name); }
        // PoolManager's authored boss array is accessed directly by GetBoss.
        // Reject its absent/out-of-range entries at this feature boundary.
        catch (IndexOutOfRangeException) { return null; }
        catch (NullReferenceException) { return null; }
    }

    private void RespawnMob(Mob mob) { }
    public void SpawnBoss(BossName boss) { StartBossSequence(boss); }
    public void SpawnTrainBoss() { StartBossSequence(BossName.TrainBoss); }
    public void SpawnEyeBoss() { StartBossSequence(BossName.EyeBoss); }
}

// Run-time ending takes priority over the route completion on the same frame.
// Request state is committed only after Spawner accepts a valid boss sequence.
public enum StageBossRequest { None, Stage, Final }

public sealed class StageBossSchedule
{
    private int requestedStageNumber;
    private bool finalRequested;
    private int finalRequestedStageNumber;

    public void Reset() { requestedStageNumber = 0; finalRequested = false; finalRequestedStageNumber = 0; }

    public StageBossRequest GetDueRequest(bool isPlaying, int stageNumber, float distance, float length,
        float gameTime, float endingTime)
    {
        if (!isPlaying || finalRequested) return StageBossRequest.None;
        if (gameTime >= endingTime) return StageBossRequest.Final;
        if (length > 0f && distance >= length && requestedStageNumber != stageNumber) return StageBossRequest.Stage;
        return StageBossRequest.None;
    }

    public void RecordRequest(StageBossRequest request, int stageNumber)
    {
        if (request == StageBossRequest.Final) { finalRequested = true; finalRequestedStageNumber = stageNumber; }
        else if (request == StageBossRequest.Stage) requestedStageNumber = stageNumber;
    }

    public void CancelRequest(StageBossRequest request, int stageNumber)
    {
        if (request == StageBossRequest.Stage && requestedStageNumber == stageNumber) requestedStageNumber = 0;
        else if (request == StageBossRequest.Final && finalRequestedStageNumber == stageNumber)
        {
            finalRequested = false;
            finalRequestedStageNumber = 0;
        }
    }
}
