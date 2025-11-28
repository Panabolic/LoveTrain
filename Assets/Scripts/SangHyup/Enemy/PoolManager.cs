
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyName
{
    DeepOne, OldOne
}

public enum BossName
{
    TrainBoss
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    [SerializeField] private GameObject[]       mobs;           // Mob prefab array
                     private List<GameObject>[] mobPools;       // Array of pooled mob lists
    [SerializeField] private GameObject[]       eliteMobs;       // Elite Mob prefab array
                     private List<GameObject>[] eliteMobPools;  // Array of pooled elite mob lists
    [SerializeField] private GameObject[]       bosses;

    public List<Enemy> activeEnemies = new List<Enemy>();

    public int eventDebuffPercent = 0;

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

    private void Start()
    {
        mobPools = new List<GameObject>[mobs.Length];
        for (int i = 0; i < mobPools.Length; i++)
        {
            mobPools[i] = new List<GameObject>();
        }

        eliteMobPools = new List<GameObject>[eliteMobs.Length];
        for (int i = 0; i < eliteMobPools.Length; i++)
        {
            eliteMobPools[i] = new List<GameObject>();
        }
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
    
    public void DespawnAllEnemies()
    {
        // 리스트를 순회하며 비활성화 (역순 순회 권장: 리스트 변경 가능성 대비)
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].DespawnWithoutExp();
            }
        }
    }

    public GameObject GetMob(int index)
    {
        GameObject selected = null;

        foreach (GameObject enemy in mobPools[index])
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
            selected = Instantiate(mobs[index], transform);
            mobPools[index].Add(selected);
        }

        return selected;
    }

    public GameObject GetEliteMob(int index)
    {
        GameObject selected = null;

        foreach (GameObject enemy in eliteMobPools[index])
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
            selected = Instantiate(eliteMobs[index], transform);
            eliteMobPools[index].Add(selected);
        }

        return selected;
    }

    public GameObject GetBoss(BossName boss)
    {
        return bosses[(int)boss];
    }

    public int GetPooledEnemyCount(EnemyName enemyName) { return mobPools[(int)enemyName].Count; }
}
