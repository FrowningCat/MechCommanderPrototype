using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask targetLayer;

    private NavMeshAgent agent;
    private Weapon weapon;
    private ITargetable currentTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        weapon = GetComponent<Weapon>();
    }

    private void Update()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
            currentTarget = FindNearestTarget();

        if (currentTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, currentTarget.Transform.position);

        if (distance > weapon.Range)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.Transform.position);
        }
        else
        {
            agent.isStopped = true;
            weapon.TryAttack(currentTarget);
        }
    }

    private ITargetable FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            targetLayer
        );

        ITargetable nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            ITargetable target = hit.GetComponentInParent<ITargetable>();

            if (target == null || !target.IsAlive)
                continue;

            float distance = Vector3.Distance(transform.position, target.Transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}