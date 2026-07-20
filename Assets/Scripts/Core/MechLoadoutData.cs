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
    // Null = random map size (default). Set from the MechSetup screen to force a specific size.
    public MapSize? SelectedMapSize = null;

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
