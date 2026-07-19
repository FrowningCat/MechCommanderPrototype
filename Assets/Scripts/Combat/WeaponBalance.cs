using UnityEngine;

// Single source of truth for how WeaponType and WeaponFireMode modify a weapon's base stats.
// Base Weapon.damage/cooldown/heatPerShot values on prefabs stay untouched — everything here is
// applied dynamically at the moment it matters (dealing damage, checking cooldown, adding heat),
// and the exact same formulas are reused by the MechSetup stats panel so displayed numbers can
// never drift from what actually happens in combat.
public static class WeaponBalance
{
    public static float GetDamageMultiplier(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Energy => 0.8f,   // weaker per hit, partially ignores armor (see Health.GetEffectiveArmor)
            WeaponType.Missile => 1.3f,  // stronger per hit, plus splash
            _ => 1f,                     // Ballistic — baseline
        };
    }

    public static float GetCooldownMultiplier(WeaponFireMode fireMode)
    {
        return fireMode == WeaponFireMode.AlphaStrike ? 1.3f : 0.7f;
    }

    public static float GetHeatMultiplier(WeaponFireMode fireMode)
    {
        return fireMode == WeaponFireMode.AlphaStrike ? 1.5f : 0.6f;
    }

    public static int ComputeEffectiveDamage(int baseDamage, WeaponType weaponType)
    {
        return Mathf.RoundToInt(baseDamage * GetDamageMultiplier(weaponType));
    }

    public static float ComputeEffectiveCooldown(float baseCooldown, WeaponFireMode fireMode)
    {
        return baseCooldown * GetCooldownMultiplier(fireMode);
    }

    public static float ComputeEffectiveHeat(float baseHeat, WeaponFireMode fireMode)
    {
        return baseHeat * GetHeatMultiplier(fireMode);
    }
}
