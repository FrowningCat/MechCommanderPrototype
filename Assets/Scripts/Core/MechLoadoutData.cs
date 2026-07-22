using System.Collections.Generic;
using UnityEngine;

public class MechLoadoutData : MonoBehaviour
{
    // Per-mech cumulative run upgrades (Stage 38). Replaces the old flat Run*Multiplier fields —
    // once a boss victory can add a second (or third...) player mech, "the run's upgrade level" is
    // no longer a single number: each mech carries its own multipliers from the point it joined the
    // squad, so an original mech and a boss-reward mech can legitimately sit at different real power
    // levels at the same time (see LevelUpgradeUI.ApplyUpgrade, which still grants exactly one pick
    // per level but applies it to every currently-alive mech's own multipliers).
    [System.Serializable]
    public class MechRunState
    {
        public float RunHealthMultiplier = 1f;
        public float RunArmorMultiplier = 1f;
        public float RunDamageMultiplier = 1f;
        public float RunFireRateMultiplier = 1f; // >1 = faster (shorter weapon cooldown)
    }

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

    // One entry per currently-alive player mech, index-matched to spawn order within a level (the
    // hand-placed scene "Mech" is always index 0; any boss-reward mechs are indices 1+, see
    // LevelGenerator.SpawnAdditionalPlayerMechs). LevelUpgradeUI trims this down to only the
    // survivors' states right after a Victory (Stage 38 rule: dead mechs don't carry over or get
    // replaced), and appends a fresh MechRunState when a boss reward mech is granted.
    public List<MechRunState> MechStates = new List<MechRunState> { new MechRunState() };

    [Header("Boss Progression (Stage 38)")]
    // Set once the first boss level (see LevelGenerator.bossLevelInterval) is won. From that point
    // on, for the rest of the run, LevelGenerator.ResolveMapSize refuses to pick MapSize.Small
    // (player selection or random roll alike) — a run that has already survived a boss should not
    // suddenly shrink to the easiest map size. Reset on ResetRun().
    public bool HasDefeatedFirstBoss = false;

    // Incremented by one every time a boss level is won. LevelGenerator.ComputeEnemyCount adds this
    // straight onto the area-based enemy count on every subsequent normal (non-boss) level —
    // cumulative and intentionally not re-clamped to MapSizeSettings' maxEnemies, since the whole
    // point is normal levels keep getting harder after each boss. Reset on ResetRun().
    public int BossesDefeatedCount = 0;

    // Set by LevelGenerator.Generate() every time the gameplay scene loads, so LevelUpgradeUI (and
    // anything else reacting to a Victory) can tell whether the level that was just won was a boss
    // level without duplicating LevelGenerator's own bossLevelInterval math.
    public bool CurrentLevelIsBoss = false;

    // Called when a fresh run starts (MechSetupController.StartMission) and when a run ends in
    // defeat (MissionResultUI) — both are explicit "the run is over" boundaries.
    public void ResetRun()
    {
        CurrentRunLevel = 1;
        MechStates = new List<MechRunState> { new MechRunState() };
        HasDefeatedFirstBoss = false;
        BossesDefeatedCount = 0;
        CurrentLevelIsBoss = false;
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
