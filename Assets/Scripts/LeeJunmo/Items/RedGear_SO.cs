using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RedGear", menuName = "Items/RedGear")]
public class RedGear_SO : Item_SO
{
    [Header("붉은 톱니바퀴 데이터")]
    [Tooltip("레벨별 공격력 증가량 (%)")]
    public float[] DamageByLevel = { 10f, 20f, 30f };

    internal override ItemDefinition CreateRuntimeDefinition() => new ItemDefinition(this,
        CompositionDefinition.Visual(this),
        CompositionDefinition.Stat(StatChannel.GunDamage, level => DamageByLevel[CompositionDefinition.LevelIndex(level, DamageByLevel.Length)] / 100f));

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, DamageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", DamageByLevel[index].ToString() },
        };
    }
}
