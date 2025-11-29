using UnityEngine;
using System.Collections;

public class HealingItem : MonoBehaviour, IInstantiatedItem
{
    private HealingItem_SO itemData;
    private Train train;

    [Header("자식 오브젝트 연결")]
    [Tooltip("평소에 숫자를 보여줄 자식 오브젝트의 SpriteRenderer")]
    [SerializeField] private SpriteRenderer numberRenderer;

    [Tooltip("회복 시 잠깐 켜질 하트 애니메이션 자식 오브젝트")]
    [SerializeField] private GameObject heartObject;

    [Tooltip("하트 애니메이션 재생 시간 (초)")]
    [SerializeField] private float heartAnimDuration = 1.5f;

    // --- 상태 변수 ---
    private int currentKillCount = 0;
    private int targetKillCount;
    private bool isAnimating = false; // 현재 하트 연출 중인가?

    // --- 스탯 ---
    private float healPercent;
    private Sprite[] countSprites;

    public void Initialize(HealingItem_SO data, GameObject user)
    {
        this.itemData = data;
        this.train = user.GetComponent<Train>();

        if (train == null) Debug.LogError("[HealingItem] Train을 찾을 수 없습니다.");

        // ✨ 초기 상태 설정: 숫자는 켜고, 하트는 끈다.
        if (numberRenderer != null) numberRenderer.gameObject.SetActive(true);
        if (heartObject != null) heartObject.SetActive(false);
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        // 데이터 갱신 (레벨 인덱스 = 현재레벨 - 1)
        int levelIdx = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.maxSpeedBonusByLevel.Length - 1);

        this.healPercent = itemData.healPercentByLevel[levelIdx];
        this.targetKillCount = itemData.killCountCondition[levelIdx];
        this.countSprites = itemData.countSprites;

        // 최대 속도 증가 로직
        float currentBonus = itemData.maxSpeedBonusByLevel[levelIdx];
        float prevBonus = (levelIdx > 0) ? itemData.maxSpeedBonusByLevel[levelIdx - 1] : 0f;
        float increaseAmount = currentBonus - prevBonus;

        if (train != null && increaseAmount > 0f)
        {
            train.IncreaseMaxSpeed(increaseAmount);
            train.ModifySpeed(increaseAmount); // 늘어난 만큼 회복
            Debug.Log($"[HealingItem] Lv.{instance.currentUpgrade} 보너스 적용: +{increaseAmount}");
        }

        // 숫자 표시 갱신
        UpdateVisual();
    }

    // 적 처치 시 호출 (Inventory -> Item_SO -> 여기)
    public void OnEnemyKilled()
    {
        currentKillCount++;

        // 목표 달성 체크
        if (currentKillCount >= targetKillCount)
        {
            TriggerHeal();
            currentKillCount = 0; // 카운트 초기화
        }

        // 애니메이션 중이 아닐 때만 숫자 갱신 (하트가 켜져있을 땐 굳이 안 바꿈)
        if (!isAnimating)
        {
            UpdateVisual();
        }
    }

    private void TriggerHeal()
    {
        // 1. 실제 회복 적용
        if (train != null)
        {
            train.HealPercent(this.healPercent);
        }

        // 2. 하트 애니메이션 연출 시작
        StartCoroutine(PlayHealAnimation());
    }

    private IEnumerator PlayHealAnimation()
    {
        isAnimating = true;

        // 숫자는 끄고, 하트는 켠다 (Active가 켜지면 애니메이션 자동 재생됨)
        if (numberRenderer != null) numberRenderer.gameObject.SetActive(false);
        if (heartObject != null) heartObject.SetActive(true);

        // 애니메이션 시간만큼 대기
        yield return new WaitForSeconds(heartAnimDuration);

        // 하트는 끄고, 숫자는 다시 켠다
        if (heartObject != null) heartObject.SetActive(false);
        if (numberRenderer != null) numberRenderer.gameObject.SetActive(true);

        isAnimating = false;

        // 대기하는 동안 킬 카운트가 변했을 수 있으니 숫자 즉시 갱신
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (countSprites == null || numberRenderer == null) return;

        // 남은 킬 수 계산
        int remaining = Mathf.Max(0, targetKillCount - currentKillCount);

        // 0 이하면 처리 안 함 (하트가 나올 것이므로)
        if (remaining <= 0) return;

        // ✨ [수정] 값(1~20)을 배열 인덱스(0~19)로 변환
        // 예: 남은 수 1 -> 인덱스 0
        // 예: 남은 수 20 -> 인덱스 19
        int spriteIndex = remaining - 1;

        // 배열 범위를 벗어나지 않게 안전장치 (혹시 목표가 20보다 클 경우를 대비)
        spriteIndex = Mathf.Clamp(spriteIndex, 0, countSprites.Length - 1);

        if (countSprites.Length > spriteIndex)
        {
            numberRenderer.sprite = countSprites[spriteIndex];
        }
    }
}