using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AcquireItem", menuName = "Event System/Effects/Acquire Item")]
public class Effect_AcquireItem : GameEffectSO
{

    public override string Execute(GameObject target, EffectParameters parameters)
    {
        // 1. 'parameters'에서 데이터를 꺼내 씀
        Item_SO itemToGive = parameters.soReference as Item_SO;
        int acquireCount = parameters.intValue;

        // 2. (기본값 설정)
        if (acquireCount <= 0) acquireCount = 1;
        if (itemToGive == null)
        {
            // soReference가 비어있으면 "랜덤 아이템 획득" 로직으로 분기
            return ExecuteRandomAcquire(target, parameters);
        }

        Inventory inventory = target.GetComponent<Inventory>();
        if (inventory == null) return null;

        // --- [핵심 수정] ---

        // 3. 로직 실행 '전'의 상태를 저장
        ItemInstance instance = inventory.FindItem(itemToGive);
        int oldLevel = (instance != null) ? instance.currentUpgrade : 0;
        bool isNewItem = (instance == null);

        // 4. (기존 아이템) 이미 최대 레벨이면 즉시 종료
        if (!isNewItem && oldLevel >= itemToGive.MaxUpgrade)
        {
            return $"<{itemToGive.itemName}>(이)가 이미 최대 레벨(MAX)입니다.";
        }

        // 5. 로직 실행 (N번 반복)
        for (int i = 0; i < acquireCount; i++)
        {
            // 5a. [신규] 첫 번째 획득
            if (isNewItem && i == 0)
            {
                inventory.AcquireItem(itemToGive); // [실행 1] (이때 1레벨이 됨)
                instance = inventory.FindItem(itemToGive); // 인스턴스 참조 갱신
            }
            else // 5b. [업그레이드] (기존 아이템 또는 신규 아이템의 2번째 획득부터)
            {
                // 최대 레벨이면 중단
                if (instance.currentUpgrade >= instance.itemData.MaxUpgrade) break;

                instance.UpgradeLevel(); // [실행 2...N]
            }
        }
        // --- [수정 끝] ---


        // 6. 최종 결과 텍스트 반환
        string levelText = (instance.currentUpgrade >= instance.itemData.MaxUpgrade) ? "MAX" : $"Lv.{instance.currentUpgrade}";

        if (isNewItem) // (oldLevel이 0이었음)
        {
            if (acquireCount > 1) // "NEW → N"
            {
                return $"<{itemToGive.itemName}>(이)가 (NEW → {levelText})로 업그레이드되었습니다.";
            }
            else // "NEW" (acquireCount가 1이었음)
            {
                return $"새로운 아이템 <{itemToGive.itemName}>(을)를 획득했습니다.";
            }
        }
        else // (oldLevel이 1 이상이었음)
        {
            // "Lv.N → Lv.M"
            return $"<{itemToGive.itemName}>(이)가 (Lv.{oldLevel} → {levelText})로 업그레이드되었습니다.";
        }
    }

    /// <summary>
    /// [추가] "랜덤 아이템 획득" 로직
    /// (이 부분은 ItemDatabase에 접근하는 방식에 따라 추가 구현이 필요합니다)
    /// </summary>
    private string ExecuteRandomAcquire(GameObject target, EffectParameters parameters)
    {
        int acquireCount = parameters.intValue;
        if (acquireCount <= 0) acquireCount = 1;

        // (ItemDatabase를 찾는 로직이 필요 - 예: Resources.Load 또는 싱글톤)
        return "랜덤 아이템 획득 로직 실행 (ItemDatabase 필요)";
    }
}