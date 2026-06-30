using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log($"{gameObject.name} took {damage} damage. HP = {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} destroyed");
        Destroy(gameObject);
    }

}