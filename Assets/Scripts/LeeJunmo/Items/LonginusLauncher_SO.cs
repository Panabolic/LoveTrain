using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LonginusLauncher", menuName = "Items/LonginusLauncher")]
public class LonginusLauncher_SO : Item_SO
{
    [Header("롱기누스의 창 스탯")]
    public float[] damageByLevel = { 50f, 80f, 120f };
    public float[] cooldownByLevel = { 8f, 7f, 6f };
    public float spearSpeed = 20f;

    [Tooltip("창이 발사된 후 사라지기까지 걸리는 시간 (초)")]
    public float spearLifeTime = 3.0f; // ✨ [추가] 기본 3초

    [Header("프리팹")]
    public GameObject SpearPrefab;

    [Tooltip("LonginusPathData가 붙은 경로 프리팹")]
    public GameObject PathDataPrefab;

    protected void Awake()
    {
        attachmentSocketName = "RoofSocket";
    }

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject obj = InstantiateVisual(user);
        if (obj == null) return null;

        LonginusLauncher logic = obj.GetComponent<LonginusLauncher>();
        if (logic == null) logic = obj.AddComponent<LonginusLauncher>();

        logic.Initialize(this, user);
        logic.UpgradeInstItem(instance);

        return obj;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }
}