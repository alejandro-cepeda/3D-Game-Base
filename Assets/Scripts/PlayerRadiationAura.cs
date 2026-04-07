using UnityEngine;

public sealed class PlayerRadiationAura : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private float tickSeconds = 0.25f;
    [SerializeField] private LayerMask hitLayers = ~0;

    private float nextTick;

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
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
            if (h == null)
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

    public void AddRadius(float additionalRadius)
    {
        radius = Mathf.Max(0f, radius + additionalRadius);
    }
}
