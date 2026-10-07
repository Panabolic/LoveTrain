using System;
using UnityEngine;

// A cached capability view. UnityOwner guards Unity's destroyed-object null semantics.
public sealed class TargetHandle
{
    private readonly Func<bool> alive;
    private readonly Func<bool> targetable;
    private readonly Action<float> damage;
    private readonly Action<Vector2, float> knockback;
    private readonly Action cleanup;
    public MonoBehaviour UnityOwner { get; }
    public GameObject Owner => UnityOwner != null ? UnityOwner.gameObject : null;
    public Vector3 Position => UnityOwner != null ? UnityOwner.transform.position : Vector3.zero;
    public bool IsAlive => UnityOwner != null && alive();
    public bool IsTargetable => UnityOwner != null && targetable();
    public bool IsActive => UnityOwner != null && UnityOwner.gameObject.activeSelf;
    public bool SupportsKnockback => knockback != null;
    public bool KeepOnBossClear { get; }

    internal TargetHandle(MonoBehaviour owner, Func<bool> alive, Func<bool> targetable,
        Action<float> damage, Action cleanup, bool keepOnBossClear, Action<Vector2, float> knockback)
    {
        UnityOwner = owner;
        this.alive = alive;
        this.targetable = targetable;
        this.damage = damage;
        this.cleanup = cleanup;
        this.knockback = knockback;
        KeepOnBossClear = keepOnBossClear;
    }
    public void RequestDamage(float amount) { if (UnityOwner != null) damage(amount); }
    public void Knockback(Vector2 direction, float power) { if (UnityOwner != null) knockback?.Invoke(direction, power); }
    public void DespawnWithoutReward() { if (UnityOwner != null) cleanup(); }
}
