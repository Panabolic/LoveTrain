using UnityEngine;

internal sealed class KnockbackState
{
    public bool IsStunned { get; set; }
    public float LastAppliedTime { get; private set; } = -999f;
    public void Reset() { IsStunned = false; LastAppliedTime = -999f; }
    public bool CanApply(float time, float cooldown) { return time >= LastAppliedTime + cooldown; }
    public void MarkApplied(float time) { LastAppliedTime = time; }
    public static Vector2 Impulse(Vector2 direction, float power) { return direction.normalized * power; }
}
