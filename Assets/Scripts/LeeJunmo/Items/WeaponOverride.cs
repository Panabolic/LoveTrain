using UnityEngine;

// Owns the complete strategy override/restore transaction for one attached weapon.
internal sealed class WeaponOverride : IItemEffect, IItemCooldownView
{
    private readonly System.Func<int, WeaponLevelStats> statsAtLevel;
    private readonly Gun gun;
    private GunStats previous;
    private LaserSpriteStrategy strategy;
    private int level = 1;
    private bool released;
    private bool equipped;
    public bool HasCooldown => strategy != null && strategy.HasCooldown;
    public float GetCooldownFillAmount() => strategy != null ? strategy.GetCooldownFillAmount() : 0f;

    internal WeaponOverride(GameObject owner, System.Func<int, WeaponLevelStats> statsAtLevel)
    {
        this.statsAtLevel = statsAtLevel;
        gun = owner.GetComponentInChildren<Gun>();
    }
    public void Equip()
    {
        if (equipped || gun == null) return;
        equipped = true;
        previous = gun.BaseStats;
    }
    public void Upgrade(int previousLevel) => ApplyLevel(level);
    internal void ApplyLevel(int value)
    {
        level = value;
        if (released || !equipped || gun == null) return;
        WeaponLevelStats definition = statsAtLevel(level);
        gun.SetWeaponDamageRatio(definition.DamageRatio);
        GunStats stats = gun.BaseStats;
        stats.fireRate = definition.TickRate;
        stats.laserPrefab = definition.ProjectilePrefab;
        gun.ChangeBaseStats(stats);
        strategy = new LaserSpriteStrategy();
        strategy.SetLaserStats(definition.Duration, definition.Cooldown, definition.Scale);
        gun.SetWeapon(strategy);
        Debug.Log($"레이저 세팅 완료: 최종데미지 {gun.CurrentStats.damage}");
    }
    public void Release()
    {
        if (released || !equipped || gun == null) return;
        released = true;
        gun.SetWeapon(new ProjectileStrategy());
        gun.ChangeBaseStats(previous);
        gun.SetWeaponDamageRatio(1f);
        gun.UnequipVisual();
    }
}

internal readonly struct WeaponLevelStats
{
    internal readonly float DamageRatio, TickRate, Duration, Cooldown, Scale;
    internal readonly GameObject ProjectilePrefab;
    internal WeaponLevelStats(float damageRatio, float tickRate, float duration, float cooldown, float scale, GameObject prefab)
    { DamageRatio = damageRatio; TickRate = tickRate; Duration = duration; Cooldown = cooldown; Scale = scale; ProjectilePrefab = prefab; }
}
