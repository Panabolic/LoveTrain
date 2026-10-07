using System;
using UnityEngine;

/// <summary>Captures a target, emits on each animation signal, and resumes after each normal child completion.</summary>
internal sealed class SummonAndAwait
{
    private readonly Func<bool> canBegin;
    private readonly Func<Vector3> captureTarget;
    private readonly Action beginPresentation;
    private readonly Func<int, Vector3, Action, bool> emit;
    private readonly Func<int> portCount;
    private readonly Action emittedPresentation;
    private readonly Action completedPresentation;
    private readonly Func<float> cooldown;
    private readonly Func<Action, Action> guardOwnerCallback;
    private Vector3 capturedTarget;
    private int portIndex;

    public AttackCycle Cycle { get; } = new AttackCycle();

    public SummonAndAwait(Func<bool> canBegin, Func<Vector3> captureTarget, Action beginPresentation,
        Func<int, Vector3, Action, bool> emit, Func<int> portCount, Action emittedPresentation,
        Action completedPresentation, Func<float> cooldown, Func<Action, Action> guardOwnerCallback)
    {
        this.canBegin = canBegin;
        this.captureTarget = captureTarget;
        this.beginPresentation = beginPresentation;
        this.emit = emit;
        this.portCount = portCount;
        this.emittedPresentation = emittedPresentation;
        this.completedPresentation = completedPresentation;
        this.cooldown = cooldown;
        this.guardOwnerCallback = guardOwnerCallback;
    }

    public void Advance(float deltaTime)
    {
        if (Cycle.IsWaitingForCompletion) return;
        if (Cycle.RemainingCooldown > 0f) { Cycle.Elapse(deltaTime); return; }
        if (!canBegin()) return;
        capturedTarget = captureTarget();
        Cycle.Hold();
        beginPresentation();
    }

    public void EmitFromAnimation()
    {
        // Every delivered event is valid; this is not a one-shot latch.
        Action completion = guardOwnerCallback(() =>
        {
            completedPresentation();
            Cycle.ResumeWithCooldown(cooldown());
        });
        if (!emit(portIndex, capturedTarget, completion)) return;
        emittedPresentation();
        portIndex = (portIndex + 1) % portCount();
    }
}
