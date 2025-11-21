using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RedGear", menuName = "Items/RedGear")]
public class RedGear_SO : Item_SO
{
    [Header("리볼버 전용 데이터")]
    public float[] DamageByLevel = { 10f, 20f, 30f };

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 1. 부모의 공통 함수를 호출해 '로직+시각' 프리팹 생성
        GameObject RedGear = InstantiateVisual(user);
        if (RedGear == null) return null;
        

        return RedGear;
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
