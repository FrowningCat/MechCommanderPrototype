using UnityEngine;

public class MechLoadoutData : MonoBehaviour
{
    public static MechLoadoutData Instance { get; private set; }

    [Header("Mech Loadout")]
    public int SelectedModelIndex = 0;
    public Color SelectedColor = Color.white;
    public WeaponType SelectedWeaponType = WeaponType.Energy;
    public WeaponFireMode SelectedFireMode = WeaponFireMode.AlphaStrike;
    public UnitStance SelectedStance = UnitStance.Defensive;

    [Header("Mission")]
    // Defaults to Medium so a fresh MechLoadoutData always has a real, displayable selection (see
    // MechSetupController's map-size button highlight). Null is still a valid value elsewhere —
    // LevelGenerator treats a null SelectedMapSize (or no MechLoadoutData instance at all, e.g.
    // launching the gameplay scene directly) as "pick a random size" — this default just ensures
    // that state is never the unintentional starting point from the MechSetup screen.
    public MapSize? SelectedMapSize = MapSize.Medium;

    [Header("Ads bonus")]
    // Granted by watching a rewarded ad on the MechSetup screen. Consumed (reset to false) by
    // LevelGenerator once the extra pickup has been spawned.
    public bool BonusHealthPickup = false;

    [Header("Run Progression (Stage 33)")]
    // Level number within the current run-based campaign. Starts at 1 when a new mission is
    // launched from MechSetup, incremented by one on every LevelUpgradeUI pick after a Victory.
    // EnemyLevelScaler reads this to scale freshly spawned enemies (see RunProgression).
    public int CurrentRunLevel = 1;

    // Cumulative multipliers from LevelUpgradeUI picks, applied on top of MechLoadoutApplier's
    // base ModelStats/Weapon values every time the gameplay scene loads. 1 = no bonus yet. Kept
    // here (not on WeaponBalance, which is about weapon type/mode, not level growth, and not on
    // prefab base values) so a run's progress survives the scene reload between levels the same
    // way the rest of the loadout does.
    public float RunHealthMultiplier = 1f;
    public float RunArmorMultiplier = 1f;
    public float RunDamageMultiplier = 1f;
    public float RunFireRateMultiplier = 1f; // >1 = faster (shorter weapon cooldown)

    // Called when a fresh run starts (MechSetupController.StartMission) and when a run ends in
    // defeat (MissionResultUI) — both are explicit "the run is over" boundaries.
    public void ResetRun()
    {
        CurrentRunLevel = 1;
        RunHealthMultiplier = 1f;
        RunArmorMultiplier = 1f;
        RunDamageMultiplier = 1f;
        RunFireRateMultiplier = 1f;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
