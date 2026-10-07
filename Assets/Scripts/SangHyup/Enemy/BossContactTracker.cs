using System;
using System.Collections.Generic;
using UnityEngine;

// Physics bindings stay local to the adapter. This part owns contact reservations and release.
internal sealed class BossContactTracker
{
    internal readonly struct Binding
    {
        public readonly UnityEngine.Object Owner;
        public readonly Func<bool> IsValid;
        public readonly Action<bool> SetConstraint;
        public readonly Action<float> Push;
        public Binding(UnityEngine.Object owner, Func<bool> valid, Action<bool> constraint, Action<float> push)
        {
            Owner = owner; IsValid = valid; SetConstraint = constraint; Push = push;
        }
    }

    private readonly List<Collider2D> colliders = new List<Collider2D>();
    private readonly Func<Collider2D, Binding> resolve;
    private readonly Func<Collider2D, bool> overlaps;
    private readonly Action acquired;
    private Binding target;

    public BossContactTracker(Func<Collider2D, Binding> resolve, Func<Collider2D, bool> overlaps, Action acquired)
    {
        this.resolve = resolve; this.overlaps = overlaps; this.acquired = acquired;
    }
    public void Register(Collider2D collider)
    {
        if (collider == null) return;
        Binding candidate = resolve(collider);
        if (candidate.Owner == null || !candidate.IsValid() || (target.Owner != null && target.Owner != candidate.Owner)) return;
        if (!colliders.Contains(collider)) colliders.Add(collider);
        if (target.Owner == null) { target = candidate; acquired?.Invoke(); }
        target.SetConstraint(true);
    }
    public void Exit(Collider2D collider)
    {
        if (!overlaps(collider)) colliders.Remove(collider);
        Refresh();
    }
    public void Refresh()
    {
        for (int i = colliders.Count - 1; i >= 0; i--)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || !overlaps(collider))
                colliders.RemoveAt(i);
        }
        if (colliders.Count == 0 || target.Owner == null || !target.IsValid()) Clear();
    }
    public void Advance(float pushDistance)
    {
        Refresh();
        if (target.Owner == null) return;
        target.SetConstraint(true);
        target.Push?.Invoke(pushDistance);
    }
    public void Clear()
    {
        if (target.Owner != null) target.SetConstraint(false);
        target = default;
        colliders.Clear();
    }
}
