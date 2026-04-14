using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath = false; // Set to false for enemies with death animations
    [SerializeField] private float destroyDelay = 3f;

    private int currentHealth;
    public bool IsDead { get; private set; }

    // Events to notify the EnemyController
    public event Action<int>? Damaged; 
    public event Action<Health>? Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            IsDead = true;
            
            // Notify listeners that this object has died
            Died?.Invoke(this);

            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
        else
        {
            // Notify listeners that damage was taken (for the 'Hit' animation)
            Damaged?.Invoke(damage);
        }
    }

    // Optional: Public method to get current health percentage for UI
    public float GetHealthNormalized()
    {
        return (float)currentHealth / maxHealth;
    }
}