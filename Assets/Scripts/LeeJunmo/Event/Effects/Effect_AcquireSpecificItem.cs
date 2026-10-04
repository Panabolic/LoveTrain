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
        if (itemToGive == null) return EnglishLocalization.Get("result.error.item_missing", "오류: 획득할 아이템(soReference)이 지정되지 않았습니다.");

        Inventory inventory = target.GetComponent<Inventory>();
        if (inventory == null) return EnglishLocalization.Get("result.error.inventory_missing", "오류: Inventory를 찾을 수 없습니다.");

        if (!inventory.CanAcquireItem(itemToGive))
            return $"<{itemToGive.LocalizedName}>: already owned or no attachment slot available.";
        inventory.AcquireItem(itemToGive);
        return EnglishLocalization.Format("result.item_acquired", "새로운 아이템 <{0}>(을)를 획득했습니다.", itemToGive.LocalizedName);

    }
}