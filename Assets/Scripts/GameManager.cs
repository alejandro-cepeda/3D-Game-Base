using System; // Required for the 'Action' delegate used in Health events
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Simplified scene management

public sealed class GameManager : MonoBehaviour
{
    public static GameManager? Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI? scoreText;
    [SerializeField] private GameObject? gameOverUI;

    private int score;
    private bool isGameOver;
    private Health? playerHealth; // Cached reference to allow unsubscription

    private void Awake()
    {
        // Singleton Pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateScoreText();
        
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        // Find the player and subscribe to the death event
        PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Subscribe to the event-driven death signal
                playerHealth.Died += OnPlayerDied;
            }
        }
    }

    private void OnDisable()
    {
        // Proper Software Engineering: Unsubscribe to prevent memory leaks
        // This is critical when reloading scenes or destroying objects.
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        score += amount;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void OnPlayerDied(Health health)
    {
        EndGame();
    }

    private void EndGame()
    {
        isGameOver = true;
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // Pause the game world
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        // Reset time scale before reloading the scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}