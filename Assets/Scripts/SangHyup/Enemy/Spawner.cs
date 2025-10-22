using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Enemy Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private float timer;

    private void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
    }

    private void Update()
    {
        // ToDo: 각 Enemy별로 스폰 타이머 만들기
        timer += Time.deltaTime;

        // ToDo: 각 Enemy별로 if문 만들기
        // Pooled Enemy가 5마리 이하일 때 1초마다 스폰
        if (timer > 1.0f && PoolManager.instance.GetPooledEnemyCount(EnemyName.Monster) +
                            PoolManager.instance.GetPooledEnemyCount(EnemyName.FlyMonster) <= 5)
        {
            Spawn();

            timer = 0.0f;
        }
    }

    private void Spawn()
    {
        GameObject enemy = PoolManager.instance.GetEnemy(Random.Range(0, 0));

        enemy.transform.position = spawnPoints[Random.Range(1, spawnPoints.Length)].position;

        enemy.GetComponent<Mob>().OnDied -= RespawnEnemy; // 중복 구독 방지
        enemy.GetComponent<Mob>().OnDied += RespawnEnemy;
    }

    private void RespawnEnemy(Mob mob)
    {

    }
}
