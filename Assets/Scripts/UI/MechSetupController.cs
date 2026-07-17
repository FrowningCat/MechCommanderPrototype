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

    private static readonly string[] ModelNames = { "George", "Leela", "Mike", "Stan" };
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

    private Text modelNameText;

    private int currentModelIndex;
    private Color currentColor;
    private WeaponType currentWeaponType;
    private WeaponFireMode currentFireMode;
    private UnitStance currentStance;

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

        GameObject modelNameObject = GameObject.Find("Text_ModelName");
        modelNameText = modelNameObject != null ? modelNameObject.GetComponent<Text>() : null;

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

        BindClick("Btn_Start", StartMission);
        BindClick("Btn_Back", BackToMainMenu);

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
        }
        else
        {
            currentModelIndex = 0;
            currentColor = ColorPresets[0];
            currentWeaponType = WeaponType.Energy;
            currentFireMode = WeaponFireMode.AlphaStrike;
            currentStance = UnitStance.Defensive;
        }

        ApplyModelSelection();
        RefreshColorButtons();
        RefreshGroupSelection(weaponButtons, (int)currentWeaponType);
        RefreshGroupSelection(fireModeButtons, (int)currentFireMode);
        RefreshGroupSelection(stanceButtons, (int)currentStance);
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
    }

    private void SelectFireMode(WeaponFireMode fireMode)
    {
        currentFireMode = fireMode;
        RefreshGroupSelection(fireModeButtons, (int)fireMode);
    }

    private void SelectStance(UnitStance stance)
    {
        currentStance = stance;
        RefreshGroupSelection(stanceButtons, (int)stance);
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
        loadout.SelectedModelIndex = currentModelIndex;
        loadout.SelectedColor = currentColor;
        loadout.SelectedWeaponType = currentWeaponType;
        loadout.SelectedFireMode = currentFireMode;
        loadout.SelectedStance = currentStance;

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
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
