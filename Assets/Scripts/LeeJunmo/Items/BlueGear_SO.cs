using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlueGear", menuName = "Items/BlueGear")]
public class BlueGear_SO : Item_SO
{
    [Header("푸른 톱니바퀴 데이터")]
    [Tooltip("레벨별 공격속도 증가량 (%)")]
    public float[] AttackSpeedByLevel = { 10f, 20f, 30f };

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject BlueGearGO = InstantiateVisual(user);

        // 장착 시 1레벨 효과 적용
        ApplyStats(user, AttackSpeedByLevel[0] / 100f);

        if (BlueGearGO == null) return null;
        return BlueGearGO;
    }

    public override void UpgradeLevel(ItemInstance instance)
    {
        base.UpgradeLevel(instance);

        int currentLevelIdx = instance.currentUpgrade - 1;
        int prevLevelIdx = currentLevelIdx - 1;

        if (currentLevelIdx < AttackSpeedByLevel.Length && prevLevelIdx >= 0)
        {
            float difference = AttackSpeedByLevel[currentLevelIdx] - AttackSpeedByLevel[prevLevelIdx];

            // ✨ [수정] FindFirstObjectByType 사용
            Gun gun = FindFirstObjectByType<Gun>();
            if (gun != null)
            {
                gun.AddFireRateMultiplier(difference / 100f);
            }
        }
    }

    private void ApplyStats(GameObject user, float amount)
    {
        Gun gun = user.GetComponent<Gun>();
        // ✨ [수정] FindFirstObjectByType 사용
        if (gun == null) gun = FindFirstObjectByType<Gun>();

        if (gun != null)
        {
            gun.AddFireRateMultiplier(amount);
        }
        else
        {
            Debug.LogWarning("[BlueGear] Gun 컴포넌트를 찾을 수 없습니다.");
        }
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, AttackSpeedByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "AttackSpeed", AttackSpeedByLevel[index].ToString() },
        };
    }
}