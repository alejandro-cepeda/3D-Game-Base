using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class PlayerHealthRegen : MonoBehaviour
{
    [SerializeField] private float healthPerSecond = 2f;

    private Health health = null!;
    private float accumulator;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Update()
    {
        if (Time.timeScale == 0f || health.IsDead)
        {
            return;
        }

        if (health.CurrentHealth >= health.MaxHealth)
        {
            accumulator = 0f;
            return;
        }

        accumulator += healthPerSecond * Time.deltaTime;
        int healAmount = Mathf.FloorToInt(accumulator);
        if (healAmount <= 0)
        {
            return;
        }

        accumulator -= healAmount;
        health.Heal(healAmount);
    }

    public void AddRegen(float additionalPerSecond)
    {
        healthPerSecond = Mathf.Max(0f, healthPerSecond + additionalPerSecond);
    }
}
