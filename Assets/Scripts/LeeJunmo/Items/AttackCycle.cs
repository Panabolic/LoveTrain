/// <summary>Owns one attack timer. Callers choose whether readiness is checked before or after elapsed time.</summary>
internal sealed class AttackCycle
{
    public float RemainingCooldown { get; private set; }
    public float Duration { get; private set; }
    public bool IsWaitingForCompletion { get; private set; }
    public bool IsReady => !IsWaitingForCompletion && RemainingCooldown <= 0f;

    public void Elapse(float deltaTime)
    {
        if (!IsWaitingForCompletion && RemainingCooldown > 0f) RemainingCooldown -= deltaTime;
    }

    public void StartCooldown(float duration)
    {
        Duration = duration;
        RemainingCooldown = duration;
    }

    public void Hold() { IsWaitingForCompletion = true; }

    public void ResumeWithCooldown(float duration)
    {
        IsWaitingForCompletion = false;
        StartCooldown(duration);
    }

    public void SetRemaining(float value) { RemainingCooldown = value; }

    public void Reset(float remaining = 0f, float duration = 0f, bool waiting = false)
    {
        RemainingCooldown = remaining;
        Duration = duration;
        IsWaitingForCompletion = waiting;
    }
}
