// ItemInstance.cs
using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public Item_SO itemData;
    public int stackCount;
    public float currentCooldown;
    private GameObject instantiatedObject = null; // 실체화된 오브젝트 참조

    // --- 업그레이드를 위한 새 변수 ---
    public int currentUpgrade = 1; // 획득 시 기본 1레벨

    public ItemInstance(Item_SO data)
    {
        itemData = data;
        stackCount = 1;
        currentUpgrade = 1;

        // 생성 시, 1레벨에 맞는 쿨타임으로 초기화
        currentCooldown = itemData.GetCooldownForLevel(currentUpgrade);
    }

    public void HandleEquip(GameObject user)
    {
        // SO의 OnEquip을 호출하고, 그 결과를 저장
        instantiatedObject = itemData.OnEquip(user, this);
    }

    public void Tick(float deltaTime, GameObject user)
    {
        float maxCooldown = itemData.GetCooldownForLevel(currentUpgrade);

        if (maxCooldown > 0f) // 쿨타임이 있는 아이템만
        {
            currentCooldown -= deltaTime;
            if (currentCooldown <= 0f)
            {
                // 훅 호출 시 'this'(ItemInstance 자신)를 넘겨줍니다.
                itemData.OnCooldownComplete(user, this);

                // 쿨타임을 현재 레벨에 맞게 다시 초기화
                currentCooldown = maxCooldown;
            }
        }
    }

    /// <summary>
    /// 아이템을 업그레이드할 때 외부에서 호출할 함수
    /// </summary>
    public void UpgradeLevel()
    {
        // SO의 업그레이드 로직을 호출하여 책임을 위임
        itemData.UpgradeLevel(this);
    }

    public void instantiatedItemUpgrade()
    {
        if (instantiatedObject != null)
        {
            // 4. 실체화된 아이템의 인터페이스 함수를 찾아 호출
            IInstantiatedItem Institem = instantiatedObject.GetComponent<IInstantiatedItem>();

            // (다음 단계에서 정의할 함수)
            Institem?.UpgradeInstItem(this);
        }
    }

}