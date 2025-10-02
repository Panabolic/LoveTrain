using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoints;

    private float timer;

    private void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > 1.0f)
        {
            Spawn();

            timer = 0.0f;
        }
    }

    private void Spawn()
    {
        GameObject enemy = PoolManager.instance.GetEnemy(Random.Range(0, 0));

        enemy.transform.position = spawnPoints[Random.Range(1, spawnPoints.Length)].position;
    }
}
