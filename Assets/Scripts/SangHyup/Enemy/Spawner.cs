using UnityEngine;
using System; // Action �̺�Ʈ�� ���� �ʿ�


public class Spawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] mobSpawnPoints;
    [SerializeField] private Transform[] bossSpawnPoints;


    private float timer;

    private bool isSpawning = false; // GameManager�� ����

    private void Start()
    {
        // GameManager�� ���� ���� �̺�Ʈ�� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
            // ���� ���·� �ʱ�ȭ
            HandleGameStateChange(GameManager.Instance.CurrentState);
        }
    }

    private void Update()
    {
        // 'Playing' ���°� �ƴϸ� �ƹ��͵� ���� ����
        if (!isSpawning) return;

        // ToDo: �� Enemy���� ���� Ÿ�̸� �����
        timer += Time.deltaTime;

        // ToDo: �� Enemy���� if�� �����
        // Pooled Enemy�� 5���� ������ �� 1�ʸ��� ����
        if (timer > 1.0f)
        {
            SpawnMob();

            timer = 0.0f;
        }
    }

    /// <summary>
    /// GameManager�� ���� ���濡 ���� ���� ����/������ �����մϴ�.
    /// </summary>
    private void HandleGameStateChange(GameState newState)
    {
        isSpawning = (newState == GameState.Playing);
    }

    private void SpawnMob()
    {
        GameObject enemy = PoolManager.instance.GetEnemy(UnityEngine.Random.Range(0, 2));

        enemy.transform.position = mobSpawnPoints[UnityEngine.Random.Range(1, mobSpawnPoints.Length)].position;

        enemy.GetComponent<Mob>().OnDied -= RespawnMob; // �ߺ� ���� ����
        enemy.GetComponent<Mob>().OnDied += RespawnMob;
    }

    private void RespawnMob(Mob mob)
    {

    }

    /// <summary>
    /// For test button
    /// </summary>
    public void SpawnBoss()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        GameManager.Instance.AppearBoss();

        Debug.Log(PoolManager.instance.GetBoss(BossName.TrainBoss));
        Debug.Log(bossSpawnPoints[(int)BossName.TrainBoss]);

        GameObject selected = Instantiate(PoolManager.instance.GetBoss(BossName.TrainBoss), bossSpawnPoints[(int)BossName.TrainBoss]);
    }

    /// <summary>
    /// 이게 정실
    /// </summary>
    /// <param name="boss"></param>
    public void SpawnBoss(BossName boss)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        switch (boss)
        {
            case BossName.TrainBoss:

                GameManager.Instance.AppearBoss();

                GameObject selected = Instantiate(PoolManager.instance.GetBoss(boss), bossSpawnPoints[(int)boss]);

                break;
        }

    }
}
