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

    public enum MapShape
    {
        Square,
        Elongated,
    }

    private struct GroundBounds
    {
        public float minX, maxX, minZ, maxZ;
    }

    [Header("Testing override")]
    [SerializeField] private bool overrideMapSize = false;
    [SerializeField] private MapSize forcedMapSize = MapSize.Medium;

    [Header("Map size parameters")]
    [SerializeField] private MapSizeSettings small = new MapSizeSettings { areaSize = 40f, minEnemies = 1, maxEnemies = 2 };
    [SerializeField] private MapSizeSettings medium = new MapSizeSettings { areaSize = 70f, minEnemies = 4, maxEnemies = 6 };
    [SerializeField] private MapSizeSettings large = new MapSizeSettings { areaSize = 100f, minEnemies = 5, maxEnemies = 7 };

    [Header("Scene refs — core systems")]
    [SerializeField] private Transform groundPlane;
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private MissionController missionController;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private IntroCutsceneController introCutsceneController;
    [SerializeField] private Transform playerMech;
    [SerializeField] private RTSCameraController rtsCameraController;

    [Header("Prefabs")]
    [SerializeField] private GameObject waterZonePrefab;
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject repairZonePrefab;
    [SerializeField] private GameObject extractionZonePrefab;

    [Header("Boss levels (Stage 35)")]
    [Tooltip("Every Nth run level (3, 6, 9, ...) spawns a single boss instead of the normal enemy roster.")]
    [SerializeField] private int bossLevelInterval = 3;
    [Tooltip("Visual/stat base for the boss — Enemy_Variant2 (Mike) chosen because it's already the tankiest roster entry (highest base HP/armor, slowest), matching a boss archetype better than a bigger-but-squishier variant; see Stage 35 report.")]
    [SerializeField] private GameObject bossEnemyPrefab;
    [Tooltip("Extra multiplier stacked on top of EnemyLevelScaler's usual per-level scaling for the boss only (e.g. 1.3 = +30%).")]
    [SerializeField] private float bossExtraStatMultiplier = 1.3f;
    [Tooltip("Uniform scale multiplier applied to the boss instance's transform so it visually reads as bigger than a normal enemy.")]
    [SerializeField] private float bossVisualScale = 1.4f;

    [Header("Level objective UI (Stage 35)")]
    [Tooltip("Shown briefly at level start with the win condition (defeat all enemies / reach extraction).")]
    [SerializeField] private TMPro.TMP_Text levelObjectiveText;
    [SerializeField] private float levelObjectiveDisplayDuration = 4f;

    [Header("Legacy decorative zones (repositioned/rescaled to match the generated area)")]
    [SerializeField] private GameObject[] legacyGroundZones;
    [Tooltip("The areaSize the legacyGroundZones' authored positions/scales were hand-placed for (currently the Large preset, 100). Every zone is uniformly scaled from the world origin by (current areaSize / this value), so the same hand-authored road/mud/rocks layout grows or shrinks with the generated map instead of clipping or leaving gaps.")]
    [SerializeField] private float terrainZoneReferenceAreaSize = 100f;
    [Tooltip("Stage 34: alternate hand-placed terrain zone layout for elongated (~2:1) maps, used instead of legacyGroundZones when MapShape.Elongated is picked. NOT authored yet — this array is empty until someone hand-places a long-rectangle road/mud/rocks layout in the scene the same way legacyGroundZones was (around the world origin, sized for terrainZoneReferenceAreaSize). Scaling non-uniformly to stretch the existing square layout would distort the art, so this needs its own authored set rather than a scale trick; ResolveMapShape() below falls back to Square whenever this is empty, so nothing breaks in the meantime.")]
    [SerializeField] private GameObject[] elongatedLegacyGroundZones;
    [Tooltip("Chance of picking MapShape.Elongated over Square when elongatedLegacyGroundZones is populated.")]
    [SerializeField] private float elongatedMapChance = 0.5f;

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
    [Tooltip("Gap left between the real NavMesh bounding box edge and the inner edge of the water ring, so the water never overlaps real dry ground (the bounding box is not a square — e.g. a road segment's tip can stick out past the map's nominal half-size).")]
    [SerializeField] private float waterInnerGap = 2f;
    [Tooltip("The hand-placed terrain zones' visible ground mesh (Ground_Mud/Rocks/Road) doesn't line up exactly with the NavMesh bounding box the water ring is built from (the mesh runs a bit past the actual walkable triangulation), which used to leave a thin gap of bare skybox-lit backdrop plane showing through as a dark seam right at the land/water border (Stage 36). This only grows the RENDERED water quad (not its NavMeshModifierVolume carve-out) so it tucks under the land mesh's edge and closes that gap, on every side, for every map size.")]
    [SerializeField] private float waterVisualOverlap = 3f;
    [Tooltip("The water ring's outer edge extends this many times the NavMesh bounding box's largest dimension beyond its own edge, so water is always well past the camera's pan/zoom limits (see cameraBoundsSlack) no matter the map shape. Water no longer needs to track areaSize precisely (see design notes) — it's a background visual only. This is only a floor, though — see ComputeBackdropOuterHalf, which also sizes the backdrop off the camera's actual max zoom-out view distance, since on small maps that distance outgrows a footprint-based multiplier and used to expose the skybox's grey horizon past the water's edge (Stage 34).")]
    [SerializeField] private float waterExtentMultiplier = 4f;
    [Tooltip("Safety multiplier applied to RTSCameraController.ComputeMaxGroundViewDistance() when sizing the water/ground backdrop — covers frustum-corner rays (which reach slightly less far than the pure top-edge ray this is based on, but the margin is cheap) and any future FOV/zoom tuning.")]
    [SerializeField] private float cameraViewDistanceSafetyFactor = 1.3f;
    [Tooltip("Camera pan bounds extend this far OUTSIDE the real NavMesh bounding box on every side, so panning to the limit shows a comfortable strip of water instead of stopping dead at the dry-ground edge.")]
    [SerializeField] private float cameraBoundsSlack = 6f;
    [Tooltip("Extra slack added on top of cameraBoundsSlack for the bottom (-Z) camera bound specifically — panning toward the player's start edge felt like it hit a wall.")]
    [SerializeField] private float cameraBoundsBottomExtraSlack = 6f;
    [Tooltip("Must clear EnemyAI's detectionRadius (15, see EnemyAI.cs) — otherwise an enemy spawns already inside its own detection sphere and starts moving/firing on the very first frame. 20 = detectionRadius + a 5-unit margin, which also comfortably clears both weapons' ranges (enemy 10, player mech 12, see Weapon prefabs) so nothing is in firing range at spawn either.")]
    [SerializeField] private float enemyMinDistanceFromPlayer = 20f;
    [SerializeField] private float enemyMinDistanceFromOthers = 9f;
    [SerializeField] private float pickupMinDistanceFromOthers = 5f;
    [SerializeField] private float repairZoneForwardOffset = 8f;
    [Tooltip("General pickup rule (Stage 34): exactly this many enemies places 2 HealthPickups instead of the usual 1; more than this places a RepairZone instead of any HealthPickup. Fewer keeps the default 1 HealthPickup.")]
    [SerializeField] private int enemiesRequiredForRepairZone = 3;
    [Tooltip("Hard cap on enemy count for the first level of a run (RunLevel 1), regardless of what the area-based formula and MapSizeSettings would otherwise allow.")]
    [SerializeField] private int firstLevelMaxEnemies = 2;
    [SerializeField] private int playerSpawnMaxAttempts = 40;
    [SerializeField] private int enemySpawnMaxAttempts = 150;
    [Tooltip("Max distance NavMesh.SamplePosition is allowed to snap a randomly-picked ground point by. Small on purpose — the point already came from the NavMesh triangulation itself, this just rejects points that land in carved-out holes (e.g. inside a building footprint).")]
    [SerializeField] private float groundSampleMaxDistance = 0.5f;

    [Header("Enemy count from real walkable area")]
    [Tooltip("enemyCount = round(realWalkableNavMeshArea / this), then clamped to the resolved MapSizeSettings' min/maxEnemies. Tuned from the Large preset's measured walkable area (~9384 sq. units at areaSize=100) landing near the middle of its 5-7 enemy range.")]
    [SerializeField] private float navMeshAreaPerEnemy = 1550f;

    public MapSize GeneratedMapSize { get; private set; }
    public MapShape GeneratedMapShape { get; private set; }
    public bool GeneratedWinOnAllEnemiesDefeated { get; private set; }

    private NavMeshTriangulation cachedTriangulation;
    private float[] cachedCumulativeTriangleAreas;
    private float cachedTotalWalkableArea;

    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        MapSizeSettings settings = ResolveMapSize(out MapSize mapSize);
        GeneratedMapSize = mapSize;

        MapShape mapShape = ResolveMapShape();
        GeneratedMapShape = mapShape;

        GameObject[] activeZones = mapShape == MapShape.Elongated ? elongatedLegacyGroundZones : legacyGroundZones;
        GameObject[] inactiveZones = mapShape == MapShape.Elongated ? legacyGroundZones : elongatedLegacyGroundZones;
        DeactivateZones(inactiveZones);

        // Only the terrain zones (the actual dry-ground meshes) need to be in place before the
        // bake — ground/water are purely background visuals now and are sized AFTER the real
        // walkable footprint is known (below), instead of from the nominal areaSize/half square.
        // The old order (resize ground+water from `half`, THEN bake) is what caused the
        // ground-truth bug this pass fixes: legacyGroundZones are hand-authored around the origin
        // and uniformly scaled by areaSize, so their real extent does not necessarily fit inside
        // the nominal half-square (e.g. a road segment's tip can stick out past `half` on one
        // axis) — water built from `half` then visually overlapped real dry ground at those tips.
        ScaleLegacyZones(activeZones, settings.areaSize);

        // Static geometry (repositioned terrain zones) is final now — bake before anything below
        // queries the NavMesh (player spawn validation, agent placement, pathfinding).
        // BuildNavMesh() only registers the result into the live NavMesh query system (AddData())
        // when the surface component is already "active and enabled" — which, this early in
        // Awake(), it may not be yet (OnEnable for other scene objects hasn't run). Register
        // explicitly so the freshly baked mesh is queryable immediately, in this same Awake() call.
        if (navMeshSurface != null)
        {
            // RemoveAllNavMeshData() guards against stale NavMeshData instances lingering in the
            // live NavMesh query system from a previous bake (e.g. domain-reload-disabled editor
            // sessions, or manual re-bakes) — CalculateTriangulation() below sums every currently
            // registered NavMeshData, so leftover data would silently inflate every area/bounds
            // calculation that depends on it.
            NavMesh.RemoveAllNavMeshData();
            navMeshSurface.BuildNavMesh();
            navMeshSurface.AddData();
        }

        CacheTriangulation();
        GroundBounds groundBounds = ComputeGroundBounds();

        // Ground backdrop + water are sized from the REAL baked bounding box, not `half` — this
        // guarantees the water ring's inner edge never overlaps real dry ground regardless of the
        // terrain shape, and its outer edge is pushed well past any camera bounds/view distance
        // (see cameraBoundsSlack below) so the player can never pan or zoom out far enough to see
        // its edge.
        ResizeAndPlaceGround(groundBounds);
        BuildWaterBorder(groundBounds);

        int runLevel = MechLoadoutData.Instance != null ? MechLoadoutData.Instance.CurrentRunLevel : 1;
        bool isBossLevel = bossLevelInterval > 0 && runLevel % bossLevelInterval == 0;

        int enemyCount = isBossLevel ? 1 : ComputeEnemyCount(settings);

        Vector3 oldPlayerPos = playerMech != null ? playerMech.position : Vector3.zero;
        Vector3 newPlayerPos = FindValidPlayerSpawn(groundBounds, oldPlayerPos.y);
        Vector3 delta = newPlayerPos - oldPlayerPos;

        NavMeshAgent playerAgent = playerMech != null ? playerMech.GetComponent<NavMeshAgent>() : null;
        if (playerAgent != null)
            playerAgent.enabled = false;

        if (playerMech != null)
            playerMech.position = newPlayerPos;

        ShiftAndClamp(introCameraShots, delta, groundBounds);
        ShiftAndClamp(dialogueTriggers, delta, groundBounds);

        if (rtsCameraController != null)
        {
            // Bounds extend OUTSIDE the real dry-ground edge (asymmetric: extra slack on -Z) so
            // panning to the limit shows a comfortable strip of water instead of stopping dead at
            // the ground edge — water (built above, waterExtentMultiplier times the bounding box)
            // extends far enough past this that its own edge is never visible.
            rtsCameraController.SetBounds(
                groundBounds.minX - cameraBoundsSlack, groundBounds.maxX + cameraBoundsSlack,
                groundBounds.minZ - cameraBoundsSlack - cameraBoundsBottomExtraSlack, groundBounds.maxZ + cameraBoundsSlack);
            rtsCameraController.FocusOnPoint(newPlayerPos);
        }

        // Boss levels (Stage 35): always "defeat all enemies" — an ExtractionZone win condition
        // would let the player walk past the boss entirely, which defeats the point of a boss level.
        bool winOnAllEnemiesDefeated = isBossLevel || Random.value < 0.5f;
        GeneratedWinOnAllEnemiesDefeated = winOnAllEnemiesDefeated;

        Vector3 introDestination = FindIntroDestination(newPlayerPos, groundBounds);

        // Stage 35: the briefing cutscene only plays on the run's first level. Disabling the
        // component here — before its own Start() ever runs (LevelGenerator's DefaultExecutionOrder
        // guarantees that) — skips it entirely rather than just fast-forwarding it, so level 2+
        // never pauses input/camera/combat for it at all.
        if (introCutsceneController != null)
        {
            if (runLevel <= 1)
                introCutsceneController.SetIntroDestination(introDestination);
            else
                introCutsceneController.enabled = false;
        }

        PlaceDeadMechs(newPlayerPos, introDestination, groundBounds);

        if (playerAgent != null)
        {
            playerAgent.enabled = true;
            playerAgent.Warp(newPlayerPos);
        }

        CleanupOldEnemySpawnPoints();
        List<Vector3> enemyPositions = GenerateEnemyPositions(enemyCount, newPlayerPos);

        if (isBossLevel)
        {
            SpawnBoss(enemyPositions.Count > 0 ? enemyPositions[0] : newPlayerPos);
        }
        else
        {
            CreateEnemySpawnPoints(enemyPositions);

            if (unitSpawner != null)
                unitSpawner.SpawnAll();
        }

        PlaceEnemySilhouette(newPlayerPos, enemyPositions);

        List<Vector3> occupiedPoints = new List<Vector3>(enemyPositions) { newPlayerPos };

        // Boss levels (Stage 35): exactly one HealthPickup on the map, no RepairZone, regardless
        // of the Stage 34 enemy-count-based pickup rules below (those only apply on normal levels).
        if (isBossLevel)
        {
            PlaceHealthPickup(newPlayerPos, occupiedPoints, enemyCount, forceSingle: true);
        }
        else
        {
            PlaceHealthPickup(newPlayerPos, occupiedPoints, enemyCount);

            if (enemyCount > enemiesRequiredForRepairZone)
                PlaceRepairZone(newPlayerPos);
        }

        if (!winOnAllEnemiesDefeated)
            PlaceExtractionZone(groundBounds, newPlayerPos);

        if (missionController != null)
            missionController.SetWinCondition(winOnAllEnemiesDefeated);

        ShowLevelObjective(winOnAllEnemiesDefeated);
    }

    // Boss stats = the normal enemy stat multiplier for this run level (EnemyLevelScaler's usual
    // +10%/level) plus bossExtraStatMultiplier on top, per Stage 35. bossEnemyPrefab is spawned
    // directly here instead of through UnitSpawner/SpawnPoint, since that pipeline has no concept
    // of "exactly one specific prefab" — it always picks randomly from enemyUnitPrefabs.
    private void SpawnBoss(Vector3 position)
    {
        if (bossEnemyPrefab == null)
        {
            Debug.LogWarning("[LevelGenerator] bossEnemyPrefab не назначен — босс не заспавнен.");
            return;
        }

        GameObject boss = Instantiate(bossEnemyPrefab, position, Quaternion.identity);
        boss.transform.localScale *= bossVisualScale;
        EnemyLevelScaler.ApplyRunLevelScaling(boss, bossExtraStatMultiplier);
    }

    // Stage 35: briefly surfaces the win condition LevelGenerator already decided above (Stage 29)
    // so the player knows the objective at level start, instead of only ever seeing it implicitly.
    private void ShowLevelObjective(bool winOnAllEnemiesDefeated)
    {
        if (levelObjectiveText == null)
            return;

        levelObjectiveText.text = winOnAllEnemiesDefeated
            ? "Цель: уничтожить всех противников"
            : "Цель: дойти до точки эвакуации";

        levelObjectiveText.gameObject.SetActive(true);
        StartCoroutine(HideLevelObjectiveAfterDelay());
    }

    private System.Collections.IEnumerator HideLevelObjectiveAfterDelay()
    {
        yield return new WaitForSeconds(levelObjectiveDisplayDuration);

        if (levelObjectiveText != null)
            levelObjectiveText.gameObject.SetActive(false);
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

    // Elongated is only ever picked once an alternate hand-authored layout exists in
    // elongatedLegacyGroundZones (see its Tooltip) — until then this always resolves to Square.
    private MapShape ResolveMapShape()
    {
        if (elongatedLegacyGroundZones == null || elongatedLegacyGroundZones.Length == 0)
            return MapShape.Square;

        return Random.value < elongatedMapChance ? MapShape.Elongated : MapShape.Square;
    }

    private void DeactivateZones(GameObject[] zones)
    {
        if (zones == null)
            return;

        foreach (GameObject zone in zones)
        {
            if (zone != null)
                zone.SetActive(false);
        }
    }

    // Half-size to use for both the ground backdrop and the water ring's outer edge (must match
    // exactly, or the backdrop's default-grey material — see Stage 34 — pokes out past the water).
    // Two candidates are combined by taking the larger:
    //  - footprint-based: the old behavior, scales with map size, generous on Medium/Large.
    //  - camera-based: map half-extent + camera pan slack + the camera's own worst-case top-of-
    //    screen view distance at max zoom-out (see RTSCameraController.ComputeMaxGroundViewDistance).
    // The footprint-based figure alone isn't enough on small maps: camera zoom range (10-60 height)
    // doesn't scale down with map size, so at max zoom-out on a Small map the top of the screen
    // could see well past a footprint-scaled backdrop — exposing the skybox's flat-grey
    // GroundColor beyond the backdrop's edge as a diagonal band (Camera.clearFlags is Skybox).
    private float ComputeBackdropOuterHalf(GroundBounds bounds)
    {
        float footprint = Mathf.Max(bounds.maxX - bounds.minX, bounds.maxZ - bounds.minZ);
        float footprintBasedHalf = footprint * waterExtentMultiplier * 0.5f;

        if (rtsCameraController == null)
            return footprintBasedHalf;

        float maxViewDistance = rtsCameraController.ComputeMaxGroundViewDistance() * cameraViewDistanceSafetyFactor;
        float panSlack = cameraBoundsSlack + cameraBoundsBottomExtraSlack;
        float cameraBasedHalf = footprint * 0.5f + panSlack + maxViewDistance;

        return Mathf.Max(footprintBasedHalf, cameraBasedHalf);
    }

    // Sized and centered from the real baked NavMesh bounding box (not the nominal areaSize
    // square) — see the note in Generate() for why the square math was wrong. This is a pure
    // background visual with no gameplay role, so it's simply blown out to comfortably outrun the
    // water ring (which itself outruns the camera bounds) rather than needing an exact fit.
    private void ResizeAndPlaceGround(GroundBounds bounds)
    {
        if (groundPlane == null)
            return;

        float centerX = (bounds.minX + bounds.maxX) * 0.5f;
        float centerZ = (bounds.minZ + bounds.maxZ) * 0.5f;
        // Must match BuildWaterBorder's outer span exactly — this used to be double that, so the
        // ground backdrop stuck out past the water ring's outer edge as a visible grey band around
        // small maps, where the extra margin is a much bigger fraction of what's on screen at
        // normal zoom.
        float outerSize = ComputeBackdropOuterHalf(bounds) * 2f;

        groundPlane.position = new Vector3(centerX, groundPlane.position.y, centerZ);
        // Unity's default Plane mesh is 10x10 at scale 1.
        float scale = outerSize / 10f;
        groundPlane.localScale = new Vector3(scale, groundPlane.localScale.y, scale);
    }

    // The hand-placed road/mud/rocks meshes (and the Cover prop sitting among them) were authored
    // once, around the world origin, to fit terrainZoneReferenceAreaSize (the Large preset). They
    // don't render or collide — they sit on the VisualOnly layer purely as ground texture, with the
    // actual NavMesh baked directly from them (see the Plane's NavMeshSurface layer mask) — so
    // uniformly scaling every zone's position and (for the terrain meshes, not the Cover prop) size
    // by the same factor the map itself is scaled by keeps the baked walkable area exactly matching
    // what's rendered, at every MapSize, without needing per-size on/off toggling.
    private void ScaleLegacyZones(GameObject[] zones, float areaSize)
    {
        if (zones == null)
            return;

        float factor = areaSize / terrainZoneReferenceAreaSize;

        foreach (GameObject zone in zones)
        {
            if (zone == null)
                continue;

            zone.transform.position *= factor;

            if (zone.name.StartsWith("TerrainZone_"))
                zone.transform.localScale *= factor;

            zone.SetActive(true);
        }
    }

    // Built from the real baked NavMesh bounding box, not the nominal areaSize/half square — the
    // ring's inner edge sits waterInnerGap outside `bounds`, which by definition contains every
    // walkable vertex, so this can never overlap real dry ground no matter how irregular the
    // terrain shape is (unlike the old half-based square, which didn't account for terrain zones
    // whose scaled extent pokes past the nominal half on one axis). The outer edge is pushed out by
    // ComputeBackdropOuterHalf — comfortably past both the camera pan bounds AND its actual
    // zoomed-out view distance (see that method) — so its own far edge, and the skybox beyond it,
    // are never visible.
    private void BuildWaterBorder(GroundBounds bounds)
    {
        if (waterZonePrefab == null)
        {
            Debug.LogWarning("[LevelGenerator] waterZonePrefab не назначен — водная граница не создана.");
            return;
        }

        Transform root = new GameObject("GeneratedWaterBorder").transform;
        root.SetParent(transform, false);

        float innerMinX = bounds.minX - waterInnerGap;
        float innerMaxX = bounds.maxX + waterInnerGap;
        float innerMinZ = bounds.minZ - waterInnerGap;
        float innerMaxZ = bounds.maxZ + waterInnerGap;

        float centerX = (bounds.minX + bounds.maxX) * 0.5f;
        float centerZ = (bounds.minZ + bounds.maxZ) * 0.5f;
        float outerHalf = ComputeBackdropOuterHalf(bounds);

        float outerMinX = centerX - outerHalf;
        float outerMaxX = centerX + outerHalf;
        float outerMinZ = centerZ - outerHalf;
        float outerMaxZ = centerZ + outerHalf;

        // North/south strips span the full outer width (covering the corners); east/west strips
        // fill only the middle band between them, so the four segments tile without gaps or
        // double-covered corners.
        CreateWaterSegment(root, (outerMinX + outerMaxX) * 0.5f, (innerMaxZ + outerMaxZ) * 0.5f, outerMaxX - outerMinX, outerMaxZ - innerMaxZ);
        CreateWaterSegment(root, (outerMinX + outerMaxX) * 0.5f, (outerMinZ + innerMinZ) * 0.5f, outerMaxX - outerMinX, innerMinZ - outerMinZ);
        CreateWaterSegment(root, (innerMaxX + outerMaxX) * 0.5f, (innerMinZ + innerMaxZ) * 0.5f, outerMaxX - innerMaxX, innerMaxZ - innerMinZ);
        CreateWaterSegment(root, (outerMinX + innerMinX) * 0.5f, (innerMinZ + innerMaxZ) * 0.5f, innerMinX - outerMinX, innerMaxZ - innerMinZ);
    }

    private void CreateWaterSegment(Transform parent, float centerX, float centerZ, float sizeX, float sizeZ)
    {
        GameObject segment = Instantiate(waterZonePrefab, new Vector3(centerX, 0f, centerZ), Quaternion.identity, parent);
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
        // Ground_Water's mesh is the default 10x10 Unity Plane (like every other Ground_*), so its
        // localScale has to be in "mesh units" (divide by 10), not world units directly — otherwise
        // the rendered/baked water strip ends up 10x too big in X/Z. This was a pre-existing bug:
        // harmless before (the water mesh's VisualOnly layer wasn't in the NavMeshSurface's bake
        // mask), but it silently added a huge extra walkable area once VisualOnly was included to
        // pick up the real terrain-zone meshes, because Zone_Water's NavMeshModifierVolume below is
        // sized correctly (world units) and only carved out the small, correctly-sized portion.
        // The rendered quad is grown by waterVisualOverlap on every side (symmetric about the same
        // center) purely to hide the land/water seam (see its tooltip) — the NavMeshModifierVolume
        // below still uses the original, un-grown sizeX/sizeZ so pathing/carve-out behavior is
        // unchanged.
        float visualSizeX = sizeX + waterVisualOverlap * 2f;
        float visualSizeZ = sizeZ + waterVisualOverlap * 2f;
        groundWater.localScale = new Vector3(visualSizeX / 10f, 0.4f, visualSizeZ / 10f);

        if (zoneWater == null)
            return;

        zoneWater.localPosition = new Vector3(0f, 1.5f, 0f);
        zoneWater.localRotation = Quaternion.identity;
        zoneWater.localScale = Vector3.one;

        NavMeshModifierVolume modifier = zoneWater.GetComponent<NavMeshModifierVolume>();
        if (modifier != null)
        {
            modifier.size = new Vector3(sizeX, 4f, sizeZ);
            modifier.center = Vector3.zero;
        }
    }

    private void ShiftAndClamp(Transform[] targets, Vector3 delta, GroundBounds bounds)
    {
        if (targets == null)
            return;

        foreach (Transform t in targets)
        {
            if (t == null)
                continue;

            t.position = ClampToBounds(t.position + delta, bounds);
        }
    }

    private Vector3 ClampToBounds(Vector3 pos, GroundBounds bounds)
    {
        pos.x = Mathf.Clamp(pos.x, bounds.minX + edgeMargin, bounds.maxX - edgeMargin);
        pos.z = Mathf.Clamp(pos.z, bounds.minZ + edgeMargin, bounds.maxZ - edgeMargin);
        return pos;
    }

    // Builds the weighted-by-area triangle lookup once per generation and computes the real
    // walkable NavMesh bounding box — both are reused by every spawn/placement/camera-bounds call
    // below instead of re-triangulating per point. Must run after the NavMesh has been (re)baked.
    private void CacheTriangulation()
    {
        cachedTriangulation = NavMesh.CalculateTriangulation();
        int triCount = cachedTriangulation.indices.Length / 3;
        cachedCumulativeTriangleAreas = new float[triCount];

        float running = 0f;
        for (int i = 0; i < triCount; i++)
        {
            Vector3 a = cachedTriangulation.vertices[cachedTriangulation.indices[i * 3]];
            Vector3 b = cachedTriangulation.vertices[cachedTriangulation.indices[i * 3 + 1]];
            Vector3 c = cachedTriangulation.vertices[cachedTriangulation.indices[i * 3 + 2]];
            running += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            cachedCumulativeTriangleAreas[i] = running;
        }

        cachedTotalWalkableArea = running;
    }

    private GroundBounds ComputeGroundBounds()
    {
        GroundBounds bounds = new GroundBounds
        {
            minX = float.MaxValue,
            maxX = float.MinValue,
            minZ = float.MaxValue,
            maxZ = float.MinValue,
        };

        foreach (Vector3 v in cachedTriangulation.vertices)
        {
            if (v.x < bounds.minX) bounds.minX = v.x;
            if (v.x > bounds.maxX) bounds.maxX = v.x;
            if (v.z < bounds.minZ) bounds.minZ = v.z;
            if (v.z > bounds.maxZ) bounds.maxZ = v.z;
        }

        return bounds;
    }

    private int ComputeEnemyCount(MapSizeSettings settings)
    {
        int raw = Mathf.RoundToInt(cachedTotalWalkableArea / navMeshAreaPerEnemy);
        int count = Mathf.Clamp(raw, settings.minEnemies, settings.maxEnemies);

        int runLevel = MechLoadoutData.Instance != null ? MechLoadoutData.Instance.CurrentRunLevel : 1;
        if (runLevel <= 1)
            count = Mathf.Min(count, firstLevelMaxEnemies);

        return count;
    }

    // Picks a uniformly-random point on the real baked walkable surface: a triangle is chosen with
    // probability proportional to its area (so large open patches aren't under-represented relative
    // to tiny sliver triangles), then a random barycentric point is taken inside it. This can only
    // ever land exactly on real ground — there is no square/half math involved.
    private Vector3 RandomPointOnNavMesh()
    {
        float target = Random.value * cachedTotalWalkableArea;

        int lo = 0, hi = cachedCumulativeTriangleAreas.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (cachedCumulativeTriangleAreas[mid] < target)
                lo = mid + 1;
            else
                hi = mid;
        }

        int triIndex = lo;
        Vector3 a = cachedTriangulation.vertices[cachedTriangulation.indices[triIndex * 3]];
        Vector3 b = cachedTriangulation.vertices[cachedTriangulation.indices[triIndex * 3 + 1]];
        Vector3 c = cachedTriangulation.vertices[cachedTriangulation.indices[triIndex * 3 + 2]];

        float r1 = Random.value;
        float r2 = Random.value;
        if (r1 + r2 > 1f)
        {
            r1 = 1f - r1;
            r2 = 1f - r2;
        }

        return a + r1 * (b - a) + r2 * (c - a);
    }

    // Mandatory final check on top of RandomPointOnNavMesh(): a point picked from the raw
    // triangulation can still land inside a carved-out hole (e.g. a building footprint) that the
    // triangulation itself doesn't represent as a gap at triangle-corner resolution. SamplePosition
    // with a small maxDistance catches that — if it still misses, the point is rejected outright
    // rather than snapped somewhere unrelated.
    private bool TryGetRandomGroundPoint(out Vector3 result)
    {
        Vector3 candidate = RandomPointOnNavMesh();

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, groundSampleMaxDistance, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = default;
        return false;
    }

    // Requires the NavMesh to already be baked and registered (see Generate()). Tries the
    // hand-picked "back edge, facing forward" spot first (for a consistent intro-cutscene framing),
    // and only falls back to a uniformly-random ground point if that specific spot doesn't happen
    // to land on real ground for this map's shape.
    private Vector3 FindValidPlayerSpawn(GroundBounds bounds, float y)
    {
        float centerX = (bounds.minX + bounds.maxX) * 0.5f;
        Vector3 preferred = new Vector3(centerX, y, bounds.minZ + playerSpawnEdgeInset);
        if (NavMesh.SamplePosition(preferred, out NavMeshHit preferredHit, 3f, NavMesh.AllAreas))
            return preferredHit.position;

        for (int attempt = 0; attempt < playerSpawnMaxAttempts; attempt++)
        {
            if (TryGetRandomGroundPoint(out Vector3 candidate))
                return new Vector3(candidate.x, y, candidate.z);
        }

        Debug.LogWarning($"[LevelGenerator] Не удалось найти валидную точку спавна игрока за {playerSpawnMaxAttempts} попыток — использую центр карты как запасной вариант.");

        Vector3 center = new Vector3(centerX, y, (bounds.minZ + bounds.maxZ) * 0.5f);
        if (NavMesh.SamplePosition(center, out NavMeshHit fallback, 10f, NavMesh.AllAreas))
            return fallback.position;

        return center;
    }

    private Vector3 FindIntroDestination(Vector3 playerPos, GroundBounds bounds)
    {
        float destinationZ = Mathf.Min(playerPos.z + introForwardDistance, bounds.maxZ - edgeMargin);
        Vector3 desired = new Vector3(0f, playerPos.y, destinationZ);

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return hit.position;

        return TryGetRandomGroundPoint(out Vector3 fallback) ? new Vector3(fallback.x, playerPos.y, fallback.z) : playerPos;
    }

    private void PlaceDeadMechs(Vector3 pathStart, Vector3 pathEnd, GroundBounds bounds)
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
            Vector3 newPos = ClampToBounds(basePos + offset, bounds);

            if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                newPos = hit.position;

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

    private List<Vector3> GenerateEnemyPositions(int count, Vector3 playerPos)
    {
        List<Vector3> result = new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < enemySpawnMaxAttempts && !placed; attempt++)
            {
                if (!TryGetRandomGroundPoint(out Vector3 candidate))
                    continue;

                candidate.y = playerPos.y;

                if (Vector3.Distance(candidate, playerPos) < enemyMinDistanceFromPlayer)
                    continue;

                if (TooCloseToAny(candidate, result, enemyMinDistanceFromOthers))
                    continue;

                result.Add(candidate);
                placed = true;
            }

            if (!placed)
            {
                Debug.LogWarning($"[LevelGenerator] Не удалось найти валидную точку спавна врага #{i} за {enemySpawnMaxAttempts} попыток — использую последнюю попавшуюся точку на земле без полной проверки дистанций.");

                if (TryGetRandomGroundPoint(out Vector3 fallback))
                {
                    fallback.y = playerPos.y;
                    result.Add(fallback);
                }
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

    // General pickup rule (Stage 34): exactly enemiesRequiredForRepairZone (3) enemies bumps the
    // usual 1 HealthPickup up to 2 (a bit of extra cushion for a fight that's not yet hard enough
    // for a RepairZone); more than that replaces HealthPickup entirely with a RepairZone (see the
    // call site) since sustained healing matters more than a one-time heal once there are 4+
    // enemies. 1-2 enemies keep the original single HealthPickup. The run-upgrade bonus pickup
    // (MechLoadoutData.BonusHealthPickup) still adds one on top of whatever this rule produces,
    // including the 0 case, so that upgrade always delivers something.
    private void PlaceHealthPickup(Vector3 playerPos, List<Vector3> avoidPoints, int enemyCount, bool forceSingle = false)
    {
        if (healthPickupPrefab == null)
            return;

        int count;

        if (forceSingle)
        {
            // Boss levels (Stage 35): exactly 1 HealthPickup, no exceptions — even a pending ad
            // bonus is consumed here rather than stacking a 2nd pickup, since the level is only
            // supposed to have the one.
            count = 1;

            if (MechLoadoutData.Instance != null)
                MechLoadoutData.Instance.BonusHealthPickup = false;
        }
        else
        {
            count = enemyCount == enemiesRequiredForRepairZone ? 2 : (enemyCount > enemiesRequiredForRepairZone ? 0 : 1);

            if (MechLoadoutData.Instance != null && MechLoadoutData.Instance.BonusHealthPickup)
            {
                count += 1;
                MechLoadoutData.Instance.BonusHealthPickup = false;
            }
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = FindValidPoint(playerPos, avoidPoints, pickupMinDistanceFromOthers);
            Instantiate(healthPickupPrefab, pos, Quaternion.identity);
            avoidPoints.Add(pos);
        }
    }

    private void PlaceRepairZone(Vector3 playerPos)
    {
        if (repairZonePrefab == null)
            return;

        Vector3 desired = playerPos + Vector3.forward * repairZoneForwardOffset;
        Vector3 pos;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            pos = hit.position;
        }
        else if (!TryGetRandomGroundPoint(out pos))
        {
            pos = playerPos;
        }

        Instantiate(repairZonePrefab, pos, Quaternion.identity);
    }

    private void PlaceExtractionZone(GroundBounds bounds, Vector3 playerPos)
    {
        if (extractionZonePrefab == null)
            return;

        float centerX = (bounds.minX + bounds.maxX) * 0.5f;
        Vector3 desired = new Vector3(centerX, playerPos.y, bounds.maxZ - edgeMargin);
        Vector3 pos;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            pos = hit.position;
        }
        else
        {
            // Fall back to the farthest real ground point from the player — covers oddly-shaped
            // maps where the analytic "north edge" doesn't happen to sit on any walkable triangle.
            pos = playerPos;
            float bestDist = -1f;
            foreach (Vector3 v in cachedTriangulation.vertices)
            {
                float d = Vector3.Distance(v, playerPos);
                if (d > bestDist)
                {
                    bestDist = d;
                    pos = new Vector3(v.x, playerPos.y, v.z);
                }
            }
        }

        GameObject zone = Instantiate(extractionZonePrefab, pos, Quaternion.identity);

        float boundsSpan = Mathf.Min(bounds.maxX - bounds.minX, bounds.maxZ - bounds.minZ);
        float scaleX = Mathf.Clamp(boundsSpan * 0.25f, 4f, 10f);
        Vector3 scale = zone.transform.localScale;
        zone.transform.localScale = new Vector3(scaleX, scale.y, scale.z);

        ExtractionZone extractionZone = zone.GetComponent<ExtractionZone>();
        if (extractionZone != null)
            extractionZone.SetMissionController(missionController);
    }

    private Vector3 FindValidPoint(Vector3 playerPos, List<Vector3> avoidPoints, float minDistance)
    {
        for (int attempt = 0; attempt < 500; attempt++)
        {
            if (!TryGetRandomGroundPoint(out Vector3 candidate))
                continue;

            candidate.y = playerPos.y;

            if (!TooCloseToAny(candidate, avoidPoints, minDistance))
                return candidate;
        }

        if (TryGetRandomGroundPoint(out Vector3 fallback))
        {
            fallback.y = playerPos.y;
            return fallback;
        }

        return playerPos;
    }
}
