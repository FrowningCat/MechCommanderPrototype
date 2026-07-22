using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MechSetupController : MonoBehaviour
{
    [Tooltip("Точное имя геймплейной сцены, регистр важен")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    [Tooltip("Точное имя сцены главного меню, регистр важен")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [SerializeField] private float previewRotationSpeed = 20f;

    [Tooltip("Player Mech prefab — read directly (not instantiated) as the single source of truth for the live stats panel, so displayed numbers can never drift from what MechLoadoutApplier actually applies in-game.")]
    [SerializeField] private GameObject mechPrefabReference;

    // Kept in sync with in-game UnitInfoPanel names via MechLoadoutApplier.ModelNames.
    private static readonly string[] ModelNames = MechLoadoutApplier.ModelNames;
    private static readonly Color[] ColorPresets =
    {
        Color.white,
        Color.red,
        new Color(0.2f, 0.5f, 1f),
        Color.green,
        Color.yellow,
        new Color(0.25f, 0.25f, 0.25f)
    };

    private static readonly Color NormalButtonColor = new Color(0.3f, 0.3f, 0.3f);
    private static readonly Color SelectedButtonColor = new Color(0.2f, 0.6f, 0.9f);

    private Transform previewPivot;
    private GameObject[] modelInstances;

    private Button[] colorButtons;
    private Button[] weaponButtons;
    private Button[] fireModeButtons;
    private Button[] stanceButtons;
    private Button[] mapSizeButtons;
    private Button adBonusButton;

    private Text modelNameText;
    private Text statsText;

    private MechLoadoutApplier mechPrefabApplier;
    private Health mechPrefabHealth;
    private Weapon mechPrefabWeapon;

    private int currentModelIndex;
    private Color currentColor;
    private WeaponType currentWeaponType;
    private WeaponFireMode currentFireMode;
    private UnitStance currentStance;
    private MapSize? currentMapSize;

    private void Awake()
    {
        GameObject pivotObject = GameObject.Find("PreviewPivot");
        previewPivot = pivotObject != null ? pivotObject.transform : null;

        modelInstances = new GameObject[ModelNames.Length];
        for (int i = 0; i < ModelNames.Length; i++)
        {
            if (previewPivot != null)
            {
                Transform modelTransform = previewPivot.Find("Model_" + ModelNames[i]);
                modelInstances[i] = modelTransform != null ? modelTransform.gameObject : null;
            }
        }

        colorButtons = new[]
        {
            FindButton("Btn_Color_0"), FindButton("Btn_Color_1"), FindButton("Btn_Color_2"),
            FindButton("Btn_Color_3"), FindButton("Btn_Color_4"), FindButton("Btn_Color_5")
        };

        weaponButtons = new[]
        {
            FindButton("Btn_Weapon_Energy"), FindButton("Btn_Weapon_Ballistic"), FindButton("Btn_Weapon_Missile")
        };

        fireModeButtons = new[]
        {
            FindButton("Btn_FireMode_AlphaStrike"), FindButton("Btn_FireMode_ChainFire")
        };

        stanceButtons = new[]
        {
            FindButton("Btn_Stance_Passive"), FindButton("Btn_Stance_Defensive"), FindButton("Btn_Stance_Aggressive")
        };

        mapSizeButtons = new[]
        {
            FindButton("Btn_MapSize_Small"), FindButton("Btn_MapSize_Medium"), FindButton("Btn_MapSize_Large")
        };

        GameObject modelNameObject = GameObject.Find("Text_ModelName");
        modelNameText = modelNameObject != null ? modelNameObject.GetComponent<Text>() : null;

        GameObject statsObject = GameObject.Find("Text_Stats");
        statsText = statsObject != null ? statsObject.GetComponent<Text>() : null;

        if (mechPrefabReference != null)
        {
            mechPrefabApplier = mechPrefabReference.GetComponent<MechLoadoutApplier>();
            mechPrefabHealth = mechPrefabReference.GetComponent<Health>();
            mechPrefabWeapon = mechPrefabReference.GetComponent<Weapon>();
        }

        BindClick("Btn_PrevModel", PreviousModel);
        BindClick("Btn_NextModel", NextModel);

        for (int i = 0; i < colorButtons.Length; i++)
        {
            int index = i;
            if (colorButtons[i] != null)
                colorButtons[i].onClick.AddListener(() => SelectColor(index));
        }

        BindClick("Btn_Weapon_Energy", () => SelectWeaponType(WeaponType.Energy));
        BindClick("Btn_Weapon_Ballistic", () => SelectWeaponType(WeaponType.Ballistic));
        BindClick("Btn_Weapon_Missile", () => SelectWeaponType(WeaponType.Missile));

        BindClick("Btn_FireMode_AlphaStrike", () => SelectFireMode(WeaponFireMode.AlphaStrike));
        BindClick("Btn_FireMode_ChainFire", () => SelectFireMode(WeaponFireMode.ChainFire));

        BindClick("Btn_Stance_Passive", () => SelectStance(UnitStance.Passive));
        BindClick("Btn_Stance_Defensive", () => SelectStance(UnitStance.Defensive));
        BindClick("Btn_Stance_Aggressive", () => SelectStance(UnitStance.Aggressive));

        BindClick("Btn_MapSize_Small", () => SelectMapSize(MapSize.Small));
        BindClick("Btn_MapSize_Medium", () => SelectMapSize(MapSize.Medium));
        BindClick("Btn_MapSize_Large", () => SelectMapSize(MapSize.Large));

        BindClick("Btn_Start", StartMission);
        BindClick("Btn_Back", BackToMainMenu);

        adBonusButton = FindButton("Btn_AdBonus");
        if (adBonusButton != null)
            adBonusButton.onClick.AddListener(OnAdBonusClicked);

        InitializeFromExistingLoadout();
    }

    private void Update()
    {
        if (previewPivot != null)
            previewPivot.Rotate(Vector3.up, previewRotationSpeed * Time.deltaTime, Space.World);
    }

    private void InitializeFromExistingLoadout()
    {
        MechLoadoutData loadout = MechLoadoutData.Instance;

        if (loadout != null)
        {
            currentModelIndex = Mathf.Clamp(loadout.SelectedModelIndex, 0, ModelNames.Length - 1);
            currentColor = loadout.SelectedColor;
            currentWeaponType = loadout.SelectedWeaponType;
            currentFireMode = loadout.SelectedFireMode;
            currentStance = loadout.SelectedStance;
            currentMapSize = loadout.SelectedMapSize;
        }
        else
        {
            currentModelIndex = 0;
            currentColor = ColorPresets[0];
            currentWeaponType = WeaponType.Energy;
            currentFireMode = WeaponFireMode.AlphaStrike;
            currentStance = UnitStance.Defensive;
            currentMapSize = MapSize.Medium;
        }

        ApplyModelSelection();
        RefreshColorButtons();
        RefreshGroupSelection(weaponButtons, (int)currentWeaponType);
        RefreshGroupSelection(fireModeButtons, (int)currentFireMode);
        RefreshGroupSelection(stanceButtons, (int)currentStance);
        RefreshGroupSelection(mapSizeButtons, currentMapSize.HasValue ? (int)currentMapSize.Value : -1);

        // A previously earned ad bonus survives a trip back to the main menu (only LevelGenerator
        // consumes/resets it, on the next mission start) — without this, a fresh MechSetupController
        // instance has no memory of that and lets the button be clicked again pointlessly.
        if (adBonusButton != null)
            adBonusButton.interactable = loadout == null || !loadout.BonusHealthPickup;
    }

    private void NextModel()
    {
        currentModelIndex = (currentModelIndex + 1) % ModelNames.Length;
        ApplyModelSelection();
    }

    private void PreviousModel()
    {
        currentModelIndex = (currentModelIndex - 1 + ModelNames.Length) % ModelNames.Length;
        ApplyModelSelection();
    }

    private void ApplyModelSelection()
    {
        for (int i = 0; i < modelInstances.Length; i++)
        {
            if (modelInstances[i] != null)
                modelInstances[i].SetActive(i == currentModelIndex);
        }

        if (modelNameText != null)
            modelNameText.text = ModelNames[currentModelIndex];

        MechColorUtility.ApplyPlayerColor(modelInstances[currentModelIndex], currentColor);

        RefreshStatsPanel();
    }

    // Pulls the same real values MechLoadoutApplier applies in-game (Health.maxHealth/armorValue
    // per selected model, Weapon.damage) straight off the player Mech prefab, so this panel can
    // never show a made-up number. Weapon type/fire mode/stance currently don't change any of
    // these numbers on the actual mech, so they intentionally aren't reflected here — showing a
    // fake dependency would be worse than not showing one.
    private void RefreshStatsPanel()
    {
        if (statsText == null)
            return;

        if (mechPrefabApplier == null || mechPrefabHealth == null)
        {
            statsText.text = string.Empty;
            return;
        }

        MechLoadoutApplier.ModelStats stats = mechPrefabApplier.GetModelStats(currentModelIndex);

        int damage = 0;
        float cooldown = 0f;

        if (mechPrefabWeapon != null)
        {
            damage = WeaponBalance.ComputeEffectiveDamage(mechPrefabWeapon.Damage, currentWeaponType);
            cooldown = WeaponBalance.ComputeEffectiveCooldown(mechPrefabWeapon.Cooldown, currentFireMode);
        }

        statsText.text =
            "HP: " + stats.maxHealth + "\n" +
            "Броня: " + stats.armorValue + "\n" +
            "Урон: " + damage + "\n" +
            "Кулдаун: " + cooldown.ToString("0.0") + " сек\n" +
            "Скорость: " + stats.agentSpeed;
    }

    private void SelectColor(int index)
    {
        currentColor = ColorPresets[index];
        RefreshColorButtons();
        MechColorUtility.ApplyPlayerColor(modelInstances[currentModelIndex], currentColor);
    }

    private void RefreshColorButtons()
    {
        int selectedIndex = System.Array.IndexOf(ColorPresets, currentColor);

        for (int i = 0; i < colorButtons.Length; i++)
        {
            if (colorButtons[i] == null)
                continue;

            colorButtons[i].transform.localScale = i == selectedIndex ? new Vector3(1.2f, 1.2f, 1.2f) : Vector3.one;
        }
    }

    private void SelectWeaponType(WeaponType weaponType)
    {
        currentWeaponType = weaponType;
        RefreshGroupSelection(weaponButtons, (int)weaponType);
        RefreshStatsPanel();
    }

    private void SelectFireMode(WeaponFireMode fireMode)
    {
        currentFireMode = fireMode;
        RefreshGroupSelection(fireModeButtons, (int)fireMode);
        RefreshStatsPanel();
    }

    private void SelectStance(UnitStance stance)
    {
        currentStance = stance;
        RefreshGroupSelection(stanceButtons, (int)stance);
    }

    private void SelectMapSize(MapSize mapSize)
    {
        currentMapSize = mapSize;
        RefreshGroupSelection(mapSizeButtons, (int)mapSize);
    }

    private void RefreshGroupSelection(Button[] buttons, int selectedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            Image image = buttons[i].GetComponent<Image>();
            if (image != null)
                image.color = i == selectedIndex ? SelectedButtonColor : NormalButtonColor;
        }
    }

    private void StartMission()
    {
        if (MechLoadoutData.Instance == null)
        {
            GameObject loadoutObject = new GameObject("MechLoadoutData");
            loadoutObject.AddComponent<MechLoadoutData>();
        }

        MechLoadoutData loadout = MechLoadoutData.Instance;
        loadout.ResetRun();
        loadout.SelectedModelIndex = currentModelIndex;
        loadout.SelectedColor = currentColor;
        loadout.SelectedWeaponType = currentWeaponType;
        loadout.SelectedFireMode = currentFireMode;
        loadout.SelectedStance = currentStance;
        loadout.SelectedMapSize = currentMapSize;

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnAdBonusClicked()
    {
        AdsManager.Instance.ShowRewarded(
            onRewardGranted: () =>
            {
                if (MechLoadoutData.Instance == null)
                {
                    GameObject loadoutObject = new GameObject("MechLoadoutData");
                    loadoutObject.AddComponent<MechLoadoutData>();
                }

                MechLoadoutData.Instance.BonusHealthPickup = true;

                if (adBonusButton != null)
                    adBonusButton.interactable = false;
            },
            onClosedWithoutReward: null);
    }

    private static Button FindButton(string name)
    {
        GameObject buttonObject = GameObject.Find(name);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private void BindClick(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindButton(buttonName);
        if (button != null)
            button.onClick.AddListener(action);
    }
}
