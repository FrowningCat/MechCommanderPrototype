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

    [Tooltip("Index-matched to modelVariants. Portrait shown in UnitInfoPanel for whichever model is active.")]
    [SerializeField] private Sprite[] modelPortraits;

    [Tooltip("Index-matched to modelVariants. Defines the HP/armor/speed archetype for each model.")]
    [SerializeField]
    private ModelStats[] modelStats =
    {
        new ModelStats { maxHealth = 120, armorValue = 4, agentSpeed = 6f },   // George — balanced
        new ModelStats { maxHealth = 90,  armorValue = 1, agentSpeed = 7.5f }, // Leela — light/fast scout
        new ModelStats { maxHealth = 160, armorValue = 7, agentSpeed = 4.5f }, // Mike — heavy tank
        new ModelStats { maxHealth = 130, armorValue = 5, agentSpeed = 5.5f }, // Stan — heavy hitter
    };

    // Which slot in MechLoadoutData.MechStates this mech instance reads its cumulative run
    // upgrades from (Stage 38). 0 for the hand-placed scene "Mech" (the default — it never needs
    // to be set explicitly). Boss-reward mechs are Instantiate()'d by LevelGenerator, which calls
    // ApplyLoadout(index) again right after Instantiate with their real index — Awake() below will
    // already have run once with the default 0 by then, which ApplyLoadout is idempotent about.
    public int MechIndex { get; private set; }

    private void Awake()
    {
        ApplyLoadout(MechIndex);
    }

    // Re-runs the full loadout application for a specific MechStates slot. Safe to call more than
    // once (e.g. Awake()'s implicit index-0 pass followed by LevelGenerator correcting the index
    // for a freshly instantiated extra mech) — every step just re-sets live component state from
    // scratch, nothing accumulates across calls.
    public void ApplyLoadout(int mechIndex)
    {
        if (MechLoadoutData.Instance == null)
            return;

        MechIndex = mechIndex;
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

        ApplyRunUpgrades(GetMechRunState(loadout, mechIndex));
    }

    // A boss-reward mech is granted fresh base stats (Stage 38 rule) — a MechRunState with all
    // multipliers still at 1 (no accumulated upgrades), same as index 0 was on RunLevel 1. Falling
    // back to a brand-new MechRunState (instead of indexing out of range) covers the one-frame
    // window where LevelGenerator has Instantiate()'d the extra mech but not yet appended its
    // MechRunState to MechStates.
    private static MechLoadoutData.MechRunState GetMechRunState(MechLoadoutData loadout, int mechIndex)
    {
        if (loadout.MechStates != null && mechIndex >= 0 && mechIndex < loadout.MechStates.Count)
            return loadout.MechStates[mechIndex];

        return new MechLoadoutData.MechRunState();
    }

    // Applies this mech's own cumulative Stage 33/38 run upgrades (see MechLoadoutData.MechRunState
    // and LevelUpgradeUI) on top of the base model stats set by ApplyModelStats — never touches
    // WeaponBalance or the prefab's own serialized values.
    private void ApplyRunUpgrades(MechLoadoutData.MechRunState state)
    {
        Health health = GetComponent<Health>();
        if (health != null)
        {
            int scaledMaxHealth = Mathf.RoundToInt(health.MaxHealth * state.RunHealthMultiplier);
            int scaledArmor = Mathf.RoundToInt(health.ArmorValue * state.RunArmorMultiplier);
            health.ConfigureStats(scaledMaxHealth, scaledArmor);
        }

        foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
            weapon.SetDamageMultiplier(state.RunDamageMultiplier);

        MechWeaponSystem weaponSystem = GetComponent<MechWeaponSystem>();
        if (weaponSystem != null)
            weaponSystem.SetFireRateMultiplier(state.RunFireRateMultiplier);
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

        ApplyPortrait(modelIndex);

        return activeModel;
    }

    private void ApplyPortrait(int modelIndex)
    {
        if (modelPortraits == null || modelPortraits.Length == 0)
            return;

        modelIndex = Mathf.Clamp(modelIndex, 0, modelPortraits.Length - 1);

        UnitSelectable unitSelectable = GetComponent<UnitSelectable>();
        if (unitSelectable != null)
            unitSelectable.SetPortrait(modelPortraits[modelIndex]);
    }
}
