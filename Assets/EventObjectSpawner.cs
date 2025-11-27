using System.Collections;
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

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // 주기만큼 대기
            yield return new WaitForSeconds(spawnInterval);

            // 게임이 플레이 중일 때만 스폰 (이벤트 중이거나 보스전 등 제외)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                SpawnEventObject();
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