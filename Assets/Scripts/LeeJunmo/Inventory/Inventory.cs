using UnityEngine;
using System.Collections.Generic;
using System; // Action (이벤트)을 사용하기 위해 필요

public class Inventory : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("현재 소지(장착)한 아이템 인스턴스 목록")]
    public List<ItemInstance> items = new List<ItemInstance>();

    [Tooltip("아이템 획득/업그레이드 시 UI가 갱신되도록 알리는 이벤트")]
    public event Action OnInventoryChanged;

    /// <summary>
    // 매 프레임 모든 아이템의 '패시브' 쿨타임을 갱신합니다.
    /// </summary>
    void Update()
    {
        // 'this.gameObject'는 플레이어 자신을 의미합니다.
        foreach (ItemInstance instance in items)
        {
            instance.Tick(Time.deltaTime, this.gameObject);
        }
    }

    /// <summary>
    /// 적 처치 이벤트를 처리하고, 모든 장착 아이템의 OnKillEnemy 훅을 호출합니다.
    /// 이 함수는 Enemy.cs의 Die()에서 호출됩니다.
    /// </summary>
    public void ProcessKillEvent(GameObject killedEnemy)
    {
        // 'this.gameObject'는 플레이어(Train) 자신입니다.
        foreach (ItemInstance instance in items)
        {
            // Item_SO에 정의된 OnKillEnemy 훅 호출
            instance.itemData.OnKillEnemy(this.gameObject, killedEnemy);
        }
    }


    /// <summary>
    /// 새 아이템을 획득(또는 업그레이드)합니다.
    /// UI 갱신 이벤트를 호출합니다.
    /// </summary>
    public void AcquireItem(Item_SO newItemSO)
    {
        // 1. 이미 가진 아이템인지 SO 참조로 비교
        foreach (ItemInstance instance in items)
        {
            if (instance.itemData == newItemSO)
            {
                // 2. 이미 있으면 업그레이드 요청
                // (최대 레벨 체크는 ItemInstance 또는 Item_SO의 로직이 담당)
                instance.UpgradeLevel();

                // 3. UI 갱신 알림
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        // 4. 없으면 신규 아이템으로 추가
        ItemInstance newInstance = new ItemInstance(newItemSO);
        items.Add(newInstance);

        // 5. 아이템 장착(실체화) 로직 실행
        newInstance.HandleEquip(this.gameObject);

        // 6. UI 갱신 알림
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// [추가] 특정 아이템 인스턴스를 1레벨 업그레이드하고 UI를 갱신합니다.
    /// (이벤트 시스템의 Upgrade 효과들이 이 함수를 사용)
    /// </summary>
    public void UpgradeItemInstance(ItemInstance instance)
    {
        if (instance == null) return;

        // 1. 실제 업그레이드 로직 실행
        // (최대 레벨 체크는 호출하는 쪽에서 하거나 여기서 추가로 해도 됨)
        if (instance.currentUpgrade < instance.itemData.MaxUpgrade)
        {
            instance.UpgradeLevel(); // 내부 로직(스탯 갱신 등) 실행

            // 2. [핵심] UI 갱신 알림
            OnInventoryChanged?.Invoke();
        }
    }


    // --- 헬퍼 함수 (UI 및 이벤트 시스템용) ---

    /// <summary>
    /// 인벤토리에서 특정 아이템(SO)에 해당하는 인스턴스를 찾습니다.
    /// </summary>
    /// <returns>찾은 ItemInstance, 없으면 null</returns>
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

    /// <summary>
    /// 현재 소지한 아이템 중 '최대 레벨'이 아닌 아이템 목록을 반환합니다.
    /// (이벤트 시스템의 '랜덤 업그레이드' 효과가 사용)
    /// </summary>
    public List<ItemInstance> GetUpgradableItems()
    {
        List<ItemInstance> upgradableList = new List<ItemInstance>();
        foreach (ItemInstance instance in items)
        {
            if (instance.currentUpgrade < instance.itemData.MaxUpgrade)
            {
                upgradableList.Add(instance);
            }
        }
        return upgradableList;
    }

    /// <summary>
    /// 특정 아이템 SO가 현재 인벤토리에서 최대 레벨인지 확인합니다.
    /// (레벨 업 UI, 이벤트 시스템에서 사용)
    /// </summary>
    public bool IsItemMaxed(Item_SO itemToFind)
    {
        ItemInstance instance = FindItem(itemToFind);
        if (instance == null) return false; // 아직 없으므로 Max가 아님

        return instance.currentUpgrade >= instance.itemData.MaxUpgrade;
    }
}