using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool debugEvents;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public float Normalized => maxHealth <= 0 ? 0f : Mathf.Clamp01((float)CurrentHealth / maxHealth);

    public event Action<Health>? Died;
    public event Action<Health, int>? Damaged;
    public event Action<Health>? Changed;

    private void Awake()
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        if (CurrentHealth == 0)
        {
            CurrentHealth = maxHealth;
        }

        Changed?.Invoke(this);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (debugEvents)
        {
            Debug.Log($"[{name}] Took damage: {amount}. HP: {CurrentHealth}/{maxHealth}", this);
        }

        Damaged?.Invoke(this, amount);
        Changed?.Invoke(this);

        if (CurrentHealth == 0)
        {
            if (debugEvents)
            {
                Debug.Log($"[{name}] Died.", this);
            }

            Died?.Invoke(this);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

        if (debugEvents)
        {
            Debug.Log($"[{name}] Healed: {amount}. HP: {CurrentHealth}/{maxHealth}", this);
        }

        Changed?.Invoke(this);
    }

    public void AddMaxHealth(int amount, bool healForAmount)
    {
        if (amount <= 0)
        {
            return;
        }

        maxHealth = Mathf.Max(1, maxHealth + amount);

        if (healForAmount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }
        else
        {
            CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        }

        Changed?.Invoke(this);
    }
}
