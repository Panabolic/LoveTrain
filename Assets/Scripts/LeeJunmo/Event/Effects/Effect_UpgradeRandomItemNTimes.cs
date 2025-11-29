// Effect_UpgradeRandomItemNTimes.cs (10번과 11번이 통합된 버전)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Effect_UpgradeRandomItemNTimes", menuName = "Event System/Effects/Upgrade Random Item (N Times)")]
public class Effect_UpgradeRandomItemNTimes : GameEffectSO
{
    public override string Execute(GameObject target, EffectParameters parameters)
    {
        // intValue = 몇 개의 아이템을 뽑을지
        int itemCount = parameters.intValue;
        // intValue2 = 뽑힌 아이템을 몇 레벨 업그레이드할지
        int upgradeAmount = parameters.intValue2;

        if (itemCount <= 0) itemCount = 1;
        if (upgradeAmount <= 0) upgradeAmount = 1; // [중요] 0으로 설정하면 1로 보정

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

            // [실행] N번 반복 업그레이드
            for (int i = 0; i < upgradeAmount; i++)
            {
                if (instance.currentUpgrade >= instance.itemData.MaxUpgrade) break;
                // [수정]
                inventory.UpgradeItemInstance(instance);
            }

            string levelText = (instance.currentUpgrade >= instance.itemData.MaxUpgrade) ? "MAX" : $"Lv.{instance.currentUpgrade}";
            results.Add($"<{instance.itemData.itemName}> (Lv.{oldLevel} → {levelText})");
        }

        return $"아이템 {upgradeAmount}회 업그레이드:\n- " + string.Join("\n- ", results);
    }
}