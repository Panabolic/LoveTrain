using System;
using UnityEngine;

// A real construction recipe, shared by instances. Delegates capture authoring data only.
internal sealed class CompositionDefinition
{
    private readonly Func<ItemInstance, IItemEffect> create;
    internal CompositionDefinition(Func<ItemInstance, IItemEffect> create) { this.create = create; }
    internal IItemEffect Create(ItemInstance instance) => create(instance);

    internal static CompositionDefinition Visual(Item_SO source) => new CompositionDefinition(instance =>
        new ItemAttachment(instance, () => source.CreateAttachedVisual(instance.Owner, instance)));

    internal static CompositionDefinition Attach<T>(Item_SO source, Action<T, GameObject, ItemInstance> initialize,
        bool addIfMissing = true, Action<T> release = null) where T : Component, IInstantiatedItem
    {
        return new CompositionDefinition(instance => new ItemAttachment(instance,
            () => source.CreateAttachedVisual(instance.Owner, instance), view =>
            {
                T behaviour = view.GetComponent<T>();
                if (behaviour == null && addIfMissing) behaviour = view.AddComponent<T>();
                if (behaviour == null)
                {
                    Debug.LogError($"{source.instantiatedPrefab.name}에 {typeof(T).Name}가 없습니다!");
                    return null;
                }
                initialize(behaviour, instance.Owner, instance);
                return behaviour;
            }, view => { if (release != null) release(view.GetComponent<T>()); }));
    }

    internal static CompositionDefinition Stat(StatChannel channel, Func<int, float> amount) =>
        new CompositionDefinition(instance => new StatModifier(() => instance.currentUpgrade, amount,
            (value, phase) => StatModifier.Apply(channel, instance.Owner, value, phase)));

    internal static CompositionDefinition Healing(HealingItem_SO source) => new CompositionDefinition(instance =>
        CombatProc.CountKills(() => source.killCountCondition[LevelIndex(instance.currentUpgrade, source.maxSpeedBonusByLevel.Length)],
            () =>
            {
                int index = LevelIndex(instance.currentUpgrade, source.maxSpeedBonusByLevel.Length);
                instance.Owner.GetComponent<Train>()?.HealPercent(source.healPercentByLevel[index]);
                instance.InstantiatedObject?.GetComponent<HealingItem>()?.PlayHealPresentation();
            }, () => instance.InstantiatedObject?.GetComponent<HealingItem>()?.RefreshCounter()));

    internal static CompositionDefinition Ricochet(MagicBullet_SO source) => new CompositionDefinition(instance =>
        CombatProc.OnHit((target, attack) => CombatProc.Ricochet(instance, source.bounceCounts, target, attack)));

    // Preserve the public virtual cooldown extension contract for custom authoring adapters.
    internal static CompositionDefinition Timed(Item_SO source) => new CompositionDefinition(instance =>
        new TimedProc(instance, source.GetCooldownForLevel, () => source.IsManualCooldown,
            user => source.OnCooldownComplete(user, instance)));

    internal static CompositionDefinition Heart(BeatingHeart_SO source) => new CompositionDefinition(instance =>
        new TimedProc(instance, source.GetCooldownForLevel, () => source.IsManualCooldown, user => TimedProc.Pulse(instance,
            source.damageByLevel[LevelIndex(instance.currentUpgrade, source.damageByLevel.Length)], source.EffectPrefab)));

    internal static CompositionDefinition Bible(BloodyBible_SO source) => new CompositionDefinition(instance =>
        new TimedProc(instance, source.GetCooldownForLevel, () => source.IsManualCooldown, user =>
        {
            int index = LevelIndex(instance.currentUpgrade, source.durationByLevel.Length);
            TimedProc.CreateBuffZone(instance, source.ZonePrefab, source.SpawnRangeX,
                source.cooldownByLevel[index], source.durationByLevel[index], source.buffAmountByLevel[index], user);
        }));

    internal static WeaponOverride CreateWeapon(LaserGun_SO source, GameObject owner) =>
        new WeaponOverride(owner, level => new WeaponLevelStats(source.damageRatio,
            source.tickRateByLevel[level - 1], source.durationByLevel[level - 1], source.cooldownByLevel[level - 1],
            source.laserScale[level - 1], source.LaserProjectilePrefab));

    internal static CompositionDefinition Weapon(LaserGun_SO source) => new CompositionDefinition(instance =>
        new ItemAttachment(instance, () =>
        {
            Gun gun = instance.Owner.GetComponentInChildren<Gun>();
            if (gun == null)
            {
                Debug.LogError($"[LaserGun_SO] {instance.Owner.name}에서 Gun을 찾을 수 없습니다!");
                return null;
            }
            return gun.EquipVisual(source.instantiatedPrefab, source.HolderSprite);
        }, view =>
        {
            LaserGun behaviour = view.GetComponent<LaserGun>();
            if (behaviour == null) behaviour = view.AddComponent<LaserGun>();
            behaviour.Initialize(source, instance.Owner);
            return behaviour;
        }, view => view.GetComponent<LaserGun>()?.RestoreProjectileWeapon()));

    internal static int LevelIndex(int level, int count) => Mathf.Clamp(level - 1, 0, count - 1);
}
