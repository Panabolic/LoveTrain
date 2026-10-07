using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BeatingHeart", menuName = "Items/BeatingHeart")]
public class BeatingHeart_SO : Item_SO
{
    [Header("박동하는 심장 전용 데이터")]
    public int[] damageByLevel = { 200, 200, 500 };
    public float[] cooldownByLevel = { 10f, 7f, 7f };

    [Header("시각 효과")]
    public GameObject EffectPrefab;

    /// <summary>
    /// [추가] '박동하는 심장'도 비주얼이 있으므로 OnEquip 재정의
    /// </summary>

    /// <summary>
    /// 이 아이템의 현재 레벨에 맞는 쿨타임을 Inventory에게 알려줍니다.
    /// </summary>
    internal override ItemDefinition CreateRuntimeDefinition() => new ItemDefinition(this,
        CompositionDefinition.Visual(this), CompositionDefinition.Heart(this));

    public override float GetCooldownForLevel(int level)
    {
        // 배열 범위를 벗어나지 않도록 Clamp (안전장치)
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    /// <summary>
    /// 쿨타임이 완료될 때마다 Inventory에 의해 호출됩니다.
    /// </summary>

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "CoolTime", cooldownByLevel[index].ToString() }
        };
    }

}
