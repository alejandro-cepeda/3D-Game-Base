using UnityEngine;
using UnityEngine.AI;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private float spawnIntervalSeconds = 2.0f;
    [SerializeField] private float spawnRadius = 18f;
    [SerializeField] private float minSpawnDistanceFromPlayer = 6f;

    private float nextSpawnTime;

    private void Update()
    {
        if (enemyPrefab == null)
        {
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        if (CountAlive() >= maxAlive)
        {
            return;
        }

        Transform? player = FindPlayer();
        if (player == null)
        {
            return;
        }

        Vector3? spawnPosition = FindSpawnPosition(player.position);
        if (spawnPosition == null)
        {
            nextSpawnTime = Time.time + 0.25f;
            return;
        }

        Instantiate(enemyPrefab, spawnPosition.Value, Quaternion.identity);
        nextSpawnTime = Time.time + spawnIntervalSeconds;
    }

    private int CountAlive()
    {
        return FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
    }

    private Transform? FindPlayer()
    {
        PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
        return player != null ? player.transform : null;
    }

    private Vector3? FindSpawnPosition(Vector3 playerPosition)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(random2D.x, 0f, random2D.y);

            float distanceToPlayer = Vector3.Distance(candidate, playerPosition);
            if (distanceToPlayer < minSpawnDistanceFromPlayer)
            {
                continue;
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return null;
    }
}
