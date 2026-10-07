using UnityEngine;

// Existing prefab/Inspector component: delegates the weapon transaction to its own effect.
public class LaserGun : MonoBehaviour, IInstantiatedItem, IItemCooldownView
{
    private WeaponOverride weapon;
    public bool HasCooldown => weapon != null && weapon.HasCooldown;
    public float GetCooldownFillAmount() => weapon != null ? weapon.GetCooldownFillAmount() : 0f;

    public void Initialize(LaserGun_SO data, GameObject user)
    {
        weapon = CompositionDefinition.CreateWeapon(data, user);
        weapon.Equip();
    }

    public void RestoreProjectileWeapon() => weapon?.Release();
    public void UpgradeInstItem(ItemInstance instance) => weapon?.ApplyLevel(instance.currentUpgrade);
}
