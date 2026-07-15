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

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;

    private float lastAttackTime = -999f;
    private Animator animator;

    public WeaponType WeaponType => weaponType;
    public int Damage => damage;
    public float Range => range;
    public float Cooldown => cooldown;
    public float HeatPerShot => heatPerShot;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

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

        if (animator != null)
            animator.SetTrigger("Attack");

        AudioManager.PlaySfxAtPoint(fireSound, transform.position);

        if (TryGetCoverHit(target, out RaycastHit coverHit))
        {
            ITargetable coverTarget = coverHit.collider.GetComponentInParent<ITargetable>();

            if (coverTarget != null)
            {
                coverTarget.TakeDamage(damage);
                Debug.Log($"{gameObject.name} ({weaponType}) hit cover ({coverHit.collider.gameObject.name}) for {damage} damage");
            }
            else
            {
                Debug.Log($"{gameObject.name}: shot blocked by indestructible obstacle ({coverHit.collider.gameObject.name}), no damage dealt");
            }

            return;
        }

        target.TakeDamage(damage);

        Debug.Log($"{gameObject.name} ({weaponType}) attacked {target.Transform.gameObject.name} for {damage} damage");
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