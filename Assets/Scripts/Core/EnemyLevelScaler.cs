using UnityEngine;

// Scales a freshly spawned enemy's Health and weapon damage to the current run level (see
// RunProgression). Runs once per spawn, right after Instantiate — prefab-authored base values on
// Health/Weapon are never touched, only the live instance is adjusted.
public static class EnemyLevelScaler
{
    public static void ApplyRunLevelScaling(GameObject enemyInstance)
    {
        if (enemyInstance == null)
            return;

        int runLevel = MechLoadoutData.Instance != null ? MechLoadoutData.Instance.CurrentRunLevel : 1;
        float multiplier = RunProgression.GetEnemyStatMultiplier(runLevel);

        if (Mathf.Approximately(multiplier, 1f))
            return;

        Health health = enemyInstance.GetComponent<Health>();
        if (health != null)
        {
            int scaledMaxHealth = Mathf.Max(1, Mathf.RoundToInt(health.MaxHealth * multiplier));
            health.ConfigureStats(scaledMaxHealth, health.ArmorValue);
        }

        foreach (Weapon weapon in enemyInstance.GetComponentsInChildren<Weapon>())
            weapon.SetDamageMultiplier(multiplier);
    }
}
