using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    public const int EquipmentSlotCount = 10;

    [Header("데이터")]
    [Tooltip("현재 소지(장착)한 아이템 인스턴스 목록")]
    public List<ItemInstance> items = new List<ItemInstance>();

    [Header("실제 기차 부착 위치 (미지정 시 기차 칸 아래 자동 생성)")]
    [Tooltip("머리 위/앞/중앙, 가운데 위/왼쪽/오른쪽, 꼬리 위/뒤/중앙, 바퀴 순서")]
    [SerializeField] private Transform[] equipmentAnchors = new Transform[EquipmentSlotCount];
    [SerializeField] private Vector3[] equipmentSlotOffsets =
    {
        new Vector3(0f, 1.3f, 0f), new Vector3(1.7f, 0.1f, 0f), new Vector3(-0.6f, 0.1f, 0f),
        new Vector3(0f, 1.3f, 0f), new Vector3(-0.9f, 0.1f, 0f), new Vector3(0.9f, 0.1f, 0f),
        new Vector3(0f, 1.3f, 0f), new Vector3(-1.7f, 0.1f, 0f), new Vector3(0.6f, 0.1f, 0f),
        new Vector3(0f, -0.75f, 0f)
    };
    private readonly Transform[] generatedAnchors = new Transform[EquipmentSlotCount];

    public event Action OnInventoryChanged;

    private void Awake() => EnsureSlotAssignments();

    private void Update()
    {
        foreach (ItemInstance instance in items)
            if (instance != null && instance.itemData != null) instance.Tick(Time.deltaTime, gameObject);
    }

    public void ProcessKillEvent(GameObject killedEnemy)
    {
        foreach (ItemInstance instance in items)
            if (instance != null && instance.itemData != null) instance.itemData.OnKillEnemy(gameObject, killedEnemy);
    }

    public void ProcessHitEvent(GameObject target, GameObject source)
    {
        foreach (ItemInstance instance in items)
            if (instance != null && instance.itemData != null) instance.itemData.OnDealDamage(gameObject, target, source, instance);
    }

    public static int AttachmentSection(Item_SO item)
    {
        if (item == null) return -1;
        if (item is BlueGear_SO || item is RedGear_SO) return 3;
        if (item.attachmentSocketName == "TrainF") return 0;
        if (item.attachmentSocketName == "TrainR") return 2;
        return 1;
    }

    public static int SlotSection(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= EquipmentSlotCount) return -1;
        return slotIndex == 9 ? 3 : slotIndex / 3;
    }

    public bool CanAcquireItem(Item_SO item)
    {
        if (item == null || !PermanentUpgradeProgress.IsItemUnlocked(item) || FindItem(item) != null) return false;
        EnsureSlotAssignments();
        for (int i = 0; i < EquipmentSlotCount; i++)
            if (item.CanEquipInSlot(i) && FindAtAssignedSlot(i) == null) return true;
        return false;
    }

    public bool CanEquipAt(Item_SO item, int slotIndex)
    {
        if (item == null || !PermanentUpgradeProgress.IsItemUnlocked(item)
            || !item.CanEquipInSlot(slotIndex) || FindItem(item) != null) return false;
        EnsureSlotAssignments();
        return FindAtAssignedSlot(slotIndex) == null;
    }

    public bool TryEquipAt(Item_SO item, int slotIndex, out ItemInstance equippedInstance)
    {
        equippedInstance = null;
        if (!CanEquipAt(item, slotIndex)) return false;

        ItemInstance instance = new ItemInstance(item) { equippedSlotIndex = slotIndex };
        items.Add(instance);
        instance.HandleEquip(gameObject);
        equippedInstance = instance;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // 기존 이벤트의 자동 획득은 첫 번째 빈 허용 위치를 사용한다.
    public void AcquireItem(Item_SO item)
    {
        for (int i = 0; i < EquipmentSlotCount; i++)
            if (TryEquipAt(item, i, out _)) return;
    }

    public ItemInstance GetItemAtSlot(int slotIndex)
    {
        if (SlotSection(slotIndex) < 0) return null;
        EnsureSlotAssignments();
        return FindAtAssignedSlot(slotIndex);
    }

    public int GetAssignedSlot(ItemInstance instance)
    {
        if (instance == null || !items.Contains(instance)) return -1;
        EnsureSlotAssignments();
        return instance.equippedSlotIndex;
    }

    public int OccupiedSlotCount(ItemInstance instance) => OccupiedSlotCount(instance != null ? instance.itemData : null);
    public int OccupiedSlotCount(Item_SO item) => item != null ? Mathf.Max(1, item.equipmentCellCount) : 0;

    public bool RemoveItemInstance(ItemInstance instance)
    {
        if (instance == null || !items.Remove(instance)) return false;
        instance.HandleUnequip(gameObject);
        instance.equippedSlotIndex = -1;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryUpgradeItemInstance(ItemInstance instance)
    {
        if (instance == null || instance.itemData == null || !items.Contains(instance)
            || instance.currentUpgrade >= instance.itemData.MaxUpgrade) return false;
        instance.UpgradeLevel();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void UpgradeItemInstance(ItemInstance instance) => TryUpgradeItemInstance(instance);

    public ItemInstance FindItem(Item_SO itemToFind)
    {
        if (itemToFind == null) return null;
        foreach (ItemInstance instance in items)
            if (instance != null && instance.itemData == itemToFind) return instance;
        return null;
    }

    public List<ItemInstance> GetUpgradableItems()
    {
        List<ItemInstance> result = new List<ItemInstance>();
        foreach (ItemInstance instance in items)
            if (instance != null && instance.itemData != null && instance.currentUpgrade < instance.itemData.MaxUpgrade)
                result.Add(instance);
        return result;
    }

    public bool IsItemMaxed(Item_SO itemToFind)
    {
        ItemInstance instance = FindItem(itemToFind);
        return instance != null && instance.currentUpgrade >= instance.itemData.MaxUpgrade;
    }

    // 이전 인스턴스의 슬롯(-1)을 배정하되 기존의 유효한 선택 슬롯을 먼저 보존한다.
    public void EnsureSlotAssignments()
    {
        int used = 0;
        foreach (ItemInstance instance in items)
        {
            if (instance == null || instance.itemData == null) continue;
            int index = instance.equippedSlotIndex;
            if (!instance.itemData.CanEquipInSlot(index) || (used & (1 << index)) != 0)
                instance.equippedSlotIndex = -1;
            else used |= 1 << index;
        }
        foreach (ItemInstance instance in items)
        {
            if (instance == null || instance.itemData == null || instance.equippedSlotIndex >= 0) continue;
            for (int i = 0; i < EquipmentSlotCount; i++)
            {
                if ((used & (1 << i)) != 0 || !instance.itemData.CanEquipInSlot(i)) continue;
                instance.equippedSlotIndex = i;
                used |= 1 << i;
                break;
            }
        }
    }

    public Transform GetEquipmentAnchor(int slotIndex)
    {
        if (SlotSection(slotIndex) < 0) return null;
        if (equipmentAnchors != null && slotIndex < equipmentAnchors.Length && equipmentAnchors[slotIndex] != null)
            return equipmentAnchors[slotIndex];
        if (generatedAnchors[slotIndex] != null) return generatedAnchors[slotIndex];

        int section = SlotSection(slotIndex);
        string carName = section == 0 ? "TrainF" : section == 2 ? "TrainR" : "TrainM";
        Transform parentCar = FindDescendant(transform, carName);
        if (parentCar == null) return null;
        Transform anchor = new GameObject("EquipmentSlot_" + slotIndex).transform;
        anchor.SetParent(parentCar, false);
        if (equipmentSlotOffsets != null && slotIndex < equipmentSlotOffsets.Length)
            anchor.localPosition = equipmentSlotOffsets[slotIndex];
        generatedAnchors[slotIndex] = anchor;
        return anchor;
    }

    private ItemInstance FindAtAssignedSlot(int slotIndex)
    {
        foreach (ItemInstance instance in items)
            if (instance != null && instance.itemData != null && instance.equippedSlotIndex == slotIndex) return instance;
        return null;
    }

    private static Transform FindDescendant(Transform parent, string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName) return child;
            Transform found = FindDescendant(child, objectName);
            if (found != null) return found;
        }
        return null;
    }
}
