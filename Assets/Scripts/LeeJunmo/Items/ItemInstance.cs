using UnityEngine;

[System.Serializable]
public class ItemInstance : IItemCooldownView
{
    public Item_SO itemData;
    public float currentCooldown;
    public float maxCooldown;
    private GameObject instantiatedObject = null; // 실체화된 오브젝트
    private IItemEffect[] effects;
    private SpawnScope scope;
    private bool isUnequipped;

    public int currentUpgrade = 1;
    public int equippedSlotIndex = -1;
    public GameObject InstantiatedObject => instantiatedObject;
    public bool IsUnequipped => isUnequipped;
    public GameObject Owner { get; private set; }
    internal SpawnScope RuntimeScope
    {
        get
        {
            if (scope == null) scope = new SpawnScope(Owner != null ? (Object)Owner : itemData);
            return scope;
        }
    }

    public bool HasCooldown
    {
        get
        {
            if (HasItemCooldown())
            {
                return true;
            }

            return TryGetInstantiatedCooldownView(out IItemCooldownView cooldownView) && cooldownView.HasCooldown;
        }
    }

    public ItemInstance(Item_SO data)
    {
        itemData = data;
        currentUpgrade = 1;
        // 처음 생성 시 쿨타임 적용 (게임 시작 시 바로 발사 안 되게 하려면 값 유지, 바로 쏘려면 0)
        currentCooldown = 0f;
    }

    public void HandleEquip(GameObject user)
    {
        isUnequipped = false;
        Owner = user;
        effects = null;
        scope = new SpawnScope(user);
        instantiatedObject = itemData.OnEquip(user, this);
    }

    private void EnsureEffects()
    {
        if (effects == null && itemData != null) effects = itemData.RuntimeDefinition.CreateEffects(this);
    }

    internal GameObject EquipComposition(GameObject user)
    {
        Owner = user;
        EnsureEffects();
        if (effects != null) foreach (IItemEffect effect in effects) effect.Equip();
        return instantiatedObject;
    }

    internal void SetInstantiatedObject(GameObject value) => instantiatedObject = value;
    internal T GetEffect<T>() where T : class, IItemEffect
    {
        EnsureEffects();
        if (effects != null) foreach (IItemEffect effect in effects) if (effect is T match) return match;
        return null;
    }
    internal void UpgradeComposition(int previousLevel)
    {
        EnsureEffects();
        if (effects != null) foreach (IItemEffect effect in effects) effect.Upgrade(previousLevel);
    }
    internal void ReleaseComposition()
    {
        if (effects != null) foreach (IItemEffect effect in effects) effect.Release();
    }
    internal void ProcessKill(GameObject target)
    {
        if (isUnequipped) return;
        EnsureEffects();
        if (effects != null) foreach (IItemEffect effect in effects) if (effect is CombatProc proc) proc.Kill(target);
    }
    internal void ProcessHit(GameObject target, GameObject source)
    {
        if (isUnequipped) return;
        EnsureEffects();
        if (effects != null) foreach (IItemEffect effect in effects) if (effect is CombatProc proc) proc.Hit(target, source);
    }
    internal void ActivateCooldown(GameObject user) => GetEffect<TimedProc>()?.Activate(user);

    public void TrackOwnedEffect(GameObject effect)
    {
        if (effect == null) return;
        if (isUnequipped)
        {
            effect.SetActive(false);
            Object.Destroy(effect);
            return;
        }
        RuntimeScope.Track(effect);
    }

    public void HandleUnequip(GameObject user)
    {
        if (isUnequipped) return;
        isUnequipped = true;
        if (itemData != null) itemData.OnUnequip(user, this);
        scope?.ReleaseBoundObjects();
        if (instantiatedObject != null)
        {
            instantiatedObject.SetActive(false);
            Object.Destroy(instantiatedObject);
            instantiatedObject = null;
        }
        currentCooldown = 0f;
        maxCooldown = 0f;
        Owner = null;
    }

    // ✨ [수정됨] 쿨타임 로직
    public void Tick(float deltaTime, GameObject user)
    {
        if (isUnequipped || itemData == null || GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss
            && GameManager.Instance.CurrentState != GameState.Ending) return;

        // Only inventory-owned timed procs are ticked here. Attached launchers own their Update.
        GetEffect<TimedProc>()?.Tick(deltaTime, user);
    }

    public float GetCooldownFillAmount()
    {
        if (HasItemCooldown())
        {
            return GetItemCooldownFillAmount();
        }

        if (TryGetInstantiatedCooldownView(out IItemCooldownView cooldownView) && cooldownView.HasCooldown)
        {
            return cooldownView.GetCooldownFillAmount();
        }

        return 0f;
    }

    private bool HasItemCooldown()
    {
        if (itemData == null)
        {
            return false;
        }

        return itemData.GetCooldownForLevel(currentUpgrade) > 0f;
    }

    private float GetItemCooldownFillAmount()
    {
        float levelCooldown = itemData.GetCooldownForLevel(currentUpgrade);
        if (levelCooldown <= 0f)
        {
            return 0f;
        }

        if (currentCooldown == float.MaxValue)
        {
            return 1f;
        }

        if (currentCooldown <= 0f)
        {
            return 0f;
        }

        float cooldownDuration = maxCooldown > 0f ? maxCooldown : levelCooldown;
        return ItemCooldownFill.FromRemaining(currentCooldown, cooldownDuration);
    }

    private bool TryGetInstantiatedCooldownView(out IItemCooldownView cooldownView)
    {
        cooldownView = null;
        if (instantiatedObject == null)
        {
            return false;
        }

        cooldownView = instantiatedObject.GetComponentInChildren<IItemCooldownView>(true);
        return cooldownView != null;
    }

    // ✨ [추가됨] 외부(장판)에서 호출하여 쿨타임을 강제로 시작시키는 메서드
    public void StartCooldownManual(float cooldownTime)
    {
        if (isUnequipped) return;
        TimedProc proc = GetEffect<TimedProc>();
        if (proc != null) proc.Restart(cooldownTime);
        else { maxCooldown = cooldownTime; currentCooldown = cooldownTime; }
        // Debug.Log($"[ItemInstance] 수동 쿨타임 시작: {cooldownTime}초");
    }

    public void UpgradeLevel()
    {
        itemData.UpgradeLevel(this);
    }

    public void instantiatedItemUpgrade()
    {
        if (instantiatedObject == null) return;

        GetEffect<ItemAttachment>()?.Refresh();
    }

}
