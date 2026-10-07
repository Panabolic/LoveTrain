using UnityEngine;
using System.Collections;

public class HealingItem : MonoBehaviour, IInstantiatedItem
{
    private HealingItem_SO itemData;
    private ItemInstance runtimeInstance;

    [Header("자식 오브젝트 연결")]
    [Tooltip("10의 자리 숫자를 보여줄 SpriteRenderer")]
    [SerializeField] private SpriteRenderer tensRenderer;

    [Tooltip("1의 자리 숫자를 보여줄 SpriteRenderer")]
    [SerializeField] private SpriteRenderer unitsRenderer;

    [Tooltip("회복 시 잠깐 켜질 하트 애니메이션 자식 오브젝트")]
    [SerializeField] private GameObject heartObject;

    [Tooltip("하트 애니메이션 재생 시간 (초)")]
    [SerializeField] private float heartAnimDuration = 1.5f;

    private bool isAnimating = false;

    // ✨ 0~9 숫자 스프라이트 배열 (인스펙터에서 할당 필요 없게 SO에서 가져오거나 여기서 직접 관리)
    // 여기서는 SO에 있는 countSprites 배열을 0~9 순서대로 채워져 있다고 가정하고 사용합니다.
    private Sprite[] numberSprites;

    public void Initialize(HealingItem_SO data, GameObject user)
    {
        itemData = data;
        if (user.GetComponent<Train>() == null) Debug.LogError("[HealingItem] Train을 찾을 수 없습니다.");
        SetNumberVisible(true);
        if (heartObject != null) heartObject.SetActive(false);
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        runtimeInstance = instance;
        numberSprites = itemData.countSprites;
        UpdateVisual();
    }

    public void RemoveEquipmentStats() => runtimeInstance?.GetEffect<StatModifier>()?.Release();

    public void OnEnemyKilled() => runtimeInstance?.GetEffect<CombatProc>()?.Kill(null);

    internal void RefreshCounter() { if (!isAnimating) UpdateVisual(); }
    internal void PlayHealPresentation() => StartCoroutine(PlayHealAnimation());

    private IEnumerator PlayHealAnimation()
    {
        isAnimating = true;

        // 숫자 끄기, 하트 켜기
        SetNumberVisible(false);
        if (heartObject != null) heartObject.SetActive(true);

        SoundEventBus.Publish(SoundID.Item_HealingItem);
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
        int remaining = runtimeInstance != null ? runtimeInstance.GetEffect<CombatProc>()?.RemainingKills ?? 0 : 0;

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
