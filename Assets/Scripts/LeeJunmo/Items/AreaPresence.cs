using System;
using System.Collections.Generic;

/// <summary>Tracks exact collider targets in insertion order; the caller owns its hit schedule and animation gate.</summary>
internal sealed class AreaPresence
{
    private readonly List<TargetHandle> targets = new List<TargetHandle>();
    private readonly bool catchUp;
    private float elapsed;
    private float lastNotificationTime;
    public bool HitEnabled { get; private set; }
    public bool IsStopping { get; private set; }

    public AreaPresence(bool catchUp = false, bool hitEnabled = true)
    {
        this.catchUp = catchUp;
        HitEnabled = hitEnabled;
    }

    public void Enter(TargetHandle target)
    {
        if (target != null && !targets.Contains(target)) targets.Add(target);
    }

    public void Exit(TargetHandle target)
    {
        if (target != null) targets.Remove(target);
    }

    public void Clear() { targets.Clear(); }

    public void ResetForActivation()
    {
        elapsed = 0f;
        HitEnabled = false;
        IsStopping = false;
        targets.Clear();
        // The original laser keeps its global ricochet timestamp across activations.
    }

    public void EnableHit() { HitEnabled = true; }

    public bool BeginStopping()
    {
        if (IsStopping) return false;
        IsStopping = true;
        HitEnabled = false;
        return true;
    }

    public void Advance(float deltaTime, float interval, Action<TargetHandle> hit)
    {
        if (!HitEnabled) return;
        elapsed += deltaTime;
        if (catchUp)
        {
            while (elapsed >= interval) { VisitActive(hit); elapsed -= interval; }
        }
        else if (elapsed >= interval) { VisitActive(hit); elapsed = 0f; }
    }

    public void NotifyWhenReady(float currentTime, float interval, TargetHandle target, Func<TargetHandle, bool> notify)
    {
        if (currentTime >= lastNotificationTime + interval && notify(target)) lastNotificationTime = currentTime;
    }

    public void VisitActive(Action<TargetHandle> react)
    {
        for (int index = targets.Count - 1; index >= 0; index--)
        {
            TargetHandle target = targets[index];
            if (target != null && target.IsActive) react(target);
            else targets.RemoveAt(index);
        }
    }
}
