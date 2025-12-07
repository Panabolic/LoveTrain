using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EyeBoss : Boss
{
    [Header("Eye Boss Specification")]
    [SerializeField] private float waitTimeBetweenPatterns = 2.0f;
    [Range(0.1f, 1.0f)]
    [SerializeField] private float EnragePatternThreshold   = 0.2f; // 체력 비율 임계값
    [SerializeField] private float patternWaitTime          = 7.0f;

    [Header("Tentacle Specification")]
    [SerializeField] private float tentacleHP       = 300.0f;
    [SerializeField] private float tentacleDamage   = 50.0f;
    [SerializeField] private float weakTentacleHP   = 150.0f;

    [Header("Reference")]
    [SerializeField] private GameObject tentacle;
    [SerializeField] private GameObject weakTentacle;
                     private Transform[] tentacleSpawnPoints;


    private List<GameObject> spawnedTentacles = new List<GameObject>();

    private bool canAttack              = true;
    private bool enragePatternReady     = false;
    private bool isInvokeEnragePattern  = false;

    protected override void Start()
    {
        base.Start();

        int childCount      = transform.childCount;
        tentacleSpawnPoints = new Transform[childCount];

        for (int index = 0; index < childCount; index++)
            tentacleSpawnPoints[index] = transform.GetChild(index);
    }

    private void Update()
    {
        if (!isAlive || !canAttack) return;

        if (!isInvokeEnragePattern && enragePatternReady)
        {
            StartCoroutine(EnragePattern());
        }

        int patternIndex = Random.Range(0, 2);

        if (patternIndex == 0)
            StartCoroutine(AttackPattern1());
        if (patternIndex == 1)
            StartCoroutine(AttackPattern2());
    }

    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);

        // 체력 비율 계산
        float currentHpRatio = currentHP / calibratedHP;

        // 체력 비율이 임계값 이하로 떨어지면 즉사 패턴 발동
        if (!enragePatternReady && currentHpRatio <= EnragePatternThreshold)
            enragePatternReady = true;
    }

    /// <summary>
    /// 화면 전체를 기준으로 왼편 혹은 오른편 촉수 공격 패턴
    /// </summary>
    /// <returns></returns>
    private IEnumerator AttackPattern1()
    {
        Transform[] selectedSpawnPoints = new Transform[4];

        int weakPoint;
        int leftOrRight = Random.Range(0, 2);

        canAttack = false;

        switch (leftOrRight)
        {
            case 0:
                // 왼쪽 공격 패턴
                selectedSpawnPoints = new Transform[]
                {
                    tentacleSpawnPoints[0],
                    tentacleSpawnPoints[1],
                    tentacleSpawnPoints[2],
                    tentacleSpawnPoints[3],
                };

                weakPoint = Random.Range(0, tentacleSpawnPoints.Length / 2);

                SpawnTentacle(selectedSpawnPoints, weakPoint);
                
                break;

            case 1:
                // 오른쪽 공격 패턴
                selectedSpawnPoints = new Transform[]
                {
                    tentacleSpawnPoints[4],
                    tentacleSpawnPoints[5],
                    tentacleSpawnPoints[6],
                    tentacleSpawnPoints[7],
                };

                weakPoint = Random.Range(tentacleSpawnPoints.Length / 2, tentacleSpawnPoints.Length);

                SpawnTentacle(selectedSpawnPoints, weakPoint);

                break;
        }

        yield return new WaitForSeconds(patternWaitTime);

        canAttack = true;
    }

    /// <summary>
    /// 화면 중앙 촉수 공격 패턴
    /// </summary>
    /// <returns></returns>
    private IEnumerator AttackPattern2()
    {
        Transform[] selectedSpawnPoints = new Transform[]
        {
            tentacleSpawnPoints[2],
            tentacleSpawnPoints[3],
            tentacleSpawnPoints[4],
            tentacleSpawnPoints[5],
        };

        int weakPoint = Random.Range(2, 6); ;

        canAttack = false;

        SpawnTentacle(selectedSpawnPoints, weakPoint);

        yield return new WaitForSeconds(patternWaitTime);

        canAttack = true;
    }

    /// <summary>
    /// 화면 전체 촉수 공격 패턴
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnragePattern()
    {
        isInvokeEnragePattern   = true;
        canAttack               = false;

        int weakPoint = Random.Range(1, 7);

        int[] weakPoints = new int[3]
        { weakPoint - 1, weakPoint, weakPoint + 1 };

        SpawnTentacle(tentacleSpawnPoints, weakPoints);

        yield return new WaitForSeconds(patternWaitTime);

        canAttack = true;
    }

    private void SpawnTentacle(Transform[] spawnPoints, int weakPoint)
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            bool        isWeakPoint     = spawnPoint.GetSiblingIndex() == weakPoint;
            GameObject  tentaclePrefab  = isWeakPoint ? weakTentacle : tentacle;
            float       tentacleHPValue = isWeakPoint ? weakTentacleHP : tentacleHP;

            GameObject spawnedTentacle = Instantiate(tentaclePrefab, spawnPoint.position, Quaternion.identity);

            Tentacle tentacleScript = spawnedTentacle.GetComponent<Tentacle>();

            tentacleScript.Initialize(this);
            tentacleScript.SetHP(tentacleHPValue);
            tentacleScript.SetDamage(tentacleDamage);
        }
    }

    private void SpawnTentacle(Transform[] spawnPoints, int[] weakPoints)
    {
        HashSet<int> weakPointIndexSet = new HashSet<int>(weakPoints);
    
        foreach (Transform spawnPoint in spawnPoints)
        {
            int siblingIndex = spawnPoint.GetSiblingIndex();

            bool        isWeakPoint     = weakPointIndexSet.Contains(siblingIndex);
            GameObject  tentaclePrefab  = isWeakPoint ? weakTentacle : tentacle;
            float       tentacleHPValue = isWeakPoint ? weakTentacleHP : tentacleHP;

            GameObject spawnedTentacle = Instantiate(tentaclePrefab, spawnPoint.position, Quaternion.identity);
    
            Tentacle tentacleScript = spawnedTentacle.GetComponent<Tentacle>();

            tentacleScript.Initialize(this);
            tentacleScript.SetHP(tentacleHPValue);
            tentacleScript.SetDamage(10000);
        }
    }
    public void RegisterTentacle(GameObject tentacle)
    {
        if (!spawnedTentacles.Contains(tentacle))
            spawnedTentacles.Add(tentacle);
    }

    public void UnregisterTentacle(GameObject tentacle)
    {
        spawnedTentacles.Remove(tentacle);
    }

    protected override IEnumerator Die()
    {
        yield return base.Die();

        foreach (GameObject tentacle in spawnedTentacles)
        {
            Destroy(tentacle.gameObject);
        }

        spawnedTentacles.Clear();

        // 사망 파티클 이펙트

        Destroy(gameObject);
    }
}
