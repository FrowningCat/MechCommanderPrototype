using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MechLoadoutApplier : MonoBehaviour
{
    [Header("Model Variants (index-matched to setup screen)")]
    [SerializeField] private GameObject[] modelVariants;

    private void Awake()
    {
        if (MechLoadoutData.Instance == null)
            return;

        MechLoadoutData loadout = MechLoadoutData.Instance;

        GameObject activeModel = ApplyModel(loadout.SelectedModelIndex);
        MechColorUtility.ApplyPlayerColor(activeModel, loadout.SelectedColor);

        Weapon weapon = GetComponent<Weapon>();
        if (weapon != null)
            weapon.SetWeaponType(loadout.SelectedWeaponType);

        MechWeaponSystem weaponSystem = GetComponent<MechWeaponSystem>();
        if (weaponSystem != null)
            weaponSystem.SetFireMode(loadout.SelectedFireMode);

        MechCombat combat = GetComponent<MechCombat>();
        if (combat != null)
            combat.SetStance(loadout.SelectedStance);
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
