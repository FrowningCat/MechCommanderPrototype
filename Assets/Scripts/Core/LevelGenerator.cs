using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

// Runs before any other scene script's Start() (Unity guarantees all Awake() calls finish
// before any Start() call fires), so MissionController's initial unit count and
// IntroCutsceneController's cutscene both see the fully generated level.
[DefaultExecutionOrder(-1000)]
public class LevelGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct MapSizeSettings
    {
        public float areaSize;
        public int minEnemies;
        public int maxEnemies;
    }

    [Header("Testing override")]
    [SerializeField] private bool overrideMapSize = false;
    [SerializeField] private MapSize forcedMapSize = MapSize.Medium;

    [Header("Map size parameters")]
    [SerializeField] private MapSizeSettings small = new MapSizeSettings { areaSize = 58f, minEnemies = 3, maxEnemies = 4 };
    [SerializeField] private MapSizeSettings medium = new MapSizeSettings { areaSize = 98f, minEnemies = 4, maxEnemies = 6 };
    [SerializeField] private MapSizeSettings large = new MapSizeSettings { areaSize = 140f, minEnemies = 5, maxEnemies = 7 };

    [Header("Scene refs — core systems")]
    [SerializeField] private Transform groundPlane;
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private MissionController missionController;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private IntroCutsceneController introCutsceneController;
    [SerializeField] private Transform playerMech;

    [Header("Prefabs")]
    [SerializeField] private GameObject waterZonePrefab;
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject repairZonePrefab;
    [SerializeField] private GameObject extractionZonePrefab;

    [Header("Legacy decorative zones (disabled if they no longer fit the generated area)")]
    [SerializeField] private GameObject[] legacyGroundZones;

    [Header("Intro cutscene tie-ins")]
    [SerializeField] private Transform enemySilhouetteIntro;
    [SerializeField] private Transform[] deadMechWrecks;
    [SerializeField] private Transform[] introCameraShots;
    [SerializeField] private Transform[] dialogueTriggers;
    [SerializeField] private float introForwardDistance = 35f;
    [SerializeField] private float deadMechLateralJitter = 4f;

    [Header("Placement tuning")]
    [SerializeField] private float playerSpawnEdgeInset = 8f;
    [SerializeField] private float edgeMargin = 4f;
    [SerializeField] private float waterThickness = 8f;
    [SerializeField] private float enemyMinDistanceFromPlayer = 14f;
    [SerializeField] private float enemyMinDistanceFromOthers = 9f;
    [SerializeField] private float pickupMinDistanceFromOthers = 5f;
    [SerializeField] private float repairZoneForwardOffset = 8f;
    [SerializeField] private int enemiesRequiredForRepairZone = 3;
    [Tooltip("Minimum distance any spawn point (player or enemy) must keep from the water border specifically.")]
    [SerializeField] private float spawnWaterClearance = 5f;
    [SerializeField] private int playerSpawnMaxAttempts = 40;
    [SerializeField] private int enemySpawnMaxAttempts = 150;

    public MapSize GeneratedMapSize { get; private set; }
    public bool GeneratedWinOnAllEnemiesDefeated { get; private set; }

    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        MapSizeSettings settings = ResolveMapSize(out MapSize mapSize);
        GeneratedMapSize = mapSize;

        float half = settings.areaSize * 0.5f;
        int enemyCount = Random.Range(settings.minEnemies, settings.maxEnemies + 1);

        ResizeGround(half);
        BuildWaterBorder(half);
        ToggleLegacyZones(half);

        // Static geometry (ground + water) is final now — bake before anything below queries the
        // NavMesh (player spawn validation, agent placement, pathfinding).
        // BuildNavMesh() only registers the result into the live NavMesh query system (AddData())
        // when the surface component is already "active and enabled" — which, this early in Awake(),
        // it may not be yet (OnEnable for other scene objects hasn't run). Register explicitly so the
        // freshly baked mesh is queryable immediately, in this same Awake() call.
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            navMeshSurface.RemoveData();
            navMeshSurface.AddData();
        }

        Vector3 oldPlayerPos = playerMech != null ? playerMech.position : Vector3.zero;
        Vector3 newPlayerPos = FindValidPlayerSpawn(half, oldPlayerPos.y);
        Vector3 delta = newPlayerPos - oldPlayerPos;

        NavMeshAgent playerAgent = playerMech != null ? playerMech.GetComponent<NavMeshAgent>() : null;
        if (playerAgent != null)
            playerAgent.enabled = false;

        if (playerMech != null)
            playerMech.position = newPlayerPos;

        float outerHalf = half + waterThickness;
        ShiftAndClamp(introCameraShots, delta, outerHalf);
        ShiftAndClamp(dialogueTriggers, delta, outerHalf);

        bool winOnAllEnemiesDefeated = Random.value < 0.5f;
        GeneratedWinOnAllEnemiesDefeated = winOnAllEnemiesDefeated;

        float destinationZ = Mathf.Min(newPlayerPos.z + introForwardDistance, half - edgeMargin);
        Vector3 introDestination = new Vector3(0f, newPlayerPos.y, destinationZ);

        if (introCutsceneController != null)
            introCutsceneController.SetIntroDestination(introDestination);

        PlaceDeadMechs(newPlayerPos, introDestination, outerHalf);

        if (playerAgent != null)
        {
            playerAgent.enabled = true;
            playerAgent.Warp(newPlayerPos);
        }

        CleanupOldEnemySpawnPoints();
        List<Vector3> enemyPositions = GenerateEnemyPositions(enemyCount, half, newPlayerPos);
        CreateEnemySpawnPoints(enemyPositions);

        if (unitSpawner != null)
            unitSpawner.SpawnAll();

        PlaceEnemySilhouette(newPlayerPos, enemyPositions);

        List<Vector3> occupiedPoints = new List<Vector3>(enemyPositions) { newPlayerPos };
        PlaceHealthPickup(half, newPlayerPos, occupiedPoints);

        if (enemyCount > enemiesRequiredForRepairZone)
            PlaceRepairZone(half, newPlayerPos);

        if (!winOnAllEnemiesDefeated)
            PlaceExtractionZone(half, newPlayerPos);

        if (missionController != null)
            missionController.SetWinCondition(winOnAllEnemiesDefeated);
    }

    private MapSizeSettings ResolveMapSize(out MapSize mapSize)
    {
        if (overrideMapSize)
        {
            mapSize = forcedMapSize;
        }
        else if (MechLoadoutData.Instance != null && MechLoadoutData.Instance.SelectedMapSize.HasValue)
        {
            mapSize = MechLoadoutData.Instance.SelectedMapSize.Value;
        }
        else
        {
            mapSize = (MapSize)Random.Range(0, 3);
        }

        return mapSize switch
        {
            MapSize.Small => small,
            MapSize.Large => large,
            _ => medium,
        };
    }

    private void ResizeGround(float half)
    {
        if (groundPlane == null)
            return;

        float outerSize = (half + waterThickness) * 2f;
        // Unity's default Plane mesh is 10x10 at scale 1.
        float scale = outerSize / 10f;
        groundPlane.localScale = new Vector3(scale, groundPlane.localScale.y, scale);
    }

    private void ToggleLegacyZones(float half)
    {
        if (legacyGroundZones == null)
            return;

        float limit = half - edgeMargin;

        foreach (GameObject zone in legacyGroundZones)
        {
            if (zone == null)
                continue;

            Vector3 p = zone.transform.position;
            bool fits = Mathf.Abs(p.x) <= limit && Mathf.Abs(p.z) <= limit;
            zone.SetActive(fits);
        }
    }

    private void BuildWaterBorder(float half)
    {
        if (waterZonePrefab == null)
        {
            Debug.LogWarning("[LevelGenerator] waterZonePrefab не назначен — водная граница не создана.");
            return;
        }

        Transform root = new GameObject("GeneratedWaterBorder").transform;
        root.SetParent(transform, false);

        float outer = half + waterThickness;
        float edgeCenter = half + waterThickness * 0.5f;

        CreateWaterSegment(root, new Vector3(0f, 0f, edgeCenter), 0f, outer * 2f);
        CreateWaterSegment(root, new Vector3(0f, 0f, -edgeCenter), 0f, outer * 2f);
        CreateWaterSegment(root, new Vector3(edgeCenter, 0f, 0f), 90f, half * 2f);
        CreateWaterSegment(root, new Vector3(-edgeCenter, 0f, 0f), 90f, half * 2f);
    }

    private void CreateWaterSegment(Transform parent, Vector3 worldPos, float rotationY, float length)
    {
        GameObject segment = Instantiate(waterZonePrefab, worldPos, Quaternion.Euler(0f, rotationY, 0f), parent);
        segment.name = "WaterSegment";

        Transform groundWater = segment.transform.Find("Ground_Water");
        if (groundWater == null)
        {
            Debug.LogWarning("[LevelGenerator] TerrainZone_Water.prefab не содержит 'Ground_Water' — сегмент воды пропущен.");
            return;
        }

        Transform zoneWater = groundWater.Find("Zone_Water");
        if (zoneWater != null)
            zoneWater.SetParent(segment.transform, false);

        groundWater.localPosition = new Vector3(0f, 0.2f, 0f);
        groundWater.localRotation = Quaternion.identity;
        groundWater.localScale = new Vector3(length, 0.4f, waterThickness);

        if (zoneWater == null)
            return;

        zoneWater.localPosition = new Vector3(0f, 1.5f, 0f);
        zoneWater.localRotation = Quaternion.identity;
        zoneWater.localScale = Vector3.one;

        NavMeshModifierVolume modifier = zoneWater.GetComponent<NavMeshModifierVolume>();
        if (modifier != null)
        {
            modifier.size = new Vector3(length, 4f, waterThickness);
            modifier.center = Vector3.zero;
        }
    }

    private void ShiftAndClamp(Transform[] targets, Vector3 delta, float outerHalf)
    {
        if (targets == null)
            return;

        foreach (Transform t in targets)
        {
            if (t == null)
                continue;

            t.position = ClampToBounds(t.position + delta, outerHalf);
        }
    }

    private Vector3 ClampToBounds(Vector3 pos, float outerHalf)
    {
        float limit = outerHalf - 1f;
        pos.x = Mathf.Clamp(pos.x, -limit, limit);
        pos.z = Mathf.Clamp(pos.z, -limit, limit);
        return pos;
    }

    // Rejects points that don't sample onto the NavMesh at all (e.g. landing inside a building's
    // carved-out footprint), and points that are too close to the water border specifically.
    // Water is a known ring starting exactly at |x| or |z| > half (see BuildWaterBorder), so we
    // measure clearance analytically against that boundary instead of NavMesh.FindClosestEdge —
    // FindClosestEdge also fires on ordinary building/cover edges scattered across the interior,
    // which made it reject the vast majority of the map and isn't what we're trying to avoid here.
    // Shared by the player spawn and every enemy spawn so both use exactly the same rule.
    private bool TryValidateSpawnPoint(Vector3 candidate, float half, out Vector3 validPosition)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit sample, 3f, NavMesh.AllAreas))
        {
            float distanceToWaterBoundary = half - Mathf.Max(Mathf.Abs(sample.position.x), Mathf.Abs(sample.position.z));

            if (distanceToWaterBoundary >= spawnWaterClearance)
            {
                validPosition = sample.position;
                return true;
            }
        }

        validPosition = default;
        return false;
    }

    // Requires the NavMesh to already be baked and registered (see Generate()).
    private Vector3 FindValidPlayerSpawn(float half, float y)
    {
        float limit = half - edgeMargin;
        Vector3 preferred = new Vector3(0f, y, -(half - playerSpawnEdgeInset));

        for (int attempt = 0; attempt < playerSpawnMaxAttempts; attempt++)
        {
            Vector3 candidate = attempt == 0
                ? preferred
                : new Vector3(Random.Range(-limit, limit), y, Random.Range(-limit, limit));

            if (TryValidateSpawnPoint(candidate, half, out Vector3 validPosition))
                return validPosition;
        }

        Debug.LogWarning($"[LevelGenerator] Не удалось найти валидную точку спавна игрока за {playerSpawnMaxAttempts} попыток — использую центр карты как запасной вариант.");

        Vector3 center = new Vector3(0f, y, 0f);
        if (NavMesh.SamplePosition(center, out NavMeshHit fallback, 10f, NavMesh.AllAreas))
            return fallback.position;

        return center;
    }

    private void PlaceDeadMechs(Vector3 pathStart, Vector3 pathEnd, float outerHalf)
    {
        if (deadMechWrecks == null)
            return;

        Vector3 pathDir = (pathEnd - pathStart);
        pathDir.y = 0f;
        Vector3 lateral = pathDir.sqrMagnitude > 0.01f
            ? Vector3.Cross(Vector3.up, pathDir.normalized)
            : Vector3.right;

        int count = deadMechWrecks.Length;

        for (int i = 0; i < count; i++)
        {
            Transform wreck = deadMechWrecks[i];
            if (wreck == null)
                continue;

            float t = (i + 1f) / (count + 1f);
            Vector3 basePos = Vector3.Lerp(pathStart, pathEnd, t);
            Vector3 offset = lateral * Random.Range(-deadMechLateralJitter, deadMechLateralJitter);
            Vector3 newPos = ClampToBounds(basePos + offset, outerHalf);

            wreck.position = new Vector3(newPos.x, wreck.position.y, newPos.z);
        }
    }

    private void CleanupOldEnemySpawnPoints()
    {
        SpawnPoint[] existing = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (SpawnPoint sp in existing)
        {
            if (sp.Type == SpawnPointType.Enemy)
                Destroy(sp.gameObject);
        }
    }

    private List<Vector3> GenerateEnemyPositions(int count, float half, Vector3 playerPos)
    {
        List<Vector3> result = new List<Vector3>(count);
        float limit = half - edgeMargin;

        for (int i = 0; i < count; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < enemySpawnMaxAttempts && !placed; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(-limit, limit),
                    playerPos.y,
                    Random.Range(-limit, limit));

                if (!TryValidateSpawnPoint(candidate, half, out Vector3 validPosition))
                    continue;

                if (Vector3.Distance(validPosition, playerPos) < enemyMinDistanceFromPlayer)
                    continue;

                if (TooCloseToAny(validPosition, result, enemyMinDistanceFromOthers))
                    continue;

                result.Add(validPosition);
                placed = true;
            }

            if (!placed)
            {
                Debug.LogWarning($"[LevelGenerator] Не удалось найти валидную точку спавна врага #{i} за {enemySpawnMaxAttempts} попыток — использую последнюю попавшуюся позицию без полной проверки.");

                Vector3 fallback = new Vector3(Random.Range(-limit, limit), playerPos.y, Random.Range(-limit, limit));
                if (TryValidateSpawnPoint(fallback, half, out Vector3 validFallback))
                    fallback = validFallback;

                result.Add(fallback);
            }
        }

        return result;
    }

    private bool TooCloseToAny(Vector3 candidate, List<Vector3> points, float minDistance)
    {
        foreach (Vector3 p in points)
        {
            if (Vector3.Distance(candidate, p) < minDistance)
                return true;
        }

        return false;
    }

    private void CreateEnemySpawnPoints(List<Vector3> positions)
    {
        Transform root = new GameObject("GeneratedEnemySpawns").transform;
        root.SetParent(transform, false);

        foreach (Vector3 pos in positions)
        {
            GameObject go = new GameObject("Spawn_Enemy_Generated");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.AddComponent<SpawnPoint>();
        }
    }

    private void PlaceEnemySilhouette(Vector3 playerPos, List<Vector3> enemyPositions)
    {
        if (enemySilhouetteIntro == null || enemyPositions.Count == 0)
            return;

        Vector3 farthest = enemyPositions[0];
        float best = -1f;

        foreach (Vector3 p in enemyPositions)
        {
            float d = Vector3.Distance(p, playerPos);
            if (d > best)
            {
                best = d;
                farthest = p;
            }
        }

        enemySilhouetteIntro.position = new Vector3(farthest.x, enemySilhouetteIntro.position.y, farthest.z);

        Vector3 lookDir = playerPos - enemySilhouetteIntro.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.01f)
            enemySilhouetteIntro.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    private void PlaceHealthPickup(float half, Vector3 playerPos, List<Vector3> avoidPoints)
    {
        if (healthPickupPrefab == null)
            return;

        int count = 1;
        if (MechLoadoutData.Instance != null && MechLoadoutData.Instance.BonusHealthPickup)
        {
            count = 2;
            MechLoadoutData.Instance.BonusHealthPickup = false;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = FindValidPoint(half, playerPos, avoidPoints, pickupMinDistanceFromOthers);
            Instantiate(healthPickupPrefab, pos, Quaternion.identity);
            avoidPoints.Add(pos);
        }
    }

    private void PlaceRepairZone(float half, Vector3 playerPos)
    {
        if (repairZonePrefab == null)
            return;

        Vector3 pos = ClampToBounds(playerPos + Vector3.forward * repairZoneForwardOffset, half);
        Instantiate(repairZonePrefab, pos, Quaternion.identity);
    }

    private void PlaceExtractionZone(float half, Vector3 playerPos)
    {
        if (extractionZonePrefab == null)
            return;

        Vector3 pos = new Vector3(0f, playerPos.y, half - edgeMargin);
        GameObject zone = Instantiate(extractionZonePrefab, pos, Quaternion.identity);

        float scaleX = Mathf.Clamp(half * 0.5f, 4f, 10f);
        Vector3 scale = zone.transform.localScale;
        zone.transform.localScale = new Vector3(scaleX, scale.y, scale.z);

        ExtractionZone extractionZone = zone.GetComponent<ExtractionZone>();
        if (extractionZone != null)
            extractionZone.SetMissionController(missionController);
    }

    private Vector3 FindValidPoint(float half, Vector3 playerPos, List<Vector3> avoidPoints, float minDistance)
    {
        float limit = half - edgeMargin;

        for (int attempt = 0; attempt < 500; attempt++)
        {
            Vector3 candidate = new Vector3(Random.Range(-limit, limit), playerPos.y, Random.Range(-limit, limit));

            if (!TooCloseToAny(candidate, avoidPoints, minDistance))
                return candidate;
        }

        return new Vector3(Random.Range(-limit, limit), playerPos.y, Random.Range(-limit, limit));
    }
}
