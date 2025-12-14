using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeBoss : Boss
{
    [Header("Pattern Settings")]
    [SerializeField] private float waitTimeAfterPatternEnd = 2.0f;

    [Header("Enrage Settings")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float EnragePatternThreshold = 0.2f;

    [Header("Damage & Delay Settings")]
    [SerializeField] private float normalTentacleDamage = 50f;
    [SerializeField] private float normalAttackDelay = 3.0f;
    [Space]
    [SerializeField] private float enrageTentacleDamage = 100f;
    [SerializeField] private float enrageAttackDelay = 8f;

    [Header("Reference")]
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private GameObject weakTentaclePrefab;

    private Transform[] tentacleSpawnPoints;
    private List<GameObject> spawnedTentacles = new List<GameObject>();

    private Coroutine runningPatternCoroutine;

    // 상태 변수
    private bool isInvincible = false;
    private bool isBusy = false;
    private bool enragePatternReady = false;
    private bool hasEnraged = false;

    protected override void Start()
    {
        base.Start();

        int childCount = transform.childCount;
        tentacleSpawnPoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            tentacleSpawnPoints[i] = transform.GetChild(i);
    }

    protected override void Update()
    {
        base.Update();

        // ✨ [수정] isEntranceActive가 true면 패턴 실행 안 함
        // arrivalPoint가 없어서 Spawner가 호출 안 했으면 false이므로 통과됨
        if (!isAlive || isBusy || enragePatternReady || isEntranceActive) return;

        int patternIndex = Random.Range(0, 2);
        if (patternIndex == 0)
            runningPatternCoroutine = StartCoroutine(SideAttackPattern());
        else
            runningPatternCoroutine = StartCoroutine(CenterAttackPattern());
    }

    public override void TakeDamage(float damageAmount)
    {
        if (isInvincible) return;

        float predictedHP = currentHP - damageAmount;
        float thresholdHP = calibratedMaxHP * EnragePatternThreshold;

        if (!hasEnraged && !enragePatternReady && predictedHP <= thresholdHP)
        {
            currentHP = thresholdHP;
            isInvincible = true;
            enragePatternReady = true;

            StartCoroutine(ForceEnrageRoutine());
        }
        else
        {
            base.TakeDamage(damageAmount);
        }
    }

    private IEnumerator ForceEnrageRoutine()
    {
        if (runningPatternCoroutine != null)
        {
            StopCoroutine(runningPatternCoroutine);
        }

        ClearAllTentacles();

        isBusy = true;

        yield return null;

        runningPatternCoroutine = StartCoroutine(EnragePatternRoutine());
    }

    private IEnumerator SideAttackPattern()
    {
        isBusy = true;

        int leftOrRight = Random.Range(0, 2);
        int startIdx = (leftOrRight == 0) ? 0 : 4;
        int endIdx = (leftOrRight == 0) ? 4 : 8;

        List<int> spawnIndices = new List<int>();
        for (int i = startIdx; i < endIdx; i++) spawnIndices.Add(i);

        int weakPointIdx = Random.Range(startIdx, endIdx);

        float duration = SpawnTentaclesAndGetDuration(spawnIndices, new List<int> { weakPointIdx }, false);

        yield return new WaitForSeconds(duration);
        yield return new WaitForSeconds(waitTimeAfterPatternEnd);

        isBusy = false;
    }

    private IEnumerator CenterAttackPattern()
    {
        isBusy = true;

        List<int> spawnIndices = new List<int> { 2, 3, 4, 5 };
        int weakPointIdx = Random.Range(2, 6);

        float duration = SpawnTentaclesAndGetDuration(spawnIndices, new List<int> { weakPointIdx }, false);

        yield return new WaitForSeconds(duration);
        yield return new WaitForSeconds(waitTimeAfterPatternEnd);

        isBusy = false;
    }

    private IEnumerator EnragePatternRoutine()
    {
        isBusy = true;
        hasEnraged = true;

        List<int> allIndices = new List<int>();
        for (int i = 0; i < tentacleSpawnPoints.Length; i++) allIndices.Add(i);

        int centerWeak = Random.Range(1, 7);
        List<int> weakPoints = new List<int> { centerWeak - 1, centerWeak, centerWeak + 1 };

        float duration = SpawnTentaclesAndGetDuration(allIndices, weakPoints, true);

        yield return new WaitForSeconds(duration);

        isInvincible = false;
        enragePatternReady = false;

        yield return new WaitForSeconds(waitTimeAfterPatternEnd);

        isBusy = false;
    }

    private float SpawnTentaclesAndGetDuration(List<int> spawnIndices, List<int> weakPointIndices, bool isEnrage)
    {
        float maxDuration = 0f;
        HashSet<int> weakSet = new HashSet<int>(weakPointIndices);

        float currentDamage = isEnrage ? enrageTentacleDamage : normalTentacleDamage;
        float currentDelay = isEnrage ? enrageAttackDelay : normalAttackDelay;

        foreach (int index in spawnIndices)
        {
            if (index < 0 || index >= tentacleSpawnPoints.Length) continue;

            bool isWeak = weakSet.Contains(index);
            GameObject prefab = isWeak ? weakTentaclePrefab : tentaclePrefab;

            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, tentacleSpawnPoints[index].position, Quaternion.identity);
            Tentacle tScript = obj.GetComponent<Tentacle>();

            if (tScript != null)
            {
                tScript.Setup(this, currentDamage, currentDelay);
                tScript.BeginAttack();

                float d = tScript.GetTotalDuration();
                if (d > maxDuration) maxDuration = d;
            }
        }
        return maxDuration;
    }

    private void ClearAllTentacles()
    {
        for (int i = spawnedTentacles.Count - 1; i >= 0; i--)
        {
            if (spawnedTentacles[i] != null) Destroy(spawnedTentacles[i]);
        }
        spawnedTentacles.Clear();
    }

    public void RegisterTentacle(GameObject tentacle)
    {
        if (!spawnedTentacles.Contains(tentacle)) spawnedTentacles.Add(tentacle);
    }

    public void UnregisterTentacle(GameObject tentacle)
    {
        if (spawnedTentacles.Contains(tentacle)) spawnedTentacles.Remove(tentacle);
    }

    protected override IEnumerator Die()
    {
        ClearAllTentacles();

        Instantiate(killExplosionEffect, transform.position, Quaternion.identity);

        yield return base.Die();

        yield return new WaitForSeconds(2.0f);

        if (killEvent != null && GameManager.Instance != null && !GameManager.Instance.IsTimeForEnding)
        {
            EventManager.Instance.RequestEvent(killEvent);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBossKillCount();
            GameManager.Instance.BossDied();
        }
        else
        {
            StageManager.Instance.StartStageTransitionSequence();
        }

        Destroy(gameObject);
    }
}