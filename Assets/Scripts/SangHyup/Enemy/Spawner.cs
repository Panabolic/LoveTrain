using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance;

    // ✨ [수정] struct -> class로 변경 (기본값 할당을 위해)
    [System.Serializable]
    public class BossSpawnSetting
    {
        public BossName bossName;
        [Tooltip("보스 프리팹 (PoolManager에 있다면 거기서 가져오지만, 예외적으로 직접 할당 가능)")]
        public GameObject bossPrefab;

        [Header("Timing")]
        [Tooltip("경고 UI 시작 후, 실제로 보스가 나올 때까지의 대기 시간")]
        public float spawnDelayAfterWarning = 4.0f; // class여야 기본값 할당 가능

        [Header("Sound")]
        [Tooltip("보스 등장 사운드 이름 (SoundManager Key)")]
        public string spawnSoundName;
        [Tooltip("사운드를 언제 재생할지 (0 = 경고 시작 시, 3 = 3초 뒤)")]
        public float soundPlayDelay = 0.5f;
    }

    [System.Serializable]
    public struct SpawnPhase
    {
        [Tooltip("이 페이즈가 시작되는 게임 시간 (초)")]
        public float startTime;

        [Header("Ground Mobs Range")]
        public int groundMinIndex;
        public int groundMaxIndex;

        [Header("Fly Mobs Range")]
        public int flyMinIndex;
        public int flyMaxIndex;

        [Space]
        public float spawnInterval;
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
    [SerializeField] private BossSpawnSetting[] bossSettings; // 보스 설정 배열
    [SerializeField] private float bossSpawnInterval = 180.0f;

    [Header("Interval Settings")]
    [SerializeField] private float eliteMobSpawnInterval = 20.0f;
    [SerializeField] private float firstEliteSpawnTime = 60.0f;


    [Header("Spawn Points")]
    [SerializeField] private Transform[]        mobSpawnPoints;
    [SerializeField] private BoxCollider2D[]    flyMobSpawnAreas;
    [SerializeField] private Transform[]        bossSpawnPoints;

    [Header("Spawn Points - Ground")]
    [SerializeField] private Transform[] groundFrontPoints;
    [SerializeField] private Transform[] groundRearPoints;

    [Header("Spawn Points - Fly")]
    [SerializeField] private Transform[] flyFrontPoints;
    [SerializeField] private Transform[] flyRearPoints;

    [Header("Spawn Points - Boss")]
    [SerializeField] private Transform[] bossSpawnPoints;

    // --- 제어 플래그 ---
    private bool isSpawningEnabled = true;
    private bool isRearSpawnEnabled = true;

    // --- 내부 변수 ---
    private int currentGroundMin, currentGroundMax;
    private int currentFlyMin, currentFlyMax;
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
    // 보스 시퀀스 (경고 -> 대기 -> 스폰)
    // -------------------------------------------------------
    public void StartBossSequence(BossName bossName)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        StartCoroutine(BossSpawnRoutine(bossName));
    }

    private IEnumerator BossSpawnRoutine(BossName bossName)
    {
        // 1. 설정 가져오기
        BossSpawnSetting setting = GetBossSetting(bossName);

        Debug.Log($"[Spawner] 보스({bossName}) 시퀀스 시작! 경고 UI 출력.");

        // 2. 게임 상태 변경 (경고 UI 자동 출력)
        GameManager.Instance.AppearBoss();

        // 3. 사운드 재생 예약
        if (!string.IsNullOrEmpty(setting.spawnSoundName))
        {
            StartCoroutine(PlaySoundDelayed(setting.spawnSoundName, setting.soundPlayDelay));
        }

        // 4. 대기 (경고 연출 시간)
        yield return new WaitForSeconds(setting.spawnDelayAfterWarning);

        // 5. 스폰
        Debug.Log($"[Spawner] 보스({bossName}) 출현!");
        SpawnBossObject(bossName);
    }

    private IEnumerator PlaySoundDelayed(string soundName, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (SoundManager.instance != null)
        {
            // BGM 혹은 SFX 중 맞는 타입으로 호출
            SoundManager.instance.PlaySound("BGM", soundName);
        }
    }

    private void SpawnBossObject(BossName boss)
    {
        if (PoolManager.instance != null)
        {
            GameObject bossPrefab = PoolManager.instance.GetBoss(boss);
            if (bossPrefab != null)
            {
                // 보스 스폰 위치에서 생성
                Instantiate(bossPrefab, bossSpawnPoints[(int)boss].position, Quaternion.identity);
            }
        }
    }

    private BossSpawnSetting GetBossSetting(BossName name)
    {
        foreach (var s in bossSettings)
        {
            if (s.bossName == name) return s;
        }
        // 설정이 없으면 기본값으로 임시 객체 생성하여 반환
        return new BossSpawnSetting { bossName = name, spawnDelayAfterWarning = 3.0f };
    }

    // -------------------------------------------------------
    // 외부 제어
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

    // -------------------------------------------------------
    // 일반 스폰 로직
    // -------------------------------------------------------
    private Transform GetRandomSpawnPoint(bool isFly)
    {
        List<Transform> candidates = new List<Transform>();

        if (isFly)
        {
            if (flyFrontPoints != null) candidates.AddRange(flyFrontPoints);
            if (isRearSpawnEnabled && flyRearPoints != null) candidates.AddRange(flyRearPoints);
        }
        else
        {
            if (groundFrontPoints != null) candidates.AddRange(groundFrontPoints);
            if (isRearSpawnEnabled && groundRearPoints != null) candidates.AddRange(groundRearPoints);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
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
            int     whichArea   = UnityEngine.Random.Range(0, flyMobSpawnAreas.Length);
            Bounds  bounds      = flyMobSpawnAreas[whichArea].bounds;

            float randomY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            enemy.transform.position = new Vector3(30.0f, randomY, 0f);
//
            int idx = UnityEngine.Random.Range(currentFlyMin, currentFlyMax + 1);
            SpawnMobInternal(idx, isFly: true);
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

        Transform targetPoint = GetRandomSpawnPoint(isFly);
        if (targetPoint != null)
        {
            enemy.transform.position = targetPoint.position;
            InitEnemyPhysics(enemy);
        }

        Mob mob = enemy.GetComponent<Mob>();
        if (mob != null) { mob.OnDied -= RespawnMob; mob.OnDied += RespawnMob; }
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

    private void SpawnEliteMob()
    {
        mobIndex = UnityEngine.Random.Range(0, 2);

        GameObject enemy = PoolManager.instance.GetEliteMob(mobIndex);
        if (enemy == null)
        {
            Debug.Log("Enemy가 생성되지 않았습니다.");
            return;
        }

        if (mobIndex == 0)
        {
            enemy.transform.position = mobSpawnPoints[UnityEngine.Random.Range(0, mobSpawnPoints.Length)].position;
        }
        else if (mobIndex == 1)
        {
            int whichArea = UnityEngine.Random.Range(0, flyMobSpawnAreas.Length);
            Bounds bounds = flyMobSpawnAreas[whichArea].bounds;

            float randomY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            enemy.transform.position = new Vector3(30.0f, randomY, 0f);
        }

        enemy.GetComponent<Mob>().OnDied -= RespawnMob;
        enemy.GetComponent<Mob>().OnDied += RespawnMob;

        bool isFly = UnityEngine.Random.value > 0.5f;
        int index = UnityEngine.Random.Range(0, 2);
        GameObject enemy = isFly ? PoolManager.instance.GetFlyEliteMob(index) : PoolManager.instance.GetGroundEliteMob(index);
        SpawnMobCommon(enemy, isFly);
    }

    public void SpawnMobBatch(GameObject prefab, int count, float delay, bool isFly)
    {
        StartCoroutine(SpawnBatchRoutine(prefab, count, delay, isFly));
    }

    private IEnumerator SpawnBatchRoutine(GameObject prefab, int count, float delay, bool isFly)
    {
        Transform spawnPoint = GetRandomSpawnPoint(isFly);
        if (spawnPoint == null) yield break;

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
                currentSpawnInterval = spawnPhases[i].spawnInterval;
                return;
            }
        }
    }

    private void InitEnemyPhysics(GameObject enemy)
    {
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
    }

    // (구버전 호환용: 더 이상 Update에서 직접 호출하지 않고 StartBossSequence 사용)
    public void SpawnBoss(BossName boss) { StartBossSequence(boss); }
    public void SpawnBoss() { StartBossSequence(BossName.TrainBoss); }
    private void RespawnMob(Mob mob) { }
}