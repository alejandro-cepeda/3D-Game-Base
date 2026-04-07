using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyKnockback : MonoBehaviour
{
    [SerializeField] private float baseDistance = 0.35f;
    [SerializeField] private float maxDistance = 1.0f;
    [SerializeField] private float minIntervalSeconds = 0.05f;

    private Health? health;
    private NavMeshAgent? agent;
    private float nextTime;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
        }
    }

    private void OnDamaged(Health h, int amount)
    {
        if (Time.time < nextTime)
        {
            return;
        }

        nextTime = Time.time + minIntervalSeconds;

        PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        Vector3 away = transform.position - player.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float scaled = baseDistance + Mathf.Clamp01(amount / 50f) * (maxDistance - baseDistance);
        Vector3 desired = transform.position + away.normalized * scaled;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            if (agent != null && agent.enabled)
            {
                agent.Move(hit.position - transform.position);
            }
            else
            {
                transform.position = hit.position;
            }
        }
    }
}
