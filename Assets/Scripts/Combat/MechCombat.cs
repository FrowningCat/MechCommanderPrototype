using UnityEngine;
using UnityEngine.AI;

public class MechCombat : MonoBehaviour
{
    [Header("Auto Attack")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private LayerMask enemyLayer;

    private NavMeshAgent agent;
    private Weapon weapon;

    private ITargetable currentTarget;
    private bool attackMoveEnabled;

    private Vector3 attackMoveDestination;
    private bool hasAttackMoveDestination;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        weapon = GetComponent<Weapon>();
    }

    private void Update()
    {
        if (attackMoveEnabled)
        {
            HandleAttackMove();
            return;
        }

        HandleDirectTarget();
    }

    public void SetTarget(ITargetable target)
    {
        attackMoveEnabled = false;
        hasAttackMoveDestination = false;
        currentTarget = target;
    }

    public void SetAttackMoveDestination(Vector3 destination)
    {
        attackMoveEnabled = true;
        hasAttackMoveDestination = true;
        attackMoveDestination = destination;
        currentTarget = null;

        agent.isStopped = false;
        agent.SetDestination(attackMoveDestination);
    }

    public void ClearTarget()
    {
        attackMoveEnabled = false;
        hasAttackMoveDestination = false;
        currentTarget = null;
        agent.isStopped = false;
    }

    private void HandleAttackMove()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
        {
            currentTarget = FindNearestEnemy();

            if (currentTarget == null)
            {
                ContinueAttackMoveDestination();
                return;
            }
        }

        HandleCombatAgainstCurrentTarget();
    }

    private void HandleDirectTarget()
    {
        if (currentTarget == null)
            return;

        if (!currentTarget.IsAlive)
        {
            currentTarget = null;
            agent.isStopped = false;
            return;
        }

        HandleCombatAgainstCurrentTarget();
    }

    private void HandleCombatAgainstCurrentTarget()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
            return;

        float distance = Vector3.Distance(
            transform.position,
            currentTarget.Transform.position
        );

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

    private void ContinueAttackMoveDestination()
    {
        if (!hasAttackMoveDestination)
            return;

        agent.isStopped = false;
        agent.SetDestination(attackMoveDestination);
    }

    private ITargetable FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            enemyLayer
        );

        ITargetable nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            ITargetable target = hit.GetComponentInParent<ITargetable>();

            if (target == null || !target.IsAlive)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                target.Transform.position
            );

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