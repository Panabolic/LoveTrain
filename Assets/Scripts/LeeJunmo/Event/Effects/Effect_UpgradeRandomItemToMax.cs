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
        if (inventory == null) return EnglishLocalization.Get("result.error.inventory_missing", "오류: Inventory를 찾을 수 없습니다.");

        List<ItemInstance> upgradableItems = inventory.GetUpgradableItems();
        if (upgradableItems.Count == 0) return EnglishLocalization.Get("result.no_upgrades", "업그레이드할 아이템이 없습니다.");

        System.Random rng = new System.Random();
        List<ItemInstance> itemsToUpgrade = upgradableItems.OrderBy(x => rng.Next()).Take(itemCount).ToList();

        List<string> results = new List<string>();
        foreach (ItemInstance instance in itemsToUpgrade)
        {
            int oldLevel = instance.currentUpgrade;

            // [실행] 최대 레벨이 될 때까지 반복
            while (instance.currentUpgrade < instance.itemData.MaxUpgrade)
            {
                // [수정]
                inventory.UpgradeItemInstance(instance);
            }

            results.Add($"<{instance.itemData.LocalizedName}> (Lv.{oldLevel} → MAX)");
        }

        return EnglishLocalization.Get("result.upgrade_max", "아이템 최대 레벨 업그레이드:\n- ") + string.Join("\n- ", results);
    }
}