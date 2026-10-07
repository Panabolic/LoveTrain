internal static class WeaponStatRules
{
    public static GunStats Calculate(GunStats basis, float permanentBonus, float levelDamage,
        float damageMultiplier, float fireRateMultiplier, float weaponRatio)
    {
        GunStats result = basis;
        result.damage = (basis.damage + permanentBonus + levelDamage) * (1f + damageMultiplier) * weaponRatio;
        result.fireRate = 1f + fireRateMultiplier > 0f ? basis.fireRate / (1f + fireRateMultiplier) : basis.fireRate;
        return result;
    }
}
