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

        if (!inventory.CanAcquireItem(itemToGive))
            return $"<{itemToGive.LocalizedName}>: already owned or no attachment slot available.";
        inventory.AcquireItem(itemToGive);
        return EnglishLocalization.Format("result.item_acquired", "새로운 아이템 <{0}>(을)를 획득했습니다.", itemToGive.LocalizedName);

    }

    /// <summary>
    /// [추가] "랜덤 아이템 획득" 로직
    /// (이 부분은 ItemDatabase에 접근하는 방식에 따라 추가 구현이 필요합니다)
    /// </summary>
    private string ExecuteRandomAcquire(GameObject target, EffectParameters parameters)
    {
        int acquireCount = parameters.intValue;
        if (acquireCount <= 0) acquireCount = 1;

        var wallet = target.GetComponent<TrainLevelManager>();
        if (wallet == null) return "Flesh reward unavailable.";
        int reward = 50 * acquireCount;
        wallet.AddFlesh(reward);
        return $"Flesh +{reward}";
    }
}
