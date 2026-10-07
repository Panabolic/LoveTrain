using System;
using UnityEngine;

// Instance-owned event gates. No update clock and no per-item subclass.
internal sealed class CombatProc : IItemEffect
{
    private readonly Func<int> killThreshold;
    private readonly Action killAction;
    private readonly Action counterChanged;
    private readonly Action<GameObject, GameObject> hitAction;
    private int killCount;
    private bool released;
    internal int RemainingKills => killThreshold != null ? Mathf.Max(0, killThreshold() - killCount) : 0;

    private CombatProc(Func<int> killThreshold, Action killAction, Action counterChanged,
        Action<GameObject, GameObject> hitAction)
    { this.killThreshold = killThreshold; this.killAction = killAction; this.counterChanged = counterChanged; this.hitAction = hitAction; }

    internal static CombatProc CountKills(Func<int> threshold, Action activate, Action changed) =>
        new CombatProc(threshold, activate, changed, null);
    internal static CombatProc OnHit(Action<GameObject, GameObject> activate) => new CombatProc(null, null, null, activate);
    public void Equip() { released = false; counterChanged?.Invoke(); }
    public void Upgrade(int previousLevel) => counterChanged?.Invoke();
    public void Release() { released = true; }
    internal void Kill(GameObject target)
    {
        if (released || killThreshold == null) return;
        killCount++;
        if (killCount >= killThreshold()) { killAction?.Invoke(); killCount = 0; }
        counterChanged?.Invoke();
    }
    internal void Hit(GameObject target, GameObject source) { if (!released) hitAction?.Invoke(target, source); }

    internal static void Ricochet(ItemInstance instance, int[] bounceCounts, GameObject target, GameObject attack)
    {
        if (target == null || attack == null) return;
        IRicochetSource source = attack.GetComponent<IRicochetSource>();
        if (source == null) return;
        int index = CompositionDefinition.LevelIndex(instance.currentUpgrade, bounceCounts.Length);
        if (source.GetBounceDepth() >= bounceCounts[index] || PoolManager.instance == null) return;
        TargetHandle nearest = null;
        float closest = Mathf.Infinity;
        Vector3 origin = target.transform.position;
        foreach (TargetHandle candidate in PoolManager.instance.CombatTargets.Targets)
        {
            if (!candidate.IsTargetable || candidate.Owner == target) continue;
            float distance = (candidate.Position - origin).sqrMagnitude;
            if (distance < closest) { closest = distance; nearest = candidate; }
        }
        GameObject prefab = source.GetRicochetPrefab();
        if (nearest == null || prefab == null) return;
        Vector3 direction = (nearest.Position - origin).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject child = instance.RuntimeScope.Spawn(prefab, origin, Quaternion.Euler(0f, 0f, angle), false);
        Projectile projectile = child.GetComponent<Projectile>();
        if (projectile == null) return;
        float speed = source.GetSpeed();
        if (speed <= 0.1f) { speed = projectile.GetSpeed(); if (speed <= 0f) speed = 15f; }
        if (source.GetBounceDepth() == 0) speed *= 2f;
        projectile.Init(source.GetDamage() * 0.35f, speed, direction, prefab, true,
            source.GetBounceDepth() + 1, target.GetComponent<Collider2D>());
    }
}
