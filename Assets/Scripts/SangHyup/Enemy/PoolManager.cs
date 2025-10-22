using UnityEngine;
using System.Collections.Generic;

public enum EnemyName
{
    Monster,
    FlyMonster
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    [SerializeField] private GameObject[]       enemies;    // Enemy prefab array
                     private List<GameObject>[] pools;      // Array of pooled enemy lists


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
            //PoolManager 하위에 몹 생성 후 selected에 할당, 풀에 추가
            selected = Instantiate(enemies[index], transform);
            pools[index].Add(selected);
        }

        return selected;
    }

    public int GetPooledEnemyCount(EnemyName enemyName) { return pools[(int)enemyName].Count; }
}
