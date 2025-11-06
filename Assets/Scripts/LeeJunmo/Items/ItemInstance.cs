// ItemInstance.cs
using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public Item_SO itemData;
    public int stackCount;
    public float currentCooldown;

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
        currentUpgrade++;
        // (최대 레벨 제한 로직이 필요할 수 있음)

        // 쿨타임이 즉시 반영되도록 갱신 (기획에 따라 다름)
        // 예: 7초로 줄어들었다면, 남은 쿨타임도 7초를 넘지 않게 보정
        float newMaxCooldown = itemData.GetCooldownForLevel(currentUpgrade);
        if (currentCooldown > newMaxCooldown)
        {
            currentCooldown = newMaxCooldown;
        }
    }
}