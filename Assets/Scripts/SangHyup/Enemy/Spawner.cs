using UnityEngine;
using System;


public class Spawner : MonoBehaviour
{
    [Header("Spawn Interval")]
    [SerializeField] private float mobSpawnInterval         = 1.0f;
    [SerializeField] private float eliteMobSpawnInterval    = 20.0f;
    [SerializeField] private float bossSpawnInterval        = 180.0f;

    [Header("Show up Time")]
    [SerializeField] private float firstEliteSpawnTime = 60.0f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[]        mobSpawnPoints;
    [SerializeField] private BoxCollider2D[]    flyMobSpawnAreas;
    [SerializeField] private Transform[]        bossSpawnPoints;

    private int mobIndex;

    private float mobTimer;
    private float eliteMobTimer;
    private float bossTimer;


    private void Start()
    {
        mobTimer        = 0f;
        eliteMobTimer   = 19.99f;
        bossTimer       = 0f;
    }

    private void Update()
    {
        //if (!isSpawning) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        mobTimer        += Time.deltaTime;
        if (GameManager.Instance.gameTime >= firstEliteSpawnTime)
            eliteMobTimer += Time.deltaTime;
        bossTimer       += Time.deltaTime;

        if (mobTimer >= mobSpawnInterval)
        {
            SpawnMob();

            mobTimer = 0f;
        }
        if (eliteMobTimer >= eliteMobSpawnInterval)
        {
            SpawnEliteMob();

            eliteMobTimer = 0f;
        }
        if (bossTimer >= bossSpawnInterval)
        {
            SpawnBoss(BossName.TrainBoss);

            bossTimer = 0f;
        }
    }

    private void SpawnMob()
    {
        mobIndex = UnityEngine.Random.Range(0, 2);

        GameObject enemy = PoolManager.instance.GetMob(mobIndex);
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
            int     whichArea   = UnityEngine.Random.Range(0, flyMobSpawnAreas.Length);
            Bounds  bounds      = flyMobSpawnAreas[whichArea].bounds;

            float randomY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            enemy.transform.position = new Vector3(30.0f, randomY, 0f);
        }

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // 속도 0으로 초기화
            rb.angularVelocity = 0f;    // 회전 속도 0으로 초기화
            // 필요하다면 순간적으로 중력을 끄거나, Kinematic으로 바꿨다가 Mob.cs에서 되돌리는 방법도 있음
        }

        enemy.GetComponent<Mob>().OnDied -= RespawnMob; // �ߺ� ���� ����
        enemy.GetComponent<Mob>().OnDied += RespawnMob;
    }

    private void SpawnEliteMob()
    {
        Debug.Log("Elite Mob Spawned");
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

        enemy.GetComponent<Mob>().OnDied -= RespawnMob; // �ߺ� ���� ����
        enemy.GetComponent<Mob>().OnDied += RespawnMob;
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
    /// This is Real
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

    private void RespawnMob(Mob mob)
    {

    }
}
