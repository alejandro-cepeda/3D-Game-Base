using UnityEngine;

public sealed class EnemyGasCloud : MonoBehaviour
{
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float damagePerSecond = 5f;
    [SerializeField] private float durationSeconds = 5f;
    [SerializeField] private float tickSeconds = 0.5f;
    [SerializeField] private LayerMask hitLayers = ~0;

    private float remaining;
    private float nextTick;
    private Health? ownerHealth;

    private void Awake()
    {
        ownerHealth = GetComponent<Health>();
        remaining = durationSeconds;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Destroy(this);
            return;
        }

        if (Time.time < nextTick)
        {
            return;
        }

        nextTick = Time.time + tickSeconds;

        int damage = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * tickSeconds));
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, hitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null)
            {
                continue;
            }

            Health? h = c.GetComponentInParent<Health>();
            if (h == null || h.IsDead)
            {
                continue;
            }

            if (ownerHealth != null && h == ownerHealth)
            {
                continue;
            }

            if (h.GetComponent<PlayerMovement>() != null)
            {
                continue;
            }

            h.TakeDamage(damage);
        }
    }

    public void Refresh(float newDurationSeconds, float newRadius)
    {
        durationSeconds = Mathf.Max(durationSeconds, newDurationSeconds);
        radius = Mathf.Max(radius, newRadius);
        remaining = Mathf.Max(remaining, newDurationSeconds);
    }
}
