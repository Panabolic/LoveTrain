using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlueGear", menuName = "Items/BlueGear")]
public class BlueGear_SO : Item_SO
{
    [Header("푸른 톱니바퀴 데이터")]
    public float[] AttackSpeedByLevel = { 10f, 20f, 30f };

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 1. 부모의 공통 함수를 호출해 '로직+시각' 프리팹 생성
        GameObject BlueGearGO = InstantiateVisual(user);
        if (BlueGearGO == null) return null;
        

        return BlueGearGO;
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
