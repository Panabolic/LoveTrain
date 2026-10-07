using System;

/// <summary>Starts presentation and cooldown together; each animation event independently advances an emission port.</summary>
internal sealed class AnimatedPortFire
{
    private readonly Func<bool> canBegin;
    private readonly Action beginPresentation;
    private readonly Func<int, bool> emit;
    private readonly Func<int> portCount;
    private readonly Func<float> cooldown;
    private int portIndex;
    public AttackCycle Cycle { get; } = new AttackCycle();

    public AnimatedPortFire(Func<bool> canBegin, Action beginPresentation, Func<int, bool> emit,
        Func<int> portCount, Func<float> cooldown)
    {
        this.canBegin = canBegin;
        this.beginPresentation = beginPresentation;
        this.emit = emit;
        this.portCount = portCount;
        this.cooldown = cooldown;
    }

    public void Advance(float deltaTime)
    {
        if (Cycle.RemainingCooldown > 0f) { Cycle.Elapse(deltaTime); return; }
        if (!canBegin()) return;
        beginPresentation();
        Cycle.StartCooldown(cooldown());
    }

    public void EmitFromAnimation()
    {
        if (!emit(portIndex)) return;
        portIndex = (portIndex + 1) % portCount();
    }
}
