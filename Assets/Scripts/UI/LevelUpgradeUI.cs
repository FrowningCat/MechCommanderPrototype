using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shown instead of the normal victory ResultPanel after a Victory MissionEnded event, as part of
// the Stage 33 run-based progression: reads live stats off the actual player Mech in the scene
// (never hardcoded — same formulas MechSetupController's stats panel uses), lets the player bank
// exactly one cumulative upgrade per level into MechLoadoutData, then advances the run by
// reloading the gameplay scene for a fresh LevelGenerator/MissionController/MechLoadoutApplier pass.
//
// Stage 38: a Victory also (a) drops any dead player mechs' MechRunState from
// MechLoadoutData.MechStates before anything else runs, so only survivors carry over, and (b) on a
// boss level, shows an additional "new mech" panel after the normal 4-option pick — additive to,
// not a replacement for, the usual upgrade choice, since a whole extra unit is a different kind of
// reward from a stat bump and boss levels are already the rarer, special-feeling ones.
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

    [Header("Boss reward panel (Stage 38)")]
    [Tooltip("Shown after the normal 4-option pick, only on a boss level's Victory — announces the extra mech (a fresh-stats copy of the current loadout) before advancing to the next level. Has its own continue button wired to OnContinueAfterBossReward.")]
    [SerializeField] private GameObject newMechPanel;
    [SerializeField] private TMP_Text newMechText;

    [Header("Player Lookup")]
    [Tooltip("Слой юнита игрока — тот же, что и playerUnitLayer в MissionController")]
    [SerializeField] private LayerMask playerUnitLayer;

    [Header("Scenes")]
    [Tooltip("Точное имя геймплейной сцены, регистр важен")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private Health playerHealth;
    private Weapon[] playerWeapons;
    private MechWeaponSystem playerWeaponSystem;
    private MechLoadoutData.MechRunState previewMechState;

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

        if (newMechPanel != null)
            newMechPanel.SetActive(false);

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

        // Stage 38: drop any dead mechs' MechRunState before anything else — a mech destroyed
        // during the level just won never comes back or gets replaced, only survivors proceed.
        DropDeadMechStates();

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

    // Stage 38: only a GameObject with both Health and MechLoadoutApplier on the player layer is
    // still alive and tracked — Health.Die() destroys the GameObject outright, so anything that
    // died during the level simply isn't found here. Whatever MechRunState slots remain in use
    // (by MechIndex) become the new MechStates list; everything else is gone for good.
    private void DropDeadMechStates()
    {
        MechLoadoutData loadout = MechLoadoutData.Instance;
        MechLoadoutApplier[] appliers = FindObjectsByType<MechLoadoutApplier>(FindObjectsSortMode.None);

        System.Collections.Generic.List<MechLoadoutData.MechRunState> survivors = new();

        foreach (MechLoadoutApplier applier in appliers)
        {
            if (((1 << applier.gameObject.layer) & playerUnitLayer.value) == 0)
                continue;

            int index = Mathf.Clamp(applier.MechIndex, 0, loadout.MechStates.Count - 1);
            survivors.Add(loadout.MechStates[index]);
        }

        if (survivors.Count > 0)
            loadout.MechStates = survivors;
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

            // Preview text below is necessarily based on THIS one mech's own current multipliers —
            // with per-mech run state (Stage 38), mechs can legitimately be at different power
            // levels, so a single preview can't represent all of them at once. Falls back to a
            // fresh state (1x everywhere) if this mech's MechLoadoutApplier is missing for some
            // reason, rather than crashing on a null.
            MechLoadoutApplier applier = unit.GetComponent<MechLoadoutApplier>();
            previewMechState = applier != null
                ? GetMechState(applier.MechIndex)
                : new MechLoadoutData.MechRunState();
            return;
        }
    }

    private MechLoadoutData.MechRunState GetMechState(int mechIndex)
    {
        MechLoadoutData loadout = MechLoadoutData.Instance;
        if (mechIndex >= 0 && mechIndex < loadout.MechStates.Count)
            return loadout.MechStates[mechIndex];

        return new MechLoadoutData.MechRunState();
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
            float current = WeaponBalance.ComputeEffectiveCooldown(playerWeapons[0].Cooldown, playerWeaponSystem.FireMode) / previewMechState.RunFireRateMultiplier;
            float next = current / (1f + increment);
            fireRateOptionText.text = "Кулдаун: " + current.ToString("0.00") + "с -> " + next.ToString("0.00") + "с";
        }
    }

    public void OnPickHealth()
    {
        ApplyUpgrade(state => state.RunHealthMultiplier *= 1f + RunProgression.PlayerUpgradeIncrement);
    }

    public void OnPickArmor()
    {
        ApplyUpgrade(state => state.RunArmorMultiplier *= 1f + RunProgression.PlayerUpgradeIncrement);
    }

    public void OnPickDamage()
    {
        ApplyUpgrade(state => state.RunDamageMultiplier *= 1f + RunProgression.PlayerUpgradeIncrement);
    }

    public void OnPickFireRate()
    {
        ApplyUpgrade(state => state.RunFireRateMultiplier *= 1f + RunProgression.PlayerUpgradeIncrement);
    }

    // Stage 38: exactly one pick per level, same as before — but now applied to every currently-
    // alive mech's OWN multipliers rather than a single shared one. A mech that joined later (boss
    // reward, starting at 1x) and the original mech (already several picks in) both get the same
    // relative +15% here, so they keep compounding from wherever each of them individually started
    // — which is exactly how mechs end up at different real power levels at the same time.
    private void ApplyUpgrade(System.Action<MechLoadoutData.MechRunState> applyToState)
    {
        MechLoadoutData loadout = MechLoadoutData.Instance;

        foreach (MechLoadoutData.MechRunState state in loadout.MechStates)
            applyToState(state);

        loadout.CurrentRunLevel++;

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        if (loadout.CurrentLevelIsBoss)
            ShowBossRewardPanel();
        else
            ProceedToNextLevel();
    }

    private void ShowBossRewardPanel()
    {
        MechLoadoutData loadout = MechLoadoutData.Instance;
        loadout.HasDefeatedFirstBoss = true;
        loadout.BossesDefeatedCount++;

        if (newMechText != null)
            newMechText.text = "Босс уничтожен! В отряд прибывает новый мех — та же модель и оружие, но со свежими характеристиками.";

        if (newMechPanel != null)
            newMechPanel.SetActive(true);
        else
            OnContinueAfterBossReward();
    }

    // Wired to the boss reward panel's continue button. Appends a brand-new MechRunState (all
    // multipliers at 1 — no accumulated run upgrades) for the mech LevelGenerator will instantiate
    // as a copy of the current loadout on the next scene load (see
    // LevelGenerator.SpawnAdditionalPlayerMechs).
    public void OnContinueAfterBossReward()
    {
        MechLoadoutData.Instance.MechStates.Add(new MechLoadoutData.MechRunState());

        if (newMechPanel != null)
            newMechPanel.SetActive(false);

        ProceedToNextLevel();
    }

    private void ProceedToNextLevel()
    {
        AdsManager.Instance.ShowInterstitial(() => SceneManager.LoadScene(gameplaySceneName));
    }
}
