using UnityEngine;
using UnityEngine.AI;

public class MechCombat : MonoBehaviour
{
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
        if (currentTarget == null)
            return;

        if (!currentTarget.IsAlive)
        {
            currentTarget = null;
            agent.isStopped = false;
            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                currentTarget.Transform.position
            );

        if (distance > weapon.Range)
        {
            agent.isStopped = false;
            agent.SetDestination(
                currentTarget.Transform.position
            );
        }
        else
        {
            agent.isStopped = true;
            weapon.TryAttack(currentTarget);
        }
    }

    public void SetTarget(ITargetable target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }
}