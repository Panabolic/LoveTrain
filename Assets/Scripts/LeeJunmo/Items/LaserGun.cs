using UnityEngine;

public class LaserGun : MonoBehaviour, IInstantiatedItem
{
    private LaserGun_SO itemData;
    private Gun gunController;

    public void Initialize(LaserGun_SO data, GameObject user)
    {
        itemData = data;
        gunController = user.GetComponentInChildren<Gun>();
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (gunController == null) return;

        int levelIndex = instance.currentUpgrade - 1;

        // 1. SO에서 데이터 추출
        float newDuration = itemData.durationByLevel[levelIndex];
        float newTickRate = itemData.tickRateByLevel[levelIndex]; // 틱 주기
        float newCooldown = itemData.cooldownByLevel[levelIndex];
        float newlaserScale = itemData.laserScale[levelIndex];

        // 2. Gun에게 스탯 전달

        // ✨ [핵심 수정 1] 비율 설정
        gunController.SetWeaponDamageRatio(itemData.damageRatio);

        // ✨ [핵심 수정 2] CurrentStats가 아닌 '순수 BaseStats'를 가져와서 수정해야 함
        // (이미 증폭된 데미지를 다시 Base로 넣는 실수 방지)
        GunStats newBaseStats = gunController.BaseStats;

        // 데미지는 건드리지 않습니다! (SetWeaponDamageRatio로 처리됨)
        newBaseStats.fireRate = newTickRate; // 틱 주기 설정
        newBaseStats.laserPrefab = itemData.LaserProjectilePrefab;

        // 변경된 베이스 스탯 적용 (이때 UpdateStats가 돌면서 올바른 데미지가 계산됨)
        gunController.ChangeBaseStats(newBaseStats);

        // 3. 전략 설정
        LaserSpriteStrategy strategy = new LaserSpriteStrategy();
        strategy.SetLaserStats(newDuration, newCooldown, newlaserScale);

        gunController.SetWeapon(strategy);

        Debug.Log($"레이저 세팅 완료: 최종데미지 {gunController.CurrentStats.damage}");
    }
}