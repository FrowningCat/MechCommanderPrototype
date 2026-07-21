using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MechLoadoutApplier : MonoBehaviour
{
    [System.Serializable]
    public struct ModelStats
    {
        public int maxHealth;
        public int armorValue;
        public float agentSpeed;
    }

    [Header("Model Variants (index-matched to setup screen)")]
    [SerializeField] private GameObject[] modelVariants;

    [Tooltip("Index-matched to modelVariants. Defines the HP/armor/speed archetype for each model.")]
    [SerializeField]
    private ModelStats[] modelStats =
    {
        new ModelStats { maxHealth = 120, armorValue = 4, agentSpeed = 6f },   // George — balanced
        new ModelStats { maxHealth = 90,  armorValue = 1, agentSpeed = 7.5f }, // Leela — light/fast scout
        new ModelStats { maxHealth = 160, armorValue = 7, agentSpeed = 4.5f }, // Mike — heavy tank
        new ModelStats { maxHealth = 130, armorValue = 5, agentSpeed = 5.5f }, // Stan — heavy hitter
    };

    private void Awake()
    {
        if (MechLoadoutData.Instance == null)
            return;

        MechLoadoutData loadout = MechLoadoutData.Instance;

        GameObject activeModel = ApplyModel(loadout.SelectedModelIndex);
        MechColorUtility.ApplyPlayerColor(activeModel, loadout.SelectedColor);

        ApplyModelStats(loadout.SelectedModelIndex);

        Weapon weapon = GetComponent<Weapon>();
        if (weapon != null)
            weapon.SetWeaponType(loadout.SelectedWeaponType);

        MechWeaponSystem weaponSystem = GetComponent<MechWeaponSystem>();
        if (weaponSystem != null)
            weaponSystem.SetFireMode(loadout.SelectedFireMode);

        MechCombat combat = GetComponent<MechCombat>();
        if (combat != null)
            combat.SetStance(loadout.SelectedStance);

        ApplyRunUpgrades(loadout);
    }

    // Applies the player's cumulative Stage 33 run upgrades (see MechLoadoutData.RunHealthMultiplier
    // etc. and LevelUpgradeUI) on top of the base model stats set by ApplyModelStats — never
    // touches WeaponBalance or the prefab's own serialized values.
    private void ApplyRunUpgrades(MechLoadoutData loadout)
    {
        Health health = GetComponent<Health>();
        if (health != null)
        {
            int scaledMaxHealth = Mathf.RoundToInt(health.MaxHealth * loadout.RunHealthMultiplier);
            int scaledArmor = Mathf.RoundToInt(health.ArmorValue * loadout.RunArmorMultiplier);
            health.ConfigureStats(scaledMaxHealth, scaledArmor);
        }

        foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
            weapon.SetDamageMultiplier(loadout.RunDamageMultiplier);

        MechWeaponSystem weaponSystem = GetComponent<MechWeaponSystem>();
        if (weaponSystem != null)
            weaponSystem.SetFireRateMultiplier(loadout.RunFireRateMultiplier);
    }

    // Lets other systems (e.g. the MechSetup stats panel) read a model's real archetype numbers
    // straight off this prefab, without instantiating it, so displayed values can never drift
    // from what actually gets applied in-game.
    public ModelStats GetModelStats(int modelIndex)
    {
        if (modelStats == null || modelStats.Length == 0)
            return default;

        modelIndex = Mathf.Clamp(modelIndex, 0, modelStats.Length - 1);
        return modelStats[modelIndex];
    }

    private void ApplyModelStats(int modelIndex)
    {
        if (modelStats == null || modelStats.Length == 0)
            return;

        modelIndex = Mathf.Clamp(modelIndex, 0, modelStats.Length - 1);
        ModelStats stats = modelStats[modelIndex];

        Health health = GetComponent<Health>();
        if (health != null)
            health.ConfigureStats(stats.maxHealth, stats.armorValue);

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.speed = stats.agentSpeed;
    }

    private GameObject ApplyModel(int modelIndex)
    {
        if (modelVariants == null || modelVariants.Length == 0)
            return null;

        modelIndex = Mathf.Clamp(modelIndex, 0, modelVariants.Length - 1);

        GameObject activeModel = null;

        for (int i = 0; i < modelVariants.Length; i++)
        {
            if (modelVariants[i] == null)
                continue;

            bool isSelected = i == modelIndex;
            modelVariants[i].SetActive(isSelected);

            if (isSelected)
                activeModel = modelVariants[i];
        }

        return activeModel;
    }
}
