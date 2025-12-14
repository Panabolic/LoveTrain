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
    [SerializeField] private float bossSpawnInterval = 180.0f;

    [Header("Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] groundFrontPoints;
    [SerializeField] private Transform[] groundRearPoints;
    [SerializeField] private BoxCollider2D[] flyMobSpawnAreas;

    // --- 제어 플래그 ---
    private bool isSpawningEnabled = true;
    private bool isRearSpawnEnabled = true;

    // --- 내부 변수 ---
    private int currentGroundMin, currentGroundMax;
    private int currentFlyMin, currentFlyMax;
    private int currentGroundEliteMin, currentGroundEliteMax;
    private int currentFlyEliteMin, currentFlyEliteMax;
    private float currentSpawnInterval = 1.0f;

    private float mobTimer;
    private float eliteMobTimer;
    private float nextBossSpawnTime;

    private int nextBossIndex = 0;

    private List<PeriodicSpawnTask> periodicSpawnTasks = new List<PeriodicSpawnTask>();

    private void Awake() { if (Instance == null) Instance = this; }

    private void Start()
    {
        mobTimer = 0f;
        eliteMobTimer = eliteMobSpawnInterval;

        nextBossSpawnTime = bossSpawnInterval;
        nextBossIndex = 0;

        periodicSpawnTasks.Clear();
        UpdatePhase(0f);

        isSpawningEnabled = true;
        isRearSpawnEnabled = true;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss) return;

        float gameTime = GameManager.Instance.gameTime;
        UpdatePhase(gameTime);

        mobTimer += Time.deltaTime;
        if (gameTime >= firstEliteSpawnTime) eliteMobTimer += Time.deltaTime;

        if (mobTimer >= currentSpawnInterval)
        {
            SpawnBasicMobs();
            mobTimer = 0f;
        }

        if (gameTime >= firstEliteSpawnTime && eliteMobTimer >= eliteMobSpawnInterval)
        {
            SpawnEliteMob();
            eliteMobTimer = 0f;
        }

        if (gameTime >= nextBossSpawnTime)
        {
            SpawnNextBoss();
            nextBossSpawnTime += bossSpawnInterval;
        }

        HandlePeriodicTasks();
    }

    private void SpawnNextBoss()
    {
        if (bossSequence == null || bossSequence.Length == 0)
        {
            Debug.LogWarning("[Spawner] BossSequence가 설정되지 않았습니다!");
            return;
        }

        int safeIndex = nextBossIndex % bossSequence.Length;
        BossName bossToSpawn = bossSequence[safeIndex];

        StartBossSequence(bossToSpawn);

        nextBossIndex = (nextBossIndex + 1) % bossSequence.Length;

        Debug.Log($"[Spawner] 다음 보스 인덱스 예약: {nextBossIndex} ({bossSequence[nextBossIndex]})");
    }

    private void StartBossSequence(BossName bossName)
    {
        StartCoroutine(BossSpawnRoutine(bossName));
    }

    private IEnumerator BossSpawnRoutine(BossName bossName)
    {
        BossSpawnSetting setting = GetBossSetting(bossName);
        Debug.Log($"[Spawner] {GameManager.Instance.gameTime}초: 보스({bossName}) 등장 시퀀스!");

        GameManager.Instance.AppearBoss();

        if (BossWarningLoopUI.Instance != null)
        {
            BossWarningLoopUI.Instance.ShowWarning();
        }

        yield return new WaitForSeconds(setting.spawnDelayAfterWarning);

        SpawnBossObject(bossName);
    }

    // ✨ [수정] arrivalPoint 유무에 따른 분기 처리
    private void SpawnBossObject(BossName boss)
    {
        if (PoolManager.instance != null)
        {
            GameObject bossPrefab = PoolManager.instance.GetBoss(boss);
            BossSpawnSetting setting = GetBossSetting(boss);

            if (bossPrefab != null)
            {
                // 1. 스폰 위치 결정 (SpawnPoint가 없으면 Spawner 위치)
                Vector3 spawnPos = transform.position;
                if (setting.spawnPoint != null)
                {
                    spawnPos = setting.spawnPoint.position;
                }
                else
                {
                    Debug.LogError($"[Spawner] {boss}의 Spawn Point가 BossSettings에 할당되지 않았습니다!");
                }

                // 2. 보스 생성 (SpawnPoint 위치에)
                GameObject bossObj = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

                // 3. 등장 연출 처리
                Boss bossScript = bossObj.GetComponent<Boss>();
                if (bossScript != null)
                {
                    // ✨ arrivalPoint가 존재할 때만 연출 실행
                    if (setting.arrivalPoint != null)
                    {
                        bossScript.StartEntranceRoutine(setting.arrivalPoint.position, setting.entranceDuration);
                    }
                    else
                    {
                        // arrivalPoint가 없으면 아무것도 안 함.
                        // Boss.cs의 OnEnable에서 isEntranceActive = false로 초기화되므로
                        // 즉시 패턴 로직이 작동함 (기존 방식)
                    }
                }
            }
        }
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

    private Vector3 GetSpawnPosition(bool isFly)
    {
        if (isFly)
        {
            if (flyMobSpawnAreas != null && flyMobSpawnAreas.Length > 0)
            {
                int whichArea = UnityEngine.Random.Range(0, flyMobSpawnAreas.Length);
                Bounds bounds = flyMobSpawnAreas[whichArea].bounds;
                return new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x), UnityEngine.Random.Range(bounds.min.y, bounds.max.y), 0f);
            }
        }
        else
        {
            List<Transform> candidates = new List<Transform>();
            if (groundFrontPoints != null) candidates.AddRange(groundFrontPoints);
            if (isRearSpawnEnabled && groundRearPoints != null) candidates.AddRange(groundRearPoints);
            if (candidates.Count > 0) return candidates[UnityEngine.Random.Range(0, candidates.Count)].position;
        }
        return transform.position;
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

    public void SetSpawning(bool enabled) { isSpawningEnabled = enabled; if (!enabled) StopAllCoroutines(); }
    public void SetRearSpawning(bool enabled) { isRearSpawnEnabled = enabled; }
    private void InitEnemyPhysics(GameObject enemy) { Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>(); if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; } }

    private BossSpawnSetting GetBossSetting(BossName name)
    {
        foreach (var s in bossSettings)
            if (s.bossName == name) return s;

        return new BossSpawnSetting { bossName = name, spawnDelayAfterWarning = 3.0f };
    }

    private void RespawnMob(Mob mob) { }
    public void SpawnBoss(BossName boss) { StartBossSequence(boss); }
    public void SpawnTrainBoss() { StartBossSequence(BossName.TrainBoss); }
    public void SpawnEyeBoss() { StartBossSequence(BossName.EyeBoss); }
}