using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("레벨 업 시 선택지로 나올 수 있는 모든 아이템 리스트")]
    public List<Item_SO> allItems;

    // (나중에 희귀도(Rarity)별로 리스트를 나눌 수도 있습니다)
}

