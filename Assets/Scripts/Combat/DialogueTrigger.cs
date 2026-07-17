using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private string speakerName;
    [TextArea(2, 5)]
    [SerializeField] private string message;
    [SerializeField] private float autoHideDelay = 5f;
    [SerializeField] private LayerMask affectedLayer;
    [SerializeField] private bool oneShot = true;

    [Header("Audio")]
    [SerializeField] private AudioClip voiceClip;

    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;

    private bool triggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot)
            return;

        if (!IsAffectedLayer(other.gameObject.layer))
            return;

        if (other.GetComponentInParent<Health>() == null)
            return;

        triggered = true;

        if (dialogueUI != null)
            dialogueUI.ShowMessage(speakerName, message, autoHideDelay);

        AudioManager.PlaySfxAtPoint(voiceClip, transform.position);

        if (oneShot)
            enabled = false;
    }

    private bool IsAffectedLayer(int layer)
    {
        return (affectedLayer.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.2f);

        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (zoneCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(
                transform.TransformPoint(sphere.center),
                sphere.radius * transform.lossyScale.x
            );
        }
    }
}
