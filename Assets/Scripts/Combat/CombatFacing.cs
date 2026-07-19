using UnityEngine;
using UnityEngine.AI;

// Shared by MechCombat and EnemyAI so both turn to face their attack target the same way,
// without fighting the NavMeshAgent's own movement-driven rotation.
public static class CombatFacing
{
    public static void FaceTarget(Transform self, NavMeshAgent agent, Vector3 targetPosition, float rotationSpeedDegreesPerSecond)
    {
        if (agent != null)
            agent.updateRotation = false;

        Vector3 direction = targetPosition - self.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        self.rotation = Quaternion.RotateTowards(self.rotation, targetRotation, rotationSpeedDegreesPerSecond * Time.deltaTime);
    }

    public static void ResumeAgentRotation(NavMeshAgent agent)
    {
        if (agent != null)
            agent.updateRotation = true;
    }
}
