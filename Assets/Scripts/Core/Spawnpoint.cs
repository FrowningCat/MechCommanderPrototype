using UnityEngine;

public enum SpawnPointType
{
    Player,
    Enemy
}

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private SpawnPointType type = SpawnPointType.Enemy;

    public SpawnPointType Type => type;

    private void OnDrawGizmos()
    {
        Gizmos.color = type == SpawnPointType.Player ? Color.cyan : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}   