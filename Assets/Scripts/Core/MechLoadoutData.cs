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
