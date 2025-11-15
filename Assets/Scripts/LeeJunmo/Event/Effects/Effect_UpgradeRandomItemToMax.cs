using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Effect_UpgradeRandomItemToMax", menuName = "Event System/Effects/Upgrade Random Item To Max")]
public class Effect_UpgradeRandomItemToMax : GameEffectSO
{
    public override string Execute(GameObject target, EffectParameters parameters)
    {
        int itemCount = parameters.intValue; // 몇 개의 아이템을 뽑을지
        if (itemCount <= 0) itemCount = 1;

        Inventory inventory = target.GetComponent<Inventory>();
        if (inventory == null) return "오류: Inventory를 찾을 수 없습니다.";

        List<ItemInstance> upgradableItems = inventory.GetUpgradableItems();
        if (upgradableItems.Count == 0) return "업그레이드할 아이템이 없습니다.";

        System.Random rng = new System.Random();
        List<ItemInstance> itemsToUpgrade = upgradableItems.OrderBy(x => rng.Next()).Take(itemCount).ToList();

        List<string> results = new List<string>();
        foreach (ItemInstance instance in itemsToUpgrade)
        {
            int oldLevel = instance.currentUpgrade;

            // [실행] 최대 레벨이 될 때까지 반복
            while (instance.currentUpgrade < instance.itemData.MaxUpgrade)
            {
                instance.UpgradeLevel();
            }

            results.Add($"<{instance.itemData.itemName}> (Lv.{oldLevel} → MAX)");
        }

        return $"아이템 최대 레벨 업그레이드:\n- " + string.Join("\n- ", results);
    }
}