using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    public GameObject[] enemies;
    private List<GameObject>[] pools;

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
            //PoolManager ������ �� ���� �� selected�� �Ҵ�, Ǯ�� �߰�
            selected = Instantiate(enemies[index], transform);
            pools[index].Add(selected);
        }

        return selected;
    }
}
