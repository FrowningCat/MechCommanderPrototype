using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private GameObject enemyUnitPrefab;

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
        GameObject prefab = point.Type == SpawnPointType.Player ? playerUnitPrefab : enemyUnitPrefab;

        if (prefab == null)
        {
            Debug.Log($"[UnitSpawner] Префаб для {point.Type} не назначен — точка {point.name} пропущена (это нормально, если юниты этого типа уже расставлены вручную).");
            return;
        }

        Instantiate(prefab, point.transform.position, point.transform.rotation);
    }
}