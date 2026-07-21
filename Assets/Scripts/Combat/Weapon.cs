using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private WeaponType weaponType = WeaponType.Energy;
    [SerializeField] private int damage = 10;
    [SerializeField] private float range = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float heatPerShot = 10f;

    [Header("Cover")]
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private float lineOfSightHeightOffset = 1f;

    [Header("Splash (Missile)")]
    [SerializeField] private float splashRadius = 3f;
    [SerializeField] private float splashDamageMultiplier = 0.4f;
    [SerializeField] private LayerMask splashLayer = (1 << 6) | (1 << 7) | (1 << 9);

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;

    private float lastAttackTime = -999f;
    private Animator animator;

    // Per-instance scaling on top of WeaponBalance's type/mode multipliers — used by
    // EnemyLevelScaler (run-level enemy scaling) and MechLoadoutApplier (player run upgrades).
    // Defaults to 1 (no-op) so nothing changes unless one of those explicitly sets it.
    private float damageMultiplier = 1f;

    public WeaponType WeaponType => weaponType;

    public void SetWeaponType(WeaponType newWeaponType)
    {
        weaponType = newWeaponType;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    public int Damage => damage;
    public float Range => range;
    public float Cooldown => cooldown;
    public float HeatPerShot => heatPerShot;

    // Base damage modified by the weapon type multiplier (see WeaponBalance) and this instance's
    // damageMultiplier — the real number that gets dealt on hit. Whoever needs to show or apply
    // "the damage" should use this, not the raw Damage field, so combat and UI never disagree.
    public int EffectiveDamage => Mathf.RoundToInt(WeaponBalance.ComputeEffectiveDamage(damage, weaponType) * damageMultiplier);

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public bool CanAttack(ITargetable target, float cooldownMultiplier = 1f)
    {
        if (target == null)
            return false;

        if (!target.IsAlive)
            return false;

        if (Time.time < lastAttackTime + cooldown * cooldownMultiplier)
            return false;

        float distance = Vector3.Distance(transform.position, target.Transform.position);

        if (distance > range)
            return false;

        return true;
    }

    public void TryAttack(ITargetable target, float cooldownMultiplier = 1f)
    {
        if (!CanAttack(target, cooldownMultiplier))
            return;

        lastAttackTime = Time.time;

        int effectiveDamage = EffectiveDamage;

        if (animator != null)
            animator.SetTrigger("Attack");

        AudioManager.PlaySfxAtPoint(fireSound, transform.position);

        if (TryGetCoverHit(target, out RaycastHit coverHit))
        {
            ITargetable coverTarget = coverHit.collider.GetComponentInParent<ITargetable>();

            if (coverTarget != null)
            {
                coverTarget.TakeDamage(effectiveDamage, weaponType);
                Debug.Log($"{gameObject.name} ({weaponType}) hit cover ({coverHit.collider.gameObject.name}) for {effectiveDamage} damage");
            }
            else
            {
                Debug.Log($"{gameObject.name}: shot blocked by indestructible obstacle ({coverHit.collider.gameObject.name}), no damage dealt");
            }

            return;
        }

        target.TakeDamage(effectiveDamage, weaponType);

        Debug.Log($"{gameObject.name} ({weaponType}) attacked {target.Transform.gameObject.name} for {effectiveDamage} damage");

        if (weaponType == WeaponType.Missile)
            ApplySplashDamage(target, effectiveDamage);
    }

    private void ApplySplashDamage(ITargetable primaryTarget, int effectiveDamage)
    {
        Vector3 impactPoint = primaryTarget.Transform.position;
        int splashDamage = Mathf.RoundToInt(effectiveDamage * splashDamageMultiplier);

        if (splashDamage <= 0)
            return;

        Collider[] hits = Physics.OverlapSphere(impactPoint, splashRadius, splashLayer);
        HashSet<ITargetable> damagedTargets = new HashSet<ITargetable>();

        foreach (Collider hit in hits)
        {
            ITargetable splashTarget = hit.GetComponentInParent<ITargetable>();

            if (splashTarget == null || splashTarget == primaryTarget)
                continue;

            if (!splashTarget.IsAlive)
                continue;

            if (!damagedTargets.Add(splashTarget))
                continue;

            splashTarget.TakeDamage(splashDamage, weaponType);

            Debug.Log($"{gameObject.name} ({weaponType}) splash hit {splashTarget.Transform.gameObject.name} for {splashDamage} damage");
        }
    }

    private bool TryGetCoverHit(ITargetable target, out RaycastHit hit)
    {
        Vector3 heightOffset = Vector3.up * lineOfSightHeightOffset;

        Vector3 origin = transform.position + heightOffset;
        Vector3 destination = target.Transform.position + heightOffset;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            hit = default;
            return false;
        }

        return Physics.Raycast(origin, direction.normalized, out hit, distance, coverLayer);
    }
}