using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RTSInputReader inputReader;
    [SerializeField] private GameObject panelRoot;

    [Header("Portrait & Name")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Health")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;

    [Header("Status")]
    [SerializeField] private TMP_Text stanceText;
    [SerializeField] private TMP_Text orderText;

    [Header("Stats")]
    [SerializeField] private TMP_Text statsText;

    private UnitSelectable trackedUnit;
    private Health trackedHealth;
    private MechCombat trackedCombat;
    private Weapon trackedWeapon;

    private void Update()
    {
        UnitSelectable currentUnit = GetSingleSelectedUnit();

        if (currentUnit != trackedUnit)
            TrackUnit(currentUnit);

        bool hasUnit = trackedUnit != null;

        if (panelRoot != null)
            panelRoot.SetActive(hasUnit);

        if (hasUnit)
            RefreshDisplay();
    }

    private UnitSelectable GetSingleSelectedUnit()
    {
        if (inputReader == null)
            return null;

        var selected = inputReader.GetSelectedUnits();

        if (selected.Count != 1)
            return null;

        return selected[0];
    }

    private void TrackUnit(UnitSelectable unit)
    {
        trackedUnit = unit;
        trackedHealth = unit != null ? unit.GetComponent<Health>() : null;
        trackedCombat = unit != null ? unit.GetComponent<MechCombat>() : null;
        trackedWeapon = unit != null ? unit.GetComponentInChildren<Weapon>() : null;

        if (unit == null)
            return;

        if (nameText != null)
            nameText.text = unit.gameObject.name;

        if (portraitImage != null)
        {
            portraitImage.sprite = unit.Portrait;
            portraitImage.enabled = unit.Portrait != null;
        }
    }

    private void RefreshDisplay()
    {
        if (trackedHealth != null)
        {
            if (hpText != null)
                hpText.text = $"{trackedHealth.CurrentHealth} / {trackedHealth.MaxHealth}";

            if (hpFillImage != null)
            {
                float healthFraction = trackedHealth.MaxHealth > 0
                    ? (float)trackedHealth.CurrentHealth / trackedHealth.MaxHealth
                    : 0f;

                hpFillImage.fillAmount = healthFraction;
                hpFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthFraction);
            }
        }

        if (trackedCombat != null)
        {
            if (stanceText != null)
                stanceText.text = $"Стойка: {trackedCombat.Stance}";

            if (orderText != null)
                orderText.text = $"Приказ: {trackedCombat.CurrentOrder}";
        }

        if (statsText != null && trackedWeapon != null && trackedHealth != null)
        {
            statsText.text =
                $"Урон: {trackedWeapon.Damage}\n" +
                $"Дальность: {trackedWeapon.Range}\n" +
                $"Броня: {trackedHealth.ArmorValue}";
        }
    }
}