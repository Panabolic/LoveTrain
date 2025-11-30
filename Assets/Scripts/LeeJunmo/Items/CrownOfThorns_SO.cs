using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CrownOfThorns", menuName = "Items/CrownOfThorns")]
public class CrownOfThorns_SO : Item_SO
{
    [Header("면류관 스탯")]
    public float[] damageByLevel = { 100f, 150f, 200f };
    public int[] countByLevel = { 3, 5, 7 };        // 번개 개수
    public float[] cooldownByLevel = { 10f, 9f, 8f };

    [Header("생성 설정")]
    [Tooltip("X축 생성 범위 (예: 8 = -8 ~ +8 사이 랜덤)")]
    public float spawnRangeX = 8.0f;

    [Tooltip("번개 사이의 최소 딜레이")]
    public float spawnDelayMin = 0.1f;
    [Tooltip("번개 사이의 최대 딜레이")]
    public float spawnDelayMax = 0.3f;

    [Header("프리팹")]
    public GameObject LightningPrefab;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject obj = InstantiateVisual(user);
        if (obj == null) return null;

        CrownOfThorns logic = obj.GetComponent<CrownOfThorns>();
        if (logic == null) logic = obj.AddComponent<CrownOfThorns>();

        logic.Initialize(this);
        logic.UpgradeInstItem(instance);

        return obj;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "Count", countByLevel[index].ToString() },
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }
}