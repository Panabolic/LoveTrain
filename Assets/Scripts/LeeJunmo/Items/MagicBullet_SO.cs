using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagicBullet", menuName = "Items/MagicBullet")]
public class MagicBullet_SO : Item_SO
{
    [Header("마탄 데이터")]
    public int[] bounceCounts = { 1, 2, 4 };

    internal override ItemDefinition CreateRuntimeDefinition() => new ItemDefinition(this,
        CompositionDefinition.Visual(this), CompositionDefinition.Ricochet(this));

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, bounceCounts.Length - 1);
        return new Dictionary<string, string> { { "Bounce", bounceCounts[idx].ToString() } };
    }
}
