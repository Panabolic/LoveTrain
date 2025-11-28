using UnityEngine;

public class LaserGun : MonoBehaviour
{
    private LaserGun_SO itemData;
    private Gun gunController;

    public void Initialize(LaserGun_SO data, GameObject user)
    {
        itemData = data;
        gunController = user.GetComponentInChildren<Gun>();
    }

    // 아이템 획득(OnEquip) 직후, 그리고 레벨업 시 호출됨
    public void UpgradeInstItem(ItemInstance instance)
    {
        if (gunController == null) return;

        // 1. 외형 교체는 이미 OnEquip에서 했으므로 패스 (또는 필요시 재호출)
        // gunController.EquipVisual(...) <- 여기선 생략 가능

        // 2. 스탯 업데이트
        float newDamage = itemData.damageByLevel[instance.currentUpgrade - 1];

        GunStats newBaseStats = gunController.CurrentStats;
        newBaseStats.damage = newDamage;
        newBaseStats.laserPrefab = itemData.LaserProjectilePrefab;

        // Gun 스탯 갱신
        gunController.ChangeBaseStats(newBaseStats);

        // 3. 전략 갱신 (레벨업 때마다 확실하게 다시 세팅)
        gunController.SetWeapon(new LaserSpriteStrategy());

        Debug.Log($"레이저 건 업그레이드 완료! (Lv.{instance.currentUpgrade})");
    }
}