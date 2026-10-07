using System;
using System.Collections.Generic;

internal sealed class TimedStrike
{
    private readonly HashSet<object> damagedTargets = new HashSet<object>();
    public bool AttackStarted { get; private set; }
    public bool IsActive { get; private set; }
    public float Remaining { get; private set; }

    public void Reset()
    {
        AttackStarted = false;
        IsActive = false;
        Remaining = 0f;
        damagedTargets.Clear();
    }
    public void BeginWait(float duration) { Remaining = Math.Max(0f, duration); }
    public void Advance(float deltaTime, bool running) { if (running) Remaining -= deltaTime; }
    public void MarkAttackStarted() { AttackStarted = true; }
    public void BeginAttack(float duration)
    {
        AttackStarted = true;
        IsActive = true;
        Remaining = duration;
    }
    public void EndAttack(float cleanupDuration) { IsActive = false; BeginWait(cleanupDuration); }
    public bool TryConsumeHit(object target) { return IsActive && damagedTargets.Add(target); }
    public void Cancel() { IsActive = false; Remaining = 0f; }
}
