using UnityEngine;
using System.Collections.Generic;
using System.Linq;
    
[CreateAssetMenu(fileName = "Effect_AcquireRandomItem", menuName = "Event System/Effects/Acquire Random Item")]
public class Effect_AcquireRandomItem : GameEffectSO
{
    [Header("이 로직에 필요한 에셋")]
    [Tooltip("전체 아이템 목록이 담긴 ItemDatabase 에셋")]
    public ItemDatabase itemDatabase; // (이 '로직 템플릿 에셋'에 연결 필요!)

    public override string Execute(GameObject target, EffectParameters parameters)
    {
        int acquireCount = parameters.intValue;
        if (acquireCount <= 0) acquireCount = 1;

        Inventory inventory = target.GetComponent<Inventory>();
        if (inventory == null || itemDatabase == null) return "오류: Inventory 또는 ItemDatabase가 없습니다.";

        // 1. '획득 가능한' 아이템 풀을 필터링 (최대 레벨 아이템 제외)
        List<Item_SO> availablePool = new List<Item_SO>();
        foreach (Item_SO item in itemDatabase.allItems)
        {
            if (!inventory.IsItemMaxed(item)) // (Inventory에 IsItemMaxed 헬퍼 함수 필요)
            {
                availablePool.Add(item);
            }
        }

        if (availablePool.Count == 0)
        {
            return "획득할 수 있는 새로운 아이템이 없습니다.";
        }

        // 2. 풀에서 N개 뽑기
        System.Random rng = new System.Random();
        List<Item_SO> choices = availablePool.OrderBy(x => rng.Next()).Take(acquireCount).ToList();

        // 3. 텍스트 조합 및 아이템 획득
        List<string> results = new List<string>();
        foreach (Item_SO item in choices)
        {
            bool isNew = (inventory.FindItem(item) == null);
            inventory.AcquireItem(item);

            if (isNew)
                results.Add($"<{item.itemName}> (NEW)");
            else
                results.Add($"<{item.itemName}> (UPGRADE)");
        }

        return $"랜덤 아이템 획득:\n- " + string.Join("\n- ", results);
    }
}