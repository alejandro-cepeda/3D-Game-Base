using UnityEngine;
using UnityEngine.AI;

public sealed class EnemySpawner : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private GameObject enemyPrefab = null!;
    [SerializeField] private GameObject[] enemyPrefabs = System.Array.Empty<GameObject>();
    [SerializeField] private string[] enemyPrefabResourcePaths = new[] { "Enemies/ToughZombie" };
    [SerializeField, Range(0f, 1f)] private float toughZombieSpawnChance = 0.15f;
    [SerializeField] private string toughZombieResourcePath = "Enemies/ToughZombie";
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

    private System.Collections.Generic.List<GameObject> spawnPool = new System.Collections.Generic.List<GameObject>();
    private GameObject? toughZombiePrefab;

    private void Start()
    {
        // Initialize with base values
        currentMaxAlive = initialMaxAlive;
        currentSpawnInterval = initialSpawnInterval;
        nextDifficultyIncreaseTime = Time.time + difficultyIncreaseInterval;

        BuildSpawnPool();
    }

    private void BuildSpawnPool()
    {
        spawnPool.Clear();
        toughZombiePrefab = null;

        if (enemyPrefab != null)
        {
            spawnPool.Add(enemyPrefab);
        }

        if (enemyPrefabs != null)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    spawnPool.Add(enemyPrefabs[i]);
                }
            }
        }

        string[] resourcePaths = enemyPrefabResourcePaths;
        if (resourcePaths == null || resourcePaths.Length == 0)
        {
            resourcePaths = new[] { "Enemies/ToughZombie" };
        }

        if (resourcePaths != null)
        {
            for (int i = 0; i < resourcePaths.Length; i++)
            {
                string path = resourcePaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                GameObject loaded = Resources.Load<GameObject>(path);
                if (loaded != null)
                {
                    spawnPool.Add(loaded);

                    if (!string.IsNullOrWhiteSpace(toughZombieResourcePath) && path == toughZombieResourcePath)
                    {
                        toughZombiePrefab = loaded;
                    }
                }
            }
        }

        for (int i = spawnPool.Count - 1; i >= 0; i--)
        {
            if (spawnPool[i] == null)
            {
                spawnPool.RemoveAt(i);
            }
        }
    }

    private GameObject? GetRandomEnemyPrefab()
    {
        if (spawnPool.Count == 0)
        {
            return null;
        }

        if (toughZombiePrefab != null && spawnPool.Count > 1)
        {
            float chance = Mathf.Clamp01(toughZombieSpawnChance);
            if (Random.value < chance)
            {
                return toughZombiePrefab;
            }

            for (int i = 0; i < 6; i++)
            {
                GameObject candidate = spawnPool[Random.Range(0, spawnPool.Count)];
                if (candidate != null && candidate != toughZombiePrefab)
                {
                    return candidate;
                }
            }
        }

        return spawnPool[Random.Range(0, spawnPool.Count)];
    }

    private void Update()
    {
        if (spawnPool.Count == 0)
        {
            BuildSpawnPool();
        }

        GameObject? prefab = GetRandomEnemyPrefab();
        if (prefab == null)
        {
            return;
        }

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

        Instantiate(prefab, spawnPosition.Value, Quaternion.identity);
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
