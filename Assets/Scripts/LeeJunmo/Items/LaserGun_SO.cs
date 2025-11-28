using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LaserGun", menuName = "Items/LaserGun")]
public class LaserGun_SO : Item_SO
{
    [Header("레이저 건 스탯")]
    public float[] damageByLevel = { 20f, 20f, 20f };       // 틱당 데미지

    [Header("레이저 세부 설정")]
    public float[] durationByLevel = { 1f, 3f, 5f };       // 최대 발사 지속 시간
    public float[] tickRateByLevel = { 0.07f, 0.07f, 0.07f };// 데미지 입히는 주기 (낮을수록 빠름)
    public float[] cooldownByLevel = { 0.6f, 0.6f, 0.6f };     // 과열 후 쿨타임

    public GameObject LaserProjectilePrefab;

    [Tooltip("교체할 거치대 이미지 (선택)")]
    public Sprite HolderSprite;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        Gun gun = user.GetComponentInChildren<Gun>();
        if (gun == null)
        {
            Debug.LogError($"[LaserGun_SO] {user.name}에서 Gun을 찾을 수 없습니다!");
            return null;
        }

        GameObject createdWeaponObj = gun.EquipVisual(this.instantiatedPrefab, this.HolderSprite);

        if (createdWeaponObj != null)
        {
            LaserGun logic = createdWeaponObj.GetComponent<LaserGun>();
            if (logic == null)
            {
                logic = createdWeaponObj.AddComponent<LaserGun>();
            }

            logic.Initialize(this, user);
            logic.UpgradeInstItem(instance);
        }

        return createdWeaponObj;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "Duration", durationByLevel[index].ToString() },
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }
}