using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shown instead of the normal victory ResultPanel after a Victory MissionEnded event, as part of
// the Stage 33 run-based progression: reads live stats off the actual player Mech in the scene
// (never hardcoded — same formulas MechSetupController's stats panel uses), lets the player bank
// exactly one cumulative upgrade per level into MechLoadoutData, then advances the run by
// reloading the gameplay scene for a fresh LevelGenerator/MissionController/MechLoadoutApplier pass.
public class LevelUpgradeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionController missionController;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text healthOptionText;
    [SerializeField] private TMP_Text armorOptionText;
    [SerializeField] private TMP_Text damageOptionText;
    [SerializeField] private TMP_Text fireRateOptionText;
    [Tooltip("Selected-unit stats panel (Canvas/UnitInfoPanel) — hidden while the upgrade panel is up so it doesn't show the mech's full MechSetup-style stat card next to the 4 upgrade choices.")]
    [SerializeField] private GameObject unitInfoPanel;
    [Tooltip("Static reminder shown next to the 4 upgrade choices about enemy scaling (see RunProgression.EnemyStatIncrementPerLevel).")]
    [SerializeField] private TMP_Text enemyScalingHintText;

    [Header("Player Lookup")]
    [Tooltip("Слой юнита игрока — тот же, что и playerUnitLayer в MissionController")]
    [SerializeField] private LayerMask playerUnitLayer;

    [Header("Scenes")]
    [Tooltip("Точное имя геймплейной сцены, регистр важен")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private Health playerHealth;
    private Weapon[] playerWeapons;
    private MechWeaponSystem playerWeaponSystem;

    private void OnEnable()
    {
        if (missionController != null)
            missionController.OnMissionEnded += HandleMissionEnded;
    }

    private void OnDisable()
    {
        if (missionController != null)
            missionController.OnMissionEnded -= HandleMissionEnded;
    }

    private void Start()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        if (enemyScalingHintText != null)
        {
            int percent = Mathf.RoundToInt(RunProgression.EnemyStatIncrementPerLevel * 100f);
            enemyScalingHintText.text = $"Противники получают +{percent}% к характеристикам за каждый уровень.";
        }
    }

    private void HandleMissionEnded(MissionState state)
    {
        if (state != MissionState.Victory)
            return;

        // No run in progress (e.g. gameplay scene launched directly) — MissionResultUI's normal
        // victory panel handles this case instead.
        if (MechLoadoutData.Instance == null)
            return;

        FindPlayerUnit();

        if (playerHealth == null)
            return;

        RefreshOptionTexts();

        // Hide the selected-unit stats panel — the player's mech is normally still selected at
        // the moment of victory, so without this it kept showing its full stat card (HP/armor/
        // damage/range) right next to the 4 upgrade choices below, duplicating information this
        // screen has no business displaying.
        if (unitInfoPanel != null)
            unitInfoPanel.SetActive(false);

        if (upgradePanel != null)
            upgradePanel.SetActive(true);
    }

    private void FindPlayerUnit()
    {
        Health[] allUnits = FindObjectsByType<Health>(FindObjectsSortMode.None);

        foreach (Health unit in allUnits)
        {
            if (((1 << unit.gameObject.layer) & playerUnitLayer.value) == 0)
                continue;

            playerHealth = unit;
            playerWeapons = unit.GetComponentsInChildren<Weapon>();
            playerWeaponSystem = unit.GetComponent<MechWeaponSystem>();
            return;
        }
    }

    private void RefreshOptionTexts()
    {
        MechLoadoutData loadout = MechLoadoutData.Instance;
        float increment = RunProgression.PlayerUpgradeIncrement;

        if (levelText != null)
            levelText.text = "Уровень " + loadout.CurrentRunLevel + " пройден! Выберите улучшение:";

        if (healthOptionText != null)
        {
            int current = playerHealth.MaxHealth;
            int next = Mathf.RoundToInt(current * (1f + increment));
            healthOptionText.text = "HP: " + current + " -> " + next;
        }

        if (armorOptionText != null)
        {
            int current = playerHealth.ArmorValue;
            int next = Mathf.RoundToInt(current * (1f + increment));
            armorOptionText.text = "Броня: " + current + " -> " + next;
        }

        if (damageOptionText != null && playerWeapons != null && playerWeapons.Length > 0)
        {
            int current = playerWeapons[0].EffectiveDamage;
            int next = Mathf.RoundToInt(current * (1f + increment));
            damageOptionText.text = "Урон: " + current + " -> " + next;
        }

        if (fireRateOptionText != null && playerWeapons != null && playerWeapons.Length > 0 && playerWeaponSystem != null)
        {
            float current = WeaponBalance.ComputeEffectiveCooldown(playerWeapons[0].Cooldown, playerWeaponSystem.FireMode) / loadout.RunFireRateMultiplier;
            float next = current / (1f + increment);
            fireRateOptionText.text = "Кулдаун: " + current.ToString("0.00") + "с -> " + next.ToString("0.00") + "с";
        }
    }

    public void OnPickHealth()
    {
        ApplyUpgrade(ref MechLoadoutData.Instance.RunHealthMultiplier);
    }

    public void OnPickArmor()
    {
        ApplyUpgrade(ref MechLoadoutData.Instance.RunArmorMultiplier);
    }

    public void OnPickDamage()
    {
        ApplyUpgrade(ref MechLoadoutData.Instance.RunDamageMultiplier);
    }

    public void OnPickFireRate()
    {
        ApplyUpgrade(ref MechLoadoutData.Instance.RunFireRateMultiplier);
    }

    private void ApplyUpgrade(ref float runMultiplier)
    {
        runMultiplier *= 1f + RunProgression.PlayerUpgradeIncrement;
        MechLoadoutData.Instance.CurrentRunLevel++;

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        AdsManager.Instance.ShowInterstitial(() => SceneManager.LoadScene(gameplaySceneName));
    }
}
