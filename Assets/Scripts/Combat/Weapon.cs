using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float range = 10f;
    [SerializeField] private float cooldown = 1f;

    private float lastAttackTime = -999f;

    public int Damage => damage;
    public float Range => range;
    public float Cooldown => cooldown;

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

        target.TakeDamage(damage);

        Debug.Log($"{gameObject.name} attacked {target.Transform.gameObject.name} for {damage} damage");
    }
}