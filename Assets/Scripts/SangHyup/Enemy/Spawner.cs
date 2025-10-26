using UnityEngine;
using System; // Action 이벤트를 위해 필요

public class Spawner : MonoBehaviour
{
    [Header("Enemy Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private float timer;

    private bool isSpawning = false; // GameManager가 제어

    private void Start()
    {
        // GameManager의 상태 변경 이벤트를 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
            // 현재 상태로 초기화
            HandleGameStateChange(GameManager.Instance.CurrentState);
        }
    }

    private void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
    }

    private void Update()
    {
        // 'Playing' 상태가 아니면 아무것도 하지 않음
        if (!isSpawning) return;

        // ToDo: 각 Enemy별로 스폰 타이머 만들기
        timer += Time.deltaTime;

        // ToDo: 각 Enemy별로 if문 만들기
        // Pooled Enemy가 5마리 이하일 때 1초마다 스폰
        if (timer > 1.0f)
        {
            Spawn();

            timer = 0.0f;
        }
    }

    /// <summary>
    /// GameManager의 상태 변경에 따라 스폰 시작/정지를 결정합니다.
    /// </summary>
    private void HandleGameStateChange(GameState newState)
    {
        isSpawning = (newState == GameState.Playing);
    }

    private void Spawn()
    {
        GameObject enemy = PoolManager.instance.GetEnemy(UnityEngine.Random.Range(0, 2));

        enemy.transform.position = spawnPoints[UnityEngine.Random.Range(1, spawnPoints.Length)].position;

        enemy.GetComponent<Mob>().OnDied -= RespawnEnemy; // 중복 구독 방지
        enemy.GetComponent<Mob>().OnDied += RespawnEnemy;
    }

    private void RespawnEnemy(Mob mob)
    {

    }
}
