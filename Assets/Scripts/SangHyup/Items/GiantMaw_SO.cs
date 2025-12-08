using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GiantMaw", menuName = "Items/GiantMaw")]
public class GiantMaw_SO : Item_SO
{
    [Header("거대한 아가리 Specification")]
    [SerializeField] public int[]   mawDamageByLevel    = { 100, 120, 150 };
    [SerializeField] public float[] cooldownByLevel     = { 2.0f, 1.5f, 1.0f };
    [SerializeField] public int[] healAmountByLevel = { 5, 10, 20 };
    [Space(10)]
    [SerializeField] public Vector2 knockbackDirection  = new Vector2(1.0f, 0.3f);
    [SerializeField] public float   knockbackPower      = 10.0f;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject giantMawGO = InstantiateVisual(user);
        if (giantMawGO == null) return null;

        GiantMaw giantMaw = giantMawGO.GetComponent<GiantMaw>();
        giantMaw.Initialize(this, user);
        giantMaw.UpgradeInstItem(instance);

        return giantMawGO;
    }

    public int GetDamageByLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, mawDamageByLevel.Length - 1);
        return mawDamageByLevel[index];
    }

    public float GetCooldownByLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, mawDamageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", mawDamageByLevel[index].ToString() },
            { "CoolTime", cooldownByLevel[index].ToString() },
            { "Heal", healAmountByLevel[index].ToString() }
        };
    }

}
