using UnityEngine;

public class EventObjectSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("생성할 이벤트 오브젝트 프리팹 (EventTriggerObject 스크립트 포함)")]
    [SerializeField] private GameObject eventObjectPrefab;

    [Tooltip("오브젝트가 생성될 위치들 (화면 밖)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("첫 번째 이벤트 오브젝트 충돌 시 실행할 지정 이벤트. 비워두면 랜덤 이벤트를 사용합니다.")]
    [SerializeField] private SO_Event firstEvent;

    [Tooltip("첫 번째 스폰 시간 (초)")]
    [SerializeField] private float firstSpawnTime = 60f;

    [Tooltip("이후 반복 스폰 주기 (초)")]
    [SerializeField] private float spawnInterval = 60f;

    [Header("강화 이벤트")]
    [SerializeField, Min(1)] private int eventsPerStage = 3;
    [SerializeField] private bool testFirstStageUpgrade = true;
    [SerializeField, Range(0f, 1f)] private float laterStageUpgradeProbability = 1f / 3f;

    private readonly StageEventSchedule schedule = new StageEventSchedule();
    private GameManager gameManager;
    private bool stageTransitionPending;

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null) gameManager.OnGameStateChanged += HandleGameStateChanged;
        schedule.BeginStage(1, gameManager != null ? gameManager.gameTime : 0f);
    }

    private void OnDestroy()
    {
        if (gameManager != null) gameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.StageTransition) stageTransitionPending = true;
        else if (state == GameState.Playing && stageTransitionPending)
        {
            stageTransitionPending = false;
            schedule.AdvanceStage(firstSpawnTime, spawnInterval, eventsPerStage);
        }
    }

    private void Update()
    {
        if (gameManager == null || Time.timeScale <= 0f) return;
        // The third ordinary encounter and boss are both due at 180 seconds. Allow
        // that due encounter to spawn even if the boss changed state this frame.
        if (gameManager.CurrentState != GameState.Playing && gameManager.CurrentState != GameState.Boss) return;
        if (!schedule.IsDue(gameManager.gameTime, firstSpawnTime, spawnInterval, eventsPerStage)) return;
        if (SpawnEventObject(schedule.SpawnedEvents + 1)) schedule.RecordSpawn();
    }

    private bool SpawnEventObject(int encounterNumber)
    {
        if (eventObjectPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return false;

        int randIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randIndex];
        if (spawnPoint == null) return false;

        GameObject spawnedObject = Instantiate(eventObjectPrefab, spawnPoint.position, Quaternion.identity);
        EventTriggerObject triggerObject = spawnedObject.GetComponent<EventTriggerObject>();
        if (triggerObject == null) triggerObject = spawnedObject.GetComponentInChildren<EventTriggerObject>();
        if (triggerObject == null)
        {
            Debug.LogWarning("[EventObjectSpawner] EventTriggerObject is missing from the event prefab.");
            Destroy(spawnedObject);
            return false;
        }

        bool upgrade = StageEventSchedule.IsUpgradeEncounter(schedule.StageNumber, encounterNumber,
            testFirstStageUpgrade, Random.value, laterStageUpgradeProbability);
        triggerObject.SetUpgradeEvent(upgrade);
        if (upgrade) spawnedObject.name = "UpgradeEvent";
        else if (schedule.StageNumber == 1 && encounterNumber == 1 && firstEvent != null)
        {
            triggerObject.SetEventOverride(firstEvent);
        }
        return true;
    }
}

// The encounter counter is separate from background indexes, which loop when
// the last authored stage is reached. Bosses remain owned by Spawner.
public sealed class StageEventSchedule
{
    public int StageNumber { get; private set; } = 1;
    public int SpawnedEvents { get; private set; }
    private float stageStartTime;

    public void BeginStage(int stageNumber, float gameTime)
    {
        StageNumber = System.Math.Max(1, stageNumber);
        SpawnedEvents = 0;
        stageStartTime = gameTime;
    }

    public bool IsDue(float gameTime, float firstSpawnTime, float interval, int eventLimit)
    {
        if (SpawnedEvents >= System.Math.Max(1, eventLimit)) return false;
        float dueTime = System.Math.Max(1f, firstSpawnTime) + SpawnedEvents * System.Math.Max(1f, interval);
        return gameTime - stageStartTime >= dueTime;
    }

    public void AdvanceStage(float firstSpawnTime, float interval, int eventLimit)
    {
        // Bosses use fixed run-time boundaries. Sampling the slightly overshot
        // clock after a boss would accumulate drift and lose the next third event.
        stageStartTime += System.Math.Max(1f, firstSpawnTime) +
            (System.Math.Max(1, eventLimit) - 1) * System.Math.Max(1f, interval);
        StageNumber++;
        SpawnedEvents = 0;
    }

    public void RecordSpawn() { SpawnedEvents++; }

    public static bool IsUpgradeEncounter(int stageNumber, int encounterNumber, bool testFirstStage,
        float randomValue, float laterStageProbability)
    {
        if (stageNumber <= 1) return testFirstStage && encounterNumber == 2;
        if (stageNumber == 2) return encounterNumber == 1;
        return randomValue < System.Math.Max(0f, System.Math.Min(1f, laterStageProbability));
    }
}
