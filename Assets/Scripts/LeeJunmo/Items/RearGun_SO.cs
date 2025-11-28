using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RearGun", menuName = "Items/RearGun")]
public class RearGun_SO : Item_SO
{
    [Header("후방 총기 스탯")]
    public float[] damageByLevel = { 20f, 30f, 40f };
    public float[] cooldownByLevel = { 1.0f, 0.8f, 0.6f };

    [Tooltip("레벨별 발사할 탄환 수 (예: 1, 1, 3)")]
    public int[] bulletCountByLevel = { 1, 1, 3 }; // ✨ 핵심 데이터 추가

    public float bulletSpeed = 10f;

    [Header("발사 설정")]
    [Tooltip("다중 발사 시 탄환 사이의 각도 (예: 15도)")]
    public float fanSpreadAngle = 15f;

    [Header("프리팹")]
    public GameObject BulletPrefab;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        GameObject rearGunGO = InstantiateVisual(user);
        if (rearGunGO == null) return null;

        RearGun logic = rearGunGO.GetComponent<RearGun>();
        if (logic == null) logic = rearGunGO.AddComponent<RearGun>();

        logic.Initialize(this, instance, user);
        logic.UpgradeInstItem(instance);

        return rearGunGO;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "Cooldown", cooldownByLevel[index].ToString() },
            { "Count", bulletCountByLevel[index].ToString() } // 툴팁용
        };
    }
}