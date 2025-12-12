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
    [SerializeField] private float patternWaitTime          = 9.0f;

    [Header("Reference")]
    [SerializeField] private GameObject tentacle;
    [SerializeField] private GameObject weakTentacle;
                     private Transform[] tentacleSpawnPoints;


    private List<GameObject> spawnedTentacles = new List<GameObject>();

    private float tentacleAttackTime;

    private bool isInvincible           = false;
    private bool canAttack              = true;
    private bool enragePatternReady     = false;
    private bool isInvokeEnragePattern  = false;

    protected override void Start()
    {
        base.Start();

        tentacleAttackTime  = tentacle.GetComponent<Tentacle>().GetAttackWaitTime();

        int childCount      = transform.childCount;
        tentacleSpawnPoints = new Transform[childCount];

        for (int index = 0; index < childCount; index++)
            tentacleSpawnPoints[index] = transform.GetChild(index);
    }

    protected override void Update()
    {
        base.Update();

        if (!isAlive || !canAttack) return;

        if (!isInvokeEnragePattern && enragePatternReady)
        {
            StartCoroutine(EnragePattern());

            return;
        }

        int patternIndex = Random.Range(0, 2);

        if (patternIndex == 0)
            StartCoroutine(AttackPattern1());
        if (patternIndex == 1)
            StartCoroutine(AttackPattern2());
    }

    public override void TakeDamage(float damageAmount)
    {
        if (isInvincible) return;

        base.TakeDamage(damageAmount);

        // 체력 비율 계산
        float currentHpRatio = currentHP / calibratedMaxHP;

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
        isInvincible            = true;
        canAttack               = false;

        int weakPoint = Random.Range(1, 7);

        int[] weakPoints = new int[3]
        { weakPoint - 1, weakPoint, weakPoint + 1 };

        SpawnTentacle(tentacleSpawnPoints, weakPoints);

        yield return new WaitForSeconds(tentacleAttackTime);

        isInvincible = false;

        yield return new WaitForSeconds(patternWaitTime - tentacleAttackTime);

        canAttack = true;
    }

    private void SpawnTentacle(Transform[] spawnPoints, int weakPoint)
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            bool        isWeakPoint     = spawnPoint.GetSiblingIndex() == weakPoint;
            GameObject  tentaclePrefab  = isWeakPoint ? weakTentacle : tentacle;

            GameObject spawnedTentacle = Instantiate(tentaclePrefab, spawnPoint.position, Quaternion.identity);

            Tentacle tentacleScript = spawnedTentacle.GetComponent<Tentacle>();

            tentacleScript.Initialize(this);
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

            GameObject spawnedTentacle = Instantiate(tentaclePrefab, spawnPoint.position, Quaternion.identity);
    
            Tentacle tentacleScript = spawnedTentacle.GetComponent<Tentacle>();

            tentacleScript.Initialize(this);
            tentacleScript.SetAttackWaitTime(8.0f);
            tentacleScript.SetDamage(100);
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

        yield return new WaitForSeconds(2.0f);
        // 사망 파티클 이펙트

        if (killEvent != null)
        {
            // GameManager가 있고, 아직 엔딩 시간이 아니라면 -> 보상 획득 이벤트 실행
            // (엔딩 시간이면 보상 창 안 띄움)
            if (GameManager.Instance != null && !GameManager.Instance.IsTimeForEnding)
            {
                EventManager.Instance.RequestEvent(killEvent);
            }
        }

        if (GameManager.Instance != null)
        {
            // 보스 킬 카운트 증가 + 엔딩 판정 요청
            GameManager.Instance.AddBossKillCount();
            GameManager.Instance.BossDied();
        }
        else
        {
            // 비상용 (GameManager 없을 때)
            StageManager.Instance.StartStageTransitionSequence();
        }

        Destroy(gameObject);
    }
}
