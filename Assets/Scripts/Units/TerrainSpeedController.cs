using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public struct TerrainAreaSpeed
{
    [Tooltip("Просто для удобства в инспекторе, на логику не влияет")]
    public string label;

    [Tooltip("Индекс area из Window > AI > Navigation > Areas (число рядом с User N)")]
    public int areaIndex;

    public float speedMultiplier;
}

[RequireComponent(typeof(NavMeshAgent))]
public class TerrainSpeedController : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private TerrainAreaSpeed[] terrainSpeeds;
    [SerializeField] private float sampleInterval = 0.2f;
    [SerializeField] private float sampleRadius = 1f;

    private NavMeshAgent agent;
    private MechHeat heat;

    private Dictionary<int, float> areaMultipliers;
    private float baseSpeed;
    private float sampleTimer;
    private float currentTerrainMultiplier = 1f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        heat = GetComponent<MechHeat>();

        baseSpeed = agent.speed;

        BuildAreaLookup();
    }

    private void BuildAreaLookup()
    {
        areaMultipliers = new Dictionary<int, float>();

        foreach (TerrainAreaSpeed entry in terrainSpeeds)
            areaMultipliers[entry.areaIndex] = entry.speedMultiplier;
    }

    private void Update()
    {
        sampleTimer += Time.deltaTime;

        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            currentTerrainMultiplier = SampleTerrainMultiplier();
        }

        float heatMultiplier = heat != null ? heat.SpeedMultiplier : 1f;

        agent.speed = baseSpeed * currentTerrainMultiplier * heatMultiplier;
    }

    private float SampleTerrainMultiplier()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            return 1f;

        int areaIndex = AreaMaskToIndex(hit.mask);

        return areaMultipliers.TryGetValue(areaIndex, out float multiplier) ? multiplier : 1f;
    }

    private static int AreaMaskToIndex(int areaMask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((areaMask & (1 << i)) != 0)
                return i;
        }

        return 0;
    }
}