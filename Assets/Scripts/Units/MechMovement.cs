using UnityEngine;
using UnityEngine.AI;

public class MechMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 point)
    {
        agent.SetDestination(point);
    }
}