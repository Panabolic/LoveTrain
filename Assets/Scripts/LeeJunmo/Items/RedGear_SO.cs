using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RedGear", menuName = "Items/RedGear")]
public class RedGear_SO : Item_SO
{
    [Header("붉은 톱니바퀴 데이터")]
    [Tooltip("레벨별 공격력 증가량 (%)")]
    public float[] DamageByLevel = { 10f, 20f, 30f };

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject RedGear = InstantiateVisual(user);

        // 장착 시 1레벨 효과 적용
        ApplyStats(user, DamageByLevel[0] / 100f);

        if (RedGear == null) return null;
        return RedGear;
    }

    public override void UpgradeLevel(ItemInstance instance)
    {
        base.UpgradeLevel(instance);

        int currentLevelIdx = instance.currentUpgrade - 1;
        int prevLevelIdx = currentLevelIdx - 1;

        if (currentLevelIdx < DamageByLevel.Length && prevLevelIdx >= 0)
        {
            float difference = DamageByLevel[currentLevelIdx] - DamageByLevel[prevLevelIdx];

            // ✨ [수정] FindFirstObjectByType 사용
            Gun gun = FindFirstObjectByType<Gun>();
            if (gun != null)
            {
                gun.AddDamageMultiplier(difference / 100f);
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
            gun.AddDamageMultiplier(amount);
        }
        else
        {
            Debug.LogWarning("[RedGear] Gun 컴포넌트를 찾을 수 없습니다.");
        }
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, DamageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", DamageByLevel[index].ToString() },
        };
    }
}