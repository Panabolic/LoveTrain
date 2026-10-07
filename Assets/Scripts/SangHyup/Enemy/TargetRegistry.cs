using System;
using System.Collections.Generic;
using UnityEngine;

// The public Enemy list remains the compatibility source of truth, not a second registry.
public sealed class TargetRegistry
{
    private readonly List<Enemy> compatibilityTargets;
    internal TargetRegistry(List<Enemy> compatibilityTargets) { this.compatibilityTargets = compatibilityTargets; }
    public IEnumerable<TargetHandle> Targets
    {
        get
        {
            for (int i = 0; i < compatibilityTargets.Count; i++)
            {
                Enemy adapter = compatibilityTargets[i];
                if (adapter != null) yield return adapter.CombatTarget;
            }
        }
    }
    internal void Register(Enemy adapter)
    {
        if (adapter != null) _ = adapter.CombatTarget;
        if (!compatibilityTargets.Contains(adapter)) compatibilityTargets.Add(adapter);
    }
    internal void Unregister(Enemy adapter) { compatibilityTargets.Remove(adapter); }
    internal void ForEachReverse(Action<TargetHandle> visit)
    {
        for (int i = compatibilityTargets.Count - 1; i >= 0; i--)
        {
            Enemy adapter = compatibilityTargets[i];
            if (adapter != null) visit(adapter.CombatTarget);
        }
    }
    internal void Despawn(bool keepBosses)
    {
        // Cleanup removes its own entry synchronously in OnDisable.
        for (int i = compatibilityTargets.Count - 1; i >= 0; i--)
        {
            Enemy adapter = compatibilityTargets[i];
            if (adapter == null) continue;
            TargetHandle target = adapter.CombatTarget;
            if (!keepBosses || !target.KeepOnBossClear) target.DespawnWithoutReward();
        }
    }
    public static TargetHandle Resolve(Collider2D collider)
    {
        if (collider == null) return null;
        // Match the old GetComponent<Enemy>() lookup exactly, including child colliders.
        Enemy adapter = collider.GetComponent<Enemy>();
        return adapter != null ? adapter.CombatTarget : null;
    }
}
