using UnityEngine;
using System.Collections.Generic;

public class LonginusLauncher : MonoBehaviour, IInstantiatedItem
{
    private LonginusLauncher_SO itemData;

    [Header("연결")]
    [SerializeField] private Animator animator;

    // --- 스탯 ---
    private float damage;
    private float speed;
    private float cooldown;
    private float spearLifeTime;
    private GameObject spearPrefab;
    private GameObject pathDataPrefab;

    private float cooldownTimer = 0f;
    private bool isReady = true;
    private int currentPathIndex = 0;

    private LonginusPathData activePathData;
    private Camera mainCamera;

    // ✨ [신규] 애니메이션 시작 시점의 타겟 위치 저장용
    private Vector3 lastKnownTargetPos;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    public void Initialize(LonginusLauncher_SO data, GameObject user)
    {
        this.itemData = data;
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int lv = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.damageByLevel.Length - 1);

        this.damage = itemData.damageByLevel[lv];
        this.cooldown = itemData.cooldownByLevel[lv];
        this.speed = itemData.spearSpeed;
        this.spearLifeTime = itemData.spearLifeTime;
        this.spearPrefab = itemData.SpearPrefab;
        this.pathDataPrefab = itemData.PathDataPrefab;

        if (activePathData == null && pathDataPrefab != null)
        {
            GameObject pathObj = Instantiate(pathDataPrefab, Vector3.zero, Quaternion.identity);
            pathObj.name = pathDataPrefab.name + " (ActivePath)";
            activePathData = pathObj.GetComponent<LonginusPathData>();
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        Debug.Log($"롱기누스의 창 업그레이드 완료 (Lv.{instance.currentUpgrade})");
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        if (isReady)
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }
            else
            {
                // 쿨타임 완료 & 화면 내 적이 있으면 시퀀스 시작
                if (HasVisibleEnemy())
                {
                    StartSummonSequence();
                }
            }
        }
    }

    private void StartSummonSequence()
    {
        // ✨ [핵심 1] 애니메이션 시작 전에 미리 타겟 위치를 저장(캐싱)해둠
        Transform target = FindRandomVisibleEnemy();

        if (target != null)
        {
            lastKnownTargetPos = target.position;
        }
        else
        {
            // (HasVisibleEnemy를 통과했으니 여기 올 일은 거의 없지만 안전장치)
            // 적이 없으면 대략 화면 중앙이나 오른쪽을 타겟으로 잡음
            lastKnownTargetPos = (mainCamera != null) ? mainCamera.transform.position : transform.position;
            lastKnownTargetPos.z = 0;
        }

        isReady = false;
        if (animator != null) animator.SetTrigger("Summon");
    }

    // ✨ [Animation Event]
    public void SpawnSpearFromAnim()
    {
        if (spearPrefab == null || activePathData == null || activePathData.paths.Length == 0) return;

        // 1. 경로 데이터 가져오기 (StartPoint만 사용)
        var currentPath = activePathData.paths[currentPathIndex];
        if (currentPath.startPoint == null) return;

        Vector3 startPos = currentPath.startPoint.position;

        // 2. ✨ [핵심 2] 발사 시점의 타겟 결정
        Vector3 targetPos;
        Transform currentTarget = FindRandomVisibleEnemy(); // 다시 한번 적을 찾아봄

        if (currentTarget != null)
        {
            // 적이 아직 살아있으면 그 적을 향해 발사 (정확도 UP)
            targetPos = currentTarget.position;
        }
        else
        {
            // 애니메이션 도중에 적이 사라졌다면? -> 아까 저장해둔 위치로 발사
            targetPos = lastKnownTargetPos;
        }

        // 3. 창 생성
        GameObject spearObj = Instantiate(spearPrefab, startPos, Quaternion.identity);
        LonginusSpear spearScript = spearObj.GetComponent<LonginusSpear>();

        if (spearScript != null)
        {
            spearScript.Initialize(damage, speed, spearLifeTime, startPos, targetPos, OnSpearDisappeared);
        }

        if (animator != null) animator.SetTrigger("Shoot");

        currentPathIndex = (currentPathIndex + 1) % activePathData.paths.Length;
    }

    // 화면 내 적 랜덤 반환
    private Transform FindRandomVisibleEnemy()
    {
        if (PoolManager.instance == null || mainCamera == null) return null;

        List<Enemy> candidates = new List<Enemy>();
        foreach (var enemy in PoolManager.instance.activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf && enemy.GetIsAlive())
            {
                Vector3 viewPos = mainCamera.WorldToViewportPoint(enemy.transform.position);
                if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
                {
                    candidates.Add(enemy);
                }
            }
        }

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)].transform;
    }

    // 화면 내 적 존재 여부 체크
    private bool HasVisibleEnemy()
    {
        if (PoolManager.instance == null || mainCamera == null) return false;

        for (int i = 0; i < PoolManager.instance.activeEnemies.Count; i++)
        {
            var enemy = PoolManager.instance.activeEnemies[i];
            if (enemy != null && enemy.gameObject.activeSelf && enemy.GetIsAlive())
            {
                Vector3 viewPos = mainCamera.WorldToViewportPoint(enemy.transform.position);
                if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void OnSpearDisappeared()
    {
        if (animator != null) animator.SetTrigger("Return");
        cooldownTimer = cooldown;
        isReady = true;
    }

    private void OnDestroy()
    {
        if (activePathData != null) Destroy(activePathData.gameObject);
    }
}