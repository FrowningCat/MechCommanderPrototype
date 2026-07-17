using UnityEngine;

public class MechWeaponSystem : MonoBehaviour
{
    [Header("Fire Mode")]
    [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.AlphaStrike;

    private Weapon[] weapons;
    private MechHeat heat;

    private float minRange;
    private float maxRange;

    private int chainFireIndex;

    public bool HasWeapons => weapons != null && weapons.Length > 0;
    public WeaponFireMode FireMode => fireMode;

    public void SetFireMode(WeaponFireMode newFireMode)
    {
        fireMode = newFireMode;
    }

    public float EffectiveRange
    {
        get
        {
            if (!HasWeapons)
                return 0f;

            return fireMode == WeaponFireMode.AlphaStrike ? minRange : maxRange;
        }
    }

    private void Awake()
    {
        weapons = GetComponentsInChildren<Weapon>();
        heat = GetComponent<MechHeat>();

        CacheRanges();
    }

    private void CacheRanges()
    {
        if (!HasWeapons)
        {
            minRange = 0f;
            maxRange = 0f;
            return;
        }

        minRange = float.MaxValue;
        maxRange = 0f;

        foreach (Weapon weapon in weapons)
        {
            minRange = Mathf.Min(minRange, weapon.Range);
            maxRange = Mathf.Max(maxRange, weapon.Range);
        }
    }

    public void TryFireAt(ITargetable target)
    {
        if (target == null || !HasWeapons)
            return;

        if (heat != null && heat.IsOverheated)
            return;

        if (fireMode == WeaponFireMode.AlphaStrike)
            FireAll(target);
        else
            FireChain(target);
    }

    private void FireAll(ITargetable target)
    {
        foreach (Weapon weapon in weapons)
            TryFireWeapon(weapon, target);
    }

    private void FireChain(ITargetable target)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            int index = (chainFireIndex + i) % weapons.Length;

            if (TryFireWeapon(weapons[index], target))
            {
                chainFireIndex = (index + 1) % weapons.Length;
                return;
            }
        }
    }

    private bool TryFireWeapon(Weapon weapon, ITargetable target)
    {
        if (!weapon.CanAttack(target))
            return false;

        weapon.TryAttack(target);

        if (heat != null)
            heat.AddHeat(weapon.HeatPerShot);

        return true;
    }
}