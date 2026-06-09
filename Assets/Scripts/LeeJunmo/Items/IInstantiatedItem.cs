// IInstantiatedItemLogic.cs

/// <summary>
/// 씬에 '실체화(Instantiate)'되는 아이템의 MonoBehaviour가
/// ItemInstance로부터 업그레이드/상태 갱신 신호를 받기 위해 구현하는 인터페이스입니다.
/// </summary>
public interface IInstantiatedItem
{
    /// <summary>
    /// ItemInstance에 의해 호출되며,
    /// 현재 레벨과 SO 데이터를 바탕으로 스탯을 갱신합니다.
    /// </summary>
    void UpgradeInstItem(ItemInstance instance);
}

/// <summary>
/// UI에서 아이템의 현재 쿨타임 표시 상태를 읽기 위한 계약입니다.
/// </summary>
public interface IItemCooldownView
{
    bool HasCooldown { get; }
    float GetCooldownFillAmount();
}

public static class ItemCooldownFill
{
    public static float FromRemaining(float remainingCooldown, float cooldownDuration)
    {
        if (cooldownDuration <= 0f)
        {
            return 0f;
        }

        if (remainingCooldown <= 0f)
        {
            return 0f;
        }

        return UnityEngine.Mathf.Clamp01(remainingCooldown / cooldownDuration);
    }
}
