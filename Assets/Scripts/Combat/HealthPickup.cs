using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private int healAmount = 50;
    [SerializeField] private LayerMask affectedLayer;
    [SerializeField] private bool skipIfFullHealth = true;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    private bool consumed;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed)
            return;

        if (!IsAffectedLayer(other.gameObject.layer))
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health == null)
            return;

        if (skipIfFullHealth && health.CurrentHealth >= health.MaxHealth)
            return;

        health.Heal(healAmount);

        consumed = true;

        AudioManager.PlaySfxAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }

    private bool IsAffectedLayer(int layer)
    {
        return (affectedLayer.value & (1 << layer)) != 0;
    }
}