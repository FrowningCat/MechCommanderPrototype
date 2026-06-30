using UnityEngine;

public interface ITargetable
{
    Transform Transform { get; }
    bool IsAlive { get; }

    void TakeDamage(int damage);
}