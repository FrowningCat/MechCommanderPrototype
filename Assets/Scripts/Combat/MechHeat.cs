using UnityEngine;
using UnityEngine.AI;

public class MechHeat : MonoBehaviour
{
    [Header("Heat")]
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float dissipationPerSecond = 15f;

    [Header("Overheat Penalty")]
    [SerializeField] private float overheatCooldownThreshold = 80f;
    [SerializeField] private float overheatSpeedMultiplier = 0.5f;
    [SerializeField] private float overheatDamagePerSecond = 5f;

    private float currentHeat;
    private bool isOverheated;
    private float pendingOverheatDamage;

    private Health health;
    private NavMeshAgent agent;
    private float baseSpeed;

    public float CurrentHeat => currentHeat;
    public float MaxHeat => maxHeat;
    public float HeatFraction => maxHeat <= 0f ? 0f : currentHeat / maxHeat;
    public bool IsOverheated => isOverheated;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            baseSpeed = agent.speed;
    }

    private void Update()
    {
        currentHeat = Mathf.Max(0f, currentHeat - dissipationPerSecond * Time.deltaTime);

        if (isOverheated && currentHeat <= overheatCooldownThreshold)
            isOverheated = false;

        if (agent != null)
            agent.speed = isOverheated ? baseSpeed * overheatSpeedMultiplier : baseSpeed;

        if (isOverheated)
            ApplyOverheatDamage();
    }

    public void AddHeat(float amount)
    {
        if (amount <= 0f)
            return;

        currentHeat = Mathf.Min(maxHeat, currentHeat + amount);

        if (currentHeat >= maxHeat)
            isOverheated = true;
    }

    private void ApplyOverheatDamage()
    {
        if (health == null)
            return;

        pendingOverheatDamage += overheatDamagePerSecond * Time.deltaTime;

        if (pendingOverheatDamage < 1f)
            return;

        int damageToApply = Mathf.FloorToInt(pendingOverheatDamage);
        pendingOverheatDamage -= damageToApply;

        health.TakeDamageBypassArmor(damageToApply);
    }
}