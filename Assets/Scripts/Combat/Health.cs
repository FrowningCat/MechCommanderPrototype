using UnityEngine;

public class Health : MonoBehaviour, ITargetable
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private bool isAlive = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public Transform Transform => transform;
    public bool IsAlive => isAlive;

    public event System.Action OnDamaged;

    private void Awake()
    {
        currentHealth = maxHealth;
        isAlive = true;
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive)
            return;

        if (damage <= 0)
            return;

        currentHealth -= damage;

        OnDamaged?.Invoke();

        Debug.Log($"{gameObject.name} took {damage} damage. HP = {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (!isAlive)
            return;

        if (amount <= 0)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (!isAlive)
            return;

        isAlive = false;

        Debug.Log($"{gameObject.name} destroyed");

        Destroy(gameObject);
    }
}