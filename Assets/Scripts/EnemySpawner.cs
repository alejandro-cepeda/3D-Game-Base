using UnityEngine;
using UnityEngine.AI;

public sealed class EnemySpawner : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private GameObject enemyPrefab = null!;
    [SerializeField] private float spawnRadius = 18f;
    [SerializeField] private float minSpawnDistanceFromPlayer = 6f;

    [Header("Difficulty Scaling")]
    [SerializeField] private int initialMaxAlive = 5;
    [SerializeField] private float initialSpawnInterval = 3.0f;
    [Space]
    [SerializeField] private float difficultyIncreaseInterval = 30f; // Every 30 seconds
    [SerializeField] private int maxAliveIncrease = 2;              // Add 2 to the cap
    [SerializeField] private float spawnIntervalReduction = 0.2f;    // Shorter wait time
    [SerializeField] private float minSpawnInterval = 0.5f;         // Don't spawn faster than this

    private int currentMaxAlive;
    private float currentSpawnInterval;
    private float nextSpawnTime;
    private float nextDifficultyIncreaseTime;

    private void Start()
    {
        // Initialize with base values
        currentMaxAlive = initialMaxAlive;
        currentSpawnInterval = initialSpawnInterval;
        nextDifficultyIncreaseTime = Time.time + difficultyIncreaseInterval;
    }

    private void Update()
    {
        if (enemyPrefab == null) return;

        // 1. Handle Difficulty Scaling
        if (Time.time >= nextDifficultyIncreaseTime)
        {
            IncreaseDifficulty();
        }

        // 2. Handle Spawning Logic
        if (Time.time < nextSpawnTime) return;

        if (CountAlive() >= currentMaxAlive) return;

        Transform? player = FindPlayer();
        if (player == null) return;

        Vector3? spawnPosition = FindSpawnPosition(player.position);
        if (spawnPosition == null)
        {
            // If no valid spot found, try again very soon
            nextSpawnTime = Time.time + 0.25f;
            return;
        }

        Instantiate(enemyPrefab, spawnPosition.Value, Quaternion.identity);
        nextSpawnTime = Time.time + currentSpawnInterval;
    }

    private void IncreaseDifficulty()
    {
        // Increase the population cap
        currentMaxAlive += maxAliveIncrease;

        // Decrease the interval (clamp it so it doesn't reach 0 or negative)
        currentSpawnInterval = Mathf.Max(minSpawnInterval, currentSpawnInterval - spawnIntervalReduction);

        nextDifficultyIncreaseTime = Time.time + difficultyIncreaseInterval;

        Debug.Log($"[Spawner] Difficulty Increased! Max: {currentMaxAlive}, Interval: {currentSpawnInterval:F2}s");
    }

    private int CountAlive()
    {
        // Finding objects by type is okay for small counts, 
        // but for high numbers, consider a static counter in EnemyController
        return Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
    }

    private Transform? FindPlayer()
    {
        PlayerMovement? player = Object.FindFirstObjectByType<PlayerMovement>();
        return player != null ? player.transform : null;
    }

    private Vector3? FindSpawnPosition(Vector3 playerPosition)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(random2D.x, 0f, random2D.y);

            if (Vector3.Distance(candidate, playerPosition) < minSpawnDistanceFromPlayer)
                continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return null;
    }
}