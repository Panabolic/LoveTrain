using UnityEngine;
using System.Collections.Generic;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance;

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform poolParent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요시 해제
            poolParent = new GameObject("ObjectPool").transform;
            poolParent.SetParent(transform);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        GameObject obj = null;

        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
            while (obj == null)
            {
                if (poolDictionary[prefab].Count == 0) { obj = null; break; }
                obj = poolDictionary[prefab].Dequeue();
            }
        }

        if (obj == null)
        {
            obj = Instantiate(prefab, poolParent);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    public void ReturnToPool(GameObject obj, GameObject originalPrefab)
    {
        if (obj == null || originalPrefab == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(poolParent);

        if (poolDictionary.ContainsKey(originalPrefab))
        {
            poolDictionary[originalPrefab].Enqueue(obj);
        }
        else
        {
            Queue<GameObject> newQueue = new Queue<GameObject>();
            newQueue.Enqueue(obj);
            poolDictionary.Add(originalPrefab, newQueue);
        }
    }
}