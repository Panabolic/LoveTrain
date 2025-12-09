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
        [Tooltip("이 페이즈가 시작되는 게임 시간 (초)")]
        public float startTime;

        [Header("Basic Mobs Range")]
        public int groundMinIndex;
        public int groundMaxIndex;
        public int flyMinIndex;
        public int flyMaxIndex;

        [Header("Elite Mobs Range")]
        public int groundEliteMinIndex;
        public int groundEliteMaxIndex;
        public int flyEliteMinIndex;
        public int flyEliteMaxIndex;

        [Space]
        [Tooltip("기본 스폰 주기 (난이도 조절용)")]
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
        public GameObject prefab;
        public float interval;
        public bool isFly;
        public float timer;

        public PeriodicSpawnTask(GameObject prefab, float interval, bool isFly)
        {
            this.prefab = prefab;
            this.interval = interval;
            this.isFly = isFly;
            this.timer = 0f;
        }
    }

    [Header("Phase Settings")]
    [SerializeField] private SpawnPhase[] spawnPhases;

    [Header("Boss Settings")]
    // ✨ [추가] 보스 등장 순서 (인스펙터에서 설정)
    [Tooltip("등장할 보스 순서 (예: Train -> Eye -> Train)")]
    [SerializeField] private BossName[] bossSequence;

    [SerializeField] private BossSpawnSetting[] bossSettings;
    [SerializeField] private float bossSpawnInterval = 180.0f;

    [Header("Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f;

    [Header("Spawn Points - Ground")]
    [SerializeField] private Transform[] groundFrontPoints;
    [SerializeField] private Transform[] groundRearPoints;

    [Header("Spawn Points - Fly")]
    [SerializeField] private BoxCollider2D[] flyMobSpawnAreas;

    [Header("Spawn Points - Boss")]
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

    private float mobTimer, eliteMobTimer, bossTimer;
    // ✨ [추가] 다음 보스 인덱스
    private int nextBossIndex = 0;

    private List<PeriodicSpawnTask> periodicSpawnTasks = new List<PeriodicSpawnTask>();

    private void Awake() { if (Instance == null) Instance = this; }

    private void Start()
    {
        mobTimer = 0f;
        eliteMobTimer = eliteMobSpawnInterval;

        bossTimer = 0f;
        nextBossIndex = 0; // 초기화

        periodicSpawnTasks.Clear();
        UpdatePhase(0f);

        isSpawningEnabled = true;
        isRearSpawnEnabled = true;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Boss || GameManager.Instance.CurrentState == GameState.Playing)
        {
            float gameTime = GameManager.Instance.gameTime;
            UpdatePhase(gameTime);

            mobTimer += Time.deltaTime;
            if (gameTime >= firstEliteSpawnTime) eliteMobTimer += Time.deltaTime;

            // 보스 타이머는 Playing 상태일 때만 흐르게 할 수도 있지만, 
            // 보통 시간 기반 게임은 계속 흐르게 둡니다.
            bossTimer += Time.deltaTime;

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

            // ✨ [수정] 보스 스폰 로직 개선
            if (bossTimer >= bossSpawnInterval)
            {
                // 현재 게임 상태가 Playing일 때만 보스 소환 시도 (이미 보스전 중이면 스킵 or 대기)
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    SpawnNextBoss();
                    bossTimer = 0f; // 성공적으로 소환했을 때만 리셋
                }
                // 만약 보스전 중이라 소환을 못했다면? 
                // -> 타이머를 0으로 리셋하지 않고 유지해서, 보스전 끝나자마자 바로 다음 보스 나오게 하려면 
                //    else 블록을 비워두세요. (지금 코드는 Playing이 아니면 타이머가 계속 증가함)
            }

            HandlePeriodicTasks();
        }
    }

    // ✨ [추가] 다음 순서의 보스를 소환하는 함수
    private void SpawnNextBoss()
    {
        if (bossSequence == null || bossSequence.Length == 0)
        {
            Debug.LogWarning("[Spawner] Boss Sequence가 비어있습니다! 기본값(TrainBoss)을 소환합니다.");
            StartBossSequence(BossName.TrainBoss);
            return;
        }

        // 인덱스가 배열 범위를 넘어가면? -> 마지막 보스 반복 (또는 0으로 돌려서 루프 가능)
        // 루프를 원하면: int index = nextBossIndex % bossSequence.Length;
        int index = Mathf.Clamp(nextBossIndex, 0, bossSequence.Length - 1);

        BossName bossToSpawn = bossSequence[index];
        StartBossSequence(bossToSpawn);

        // 다음 보스를 위해 인덱스 증가
        if (nextBossIndex < bossSequence.Length)
        {
            nextBossIndex++;
        }
    }

    // -------------------------------------------------------
    // 위치 계산 및 스폰 로직
    // -------------------------------------------------------
    private Vector3 GetSpawnPosition(bool isFly)
    {
        if (isFly)
        {
            if (flyMobSpawnAreas != null && flyMobSpawnAreas.Length > 0)
            {
                int whichArea = UnityEngine.Random.Range(0, flyMobSpawnAreas.Length);
                Bounds bounds = flyMobSpawnAreas[whichArea].bounds;

                float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
                float randomY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

                return new Vector3(randomX, randomY, 0f);
            }
        }
        else
        {
            List<Transform> candidates = new List<Transform>();
            if (groundFrontPoints != null) candidates.AddRange(groundFrontPoints);
            if (isRearSpawnEnabled && groundRearPoints != null) candidates.AddRange(groundRearPoints);

            if (candidates.Count > 0)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Count)].position;
            }
        }
        return transform.position;
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
        // Debug.Log("Elite Mob Spawned");

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

        if (enemy != null)
        {
            SpawnMobCommon(enemy, isFly);
        }
        else
        {
            Debug.LogWarning("Elite Mob 생성 실패 (PoolManager 확인 필요)");
        }
    }

    private void SpawnMobInternal(int index, bool isFly)
    {
        GameObject enemy = isFly ?
            PoolManager.instance.GetFlyMob(index) :
            PoolManager.instance.GetGroundMob(index);

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
        if (mob != null)
        {
            mob.OnDied -= RespawnMob;
            mob.OnDied += RespawnMob;
        }
    }

    // -------------------------------------------------------
    // 페이즈 및 주기적 작업 관리
    // -------------------------------------------------------
    private void UpdatePhase(float currentTime)
    {
        if (spawnPhases == null || spawnPhases.Length == 0) return;

        for (int i = spawnPhases.Length - 1; i >= 0; i--)
        {
            if (currentTime >= spawnPhases[i].startTime)
            {
                currentGroundMin = spawnPhases[i].groundMinIndex;
                currentGroundMax = spawnPhases[i].groundMaxIndex;
                currentFlyMin = spawnPhases[i].flyMinIndex;
                currentFlyMax = spawnPhases[i].flyMaxIndex;

                currentGroundEliteMin = spawnPhases[i].groundEliteMinIndex;
                currentGroundEliteMax = spawnPhases[i].groundEliteMaxIndex;
                currentFlyEliteMin = spawnPhases[i].flyEliteMinIndex;
                currentFlyEliteMax = spawnPhases[i].flyEliteMaxIndex;

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
            if (task.timer >= task.interval)
            {
                SpawnMobFromPrefab(task.prefab, task.isFly);
                task.timer = 0f;
            }
        }
    }

    public void AddPeriodicSpawnTask(GameObject prefab, float interval, bool isFly)
    {
        if (prefab == null) return;
        if (interval <= 0.1f) interval = 0.1f;
        periodicSpawnTasks.Add(new PeriodicSpawnTask(prefab, interval, isFly));
    }

    // -------------------------------------------------------
    // 보스 및 이벤트 스폰
    // -------------------------------------------------------
    public void StartBossSequence(BossName bossName)
    {
        // 보스전이 이미 진행 중이라면 무시 (혹은 중첩 가능하게 하려면 제거)
        if (GameManager.Instance.CurrentState == GameState.Boss) return;

        StartCoroutine(BossSpawnRoutine(bossName));
    }

    private IEnumerator BossSpawnRoutine(BossName bossName)
    {
        BossSpawnSetting setting = GetBossSetting(bossName);
        Debug.Log($"[Spawner] 보스({bossName}) 시퀀스 시작!");

        GameManager.Instance.AppearBoss();

        yield return new WaitForSeconds(setting.spawnDelayAfterWarning);

        SpawnBossObject(bossName);
    }

    private void SpawnBossObject(BossName boss)
    {
        if (PoolManager.instance != null)
        {
            GameObject bossPrefab = PoolManager.instance.GetBoss(boss);
            if (bossPrefab != null)
            {
                // 인덱스 범위 체크 (안전장치)
                int pointIndex = Mathf.Clamp((int)boss, 0, bossSpawnPoints.Length - 1);
                Instantiate(bossPrefab, bossSpawnPoints[pointIndex].position, Quaternion.identity);
            }
        }
    }

    public void SpawnMobBatch(GameObject prefab, int count, float delay, bool isFly)
    {
        StartCoroutine(SpawnBatchRoutine(prefab, count, delay, isFly));
    }

    private IEnumerator SpawnBatchRoutine(GameObject prefab, int count, float delay, bool isFly)
    {
        Vector3 spawnPos = GetSpawnPosition(isFly);

        for (int i = 0; i < count; i++)
        {
            GameObject enemy = PoolManager.instance.GetMob(prefab);
            if (enemy != null)
            {
                enemy.transform.position = spawnPos;
                InitEnemyPhysics(enemy);
            }
            yield return new WaitForSeconds(delay);
        }
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    public void SetSpawning(bool enabled)
    {
        isSpawningEnabled = enabled;
        if (!enabled) StopAllCoroutines();
    }

    public void SetRearSpawning(bool enabled)
    {
        isRearSpawnEnabled = enabled;
    }

    private void InitEnemyPhysics(GameObject enemy)
    {
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
    }

    private BossSpawnSetting GetBossSetting(BossName name)
    {
        foreach (var s in bossSettings) if (s.bossName == name) return s;
        return new BossSpawnSetting { bossName = name, spawnDelayAfterWarning = 3.0f };
    }

    private void RespawnMob(Mob mob) { }

    public void SpawnBoss(BossName boss) { StartBossSequence(boss); }
    public void SpawnTrainBoss() { StartBossSequence(BossName.TrainBoss); }
    public void SpawnEyeBoss() { StartBossSequence(BossName.EyeBoss); }
}