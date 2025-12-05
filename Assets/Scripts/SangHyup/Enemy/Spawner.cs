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

        [Header("Elite Mobs Range")] // ✨ [추가] 엘리트 몬스터도 페이즈별로 인덱스 관리
        public int groundEliteMinIndex;
        public int groundEliteMaxIndex;
        public int flyEliteMinIndex;
        public int flyEliteMaxIndex;

        [Space]
        [Tooltip("기본 스폰 주기 (난이도 조절용)")]
        public float spawnInterval;
    }

    // ... (BossSpawnSetting, PeriodicSpawnTask 등 기존 클래스/구조체 유지) ...
    [System.Serializable]
    public class BossSpawnSetting
    {
        public BossName bossName;
        public GameObject bossPrefab;
        public float spawnDelayAfterWarning = 4.0f;
        public string spawnSoundName;
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
    [SerializeField] private BossSpawnSetting[] bossSettings;
    [SerializeField] private float bossSpawnInterval = 180.0f;

    [Header("Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f;

    [Header("Spawn Points - Ground")]
    [SerializeField] private Transform[] groundFrontPoints;
    [SerializeField] private Transform[] groundRearPoints;

    [Header("Spawn Points - Fly")]
    // ✨ [핵심 수정] 공중 몬스터는 BoxCollider2D 영역을 사용
    [SerializeField] private BoxCollider2D[] flyMobSpawnAreas;

    [Header("Spawn Points - Boss")]
    [SerializeField] private Transform[] bossSpawnPoints;

    // --- 제어 플래그 ---
    private bool isSpawningEnabled = true;
    private bool isRearSpawnEnabled = true;

    // --- 내부 변수 (현재 페이즈 정보) ---
    // 일반 몹
    private int currentGroundMin, currentGroundMax;
    private int currentFlyMin, currentFlyMax;

    // 엘리트 몹 (추가됨)
    private int currentGroundEliteMin, currentGroundEliteMax;
    private int currentFlyEliteMin, currentFlyEliteMax;

    private float currentSpawnInterval = 1.0f;

    private float mobTimer, eliteMobTimer, bossTimer;
    private List<PeriodicSpawnTask> periodicSpawnTasks = new List<PeriodicSpawnTask>();

    private void Awake() { if (Instance == null) Instance = this; }

    private void Start()
    {
        mobTimer = 0f; eliteMobTimer = 19.99f; bossTimer = 0f;
        periodicSpawnTasks.Clear();
        UpdatePhase(0f);

        isSpawningEnabled = true;
        isRearSpawnEnabled = true;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || !isSpawningEnabled) return;

        float gameTime = GameManager.Instance.gameTime;
        UpdatePhase(gameTime);

        mobTimer += Time.deltaTime;
        if (gameTime >= firstEliteSpawnTime) eliteMobTimer += Time.deltaTime;
        bossTimer += Time.deltaTime;

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
            StartBossSequence(BossName.TrainBoss);
            bossTimer = 0f;
        }

        HandlePeriodicTasks();
    }

    // -------------------------------------------------------
    // ✨ [핵심 로직 수정] 위치 계산 (Transform vs Bounds 통합)
    // -------------------------------------------------------
    private Vector3 GetSpawnPosition(bool isFly)
    {
        if (isFly)
        {
            // ✨ [공중] BoxCollider2D 영역 내 랜덤 좌표 계산
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
            // ✨ [지상] Transform 배열 중 랜덤 선택 (후방 스폰 제어 포함)
            List<Transform> candidates = new List<Transform>();
            if (groundFrontPoints != null) candidates.AddRange(groundFrontPoints);
            if (isRearSpawnEnabled && groundRearPoints != null) candidates.AddRange(groundRearPoints);

            if (candidates.Count > 0)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Count)].position;
            }
        }

        // 실패 시 기본값 (0,0,0) 반환하지 않도록 에러 처리 필요하지만, 임시로 현재 위치 반환
        return transform.position;
    }

    // -------------------------------------------------------
    // 스폰 실행 로직
    // -------------------------------------------------------

    private void SpawnBasicMobs()
    {
        // 50% 확률로 지상/공중 분기
        if (UnityEngine.Random.value < 0.5f)
        {
            // 지상
            int idx = UnityEngine.Random.Range(currentGroundMin, currentGroundMax + 1);
            SpawnMobInternal(idx, isFly: false);
        }
        else
        {
            // 공중
            int idx = UnityEngine.Random.Range(currentFlyMin, currentFlyMax + 1);
            SpawnMobInternal(idx, isFly: true);
        }
    }

    // ✨ [복구 완료] 엘리트 몬스터 스폰 로직
    private void SpawnEliteMob()
    {
        Debug.Log("Elite Mob Spawned");

        // 1. 지상/공중 랜덤 선택
        bool isFly = UnityEngine.Random.value > 0.5f;
        GameObject enemy = null;

        // 2. 페이즈에 맞는 인덱스 범위 내에서 랜덤 선택
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

        // 3. 공통 배치 로직 실행
        if (enemy != null)
        {
            SpawnMobCommon(enemy, isFly);
        }
        else
        {
            Debug.LogWarning("Elite Mob 생성 실패 (PoolManager 확인 필요)");
        }
    }

    // 인덱스로 스폰
    private void SpawnMobInternal(int index, bool isFly)
    {
        GameObject enemy = isFly ?
            PoolManager.instance.GetFlyMob(index) :
            PoolManager.instance.GetGroundMob(index);

        SpawnMobCommon(enemy, isFly);
    }

    // 프리팹으로 스폰 (이벤트용)
    private void SpawnMobFromPrefab(GameObject prefab, bool isFly)
    {
        GameObject enemy = PoolManager.instance.GetMob(prefab);
        SpawnMobCommon(enemy, isFly);
    }

    // ✨ [공통 배치 함수] 위치 설정 및 물리 초기화
    private void SpawnMobCommon(GameObject enemy, bool isFly)
    {
        if (enemy == null) return;

        // 수정된 GetSpawnPosition 함수를 사용하여 위치 결정
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
                // 일반 몬스터 범위
                currentGroundMin = spawnPhases[i].groundMinIndex;
                currentGroundMax = spawnPhases[i].groundMaxIndex;
                currentFlyMin = spawnPhases[i].flyMinIndex;
                currentFlyMax = spawnPhases[i].flyMaxIndex;

                // ✨ [추가] 엘리트 몬스터 범위 업데이트
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
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        StartCoroutine(BossSpawnRoutine(bossName));
    }

    private IEnumerator BossSpawnRoutine(BossName bossName)
    {
        BossSpawnSetting setting = GetBossSetting(bossName);
        Debug.Log($"[Spawner] 보스({bossName}) 시퀀스 시작!");

        GameManager.Instance.AppearBoss();

        if (!string.IsNullOrEmpty(setting.spawnSoundName))
            StartCoroutine(PlaySoundDelayed(setting.spawnSoundName, setting.soundPlayDelay));

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
                Instantiate(bossPrefab, bossSpawnPoints[(int)boss].position, Quaternion.identity);
            }
        }
    }

    // 배치 스폰 (Event)
    public void SpawnMobBatch(GameObject prefab, int count, float delay, bool isFly)
    {
        StartCoroutine(SpawnBatchRoutine(prefab, count, delay, isFly));
    }

    private IEnumerator SpawnBatchRoutine(GameObject prefab, int count, float delay, bool isFly)
    {
        // 배치 스폰의 경우, 한 지점에서 쏟아져 나오게 할지, 랜덤하게 퍼질지 결정해야 함.
        // 요청: "랜덤한 하나의 스폰포인트에서만 생성"

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

    private IEnumerator PlaySoundDelayed(string soundName, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (SoundManager.instance != null) SoundManager.instance.PlaySound("BGM", soundName);
    }

    private BossSpawnSetting GetBossSetting(BossName name)
    {
        foreach (var s in bossSettings) if (s.bossName == name) return s;
        return new BossSpawnSetting { bossName = name, spawnDelayAfterWarning = 3.0f };
    }

    private void RespawnMob(Mob mob) { }

    // (하위 호환)
    public void SpawnBoss(BossName boss) { StartBossSequence(boss); }
    public void SpawnTrainBoss() { StartBossSequence(BossName.TrainBoss); }
    public void SpawnEyeBoss() { StartBossSequence(BossName.EyeBoss); }
}