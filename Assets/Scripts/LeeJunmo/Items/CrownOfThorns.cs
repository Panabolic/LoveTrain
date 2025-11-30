using UnityEngine;
using System.Collections;

public class CrownOfThorns : MonoBehaviour, IInstantiatedItem
{
    private CrownOfThorns_SO itemData;
    private Animator animator;

    // --- 스탯 ---
    private float damage;
    private int lightningCount;
    private float cooldown;

    private float spawnRangeX;
    private float minDelay = 0.1f;
    private float maxDelay = 0.5f;

    private GameObject lightningPrefab;

    // 내부 변수
    private float cooldownTimer = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Initialize(CrownOfThorns_SO data)
    {
        this.itemData = data;
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int lv = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.damageByLevel.Length - 1);

        this.damage = itemData.damageByLevel[lv];
        this.lightningCount = itemData.countByLevel[lv];
        this.cooldown = itemData.cooldownByLevel[lv];

        this.spawnRangeX = itemData.spawnRangeX;
        this.minDelay = itemData.spawnDelayMin;
        this.maxDelay = itemData.spawnDelayMax;
        this.lightningPrefab = itemData.LightningPrefab;

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        Debug.Log($"면류관 업그레이드 완료 (Lv.{instance.currentUpgrade})");
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        else
        {
            // 쿨타임 완료 -> 적이 있으면 발동
            if (PoolManager.instance != null && PoolManager.instance.activeEnemies.Count > 0)
            {
                StartEffectSequence();
            }
        }
    }

    private void StartEffectSequence()
    {
        // 1. 반짝이는 애니메이션 재생 (Trigger)
        if (animator != null)
        {
            animator.SetTrigger("Flash");

            // 쿨타임 재설정
            cooldownTimer = cooldown;
        }
    }

    // ✨ [Animation Event] "Flash" 애니메이션이 끝나는 시점(혹은 번쩍이는 순간)에 호출
    public void SpawnLightningFromAnim()
    {
        StartCoroutine(SpawnLightningRoutine());
    }

    private IEnumerator SpawnLightningRoutine()
    {
        for (int i = 0; i < lightningCount; i++)
        {
            SpawnSingleLightning();

            // 랜덤 딜레이 대기
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private void SpawnSingleLightning()
    {
        if (lightningPrefab == null) return;

        // 1. 랜덤 위치 계산 (플레이어 Y축 기준, X축 랜덤)
        // (BloodyBible과 동일한 로직: 맵 전체 범위 내 랜덤)
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);

        // Y축은 번개가 떨어질 높이 (기차보다 약간 위나 바닥, 이펙트에 따라 조정)
        // 일단은 기차와 동일한 Y축(선로)에 생성한다고 가정
        Vector3 spawnPos = new Vector3(randomX, -8.7f, 0f);

        // 2. 번개 생성
        GameObject boltObj = Instantiate(lightningPrefab, spawnPos, Quaternion.identity);

        // 3. 초기화 (데미지, 랜덤 스케일 등)
        LightningBolt boltScript = boltObj.GetComponent<LightningBolt>();
        if (boltScript != null)
        {
            boltScript.Initialize(damage);
        }
    }
}