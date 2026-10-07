using System;
using UnityEngine;

// Inventory is the only clock for this effect. Attached launchers own their separate clocks.
internal sealed class TimedProc : IItemEffect
{
    private readonly ItemInstance instance;
    private readonly Func<int, float> duration;
    private readonly Func<bool> manual;
    private readonly Action<GameObject> activate;
    private readonly AttackCycle cycle = new AttackCycle();
    private float exportedRemaining;
    private float exportedDuration;
    private bool released;
    private bool activating;

    internal TimedProc(ItemInstance instance, Func<int, float> duration, bool manual, Action<GameObject> activate)
        : this(instance, duration, () => manual, activate) { }
    internal TimedProc(ItemInstance instance, Func<int, float> duration, Func<bool> manual, Action<GameObject> activate)
    { this.instance = instance; this.duration = duration; this.manual = manual; this.activate = activate; }
    public void Equip() { released = false; ImportMirrors(); }
    public void Upgrade(int previousLevel) { }
    public void Release() { released = true; cycle.Reset(); ExportMirrors(); }

    private void ImportMirrors()
    {
        if (instance.currentCooldown == exportedRemaining && instance.maxCooldown == exportedDuration) return;
        bool waiting = instance.currentCooldown == float.MaxValue;
        cycle.Reset(waiting ? 0f : instance.currentCooldown, instance.maxCooldown, waiting);
    }
    private void ExportMirrors()
    {
        exportedRemaining = cycle.IsWaitingForCompletion ? float.MaxValue : cycle.RemainingCooldown;
        exportedDuration = cycle.Duration;
        instance.currentCooldown = exportedRemaining;
        instance.maxCooldown = exportedDuration;
    }

    internal void Tick(float deltaTime, GameObject owner)
    {
        if (released) return;
        ImportMirrors();
        float currentDuration = duration(instance.currentUpgrade);
        if (currentDuration <= 0f) return;
        cycle.Reset(cycle.RemainingCooldown, currentDuration, cycle.IsWaitingForCompletion);
        if (!cycle.IsWaitingForCompletion) cycle.Elapse(deltaTime);
        if (cycle.IsReady)
        {
            cycle.SetRemaining(0f);
            ExportMirrors();
            // Keep the existing public virtual dispatch, including subclasses of known recipes.
            instance.itemData.OnCooldownComplete(owner, instance);
            if (released || instance.IsUnequipped) return;
            if (manual()) cycle.Hold();
            else cycle.StartCooldown(currentDuration);
        }
        ExportMirrors();
    }
    internal void Activate(GameObject owner)
    {
        if (released || activating) return;
        activating = true;
        try { activate(owner); }
        finally { activating = false; }
    }
    internal void Restart(float value)
    {
        if (released) return;
        cycle.ResumeWithCooldown(value);
        ExportMirrors();
    }

    internal static void Pulse(ItemInstance instance, float damage, GameObject effectPrefab)
    {
        Camera camera = Camera.main;
        if (camera == null || PoolManager.instance == null) return;
        int count = 0;
        PoolManager.instance.CombatTargets.ForEachReverse(target =>
        {
            if (!target.IsAlive) return;
            Vector3 position = camera.WorldToViewportPoint(target.Position);
            if (position.z <= 0f || position.x < 0f || position.x > 1f || position.y < 0f || position.y > 1f) return;
            target.RequestDamage(damage);
            count++;
        });
        Debug.Log($"[{instance.itemData.itemName}] (Lv.{instance.currentUpgrade}) 발동! {count}명의 보이는 적 공격!");
        GameObject visual = instance.InstantiatedObject;
        if (effectPrefab != null && visual != null)
        {
            Vector3 position = visual.transform.position;
            instance.RuntimeScope.Spawn(effectPrefab, new Vector2(position.x - 1.6f, position.y), Quaternion.identity, false);
            SoundEventBus.Publish(SoundID.Item_BeatingHeart);
        }
    }

    internal static void CreateBuffZone(ItemInstance instance, GameObject prefab, float spawnRange,
        float cooldown, float duration, float buff, GameObject owner)
    {
        if (prefab == null) return;
        Vector3 position = owner.transform.position;
        position.x = UnityEngine.Random.Range(-spawnRange, spawnRange);
        GameObject child = instance.RuntimeScope.Spawn(prefab, position, Quaternion.identity, true);
        SoundEventBus.Publish(SoundID.Item_Bible);
        child.GetComponent<BloodyZone>()?.Initialize(instance, cooldown, duration, buff, owner);
    }
}
