using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour
{
    [Header("Extraction")]
    [SerializeField] private MissionController missionController;
    [SerializeField] private LayerMask playerUnitLayer;
    [SerializeField] private bool requireAllSurvivingUnits = false;

    private readonly HashSet<GameObject> unitsInZone = new();

    // Allows LevelGenerator to wire this to the scene's MissionController for procedurally instantiated zones.
    public void SetMissionController(MissionController controller)
    {
        missionController = controller;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerLayer(other.gameObject.layer))
            return;

        unitsInZone.Add(other.gameObject);

        CheckExtraction();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerLayer(other.gameObject.layer))
            return;

        unitsInZone.Remove(other.gameObject);
    }

    private void CheckExtraction()
    {
        if (missionController == null)
        {
            Debug.LogWarning("[ExtractionZone] Mission Controller не назначен в инспекторе — экстракция не сработает.");
            return;
        }

        if (!requireAllSurvivingUnits)
        {
            missionController.NotifyExtractionReached();
            return;
        }

        int survivingPlayerUnits = CountSurvivingPlayerUnits();

        if (unitsInZone.Count >= survivingPlayerUnits)
            missionController.NotifyExtractionReached();
    }

    private int CountSurvivingPlayerUnits()
    {
        Health[] allUnits = FindObjectsByType<Health>(FindObjectsSortMode.None);

        int count = 0;

        foreach (Health unit in allUnits)
        {
            if (IsPlayerLayer(unit.gameObject.layer))
                count++;
        }

        return count;
    }

    private bool IsPlayerLayer(int layer)
    {
        return (playerUnitLayer.value & (1 << layer)) != 0;
    }
}