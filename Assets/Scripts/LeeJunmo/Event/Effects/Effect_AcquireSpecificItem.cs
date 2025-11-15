using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AcquireSpecificItem", menuName = "Event System/Effects/Acquire Specific Item")]
public class Effect_AcquireSpecificItem : GameEffectSO
{
    public override string Execute(GameObject target, EffectParameters parameters)
    {
        // 1. 'parameters'에서 데이터를 꺼내 씀
        Item_SO itemToGive = parameters.soReference as Item_SO;
        int acquireCount = parameters.intValue;

        // 2. (기본값 설정)
        if (acquireCount <= 0) acquireCount = 1;
        if (itemToGive == null) return "오류: 획득할 아이템(soReference)이 지정되지 않았습니다.";

        Inventory inventory = target.GetComponent<Inventory>();
        if (inventory == null) return "오류: Inventory를 찾을 수 없습니다.";

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
            if (isNewItem && i == 0)
            {
                inventory.AcquireItem(itemToGive);
                instance = inventory.FindItem(itemToGive); // 인스턴스 참조 갱신
            }
            else
            {
                if (instance.currentUpgrade >= instance.itemData.MaxUpgrade) break;
                instance.UpgradeLevel();
            }
        }

        // 6. 최종 결과 텍스트 반환
        string levelText = (instance.currentUpgrade >= instance.itemData.MaxUpgrade) ? "MAX" : $"Lv.{instance.currentUpgrade}";

        if (isNewItem)
        {
            if (acquireCount > 1) // "NEW → N"
                return $"<{itemToGive.itemName}>(이)가 (NEW → {levelText})로 업그레이드되었습니다.";
            else // "NEW" (acquireCount가 1이었음)
                return $"새로운 아이템 <{itemToGive.itemName}>(을)를 획득했습니다.";
        }
        else // "Lv.N → Lv.M"
        {
            return $"<{itemToGive.itemName}>(이)가 (Lv.{oldLevel} → {levelText})로 업그레이드되었습니다.";
        }
    }
}