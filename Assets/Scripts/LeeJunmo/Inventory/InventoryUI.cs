using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("데이터 소스")]
    [SerializeField] private Inventory inventoryData; // 플레이어의 'Inventory' 컴포넌트

    [Header("UI 슬롯 레이아웃")]
    [SerializeField] private Transform layoutGroup1; // 1~8번 슬롯의 부모
    [SerializeField] private Transform layoutGroup2; // 9~16번 슬롯의 부모

    [Header("기차 부착 위치 UI (선택)")]
    [SerializeField] private bool useTrainLayout;
    [SerializeField] private InventorySlotUI[] headSlots = new InventorySlotUI[0];
    [SerializeField] private InventorySlotUI[] middleSlots = new InventorySlotUI[0];
    [SerializeField] private InventorySlotUI[] tailSlots = new InventorySlotUI[0];
    [SerializeField] private InventorySlotUI[] wheelSlots = new InventorySlotUI[0];
    [SerializeField] private Item_SO[] wheelItems = new Item_SO[0];
    [SerializeField] private TMPro.TMP_Text hiddenItemCountText;

    // 관리할 모든 UI 슬롯 리스트
    private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();

    void Start()
    {
        // 1. 16개의 슬롯을 자동으로 찾아 리스트에 추가
        if (!useTrainLayout)
        {
            if (layoutGroup1 != null) uiSlots.AddRange(layoutGroup1.GetComponentsInChildren<InventorySlotUI>());
            if (layoutGroup2 != null) uiSlots.AddRange(layoutGroup2.GetComponentsInChildren<InventorySlotUI>());
        }

        // 2. [핵심 연동] Inventory(데이터)의 '상태'가 바뀔 때마다,
        //    'RefreshUI' 함수를 '자동으로' 호출하도록 구독(연결)합니다.
        if (inventoryData != null)
        {
            inventoryData.OnInventoryChanged += RefreshUI;
        }

        // 3. 게임 시작 시 UI를 즉시 한 번 갱신
        RefreshUI();
    }

    void OnDestroy()
    {
        // (메모리 누수 방지) 이 UI가 파괴될 때 구독 해제
        if (inventoryData != null)
        {
            inventoryData.OnInventoryChanged -= RefreshUI;
        }
    }

    /// <summary>
    /// Inventory 데이터가 변경될 때마다 호출되는 함수
    /// </summary>
    private void RefreshUI()
    {
        if (useTrainLayout)
        {
            RefreshTrainLayout();
            return;
        }

        // 1. 16개의 모든 UI 슬롯을 순회합니다.
        for (int i = 0; i < uiSlots.Count; i++)
        {
            // 2. Inventory(데이터)에 해당 아이템이 '있는지' 확인합니다.
            if (inventoryData != null && i < inventoryData.items.Count)
            {
                // 3. 데이터가 있으면: 슬롯에 해당 아이템 정보를 넘겨 갱신
                uiSlots[i].UpdateSlot(inventoryData.items[i]);
            }
            else
            {
                // 4. 데이터가 없으면: 슬롯을 '비움' (null)
                uiSlots[i].UpdateSlot(null);
            }
        }
    }

    private void RefreshTrainLayout()
    {
        ClearSlots(headSlots);
        ClearSlots(middleSlots);
        ClearSlots(tailSlots);
        ClearSlots(wheelSlots);

        int head = 0, middle = 0, tail = 0, wheel = 0, hidden = 0;
        if (inventoryData != null)
        {
            foreach (ItemInstance item in inventoryData.items)
            {
                if (item == null || item.itemData == null) continue;

                bool shown;
                if (IsWheelItem(item.itemData))
                    shown = BindNextSlot(wheelSlots, ref wheel, item);
                else if (item.itemData.attachmentSocketName == "TrainF")
                    shown = BindNextSlot(headSlots, ref head, item);
                else if (item.itemData.attachmentSocketName == "TrainR")
                    shown = BindNextSlot(tailSlots, ref tail, item);
                else
                    shown = BindNextSlot(middleSlots, ref middle, item);

                // Presentation capacity must not delete or reject equipped items.
                if (!shown) hidden++;
            }
        }

        if (hiddenItemCountText != null)
        {
            hiddenItemCountText.text = hidden > 0 ? "+" + hidden : string.Empty;
            hiddenItemCountText.gameObject.SetActive(hidden > 0);
        }
    }

    private bool IsWheelItem(Item_SO item)
    {
        if (wheelItems == null) return false;
        for (int i = 0; i < wheelItems.Length; i++)
            if (wheelItems[i] == item) return true;
        return false;
    }

    private static void ClearSlots(InventorySlotUI[] slots)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) slots[i].UpdateSlot(null);
    }

    private static bool BindNextSlot(InventorySlotUI[] slots, ref int index, ItemInstance item)
    {
        if (slots == null) return false;
        while (index < slots.Length)
        {
            InventorySlotUI slot = slots[index++];
            if (slot == null) continue;
            slot.UpdateSlot(item);
            return true;
        }
        return false;
    }
}
