using UnityEngine;

public class MissionController : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask playerUnitLayer;
    [SerializeField] private LayerMask enemyUnitLayer;

    [Header("Win Condition")]
    [SerializeField] private bool winOnAllEnemiesDefeated = true;

    [Header("Check Interval")]
    [SerializeField] private float checkInterval = 0.5f;

    public MissionState State { get; private set; } = MissionState.InProgress;

    public event System.Action<MissionState> OnMissionEnded;

    private float checkTimer;

    private void Start()
    {
        int initialPlayerUnits = CountUnitsOnLayer(playerUnitLayer);
        int initialEnemyUnits = CountUnitsOnLayer(enemyUnitLayer);

        Debug.Log($"[MissionController] Start: playerUnits={initialPlayerUnits}, enemyUnits={initialEnemyUnits}");

        if (initialPlayerUnits == 0)
            Debug.LogWarning("[MissionController] playerUnitLayer нашёл 0 юнитов при старте — проверь LayerMask, иначе миссия завершится поражением почти сразу.");

        if (winOnAllEnemiesDefeated && initialEnemyUnits == 0)
            Debug.LogWarning("[MissionController] enemyUnitLayer нашёл 0 юнитов при старте, а winOnAllEnemiesDefeated включён — миссия завершится победой почти сразу.");
    }

    private void Update()
    {
        if (State != MissionState.InProgress)
            return;

        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;

        CheckMissionState();
    }

    private void CheckMissionState()
    {
        int playerUnitCount = CountUnitsOnLayer(playerUnitLayer);

        if (playerUnitCount == 0)
        {
            EndMission(MissionState.Defeat);
            return;
        }

        if (!winOnAllEnemiesDefeated)
            return;

        int enemyUnitCount = CountUnitsOnLayer(enemyUnitLayer);

        if (enemyUnitCount == 0)
            EndMission(MissionState.Victory);
    }

    public void NotifyExtractionReached()
    {
        if (State != MissionState.InProgress)
            return;

        EndMission(MissionState.Victory);
    }

    private void EndMission(MissionState result)
    {
        State = result;

        Debug.Log($"[MissionController] Mission ended: {result}");

        OnMissionEnded?.Invoke(result);
    }

    private int CountUnitsOnLayer(LayerMask layer)
    {
        Health[] allUnits = FindObjectsByType<Health>(FindObjectsSortMode.None);

        int count = 0;

        foreach (Health unit in allUnits)
        {
            if (IsInLayerMask(unit.gameObject.layer, layer))
                count++;
        }

        return count;
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}