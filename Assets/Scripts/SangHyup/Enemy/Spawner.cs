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

        [Header("Ground Mobs Range")]
        [Tooltip("등장할 지상 몬스터 시작 인덱스 (포함)")]
        public int groundMinIndex;
        [Tooltip("등장할 지상 몬스터 끝 인덱스 (포함)")]
        public int groundMaxIndex;

        [Header("Fly Mobs Range")]
        [Tooltip("등장할 공중 몬스터 시작 인덱스 (포함)")]
        public int flyMinIndex;
        [Tooltip("등장할 공중 몬스터 끝 인덱스 (포함)")]
        public int flyMaxIndex;

        [Space]
        [Tooltip("기본 스폰 주기 (난이도 조절용)")]
        public float spawnInterval;
    }

    // 주기적 스폰 작업을 관리하는 클래스
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
    [Tooltip("시간대별 몬스터 등장 설정 (시간순 정렬 필수)")]
    [SerializeField] private SpawnPhase[] spawnPhases;

    [Header("Spawn Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float bossSpawnInterval = 180.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] mobSpawnPoints;    // 지상
    [SerializeField] private Transform[] flyMobSpawnPoints; // 공중
    [SerializeField] private Transform[] bossSpawnPoints;

    // --- 내부 변수 (현재 적용 중인 범위) ---
    private int currentGroundMin = 0;
    private int currentGroundMax = 0;
    private int currentFlyMin = 0;
    private int currentFlyMax = 0;
    private float currentSpawnInterval = 1.0f;

    private float mobTimer;
    private float eliteMobTimer;
    private float bossTimer;

    // 현재 활성화된 주기적 스폰 목록 (영구 지속)
    [SerializeField]
    private List<PeriodicSpawnTask> periodicSpawnTasks = new List<PeriodicSpawnTask>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        mobTimer = 0f;
        eliteMobTimer = 19.99f;
        bossTimer = 0f;

        periodicSpawnTasks.Clear();
        UpdatePhase(0f);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        float gameTime = GameManager.Instance.gameTime;

        // 1. 페이즈 업데이트
        UpdatePhase(gameTime);

        // 2. 타이머 갱신
        mobTimer += Time.deltaTime;
        if (gameTime >= firstEliteSpawnTime) eliteMobTimer += Time.deltaTime;
        bossTimer += Time.deltaTime;

        // 3. 기본 스폰 로직
        if (mobTimer >= currentSpawnInterval)
        {
            SpawnBasicMobs();
            mobTimer = 0f;
        }
        if (eliteMobTimer >= eliteMobSpawnInterval)
        {
            SpawnEliteMob();
            eliteMobTimer = 0f;
        }
        if (bossTimer >= bossSpawnInterval)
        {
            SpawnBoss(BossName.TrainBoss);
            bossTimer = 0f;
        }

        // 4. 추가된 주기적 스폰 작업 처리
        HandlePeriodicTasks();
    }

    private void HandlePeriodicTasks()
    {
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

        PeriodicSpawnTask newTask = new PeriodicSpawnTask(prefab, interval, isFly);
        periodicSpawnTasks.Add(newTask);

        Debug.Log($"[Spawner] 주기적 스폰 추가: {prefab.name} (매 {interval}초)");
    }

    // -------------------------------------------------------
    // Spawn Logics
    // -------------------------------------------------------

    private void UpdatePhase(float currentTime)
    {
        if (spawnPhases == null || spawnPhases.Length == 0) return;

        // 현재 시간에 맞는 페이즈 찾기 (역순 탐색)
        for (int i = spawnPhases.Length - 1; i >= 0; i--)
        {
            if (currentTime >= spawnPhases[i].startTime)
            {
                // ✨ 인덱스 범위 갱신
                currentGroundMin = spawnPhases[i].groundMinIndex;
                currentGroundMax = spawnPhases[i].groundMaxIndex;
                currentFlyMin = spawnPhases[i].flyMinIndex;
                currentFlyMax = spawnPhases[i].flyMaxIndex;

                currentSpawnInterval = spawnPhases[i].spawnInterval;
                return;
            }
        }
    }

    private void SpawnBasicMobs()
    {
        // 50% 확률로 지상 또는 공중 스폰
        if (UnityEngine.Random.value < 0.5f)
        {
            // ✨ 지상: Min ~ Max 사이 랜덤 선택 (Max 포함)
            int randomIdx = UnityEngine.Random.Range(currentGroundMin, currentGroundMax + 1);
            SpawnMobInternal(randomIdx, isFly: false);
        }
        else
        {
            // ✨ 공중: Min ~ Max 사이 랜덤 선택 (Max 포함)
            int randomIdx = UnityEngine.Random.Range(currentFlyMin, currentFlyMax + 1);
            SpawnMobInternal(randomIdx, isFly: true);
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

        Transform[] points = isFly ? flyMobSpawnPoints : mobSpawnPoints;
        if (points != null && points.Length > 0)
        {
            enemy.transform.position = points[UnityEngine.Random.Range(0, points.Length)].position;
        }

        InitEnemyPhysics(enemy);

        Mob mob = enemy.GetComponent<Mob>();
        if (mob != null)
        {
            mob.OnDied -= RespawnMob;
            mob.OnDied += RespawnMob;
        }
    }

    private void InitEnemyPhysics(GameObject enemy)
    {
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void SpawnEliteMob()
    {
        // 엘리트 스폰 (랜덤 지상/공중)
        bool isFly = UnityEngine.Random.value > 0.5f;
        int index = UnityEngine.Random.Range(0, 2); // (임시) 실제 배열 크기에 맞춰야 함

        GameObject enemy = isFly ?
            PoolManager.instance.GetFlyEliteMob(index) :
            PoolManager.instance.GetGroundEliteMob(index);

        SpawnMobCommon(enemy, isFly);
    }

    // --- Event: Batch Spawn ---
    public void SpawnMobBatch(GameObject prefab, int count, float delay, bool isFly)
    {
        StartCoroutine(SpawnBatchRoutine(prefab, count, delay, isFly));
    }

    private IEnumerator SpawnBatchRoutine(GameObject prefab, int count, float delay, bool isFly)
    {
        Transform[] points = isFly ? flyMobSpawnPoints : mobSpawnPoints;
        if (points == null || points.Length == 0 || prefab == null) yield break;

        Transform spawnPoint = points[UnityEngine.Random.Range(0, points.Length)];

        for (int i = 0; i < count; i++)
        {
            GameObject enemy = PoolManager.instance.GetMob(prefab);
            if (enemy != null)
            {
                enemy.transform.position = spawnPoint.position;
                InitEnemyPhysics(enemy);
            }
            yield return new WaitForSeconds(delay);
        }
    }

    public void SpawnBoss(BossName boss)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        switch (boss)
        {
            case BossName.TrainBoss:
                GameManager.Instance.AppearBoss();
                Instantiate(PoolManager.instance.GetBoss(boss), bossSpawnPoints[(int)boss]);
                break;
        }
    }
    public void SpawnBoss() { SpawnBoss(BossName.TrainBoss); }
    private void RespawnMob(Mob mob) { }
}