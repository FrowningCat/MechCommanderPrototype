using UnityEngine;
using UnityEngine.AI;

public class MechMovement : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip footstepLoop;
    [SerializeField] private float movementThreshold = 0.1f;

    private NavMeshAgent agent;
    private AudioSource footstepSource;
    private Animator animator;

    // Used by RTSInputReader to keep formation slots far enough apart that agents
    // don't get clamped onto overlapping NavMesh points on narrow paths.
    public float AgentRadius => agent != null ? agent.radius : 0.5f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        if (footstepLoop != null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.clip = footstepLoop;
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        bool isMoving = agent.velocity.magnitude > movementThreshold;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);

        if (footstepSource == null)
            return;

        if (isMoving && !footstepSource.isPlaying)
            footstepSource.Play();
        else if (!isMoving && footstepSource.isPlaying)
            footstepSource.Stop();
    }

    public void MoveTo(Vector3 point)
    {
        agent.SetDestination(point);
    }
}