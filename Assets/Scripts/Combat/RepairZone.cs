using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RepairZone : MonoBehaviour
{
    [Header("Repair")]
    [SerializeField] private int healPerTick = 5;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private LayerMask affectedLayer;

    private readonly HashSet<Health> unitsInZone = new();
    private float tickTimer;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (unitsInZone.Count == 0)
            return;

        tickTimer += Time.deltaTime;

        if (tickTimer < tickInterval)
            return;

        tickTimer = 0f;

        HealUnitsInZone();
    }

    private void HealUnitsInZone()
    {
        unitsInZone.RemoveWhere(health => health == null);

        foreach (Health health in unitsInZone)
            health.Heal(healPerTick);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAffectedLayer(other.gameObject.layer))
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health != null)
            unitsInZone.Add(health);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAffectedLayer(other.gameObject.layer))
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health != null)
            unitsInZone.Remove(health);
    }

    private bool IsAffectedLayer(int layer)
    {
        return (affectedLayer.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);

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