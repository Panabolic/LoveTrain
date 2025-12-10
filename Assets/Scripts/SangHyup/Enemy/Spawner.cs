using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance;

    // ... (SpawnPhase, BossSpawnSetting, PeriodicSpawnTask 구조체들은 그대로 유지) ...
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
        public GameObject bossPrefab;
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

    [SerializeField] private BossSpawnSetting[] bossSettings;
    [SerializeField] private float bossSpawnInterval = 180.0f; // 3분

    [Header("Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f; // 50초

    [Header("Spawn Points")]
    [SerializeField] private Transform[] groundFrontPoints;
    [SerializeField] private Transform[] groundRearPoints;
    [SerializeField] private BoxCollider2D[] flyMobSpawnAreas;
    [SerializeField] private Transform[] bossSpawnPoints;

    // --- 제어 플래그 ---
    private bool isSpawningEnabled = true;
    private bool isRearSpawnEnabled = true;

    // --- 내부 변수 ---
    private int currentGroundMin, currentGroundMax;
    private int currentFlyMin, currentFlyMax;
    private int currentGroundEliteMin, currentGroundEliteMax;
    private int currentFlyEliteMin, currentFlyEliteMax;
    private float currentSpawnInterval = 1.0f;

    // ✨ [수정] bossTimer 제거 -> nextBossSpawnTime 추가
    private float mobTimer;
    private float eliteMobTimer;
    private float nextBossSpawnTime;

    private int nextBossIndex = 0;

    private List<PeriodicSpawnTask> periodicSpawnTasks = new List<PeriodicSpawnTask>();

    private void Awake() { if (Instance == null) Instance = this; }

    private void Start()
    {
        mobTimer = 0f;
        eliteMobTimer = eliteMobSpawnInterval; // 50초 땡 하면 바로 나오게 장전

        // ✨ [초기화] 첫 보스는 3분(180초)에 등장
        nextBossSpawnTime = bossSpawnInterval;
        nextBossIndex = 0;

        periodicSpawnTasks.Clear();
        UpdatePhase(0f);

        isSpawningEnabled = true;
        isRearSpawnEnabled = true;
    }

    private void Update()
    {
        // 1. 엔딩 상태 등에서는 완전 정지
        // (보스전 중에는 쫄몹 스폰을 위해 작동해야 함)
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss) return;

        float gameTime = GameManager.Instance.gameTime;
        UpdatePhase(gameTime);

        // --- 쫄몹 스폰 타이머 (보스전에도 계속 흐름: Time.deltaTime 사용) ---
        mobTimer += Time.deltaTime;

        // 50초 이후부터 엘리트 타이머 흐름
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

        // --- 보스 스폰 체크 (GameManager 시간 사용) ---
        // ✨ [핵심 수정] 보스전엔 gameTime이 멈추므로, 이 조건은 절대 충족될 수 없음 -> 예외처리 불필요!
        if (gameTime >= nextBossSpawnTime)
        {
            SpawnNextBoss();

            // 다음 보스 시간 설정 (현재 180 -> 360 -> 540 ...)
            nextBossSpawnTime += bossSpawnInterval;
        }

        HandlePeriodicTasks();
    }

    // -------------------------------------------------------
    // 스폰 로직들 (기존과 동일)
    // -------------------------------------------------------
    private void SpawnNextBoss()
    {
        if (bossSequence == null || bossSequence.Length == 0)
        {
            StartBossSequence(BossName.TrainBoss);
            return;
        }

        int index = Mathf.Clamp(nextBossIndex, 0, bossSequence.Length - 1);
        BossName bossToSpawn = bossSequence[index];
        StartBossSequence(bossToSpawn);

        if (nextBossIndex < bossSequence.Length) nextBossIndex++;
    }

    private void StartBossSequence(BossName bossName)
    {
        StartCoroutine(BossSpawnRoutine(bossName));
    }

    private IEnumerator BossSpawnRoutine(BossName bossName)
    {
        BossSpawnSetting setting = GetBossSetting(bossName);
        Debug.Log($"[Spawner] {GameManager.Instance.gameTime}초: 보스({bossName}) 등장 시퀀스!");

        GameManager.Instance.AppearBoss(); // 여기서 시간 정지됨

        yield return new WaitForSeconds(setting.spawnDelayAfterWarning);

        SpawnBossObject(bossName);
    }

    // ... (SpawnBasicMobs, SpawnEliteMob, GetSpawnPosition 등 나머지 함수들은 기존 코드 그대로 유지) ...
    // (복붙 편의를 위해 아래에 생략된 함수들도 필요하면 전체 코드를 다시 드릴 수 있습니다.)

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

    private void SpawnBossObject(BossName boss)
    {
        if (PoolManager.instance != null)
        {
            GameObject bossPrefab = PoolManager.instance.GetBoss(boss);
            if (bossPrefab != null)
            {
                int pointIndex = Mathf.Clamp((int)boss, 0, bossSpawnPoints.Length - 1);
                Instantiate(bossPrefab, bossSpawnPoints[pointIndex].position, Quaternion.identity);
            }
        }
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
    private BossSpawnSetting GetBossSetting(BossName name) { foreach (var s in bossSettings) if (s.bossName == name) return s; return new BossSpawnSetting { bossName = name, spawnDelayAfterWarning = 3.0f }; }
    private void RespawnMob(Mob mob) { }
    public void SpawnBoss(BossName boss) { StartBossSequence(boss); }
    public void SpawnTrainBoss() { StartBossSequence(BossName.TrainBoss); }
    public void SpawnEyeBoss() { StartBossSequence(BossName.EyeBoss); }
}