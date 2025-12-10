using UnityEngine;
using System.Collections;

public class HealingItem : MonoBehaviour, IInstantiatedItem
{
    private HealingItem_SO itemData;
    private Train train;

    [Header("자식 오브젝트 연결")]
    [Tooltip("10의 자리 숫자를 보여줄 SpriteRenderer")]
    [SerializeField] private SpriteRenderer tensRenderer;

    [Tooltip("1의 자리 숫자를 보여줄 SpriteRenderer")]
    [SerializeField] private SpriteRenderer unitsRenderer;

    [Tooltip("회복 시 잠깐 켜질 하트 애니메이션 자식 오브젝트")]
    [SerializeField] private GameObject heartObject;

    [Tooltip("하트 애니메이션 재생 시간 (초)")]
    [SerializeField] private float heartAnimDuration = 1.5f;

    // --- 상태 변수 ---
    private int currentKillCount = 0;
    private int targetKillCount;
    private bool isAnimating = false;

    // --- 스탯 ---
    private float healPercent;

    // ✨ 0~9 숫자 스프라이트 배열 (인스펙터에서 할당 필요 없게 SO에서 가져오거나 여기서 직접 관리)
    // 여기서는 SO에 있는 countSprites 배열을 0~9 순서대로 채워져 있다고 가정하고 사용합니다.
    private Sprite[] numberSprites;

    public void Initialize(HealingItem_SO data, GameObject user)
    {
        this.itemData = data;
        this.train = user.GetComponent<Train>();

        if (train == null) Debug.LogError("[HealingItem] Train을 찾을 수 없습니다.");

        // 초기화: 숫자는 켜고, 하트는 끈다.
        SetNumberVisible(true);
        if (heartObject != null) heartObject.SetActive(false);
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int levelIdx = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.maxSpeedBonusByLevel.Length - 1);

        this.healPercent = itemData.healPercentByLevel[levelIdx];
        this.targetKillCount = itemData.killCountCondition[levelIdx];

        // ✨ SO의 countSprites 배열이 0, 1, 2... 9 순서로 들어있다고 가정
        this.numberSprites = itemData.countSprites;

        // 최대 속도 증가 로직
        float currentBonus = itemData.maxSpeedBonusByLevel[levelIdx];
        float prevBonus = (levelIdx > 0) ? itemData.maxSpeedBonusByLevel[levelIdx - 1] : 0f;
        float increaseAmount = currentBonus - prevBonus;

        if (train != null && increaseAmount > 0f)
        {
            train.IncreaseMaxSpeed(increaseAmount);
            train.ModifySpeed(increaseAmount);
        }

        UpdateVisual();
    }

    public void OnEnemyKilled()
    {
        currentKillCount++;

        if (currentKillCount >= targetKillCount)
        {
            TriggerHeal();
            currentKillCount = 0;
        }

        if (!isAnimating)
        {
            UpdateVisual();
        }
    }

    private void TriggerHeal()
    {
        if (train != null)
        {
            train.HealPercent(this.healPercent);
        }

        StartCoroutine(PlayHealAnimation());
    }

    private IEnumerator PlayHealAnimation()
    {
        isAnimating = true;

        // 숫자 끄기, 하트 켜기
        SetNumberVisible(false);
        if (heartObject != null) heartObject.SetActive(true);

        yield return new WaitForSeconds(heartAnimDuration);

        // 하트 끄기, 숫자 켜기
        if (heartObject != null) heartObject.SetActive(false);
        SetNumberVisible(true);

        isAnimating = false;
        UpdateVisual();
    }

    // ✨ 숫자 표시 로직 (10의 자리, 1의 자리 분리)
    private void UpdateVisual()
    {
        if (numberSprites == null || numberSprites.Length < 10) return;
        if (tensRenderer == null || unitsRenderer == null) return;

        // 남은 킬 수 계산
        int remaining = Mathf.Max(0, targetKillCount - currentKillCount);

        // 0 이하면 하트가 나오고 있을 테니 무시 (또는 00으로 표시하고 싶으면 진행)
        // 여기서는 하트 연출 중엔 숫자를 끄므로 상관없음.

        // 자릿수 분리
        int tens = remaining / 10; // 10의 자리
        int units = remaining % 10; // 1의 자리

        // ✨ 10의 자리가 0이어도 '0' 스프라이트 표시 (요청사항 반영)
        // 만약 10의 자리가 0일 때 숨기고 싶다면 if(tens == 0) tensRenderer.enabled = false; 처리

        // 스프라이트 할당 (배열 인덱스 보호)
        tensRenderer.sprite = numberSprites[Mathf.Clamp(tens, 0, 9)];
        unitsRenderer.sprite = numberSprites[Mathf.Clamp(units, 0, 9)];
    }

    // 숫자 렌더러들의 켜짐/꺼짐을 한 번에 제어하는 헬퍼 함수
    private void SetNumberVisible(bool isVisible)
    {
        if (tensRenderer != null) tensRenderer.gameObject.SetActive(isVisible);
        if (unitsRenderer != null) unitsRenderer.gameObject.SetActive(isVisible);
    }
}