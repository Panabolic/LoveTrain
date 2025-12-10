using UnityEngine;

public class EventObjectSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("생성할 이벤트 오브젝트 프리팹 (EventTriggerObject 스크립트 포함)")]
    [SerializeField] private GameObject eventObjectPrefab;

    [Tooltip("오브젝트가 생성될 위치들 (화면 밖)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("스폰 주기 (초)")]
    [SerializeField] private float spawnInterval = 30f;

    // 내부 타이머 변수
    private float spawnTimer = 0f;

    private void Start()
    {
        spawnTimer = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // ✨ GameManager의 상태가 'Playing'일 때만 타이머 진행
        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnInterval)
            {
                SpawnEventObject();
                spawnTimer = 0f; // 타이머 초기화
            }
        }
    }

    private void SpawnEventObject()
    {
        if (eventObjectPrefab == null || spawnPoints.Length == 0) return;

        // 랜덤한 스폰 위치 선택
        int randIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randIndex];

        // 오브젝트 생성
        Instantiate(eventObjectPrefab, spawnPoint.position, Quaternion.identity);
    }
}