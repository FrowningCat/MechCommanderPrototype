using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private GameObject[] enemyUnitPrefabs;

    [Header("Options")]
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
            SpawnAll();
    }

    public void SpawnAll()
    {
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (SpawnPoint point in spawnPoints)
            SpawnAtPoint(point);
    }

    private void SpawnAtPoint(SpawnPoint point)
    {
        bool isEnemy = point.Type != SpawnPointType.Player;
        GameObject prefab = point.Type == SpawnPointType.Player ? playerUnitPrefab : PickRandomEnemyPrefab();

        if (prefab == null)
        {
            Debug.Log($"[UnitSpawner] Префаб для {point.Type} не назначен — точка {point.name} пропущена (это нормально, если юниты этого типа уже расставлены вручную).");
            return;
        }

        GameObject spawned = Instantiate(prefab, point.transform.position, point.transform.rotation);

        if (isEnemy)
            EnemyLevelScaler.ApplyRunLevelScaling(spawned);
    }

    private GameObject PickRandomEnemyPrefab()
    {
        if (enemyUnitPrefabs == null || enemyUnitPrefabs.Length == 0)
            return null;

        return enemyUnitPrefabs[Random.Range(0, enemyUnitPrefabs.Length)];
    }
}