using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlueGear", menuName = "Items/BlueGear")]
public class BlueGear_SO : Item_SO
{
    [Header("푸른 톱니바퀴 데이터")]
    [Tooltip("레벨별 공격속도 증가량 (%)")]
    public float[] AttackSpeedByLevel = { 10f, 20f, 30f };

    internal override ItemDefinition CreateRuntimeDefinition() => new ItemDefinition(this,
        CompositionDefinition.Visual(this),
        CompositionDefinition.Stat(StatChannel.GunFireRate, level => AttackSpeedByLevel[CompositionDefinition.LevelIndex(level, AttackSpeedByLevel.Length)] / 100f));

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, AttackSpeedByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "AttackSpeed", AttackSpeedByLevel[index].ToString() },
        };
    }
}
