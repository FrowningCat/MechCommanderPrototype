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

    private float lastAttackTime = -999f;

    public WeaponType WeaponType => weaponType;
    public int Damage => damage;
    public float Range => range;
    public float Cooldown => cooldown;
    public float HeatPerShot => heatPerShot;

    public bool CanAttack(ITargetable target)
    {
        if (target == null)
            return false;

        if (!target.IsAlive)
            return false;

        if (Time.time < lastAttackTime + cooldown)
            return false;

        float distance = Vector3.Distance(transform.position, target.Transform.position);

        if (distance > range)
            return false;

        return true;
    }

    public void TryAttack(ITargetable target)
    {
        if (!CanAttack(target))
            return;

        lastAttackTime = Time.time;

        ITargetable actualTarget = GetLineOfSightBlocker(target) ?? target;

        actualTarget.TakeDamage(damage);

        Debug.Log($"{gameObject.name} ({weaponType}) attacked {actualTarget.Transform.gameObject.name} for {damage} damage");
    }

    private ITargetable GetLineOfSightBlocker(ITargetable target)
    {
        Vector3 heightOffset = Vector3.up * lineOfSightHeightOffset;

        Vector3 origin = transform.position + heightOffset;
        Vector3 destination = target.Transform.position + heightOffset;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return null;

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, coverLayer))
            return null;

        return hit.collider.GetComponentInParent<ITargetable>();
    }
}