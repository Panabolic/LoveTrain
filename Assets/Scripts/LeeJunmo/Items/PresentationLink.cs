using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Unity presentation references and local animation signals; no attack state.</summary>
internal sealed class PresentationLink
{
    private readonly UnityEngine.Object owner;
    private readonly Animator[] animators;
    private readonly Dictionary<string, Action> signals = new Dictionary<string, Action>(StringComparer.Ordinal);
    public SpriteRenderer Sprite { get; }
    public Animator Animator => animators.Length > 0 ? animators[0] : null;

    public PresentationLink(UnityEngine.Object owner, SpriteRenderer sprite, params Animator[] animators)
    {
        this.owner = owner;
        Sprite = sprite;
        this.animators = animators ?? Array.Empty<Animator>();
    }

    public static PresentationLink From(GameObject owner)
    {
        return new PresentationLink(owner, owner.GetComponent<SpriteRenderer>(), owner.GetComponent<Animator>());
    }

    public void BindSignal(string name, Action handler) { signals[name] = handler; }
    public void Signal(string name)
    {
        if (owner != null && signals.TryGetValue(name, out Action handler)) handler?.Invoke();
    }
    public void ClearSignals() { signals.Clear(); }
    public void Trigger(string parameter)
    {
        foreach (Animator animator in animators) if (animator != null) animator.SetTrigger(parameter);
    }
    public void SetPlaybackSpeed(float speed)
    {
        foreach (Animator animator in animators) if (animator != null) animator.speed = speed;
    }
    public void SetBool(string parameter, bool value)
    {
        foreach (Animator animator in animators) if (animator != null) animator.SetBool(parameter, value);
    }
    public void SetFloat(string parameter, float value)
    {
        foreach (Animator animator in animators) if (animator != null) animator.SetFloat(parameter, value);
    }
    public void ShowSprite(bool visible) { if (Sprite != null) Sprite.enabled = visible; }
    public void SetColor(Color color) { if (Sprite != null) Sprite.color = color; }
    public void SetFlipX(bool flip) { if (Sprite != null) Sprite.flipX = flip; }

    public void ApplyUpgrade(Item_SO definition, int level)
    {
        int index = level - 1;
        if (definition == null || index < 0) return;
        if (Animator != null && definition.controllersByLevel != null && index < definition.controllersByLevel.Length)
        {
            RuntimeAnimatorController controller = definition.controllersByLevel[index];
            if (controller != null) { Animator.runtimeAnimatorController = controller; return; }
        }
        if (Sprite != null && definition.spritesByLevel != null && index < definition.spritesByLevel.Length)
        {
            UnityEngine.Sprite sprite = definition.spritesByLevel[index];
            if (sprite != null) Sprite.sprite = sprite;
        }
    }
}
