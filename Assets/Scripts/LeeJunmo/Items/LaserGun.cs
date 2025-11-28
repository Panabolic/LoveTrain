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
        float newDamage = itemData.damageByLevel[levelIndex];
        float newDuration = itemData.durationByLevel[levelIndex];
        float newTickRate = itemData.tickRateByLevel[levelIndex]; // 이게 틱 주기
        float newCooldown = itemData.cooldownByLevel[levelIndex];

        // 2. Gun에게 기본 스탯 전달
        // ✨ [중요] GunStats.fireRate 필드를 '틱 주기'로 사용합니다.
        GunStats newBaseStats = gunController.CurrentStats;
        newBaseStats.damage = newDamage;
        newBaseStats.fireRate = newTickRate; // Gun.cs가 이 값을 공속 배율로 나눌 것임
        newBaseStats.laserPrefab = itemData.LaserProjectilePrefab;

        // Gun 내부에서 (기본 틱 주기 / (1 + 공속배율)) 계산이 일어남
        gunController.ChangeBaseStats(newBaseStats);

        // 3. 전략 설정 (지속시간, 쿨타임은 전략이 관리)
        LaserSpriteStrategy strategy = new LaserSpriteStrategy();
        strategy.SetLaserStats(newDuration, newCooldown);

        gunController.SetWeapon(strategy);

        Debug.Log($"레이저 세팅 완료: 데미지{newDamage}, 기본틱{newTickRate}, 지속{newDuration}");
    }
}