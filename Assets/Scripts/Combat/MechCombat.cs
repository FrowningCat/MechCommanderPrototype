using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MechCombat : MonoBehaviour
{
    [Header("Auto Attack")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Stance")]
    [SerializeField] private UnitStance stance = UnitStance.Defensive;
    [SerializeField] private float leashRange = 14f;

    [Header("Guard")]
    [SerializeField] private float guardFollowDistance = 4f;

    [Header("Patrol")]
    [SerializeField] private float patrolWaypointThreshold = 0.5f;

    [Header("Combat Facing")]
    [SerializeField] private float combatRotationSpeed = 240f;

    private NavMeshAgent agent;
    private MechWeaponSystem weaponSystem;

    private UnitOrder currentOrder = UnitOrder.None;
    private ITargetable currentTarget;

    private Vector3 orderPosition;
    private Vector3 anchorPosition;

    private readonly List<Vector3> patrolPoints = new();
    private int patrolIndex;

    private Transform guardTarget;

    public UnitOrder CurrentOrder => currentOrder;
    public UnitStance Stance => stance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        weaponSystem = GetComponent<MechWeaponSystem>();

        anchorPosition = transform.position;
        orderPosition = transform.position;
    }

    private void Update()
    {
        switch (currentOrder)
        {
            case UnitOrder.None:
                HandleIdle();
                break;
            case UnitOrder.Move:
                HandleMove();
                break;
            case UnitOrder.AttackMove:
                HandleAttackMove();
                break;
            case UnitOrder.AttackTarget:
                HandleAttackTarget();
                break;
            case UnitOrder.HoldPosition:
                HandleHoldPosition();
                break;
            case UnitOrder.Patrol:
                HandlePatrol();
                break;
            case UnitOrder.Guard:
                HandleGuard();
                break;
        }
    }

    // ---------- Public order API ----------

    public void MoveTo(Vector3 destination)
    {
        SetOrder(UnitOrder.Move);

        orderPosition = destination;

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void AttackMoveTo(Vector3 destination)
    {
        SetOrder(UnitOrder.AttackMove);

        orderPosition = destination;

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void SetTarget(ITargetable target)
    {
        SetOrder(UnitOrder.AttackTarget);

        currentTarget = target;
    }

    public void HoldPosition()
    {
        SetOrder(UnitOrder.HoldPosition);

        orderPosition = transform.position;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void SetPatrol(Vector3 pointB)
    {
        SetOrder(UnitOrder.Patrol);

        patrolPoints.Add(transform.position);
        patrolPoints.Add(pointB);
        patrolIndex = 1;

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[patrolIndex]);
    }

    public void GuardTarget(Transform target)
    {
        if (target == null)
            return;

        SetOrder(UnitOrder.Guard);

        guardTarget = target;

        agent.isStopped = false;
    }

    public void Stop()
    {
        SetOrder(UnitOrder.None);

        anchorPosition = transform.position;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void SetStance(UnitStance newStance)
    {
        stance = newStance;
    }

    private void SetOrder(UnitOrder order)
    {
        currentOrder = order;
        currentTarget = null;
        guardTarget = null;
        patrolPoints.Clear();

        // Any freshly issued order may involve movement — release the combat-facing override
        // so the agent goes back to steering rotation from its own path direction. Engage
        // handlers turn it off again on their own each frame if/while actually firing in place.
        CombatFacing.ResumeAgentRotation(agent);
    }

    // ---------- Order handlers ----------

    private void HandleIdle()
    {
        if (TryAutoEngage(anchorPosition, useLeash: stance == UnitStance.Defensive))
            return;

        ReturnToAnchor();
    }

    private void HandleMove()
    {
        if (stance == UnitStance.Aggressive &&
            TryAutoEngage(orderPosition, useLeash: false))
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(orderPosition);
    }

    private void HandleAttackMove()
    {
        if (TryAutoEngage(orderPosition, useLeash: false, ignoreStance: true))
            return;

        agent.isStopped = false;
        agent.SetDestination(orderPosition);
    }

    private void HandleAttackTarget()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
        {
            Stop();
            return;
        }

        EngageTarget();
    }

    private void HandleHoldPosition()
    {
        agent.isStopped = true;

        if (stance == UnitStance.Passive)
            return;

        if (currentTarget == null || !currentTarget.IsAlive)
            currentTarget = FindNearestEnemy();

        if (currentTarget != null)
            EngageInPlace();
    }

    private void HandlePatrol()
    {
        if (patrolPoints.Count < 2)
            return;

        if (TryAutoEngage(transform.position, useLeash: stance == UnitStance.Defensive))
            return;

        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= patrolWaypointThreshold)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;

        agent.SetDestination(patrolPoints[patrolIndex]);
    }

    private void HandleGuard()
    {
        if (guardTarget == null)
        {
            Stop();
            return;
        }

        if (TryAutoEngage(guardTarget.position, useLeash: stance == UnitStance.Defensive))
            return;

        agent.isStopped = false;

        float distanceToGuardTarget = Vector3.Distance(transform.position, guardTarget.position);

        if (distanceToGuardTarget > guardFollowDistance)
            agent.SetDestination(guardTarget.position);
        else
            agent.ResetPath();
    }

    // ---------- Shared combat helpers ----------

    private bool TryAutoEngage(Vector3 anchor, bool useLeash, bool ignoreStance = false)
    {
        if (!ignoreStance && stance == UnitStance.Passive)
            return false;

        if (currentTarget == null || !currentTarget.IsAlive)
            currentTarget = FindNearestEnemy();

        if (currentTarget == null)
            return false;

        if (useLeash)
        {
            float anchorDistance = Vector3.Distance(anchor, currentTarget.Transform.position);

            if (anchorDistance > leashRange)
            {
                currentTarget = null;
                return false;
            }
        }

        EngageTarget();
        return true;
    }

    private void EngageTarget()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.Transform.position);

        if (distance > weaponSystem.EffectiveRange)
        {
            CombatFacing.ResumeAgentRotation(agent);
            agent.isStopped = false;
            agent.SetDestination(currentTarget.Transform.position);
        }
        else
        {
            agent.isStopped = true;
            CombatFacing.FaceTarget(transform, agent, currentTarget.Transform.position, combatRotationSpeed);
            weaponSystem.TryFireAt(currentTarget);
        }
    }

    private void EngageInPlace()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.Transform.position);

        if (distance <= weaponSystem.EffectiveRange)
        {
            CombatFacing.FaceTarget(transform, agent, currentTarget.Transform.position, combatRotationSpeed);
            weaponSystem.TryFireAt(currentTarget);
        }
    }

    private void ReturnToAnchor()
    {
        if (Vector3.Distance(transform.position, anchorPosition) <= patrolWaypointThreshold)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(anchorPosition);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (stance == UnitStance.Defensive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Application.isPlaying ? anchorPosition : transform.position, leashRange);
        }

        if (Application.isPlaying && currentOrder == UnitOrder.Guard && guardTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(guardTarget.position, guardFollowDistance);
        }
    }
}