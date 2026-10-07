using System;
using UnityEngine;

/// <summary>Per-object Unity bindings. Existing MonoBehaviours forward only their own clock/events.</summary>
internal sealed class ObjectHost
{
    private Action<float> update;
    private Action<float> fixedUpdate;
    private Action<Collider2D> collision;
    public GameObject Owner { get; }
    public Rigidbody2D Body { get; }
    public Collider2D Collider { get; }
    public PresentationLink Presentation { get; }
    public SpawnScope Scope { get; }

    public ObjectHost(GameObject owner)
    {
        Owner = owner;
        Body = owner.GetComponent<Rigidbody2D>();
        Collider = owner.GetComponent<Collider2D>();
        Presentation = PresentationLink.From(owner);
        Scope = new SpawnScope(owner);
    }

    public void BindUpdate(Action<float> step) { update = step; }
    public void BindFixedUpdate(Action<float> step) { fixedUpdate = step; }
    public void BindCollision(Action<Collider2D> react) { collision = react; }
    public void Activate() { Scope.BeginActivation(); }
    public void Update(float deltaTime) { if (Scope.IsActive) update?.Invoke(deltaTime); }
    public void FixedUpdate(float deltaTime) { if (Scope.IsActive) fixedUpdate?.Invoke(deltaTime); }
    public void Collision(Collider2D other) { if (Scope.IsActive) collision?.Invoke(other); }
    public void Deactivate() { Scope.EndActivation(); }
    public void Release()
    {
        Scope.ReleaseBoundObjects();
        Presentation.ClearSignals();
        update = null; fixedUpdate = null; collision = null;
    }
}
