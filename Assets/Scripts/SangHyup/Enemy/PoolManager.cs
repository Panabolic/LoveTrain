using System.Collections.Generic;
using UnityEngine;

public enum EnemyName
{
    DeepOne, OldOne
}

public enum BossName
{
    EyeBoss, TrainBoss
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    [Header("--- Mob Prefabs ---")]
    [SerializeField] private GameObject[] groundMobs; // 지상 일반
    [SerializeField] private GameObject[] flyMobs;    // 공중 일반

    [Space]
    [SerializeField] private GameObject[] groundEliteMobs; // ✨ 지상 엘리트
    [SerializeField] private GameObject[] flyEliteMobs;    // ✨ 공중 엘리트

    [Space]
    [SerializeField] private GameObject[] bosses;

    // --- Pooling Lists ---
    private List<GameObject>[] groundMobPools;
    private List<GameObject>[] flyMobPools;
    private List<GameObject>[] groundEliteMobPools; // ✨
    private List<GameObject>[] flyEliteMobPools;    // ✨

    // 동적 풀 (이벤트/프리팹 스폰용)
    private Dictionary<string, List<GameObject>> dynamicPools = new Dictionary<string, List<GameObject>>();

    public List<Enemy> activeEnemies = new List<Enemy>();

    [Header("Calibration")]
    public int hpIncrease = 10;
    public int eventDebuff = 0;

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
        // 풀 초기화
        InitializePools(groundMobs, out groundMobPools);
        InitializePools(flyMobs, out flyMobPools);
        InitializePools(groundEliteMobs, out groundEliteMobPools); // ✨
        InitializePools(flyEliteMobs, out flyEliteMobPools);       // ✨
    }

    private void InitializePools(GameObject[] prefabs, out List<GameObject>[] pools)
    {
        pools = new List<GameObject>[prefabs.Length];
        for (int i = 0; i < pools.Length; i++)
        {
            pools[i] = new List<GameObject>();
        }
    }

    // ------------------------------------------------------------
    // 만능 프리팹 풀링 (이벤트용)
    // ------------------------------------------------------------
    public GameObject GetMob(GameObject prefab)
    {
        if (prefab == null) return null;
        string key = prefab.name;

        if (!dynamicPools.ContainsKey(key))
        {
            dynamicPools.Add(key, new List<GameObject>());
        }

        List<GameObject> pool = dynamicPools[key];
        return GetFromPoolList(pool, prefab);
    }

    // ------------------------------------------------------------
    // 인덱스 기반 Getter (자동 스폰용)
    // ------------------------------------------------------------

    // 일반 몬스터
    public GameObject GetGroundMob(int index)
    {
        if (index < 0 || index >= groundMobs.Length) return null;
        return GetFromPool(groundMobPools[index], groundMobs[index]);
    }
    public GameObject GetFlyMob(int index)
    {
        if (index < 0 || index >= flyMobs.Length) return null;
        return GetFromPool(flyMobPools[index], flyMobs[index]);
    }

    // ✨ 엘리트 몬스터 (분리됨)
    public GameObject GetGroundEliteMob(int index)
    {
        if (index < 0 || index >= groundEliteMobs.Length) return null;
        return GetFromPool(groundEliteMobPools[index], groundEliteMobs[index]);
    }
    public GameObject GetFlyEliteMob(int index)
    {
        if (index < 0 || index >= flyEliteMobs.Length) return null;
        return GetFromPool(flyEliteMobPools[index], flyEliteMobs[index]);
    }

    public GameObject GetBoss(BossName boss)
    {
        return bosses[(int)boss];
    }

    // --- 내부 로직 ---
    private GameObject GetFromPool(List<GameObject> pool, GameObject prefab)
    {
        return GetFromPoolList(pool, prefab);
    }

    private GameObject GetFromPoolList(List<GameObject> pool, GameObject prefab)
    {
        GameObject selected = null;
        foreach (GameObject obj in pool)
        {
            if (obj != null && !obj.activeSelf)
            {
                selected = obj;
                selected.SetActive(true);
                break;
            }
        }

        if (selected == null)
        {
            selected = Instantiate(prefab, transform);
            selected.name = prefab.name;
            pool.Add(selected);
        }
        return selected;
    }

    public void RegisterEnemy(Enemy enemy) { if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy); }
    public void UnregisterEnemy(Enemy enemy) { if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy); }

    public void DespawnAllEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
            if (activeEnemies[i] != null) activeEnemies[i].DespawnWithoutExp();
    }

    public void DespawnAllEnemiesExceptBoss()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy != null)
            {
                // Boss 컴포넌트가 있으면 건너뜀 (살려둠)
                if (enemy.GetComponent<Boss>() != null) continue;

                enemy.DespawnWithoutExp();
            }
        }
    }

}