using UnityEngine;
using System; // Action �̺�Ʈ�� ���� �ʿ�

public class Spawner : MonoBehaviour
{
    [Header("Enemy Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

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

    private void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
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
            Spawn();

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

    private void Spawn()
    {
        GameObject enemy = PoolManager.instance.GetEnemy(UnityEngine.Random.Range(0, 2));

        enemy.transform.position = spawnPoints[UnityEngine.Random.Range(1, spawnPoints.Length)].position;

        enemy.GetComponent<Mob>().OnDied -= RespawnEnemy; // �ߺ� ���� ����
        enemy.GetComponent<Mob>().OnDied += RespawnEnemy;
    }

    private void RespawnEnemy(Mob mob)
    {

    }
}
