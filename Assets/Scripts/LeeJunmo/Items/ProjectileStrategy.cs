using NUnit.Framework.Interfaces;
using UnityEngine;

public class ProjectileStrategy : IWeaponStrategy
{
    private Gun gun;
    private float currentCooldown = 0f;

    public void Initialize(Gun gunController, GunStats stats)
    {
        this.gun = gunController;
    }

    public void Process(bool isTriggerHeld)
    {
        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

        // 버튼 누름 + 쿨타임 완료 -> 발사
        if (isTriggerHeld && currentCooldown <= 0f)
        {
            Fire();
            currentCooldown = gun.CurrentStats.fireRate;
        }
    }

    private void Fire()
    {
        // BulletPoolManager 사용
        GameObject bullet = BulletPoolManager.Instance.Spawn(
            gun.CurrentStats.projectilePrefab,
            gun.FirePoint.position,
            gun.FirePoint.rotation
        );

        SoundEventBus.Publish(SoundID.Player_Shoot);

        // Projectile (또는 Bullet) 컴포넌트 초기화
        Projectile bulletScript = bullet.GetComponent<Projectile>();
        if (bulletScript != null)
        {
            bulletScript.Init(gun.CurrentStats.damage, gun.CurrentStats.speed, gun.FirePoint.right, gun.CurrentStats.projectilePrefab, true);
        }
    }

    public void Unequip() { }
}