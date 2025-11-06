using UnityEngine;
using System.Collections.Generic;

public enum EnemyName
{
    Monster, FlyMonster
}

public enum BossName
{
    TrainBoss
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    [SerializeField] private GameObject[]       enemies;    // Enemy prefab array
                     private List<GameObject>[] pools;      // Array of pooled enemy lists
    [SerializeField] private GameObject[]       bosses;

    public List<Enemy> activeEnemies = new List<Enemy>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    private void Start()
    {
        pools = new List<GameObject>[enemies.Length];
        
        for (int i = 0; i < pools.Length; i++)
        {
            pools[i] = new List<GameObject>();
        }
    }

    public GameObject GetEnemy(int index)
    {
        GameObject selected = null;

        foreach (GameObject enemy in pools[index])
        {
            if (!enemy.activeSelf)
            {
                selected = enemy;
                selected.SetActive(true);

                break;
            }
        }

        if (selected == null)
        {
            selected = Instantiate(enemies[index], transform);
            pools[index].Add(selected);
        }

        return selected;
    }

    public GameObject GetBoss(BossName boss)
    {
        return bosses[(int)boss];
    }

    public int GetPooledEnemyCount(EnemyName enemyName) { return pools[(int)enemyName].Count; }
}
