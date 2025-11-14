// Inventory.cs (이전의 EquipmentManager.cs)
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // 소지(장착)한 아이템 인스턴스 목록
    public List<ItemInstance> items;

    /// <summary>
    /// UI가 구독(연동)할 수 있는 "인벤토리 변경 알림" 이벤트
    /// </summary>
    public event Action OnInventoryChanged;

    // (Update()에서 items의 Tick()을 돌려주는 로직...)
    void Update()
    {
        foreach (ItemInstance instance in items)
        {
            // 'user'는 이 Inventory 컴포넌트가 붙어있는 
            // 플레이어 GameObject를 의미합니다.
            instance.Tick(Time.deltaTime, this.gameObject);
        }
    }

    /// <summary>
    /// 새 아이템을 획득(또는 업그레이드)합니다.
    /// </summary>
    public void AcquireItem(Item_SO newItemSO)
    {
        // 1. ItemSO 참조로 비교
        foreach (ItemInstance instance in items)
        {
            if (instance.itemData == newItemSO)
            {
                // 2. 업그레이드
                instance.UpgradeLevel();
                Debug.Log($"{newItemSO.itemName} 업그레이드! (현재 레벨: {instance.currentUpgrade})");

                OnInventoryChanged?.Invoke();

                return;
            }
        }

        // 3. 신규 아이템 추가
        ItemInstance newInstance = new ItemInstance(newItemSO);
        items.Add(newInstance);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// [추가] UI가 아이템을 찾기 위한 헬퍼 함수
    /// </summary>
    public ItemInstance FindItem(Item_SO itemToFind)
    {
        foreach (ItemInstance instance in items)
        {
            if (instance.itemData == itemToFind)
            {
                return instance; // 찾았음
            }
        }
        return null; // 못 찾았음
    }

    // (BroadcastOnTakeDamage, BroadcastOnKillEnemy 등...)
}