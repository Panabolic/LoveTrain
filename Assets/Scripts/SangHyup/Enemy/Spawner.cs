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
        // ToDo: �� Enemy���� ���� Ÿ�̸� �����
        timer += Time.deltaTime;

        // ToDo: �� Enemy���� if�� �����
        // Pooled Enemy�� 5���� ������ �� 1�ʸ��� ����
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

        enemy.GetComponent<Mob>().OnDied -= RespawnEnemy; // �ߺ� ���� ����
        enemy.GetComponent<Mob>().OnDied += RespawnEnemy;
    }

    private void RespawnEnemy(Mob mob)
    {

    }
}
