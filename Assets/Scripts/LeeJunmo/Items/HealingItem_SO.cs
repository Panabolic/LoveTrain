using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HealingItem", menuName = "Items/HealingItem")]
public class HealingItem_SO : Item_SO
{
    [Header("회복 아이템 스탯")]
    [Tooltip("각 레벨별 최대 속도 보너스 총량 (예: {0, 200, 400})")]
    public float[] maxSpeedBonusByLevel = { 0f, 200f, 400f }; // 1렙 0, 2렙 200, 3렙 400

    [Tooltip("회복 발동에 필요한 킬 수")]
    public int[] killCountCondition = { 10, 8, 6 };

    [Tooltip("최대 속도 비례 회복량 (0.1 = 10%)")]
    public float[] healPercentByLevel = { 0.1f, 0.15f, 0.2f };

    [Header("리소스")]
    public Sprite[] countSprites;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject itemObj = InstantiateVisual(user);
        if (itemObj == null) return null;

        HealingItem logic = itemObj.GetComponent<HealingItem>();
        if (logic == null) logic = itemObj.AddComponent<HealingItem>();

        logic.Initialize(this, user);
        logic.UpgradeInstItem(instance);

        return itemObj;
    }

    public override void OnKillEnemy(GameObject user, GameObject killedEnemy)
    {
        HealingItem logic = user.GetComponentInChildren<HealingItem>();
        if (logic != null) logic.OnEnemyKilled();
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, maxSpeedBonusByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "SpeedBonus", maxSpeedBonusByLevel[index].ToString() },
            { "KillReq", killCountCondition[index].ToString() },
            { "HealAmt", (healPercentByLevel[index] * 100).ToString() + "%" }
        };
    }
}