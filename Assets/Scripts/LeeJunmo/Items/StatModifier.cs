using System;
using UnityEngine;

internal enum StatChannel { GunFireRate, GunDamage, TrainMaxSpeed }
internal enum StatApplicationPhase { Equip, Upgrade, Release }

// One lease owns exactly the amount it applied; data/assets never hold that amount.
internal sealed class StatModifier : IItemEffect
{
    private readonly Func<int> level;
    private readonly Func<int, float> amount;
    private readonly Action<float, StatApplicationPhase> apply;
    private float applied;
    private bool released;

    internal StatModifier(Func<int> level, Func<int, float> amount, Action<float, StatApplicationPhase> apply)
    { this.level = level; this.amount = amount; this.apply = apply; }

    public void Equip() => SetAmount(amount(level()), StatApplicationPhase.Equip);
    public void Upgrade(int previousLevel) => SetAmount(amount(level()), StatApplicationPhase.Upgrade);
    private void SetAmount(float value, StatApplicationPhase phase)
    {
        if (released) return;
        float delta = value - applied;
        if (delta != 0f) apply(delta, phase);
        applied = value;
    }
    public void Release()
    {
        if (released) return;
        released = true;
        if (applied != 0f) apply(-applied, StatApplicationPhase.Release);
        applied = 0f;
    }

    internal static void Apply(StatChannel channel, GameObject owner, float delta, StatApplicationPhase phase)
    {
        if (owner == null) return;
        if (channel == StatChannel.TrainMaxSpeed)
        {
            Train train = owner.GetComponent<Train>();
            if (train == null) return;
            train.IncreaseMaxSpeed(delta);
            train.ModifySpeed(phase == StatApplicationPhase.Release ? 0f : delta);
            return;
        }
        Gun gun = owner.GetComponentInChildren<Gun>();
        // Preserve the existing equip/unequip fallback; upgrade only uses the owner's gun.
        if (gun == null && phase != StatApplicationPhase.Upgrade) gun = UnityEngine.Object.FindFirstObjectByType<Gun>();
        if (gun == null) return;
        if (channel == StatChannel.GunFireRate) gun.AddFireRateMultiplier(delta);
        else gun.AddDamageMultiplier(delta);
    }
}
