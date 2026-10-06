using UnityEngine;

public class EventObjectSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("생성할 이벤트 오브젝트 프리팹 (EventTriggerObject 스크립트 포함)")]
    [SerializeField] private GameObject eventObjectPrefab;

    [Tooltip("오브젝트가 생성될 위치들 (화면 밖)")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private ForwardCameraFollow cameraFollow;

    [Tooltip("첫 번째 이벤트 오브젝트 충돌 시 실행할 지정 이벤트. 비워두면 랜덤 이벤트를 사용합니다.")]
    [SerializeField] private SO_Event firstEvent;

    // Keep legacy serialized timing values for existing scenes; route distances
    // now determine all three encounters instead of these former time settings.
    [HideInInspector]
    [SerializeField] private float firstSpawnTime = 60f;

    [HideInInspector]
    [SerializeField] private float spawnInterval = 60f;

    [Header("강화 이벤트")]
    [HideInInspector, SerializeField] private int eventsPerStage = 3;
    [SerializeField] private bool testFirstStageUpgrade = true;
    [SerializeField, Range(0f, 1f)] private float laterStageUpgradeProbability = 1f / 3f;

    private readonly StageEventSchedule schedule = new StageEventSchedule();
    private GameManager gameManager;
    private StageManager stageManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        stageManager = StageManager.Instance;
        if (stageManager != null)
        {
            stageManager.OnProgressAdvanced += HandleStageProgress;
            stageManager.OnStageStarted += HandleStageStarted;
        }
        schedule.BeginStage(stageManager != null ? stageManager.StageNumber : 1);
    }

    private void OnDestroy()
    {
        if (stageManager != null)
        {
            stageManager.OnProgressAdvanced -= HandleStageProgress;
            stageManager.OnStageStarted -= HandleStageStarted;
        }
    }

    private void HandleStageStarted(int stageNumber)
    {
        schedule.BeginStage(stageNumber);
    }

    private void HandleStageProgress()
    {
        if (!isActiveAndEnabled || gameManager == null || stageManager == null || Time.timeScale <= 0f) return;
        // Another distance consumer can start a boss earlier in this callback
        // list. Already crossed encounter boundaries still belong to this frame.
        if (gameManager.CurrentState != GameState.Playing && gameManager.CurrentState != GameState.Boss) return;
        if (schedule.StageNumber != stageManager.StageNumber) schedule.BeginStage(stageManager.StageNumber);
        while (schedule.IsDue(stageManager.StageDistance, stageManager.StageLength))
        {
            if (!SpawnEventObject(schedule.SpawnedEvents + 1)) break;
            schedule.RecordSpawn();
        }
    }

    private bool SpawnEventObject(int encounterNumber)
    {
        if (eventObjectPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return false;

        int randIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randIndex];
        if (spawnPoint == null) return false;

        Vector3 spawnPosition = spawnPoint.position;
        if (cameraFollow != null && !spawnPoint.IsChildOf(cameraFollow.transform))
            spawnPosition.x += cameraFollow.CurrentOffsetX;
        GameObject spawnedObject = Instantiate(eventObjectPrefab, spawnPosition, Quaternion.identity);
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
    public const int EncountersPerStage = 3;
    public int StageNumber { get; private set; } = 1;
    public int SpawnedEvents { get; private set; }

    public void BeginStage(int stageNumber)
    {
        StageNumber = System.Math.Max(1, stageNumber);
        SpawnedEvents = 0;
    }

    public bool IsDue(float stageDistance, float stageLength)
    {
        if (SpawnedEvents >= EncountersPerStage || stageLength <= 0f) return false;
        return stageDistance >= stageLength * ((SpawnedEvents + 1) * 0.25f);
    }

    public void RecordSpawn() { SpawnedEvents = System.Math.Min(EncountersPerStage, SpawnedEvents + 1); }

    public static bool IsUpgradeEncounter(int stageNumber, int encounterNumber, bool testFirstStage,
        float randomValue, float laterStageProbability)
    {
        if (stageNumber <= 1) return testFirstStage && encounterNumber == 2;
        if (stageNumber == 2) return encounterNumber == 1;
        return randomValue < System.Math.Max(0f, System.Math.Min(1f, laterStageProbability));
    }
}
