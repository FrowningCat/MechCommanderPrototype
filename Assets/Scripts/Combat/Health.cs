using UnityEngine;

public class Health : MonoBehaviour, ITargetable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int armorValue = 0;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destructionSound;

    private int currentHealth;
    private bool isAlive = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int ArmorValue => armorValue;

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
        ApplyDamage(damage, applyArmor: true);
    }

    public void TakeDamageBypassArmor(int damage)
    {
        ApplyDamage(damage, applyArmor: false);
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

    private void ApplyDamage(int damage, bool applyArmor)
    {
        if (!isAlive)
            return;

        if (damage <= 0)
            return;

        int mitigatedDamage = applyArmor ? Mathf.Max(0, damage - armorValue) : damage;

        if (mitigatedDamage <= 0)
        {
            Debug.Log($"{gameObject.name} absorbed {damage} damage with armor");
            return;
        }

        currentHealth -= mitigatedDamage;

        OnDamaged?.Invoke();

        AudioManager.PlaySfxAtPoint(hitSound, transform.position);

        Debug.Log($"{gameObject.name} took {mitigatedDamage} damage. HP = {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        if (!isAlive)
            return;

        isAlive = false;

        AudioManager.PlaySfxAtPoint(destructionSound, transform.position);

        Debug.Log($"{gameObject.name} destroyed");

        Destroy(gameObject);
    }
}