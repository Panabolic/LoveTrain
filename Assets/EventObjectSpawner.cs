using UnityEngine;

public class EventObjectSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("생성할 이벤트 오브젝트 프리팹 (EventTriggerObject 스크립트 포함)")]
    [SerializeField] private GameObject eventObjectPrefab;

    [Tooltip("오브젝트가 생성될 위치들 (화면 밖)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("첫 번째 스폰 시간 (초)")]
    [SerializeField] private float firstSpawnTime = 10f; // ✨ 추가됨

    [Tooltip("이후 반복 스폰 주기 (초)")]
    [SerializeField] private float spawnInterval = 30f;

    // 내부 변수
    private float spawnTimer = 0f;
    private bool isFirstSpawn = true; // ✨ 첫 스폰인지 확인하는 플래그

    private void Start()
    {
        spawnTimer = 0f;
        isFirstSpawn = true;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // GameManager의 상태가 'Playing'일 때만 타이머 진행
        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            spawnTimer += Time.deltaTime;

            // ✨ 현재 목표 시간 결정 (첫 스폰이면 firstSpawnTime, 아니면 spawnInterval)
            float currentTargetTime = isFirstSpawn ? firstSpawnTime : spawnInterval;

            if (spawnTimer >= currentTargetTime)
            {
                SpawnEventObject();

                spawnTimer = 0f;      // 타이머 초기화
                isFirstSpawn = false; // ✨ 첫 스폰 끝났음을 표시 (다음부터는 spawnInterval 사용)
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